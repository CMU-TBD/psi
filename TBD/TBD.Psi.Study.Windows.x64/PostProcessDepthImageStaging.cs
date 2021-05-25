

namespace TBD.Psi.Study
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Components;
    using MathNet.Spatial.Euclidean;
    using TBD.Psi.TransformationTree;

    public class PostProcessDepthImageStaging
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // create input & output stores
                var inputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, Constants.CalibrationSubDirectory);
                var outputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, @"depth-merger");

                var inputStore = PsiStore.Open(p, Constants.CalibrationStoreName, inputStorePath);
                var outputStore = PsiStore.Create(p, "depth-merger", outputStorePath);

                //create transformation tree and build the relationships
                var transformationSettingPath = Path.Combine(Constants.ResourceLocations, $"transformations-{Constants.StudyType}-{Constants.PartitionIdentifier}.json");
                var transformationTree = new TransformationTreeComponent(p, 1000, transformationSettingPath);

                // open each depth map and add their corresponding coordinate system
                foreach (var stream in inputStore.AvailableStreams.Where(s => s.Name.EndsWith("depth") && s.TypeName == typeof(Shared<EncodedDepthImage>).AssemblyQualifiedName))
                {
                    // get device name
                    var deviceName = stream.Name.Split('.')[0];
                    var frameName = Constants.SensorCorrespondMap[stream.Name.Split('.')[0]];
                    // get transformation
                    var transform = transformationTree.QueryTransformation("world", frameName);
                    if (transform is null)
                    {
                        throw new ArgumentException($"Cannot find transformation for {frameName}");
                    }
                    // create basic streams
                    var calibrationStream = inputStore.OpenStream<IDepthDeviceCalibrationInfo>($"{deviceName}.depth-calibration");
                    var imgStream = inputStore.OpenStream<Shared<EncodedDepthImage>>(stream.Name);
                    // flip and encode if k2
                    if (stream.Name.StartsWith("k2"))
                    {
                        imgStream = imgStream.Decode().Flip(FlipMode.AlongVerticalAxis).EncodePng();
                    }
                    // pair depth with calibration
                    imgStream.Fuse(calibrationStream.First(), Available.Nearest<IDepthDeviceCalibrationInfo>()).Select(m =>
                    {
                        return (m.Item1, m.Item2.DepthIntrinsics, transform);
                    }).Write($"{frameName}.depth", outputStore);
                }

                transformationTree.Write("world", outputStore);
                p.Diagnostics.Write("diagnostics", outputStore);
                p.Run(ReplayDescriptor.ReplayAll);
            }
        }
    }
}
