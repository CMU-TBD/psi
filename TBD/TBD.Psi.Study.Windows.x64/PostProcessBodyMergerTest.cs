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
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Calibration;

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
                    var deviceName = colorStreamName.Split('.')[0];
                    var frameName = Constants.SensorCorrespondMap[deviceName];
                    var calibrationStream = inputStore.OpenStream<IDepthDeviceCalibrationInfo>($"{deviceName}.depth-calibration");
                    // get transformation to global frame
                    var transform = transformationTree.SolveTransformation("world", frameName);
                    inputStore.OpenStream<Shared<EncodedImage>>(colorStreamName).Join(calibrationStream.First(), Reproducible.Nearest<IDepthDeviceCalibrationInfo>()).Select(m => (m.Item1, m.Item2.ColorIntrinsics, transform) ).Write($"{frameName}.color", outputStore);
                }

                // create the components
                var merger = new BodyMerger(p);
                var tracker = new BodyTracker(p);
                merger.PipeTo(tracker);
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
                    var bodies = bodiesOrigin.ChangeToFrame(transform);
                    bodies.Write($"{frameName}.bodies", outputStore);
                    // add to merger
                    merger.AddHumanBodyStream(bodies.ChangeToHumanBodies());
                }

                p.Diagnostics.Write("diagnostics", outputStore);

                // setting time for debug purposes
                //var replayDescriptor = new ReplayDescriptor(inputStore.MessageOriginatingTimeInterval.Left + TimeSpan.FromSeconds(23.5), TimeSpan.FromSeconds(2));
                var replayDescriptor = ReplayDescriptor.ReplayAll;

                p.Run(replayDescriptor); 
            }
        }
    }
}
