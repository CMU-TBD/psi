

namespace TBD.Psi.Study
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using TBD.Psi.StudyComponents;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Components;

    public class PostProcessDepthImageStaging
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // create input & output stores
                var inputStore = PsiStore.Open(p, "calibration-recording", Path.Combine(Constants.OperatingDirectory, Constants.CalibrationRecordingPath));
                var outputStore = PsiStore.Create(p, "depth-merger", Path.Combine(Constants.OperatingDirectory, @"depth-merger"));

                //create transformation tree and build the relationships
                var transformationTree = new TransformationTreeTracker(p, pathToSettings: Constants.TransformationSettingsPath);

                // open each depth map and add their corresponding coordinate system
                foreach(var stream in inputStore.AvailableStreams.Where(s => s.Name.EndsWith("depth") && s.TypeName == typeof(Shared<EncodedDepthImage>).AssemblyQualifiedName))
                {
                    // get device name
                    var deviceName = stream.Name.Split('.')[0];
                    var frameName = Constants.SensorCorrespondMap[stream.Name.Split('.')[0]];
                    // get transformation
                    var transform = transformationTree.SolveTransformation("world", frameName);
                    // create basic streams
                    var calibrationStream = inputStore.OpenStream<IDepthDeviceCalibrationInfo>($"{deviceName}.depth-calibration");
                    var imgStream = inputStore.OpenStream<Shared<EncodedDepthImage>>(stream.Name);
                    // flip and encode if k2
                    if (stream.Name.StartsWith("k2"))
                    {
                        imgStream = imgStream.Decode().Flip(FlipMode.AlongVerticalAxis).EncodePng();
                    }
                    // pair with calibration
                    imgStream.Fuse(calibrationStream.First(), Available.Nearest<IDepthDeviceCalibrationInfo>()).Select(m =>
                    {
                        return (m.Item1, m.Item2.DepthIntrinsics, transform);
                    }).Write(frameName, outputStore);
                }
                p.Diagnostics.Write("diagnostics", outputStore);
                p.Run(ReplayDescriptor.ReplayAll);
            }
        }
    }
}
