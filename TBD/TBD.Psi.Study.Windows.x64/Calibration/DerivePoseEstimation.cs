

namespace TBD.Psi.Study.Calibration
{
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using TBD.Psi.StudyComponents;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Components;
    using MathNet.Spatial.Euclidean;

    public class DeriveBoardEstimations
    {

        public static void Run()
        {
            var baxterPositionLines = new List<string>();
            var floorPositionLines = new List<string>();
            var sensorCalibrationLines = new List<string>();
            var task = Task.Run( () => _Run(baxterPositionLines, floorPositionLines, sensorCalibrationLines));
            task.Wait();
            // generate file path
            var csvPath = Path.Combine(Constants.ResourceLocations, $"board-{Constants.StudyType}-{Constants.CalibrationDatasetIdentifier}.csv");
            var tagCSVPath = Path.Combine(Constants.ResourceLocations, $"baxter-head-{Constants.StudyType}-{Constants.CalibrationDatasetIdentifier}.csv");
            var floorCSVPath = Path.Combine(Constants.ResourceLocations, $"floor-{Constants.StudyType}-{Constants.CalibrationDatasetIdentifier}.csv");
            File.WriteAllLines(csvPath, sensorCalibrationLines);
            File.WriteAllLines(tagCSVPath, baxterPositionLines);
            File.WriteAllLines(floorCSVPath, floorPositionLines);
        }

        public static async Task _Run(List<string> baxterPositionLines, List<string> floorPositionLines, List<string> sensorCalibrationLines)
        {
            var deliveryPolicy = DeliveryPolicy.SynchronousOrThrottle;

            // INFORMATION
            // all the calibration boards
            var boardMarkerSize = 0.1145f;
            var boardMarkerDist = 0.017f;
            var boardXNum = 3;
            var boardYNum = 2;
            var boardDict = OpenCV.ArucoDictionary.DICT_4X4_50;
            List<int> firstMarkerList = new List<int>() { 0, 6, 40 };

            // which marker list is the floorboard
            int[] floorBoardMarkers = { 0, 40};
            var floorInViewDeviceName = "azure2";

            // baxter face board information
            var faceMarkerSize = 0.063f;
            var facedMarkerDist = 0.0095f;
            var faceXNum = 2;
            var faceYNum = 2;
            var faceDict = OpenCV.ArucoDictionary.DICT_4X4_50;
            var faceFirstMaker = 30;
            var baxterInViewDeviceName = "azure2";
           
          
            // Open Dataset
            var dataset = Dataset.Load(Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier, "dataset.pds"));
            await dataset.CreateDerivedPartitionAsync(
                (p, importer, exporter) =>
                {
                    // create zipper to merger all the calibration input
                    var zipper = new Zip<(CoordinateSystem, string, CoordinateSystem, string)>(p);
                    // create a list of calibrators
                    List<CalibrationMerger> calibrationMergerList = new List<CalibrationMerger>();
                    foreach(var id in firstMarkerList)
                    {
                        var merger = new CalibrationMerger(p, exporter, boardXNum, boardYNum, boardMarkerSize, boardMarkerDist, boardDict, deliveryPolicy, id);
                        merger.PipeTo(zipper.AddInput($"board-{id}"));
                        calibrationMergerList.Add(merger);
                        if (floorBoardMarkers.Contains(id))
                        {
                            merger.Do(m =>
                            {
                                if (m.Item2 == $"board.{floorInViewDeviceName}-{id}")
                                {
                                    floorPositionLines.Add($"{String.Join(",", m.Item1.Storage.ToRowMajorArray())},{id}");
                                }
                                else if (m.Item4 == $"board.{floorInViewDeviceName}-{id}")
                                {
                                    floorPositionLines.Add($"{String.Join(",", m.Item3.Storage.ToRowMajorArray())},{id}");
                                }
                            });
                        }
                    }

                    // list of color streams
                    for (var i = 1; i <= 3; i++)
                    {
                        var deviceName = $"azure{i}";
                        var colorStreamName = $"{deviceName}.color";
                        var calibrationStreamName = $"{deviceName}.depth-calibration";
                        if (importer.HasStream(colorStreamName) && importer.HasStream(calibrationStreamName))
                        {
                            Console.WriteLine($"Adding Calibration of {deviceName}");
                            var color = importer.OpenStream<Shared<EncodedImage>>(colorStreamName).Decode(deliveryPolicy);
                            var calibration = importer.OpenStream<IDepthDeviceCalibrationInfo>(calibrationStreamName);
                            // add them to each calibrator
                            foreach(var merger in calibrationMergerList)
                            {
                                merger.AddSavedStreams(color.Out, calibration.Out, $"board.{deviceName}");
                            }

                            // if detector for tag above baxter
                            if (deviceName == baxterInViewDeviceName)
                            {
                                var baxterBoardDetector = new BoardDetector(p, faceXNum, faceYNum, faceMarkerSize, facedMarkerDist, faceDict, faceFirstMaker);
                                calibration.PipeTo(baxterBoardDetector.CalibrationIn);
                                color.ToGray().PipeTo(baxterBoardDetector.ImageIn);
                                baxterBoardDetector.Out.Write("board.baxter_head", exporter);
                                baxterBoardDetector.DebugImageOut.EncodeJpeg().Write("DebugTagInformation", exporter);
                                baxterBoardDetector.Out.Do(m =>
                                {
                                    baxterPositionLines.Add(String.Join(",", m.Storage.ToRowMajorArray()));
                                });
                            }

                            // if the detectors are the same
                            if (deviceName == floorInViewDeviceName)
                            {

                            }

                        }
                    }

                    zipper.Do(m =>
                    {
                        foreach (var entry in m)
                        {
                            (var cs1, var n1, var cs2, var n2) = entry;
                            sensorCalibrationLines.Add(String.Join(",", cs1.Storage.ToRowMajorArray()) + $",{n1}");
                            sensorCalibrationLines.Add(String.Join(",", cs2.Storage.ToRowMajorArray()) + $",{n2}");
                        }
                    });
                },
            "BoardEstimation",
            true,
            "BoardEstimation",
            _ => Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier, "BoardEstimation"),
            enableDiagnostics:true
            );
            dataset.Save(Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier, "dataset.pds"));
        }
    }
}
