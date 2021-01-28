

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

    public class PostProcessBoardDetection
    {
        public static void Run()
        {
            var start_time = DateTime.Now;
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // INFORMATION
                // board information
                var boardMarkerSize = 0.115f;
                var boardMarkerDist = 0.023f;
                var boardXNum = 2;
                var boardYNum = 3;

/*                var boardMarkerSize = 0.077f;
                var boardMarkerDist = 0.00151f;
                var boardXNum = 4;
                var boardYNum = 6;*/

                var deliveryPolicy = DeliveryPolicy.SynchronousOrThrottle;

                // Stores
                var inputStore = PsiStore.Open(p, "calibration-recording", Path.Combine(Constants.OperatingDirectory, Constants.CalibrationRecordingPath));
                var outputStore = PsiStore.Create(p, "board-detection", Path.Combine(Constants.OperatingDirectory, @"post-board"));

                // create the calibration tool
                var calibrationMerger = new CalibrationMerger(p, outputStore, boardXNum, boardYNum, boardMarkerSize, boardMarkerDist, "DICT_4X4_100", deliveryPolicy);

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
                        var color = inputStore.OpenStream<Shared<EncodedImage>>(colorStream.Name);
                        var calibration = inputStore.OpenStream<IDepthDeviceCalibrationInfo>(calibrationStreamName);
                        if (deviceName.StartsWith("k2"))
                        {
                            calibrationMerger.AddSavedStreams(color.Decode(deliveryPolicy).Flip(FlipMode.AlongVerticalAxis, deliveryPolicy).Out, calibration.Out, deviceName);

                        }
                        else
                        {
                            calibrationMerger.AddSavedStreams(color.Decode(deliveryPolicy).Out, calibration.Out, deviceName);
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
                System.IO.File.WriteAllLines(Constants.CalibrationCSVPath, lx);
            }
            Console.WriteLine($"total time:{(DateTime.Now - start_time).TotalSeconds}");
        }
    }
}
