

namespace TBD.Psi.Study.Lab
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using TBD.Psi.VisionComponents;
    using Microsoft.Psi.Imaging;

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
                    var frameName = Constants.SensorCorrespondMap[stream.Name.Split('.')[0]];
                    // get transformation
                    var transform = transformationTree.SolveTransformation("world", frameName);

                    if (stream.Name.StartsWith("k2"))
                    {
                        // TODO: Write a flip operation.
                        inputStore.OpenStream<Shared<EncodedDepthImage>>(stream.Name).Decode().Flip(FlipMode.AlongVerticalAxis).EncodePng().Select(s => (s, transform)).Write(frameName, outputStore);
                    }
                    else
                    {
                        inputStore.OpenStream<Shared<EncodedDepthImage>>(stream.Name).Select(s => (s, transform)).Write(frameName, outputStore);
                    }
                }
                p.Diagnostics.Write("diagnostics", outputStore);
                p.Run(ReplayDescriptor.ReplayAll);
            }
        }
    }
}
