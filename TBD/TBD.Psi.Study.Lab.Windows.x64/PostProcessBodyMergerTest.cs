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
    using Microsoft.Psi.AzureKinect;

    public class PostProcessBodyMergerTest
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // input & output store
                var inputStore = PsiStore.Open(p,Constants.TestRecordingPath.Split(Path.DirectorySeparatorChar).Last().Split('.')[0], Path.Combine(Constants.OperatingDirectory, Constants.TestRecordingPath));
                var outputStore = PsiStore.Create(p, "body-merge-test", Path.Combine(Constants.OperatingDirectory, @"body-merge-test"));

                //create transformation tree and build the relationships
                var transformationTree = new TransformationTreeTracker(p, pathToSettings: Constants.TransformationSettingsPath);
                transformationTree.WorldFrameOutput.Write("world", outputStore);


                // redirect all color streams
                foreach (var colorStreamName in inputStore.AvailableStreams.Where(s => s.Name.EndsWith("color")).Select(s => s.Name))
                {
                    var frameName = Constants.SensorCorrespondMap[colorStreamName.Split('.')[0]];
                    inputStore.OpenStream<Shared<EncodedImage>>(colorStreamName).Write($"{frameName}.color", outputStore);
                }

                // create the components
                var merger = new BodyMerger(p, DeliveryPolicy.LatestMessage);
                var tracker = new BodyTracker(p, DeliveryPolicy.LatestMessage);
                merger.PipeTo(tracker, DeliveryPolicy.LatestMessage);
                tracker.Write("trackedBodies", outputStore);

                // add azure bodies into the body mergers
                // Note: We don't use the type name because kinect body's name include versions and it breaks comparison 
                // s.typeName == typeof(List<AzureKinectBody>).QualifyingName doesn't works
                foreach (var azureBodyStreamName in inputStore.AvailableStreams.Where(s => s.Name.StartsWith("azure") && s.Name.EndsWith("bodies")).Select(s => s.Name))
                {
                    var frameName = Constants.SensorCorrespondMap[azureBodyStreamName.Split('.')[0]];
                    // get transformation to global frame
                    var transform = transformationTree.SolveTransformation("world", frameName);
                    // open stream
                    var bodiesOrigin = inputStore.OpenStream<List<AzureKinectBody>>(azureBodyStreamName);
                    var bodies = bodiesOrigin.ChangeToFrame(transform, DeliveryPolicy.LatestMessage);
                    bodies.Write($"{frameName}.bodies", outputStore);
                    // add to merger
                    merger.AddHumanBodyStream(bodies.ChangeToHumanBodies(DeliveryPolicy.LatestMessage));
                }

                p.Diagnostics.Write("diagnostics", outputStore);
                // Note to self: Cannot run at maximum speed due to the need to use pipeline time to 
                // calibrate missing data.
                p.Run(ReplayDescriptor.ReplayAll); 
            }
        }
    }
}
