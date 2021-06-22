

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

    public class DeriveCharucoEstimations
    {

        public static void Run()
        {
            var sensorCalibrationLines = new List<string>();
            var task = Task.Run(() => _Run(sensorCalibrationLines));
            task.Wait();
            // generate file path
            var csvPath = Path.Combine(Constants.CSVsLocation, $"charuco-{Constants.StudyType}-{Constants.CalibrationDatasetIdentifier}.csv");
            File.WriteAllLines(csvPath, sensorCalibrationLines);
            // push information
            Console.WriteLine($"sensor entries: {sensorCalibrationLines.Count / 2}");
        }

        public static async Task _Run(List<string> sensorCalibrationLines)
        {
            var deliveryPolicy = DeliveryPolicy.SynchronousOrThrottle;

            // INFORMATION
            // charuco board setting
            var squareX = 4; 
            var squareY = 6;
            var squareLength = 0.0839f;
            var markerLength = 0.055f;
            var boardDict = OpenCV.ArucoDictionary.DICT_4X4_50;
            var acceptableRange = TimeSpan.FromMilliseconds(15);

            // Open Dataset
            var dataset = Dataset.Load(Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier, "calibration.pds"), autoSave: true);
            await dataset.CreateDerivedPartitionAsync(
                (p, importer, exporter) =>
                {
                var detectorList = new List<(string, CharucoBoardDetect)>();
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
                        // create a detector
                        var detector = new CharucoBoardDetect(p, squareX, squareY, squareLength, markerLength, boardDict);
                        color.ToGray(deliveryPolicy).PipeTo(detector.ImageIn, deliveryPolicy);
                        calibration.PipeTo(detector.CalibrationIn, deliveryPolicy);
                        detectorList.Add((deviceName, detector));
                        detector.DebugImageOut.EncodeJpeg(30).Write($"{deviceName}.img", exporter);
                        detector.Out.Write($"{deviceName}.pose", exporter);
                    }
                }

                var zipper = new Zip<(CoordinateSystem, string, CoordinateSystem, string)>(p);
                // create a join among them
                for (var i = 0; i < detectorList.Count; i++)
                {
                    for (var j = (i+1); j < detectorList.Count; j++)
                    {
                        var name1 = detectorList[i].Item1;
                        var name2 = detectorList[j].Item1;
                        // create a joiner
                        detectorList[i].Item2.Join(detectorList[j].Item2, acceptableRange, deliveryPolicy, deliveryPolicy).Select(m =>{
                            var (pose1, pose2) = m;
                            return (pose1, name1, pose2, name2);
                        }).PipeTo(zipper.AddInput($"{name1}-{name2}"), deliveryPolicy);
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
            "charucoEstimation",
            true,
            "charucoEstimation",
            _ => Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier, "BoardEstimation"),
            enableDiagnostics:true
            );
        }
    }
}
