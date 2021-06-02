

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
    using TBD.Psi.TransformationTree;
    using MathNet.Spatial.Euclidean;

    public class DeriveDepthCheck
    {

        public static void Run()
        {
            var task = Task.Run(_Run);
            task.Wait();
        }

        public static async Task _Run()
        {
     
            // Open Dataset
            var dataset = Dataset.Load(Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier, "dataset.pds"));
            await dataset.CreateDerivedPartitionAsync(
                (p, importer, exporter) =>
                {

                    //create transformation tree and build the relationships
                    var transformationSettingPath = Path.Combine(Constants.ResourceLocations, $"transformations-{Constants.StudyType}-{Constants.PartitionIdentifier}.json");
                    var transformationTree = new TransformationTreeComponent(p, 500, transformationSettingPath);


                    for (int i = 1; i <= 3; i++)
                    {
                        var deviceName = $"azure{i}";
                        var frameName = Constants.SensorCorrespondMap[deviceName];
                        var depthStreamName = $"{deviceName}.depth";
                        var calibrationStreamName = $"{deviceName}.depth-calibration";

                        // get transformation
                        var transform = transformationTree.QueryTransformation("world", frameName);
                        if (transform is null)
                        {
                            throw new ArgumentException($"Cannot find transformation for {frameName}");
                        }
                        // add depth streams
                        if (importer.HasStream(depthStreamName) && importer.HasStream(depthStreamName))
                        {
                            // create streams
                            var calibrationStream = importer.OpenStream<IDepthDeviceCalibrationInfo>(calibrationStreamName);
                            var depthStream = importer.OpenStream<Shared<EncodedDepthImage>>(depthStreamName);
                            // pair them with calibration
                            depthStream.Fuse(calibrationStream.First(), Available.Nearest<IDepthDeviceCalibrationInfo>()).Select(m =>
                            {
                                return (m.Item1, m.Item2.DepthIntrinsics, transform);
                            }).Write($"{frameName}.depth", exporter);
                        }

                        // if we can find the pose of the board, add them
                        var boardPoseName = $"{deviceName}.pose";
                        if (importer.HasStream(boardPoseName))
                        {
                            var poseStream = importer.OpenStream<CoordinateSystem>(boardPoseName);
                            poseStream.Select(m => m.TransformBy(transform)).Write($"pose.board-{deviceName}", exporter);
                        }

                        transformationTree.Write("pose.world", exporter);
                        p.Diagnostics.Write("diagnostics", exporter);
                    }
                },
            "DepthCheck",
            true,
            "DepthCheck",
            _ => Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier),
            enableDiagnostics:true
            );
            dataset.Save(Path.Combine(Constants.RootPath, "calibration", Constants.CalibrationDatasetIdentifier, "dataset.pds"));
        }
    }
}
