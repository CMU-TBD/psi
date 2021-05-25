

namespace TBD.Psi.Study
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.StudyComponents;

    public class PostProcessCalibration
    {
        public static void Run()
        {
            var baxterPositionLines = new List<string>();
            var floorPositionLines = new List<string>();
            var start_time = DateTime.Now;
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // INFORMATION
                // main board information
                var boardMarkerSize = 0.11f;
                var boardMarkerDist = 0.022f;
                var boardXNum = 2;
                var boardYNum = 3;
                var boardDict = "DICT_5X5_50";

                // baxter face board information
                var faceMarkerSize = 0.063f;
                var facedMarkerDist = 0.0095f;
                var faceXNum = 2;
                var faceYNum = 2;
                var faceDict = TBD.Psi.OpenCV.ArucoDictionary.DICT_4X4_50;
                var faceFirstMaker = 30;
                // baxter tag information
                var baxterInViewDeviceName = "azure2";
                //floor board
                var floorBoardSize = 0.1145f;
                var floorBoardMarkerDist = 0.023f;
                var floorBoarddXNum = 2;
                var floorBoardYNum = 3;
                var floorBoardDict = TBD.Psi.OpenCV.ArucoDictionary.DICT_4X4_50;
                var floorBoardInViewDeviceName = "azure2";


                var deliveryPolicy = DeliveryPolicy.SynchronousOrThrottle;

                // Generate the store path
                var inputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, Constants.CalibrationSubDirectory);
                var outputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, @"post-board");

                // Stores
                var inputStore = PsiStore.Open(p, Constants.CalibrationStoreName, inputStorePath);
                var outputStore = PsiStore.Create(p, "board-detection", outputStorePath);

                // create the calibration tool
                var calibrationMerger = new CalibrationMerger(p, outputStore, boardXNum, boardYNum, boardMarkerSize, boardMarkerDist, boardDict, deliveryPolicy);

                // list of color streams
                var colorStreams = inputStore.AvailableStreams.Where(s => s.Name.EndsWith(".color"));
                foreach (var colorStream in colorStreams)
                {
                    var deviceName = colorStream.Name.Remove(colorStream.Name.Length - 6);
                    // see if there is a corresponding calibration stream for the color stream
                    var calibrationStreamName = deviceName + ".depth-calibration";
                    if (inputStore.AvailableStreams.Select(s => s.Name).Contains(calibrationStreamName))
                    {
                        Console.WriteLine($"Adding Calibration of {deviceName}");
                        var color = inputStore.OpenStream<Shared<EncodedImage>>(colorStream.Name).Decode(deliveryPolicy);
                        var calibration = inputStore.OpenStream<IDepthDeviceCalibrationInfo>(calibrationStreamName);
                        if (deviceName.StartsWith("k2"))
                        {
                            calibrationMerger.AddSavedStreams(color.Flip(FlipMode.AlongVerticalAxis, deliveryPolicy).Out, calibration.Out, deviceName);

                        }
                        else
                        {
                            calibrationMerger.AddSavedStreams(color.Out, calibration.Out, deviceName);
                        }

                        // handle baxter's face information
                        if (deviceName == baxterInViewDeviceName)
                        {
                            var baxterBoardDetector = new BoardDetector(p, faceXNum, faceYNum, faceMarkerSize, facedMarkerDist, faceDict, faceFirstMaker);
                            calibration.PipeTo(baxterBoardDetector.CalibrationIn);
                            color.ToGray().PipeTo(baxterBoardDetector.ImageIn);
                            baxterBoardDetector.Out.Write("baxterFace", outputStore);
                            baxterBoardDetector.DebugImageOut.EncodeJpeg().Write("debug_tag_image", outputStore);
                            baxterBoardDetector.Out.Do(m =>
                            {
                                baxterPositionLines.Add(String.Join(",", m.Storage.ToRowMajorArray()));
                            });
                        }
                        // handle floor board
                        if (deviceName == floorBoardInViewDeviceName)
                        {
                            var floorBoardDetector = new BoardDetector(p, floorBoarddXNum, floorBoardYNum, floorBoardSize, floorBoardMarkerDist, floorBoardDict);
                            calibration.PipeTo(floorBoardDetector.CalibrationIn);
                            color.ToGray().PipeTo(floorBoardDetector.ImageIn);
                            floorBoardDetector.Out.Write("floorBoard", outputStore);
                            floorBoardDetector.Out.Do(m =>
                            {
                                floorPositionLines.Add(String.Join(",", m.Storage.ToRowMajorArray()));
                            });
                        }

                    }

                }


                calibrationMerger.Write("result", outputStore);
                p.Diagnostics.Write("diagnotics", outputStore);

                // collect the information for processing
                var poseCollection = new Dictionary<string, Dictionary<string, (List<CoordinateSystem>, List<CoordinateSystem>)>>();
                calibrationMerger.Do(m =>
                 {
                     if (!poseCollection.ContainsKey(m.Item2))
                     {
                         poseCollection[m.Item2] = new Dictionary<string, (List<CoordinateSystem>, List<CoordinateSystem>)>();
                     }
                     if (!poseCollection[m.Item2].ContainsKey(m.Item4))
                     {
                         var list1 = new List<CoordinateSystem>();
                         var list2 = new List<CoordinateSystem>();
                         poseCollection[m.Item2][m.Item4] = (list1, list2);
                     }
                     poseCollection[m.Item2][m.Item4].Item1.Add(new CoordinateSystem(m.Item1));
                     poseCollection[m.Item2][m.Item4].Item2.Add(new CoordinateSystem(m.Item3));
                 }, deliveryPolicy);
            
                p.ProposeReplayTime(TimeInterval.LeftBounded(DateTime.UtcNow));
                Generators.Repeat(p, true, 2, TimeSpan.FromSeconds(180));

                p.Run(ReplayDescriptor.ReplayAll);

                var lines = new List<string>();
                var lx = new List<string>();

                //do processing here
                foreach (var item in poseCollection)
                {
                    var name1 = item.Key;
                    foreach (var poses in item.Value)
                    {
                        var name2 = poses.Key;
                        var list1 = poses.Value.Item1;
                        var list2 = poses.Value.Item2;

                        // write to solution
                        for (var i = 0; i < list1.Count; i++)
                        {
                            lx.Add(String.Join(",", list1[i].Storage.ToRowMajorArray()) + $",{name1}");
                            lx.Add(String.Join(",", list2[i].Storage.ToRowMajorArray()) + $",{name2}");
                        }
                    }
                }
                // generate file path
                var csvPath = Path.Combine(Constants.ResourceLocations, $"board-{Constants.StudyType}-{Constants.PartitionIdentifier}.csv");
                var tagCSVPath = Path.Combine(Constants.ResourceLocations, $"baxter-screen-{Constants.StudyType}-{Constants.PartitionIdentifier}.csv");
                var floorCSVPath = Path.Combine(Constants.ResourceLocations, $"floor-{Constants.StudyType}-{Constants.PartitionIdentifier}.csv");

                System.IO.File.WriteAllLines(csvPath, lx);
                System.IO.File.WriteAllLines(tagCSVPath, baxterPositionLines);
                System.IO.File.WriteAllLines(floorCSVPath, floorPositionLines);
            }
            Console.WriteLine($"total time:{(DateTime.Now - start_time).TotalSeconds}");
        }
    }
}
