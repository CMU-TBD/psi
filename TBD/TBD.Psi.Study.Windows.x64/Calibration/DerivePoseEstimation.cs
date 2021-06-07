

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
            // main board information
            var boardMarkerSize = 0.11f;
            var boardMarkerDist = 0.022f;
            var boardXNum = 2;
            var boardYNum = 3;
            var boardDict = OpenCV.ArucoDictionary.DICT_5X5_50;

            // baxter face board information
            var faceMarkerSize = 0.063f;
            var facedMarkerDist = 0.0095f;
            var faceXNum = 2;
            var faceYNum = 2;
            var faceDict = OpenCV.ArucoDictionary.DICT_4X4_50;
            var faceFirstMaker = 30;

            // baxter tag information
            var baxterInViewDeviceName = "azure2";
            //floor board
            var floorBoardSize = 0.1145f;
            var floorBoardMarkerDist = 0.023f;
            var floorBoardXNum = 2;
            var floorBoardYNum = 3;
            var floorBoardDict = OpenCV.ArucoDictionary.DICT_4X4_50;
            var floorBoardInViewDeviceName = "azure2";


            // Open Dataset
            var dataset = Dataset.Load(Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier, "dataset.pds"));
            await dataset.CreateDerivedPartitionAsync(
                (p, importer, exporter) =>
                {
                    // create the calibration merger for the tags on the door
                    var calibrationMerger = new CalibrationMerger(p, exporter, boardXNum, boardYNum, boardMarkerSize, boardMarkerDist, boardDict, deliveryPolicy);
                    var calibrationMergerFloor = new CalibrationMerger(p, 
                        exporter, 
                        floorBoardXNum,
                        floorBoardYNum,
                        floorBoardSize,
                        floorBoardMarkerDist,
                        floorBoardDict, 
                        deliveryPolicy
                    );

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
                            // add to calibration tool
                            calibrationMerger.AddSavedStreams(color.Out, calibration.Out, $"{deviceName}.door");
                            calibrationMergerFloor.AddSavedStreams(color.Out, calibration.Out, $"{deviceName}.floor");

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

                            // if detector for the floor 
                            if (deviceName == floorBoardInViewDeviceName)
                            {
                                var floorBoardDetector = new BoardDetector(p, floorBoardXNum, floorBoardYNum, floorBoardSize, floorBoardMarkerDist, floorBoardDict);
                                calibration.PipeTo(floorBoardDetector.CalibrationIn);
                                color.ToGray().PipeTo(floorBoardDetector.ImageIn);
                                floorBoardDetector.Out.Write("board.floor", exporter);
                                floorBoardDetector.Out.Do(m =>
                                {
                                    floorPositionLines.Add(String.Join(",", m.Storage.ToRowMajorArray()));
                                });
                            }
                        }
                    }

                    // add calibration information to the CSV.
                    calibrationMergerFloor.Zip(calibrationMerger).Do(m =>
                    {
                        foreach (var entry in m)
                        {
                            (var cs1, var n1, var cs2, var n2) = entry;
                            sensorCalibrationLines.Add(String.Join(",", cs1.Storage.ToRowMajorArray()) + $",{n1}");
                            sensorCalibrationLines.Add(String.Join(",", cs2.Storage.ToRowMajorArray()) + $",{n2}");
                        }
                    });

                    calibrationMerger.Write("sensor-calibration-poses", exporter);
                    p.Diagnostics.Write("diagnotics", exporter);
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
