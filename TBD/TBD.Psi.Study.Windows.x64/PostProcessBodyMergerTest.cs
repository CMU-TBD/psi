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
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Calibration;

    public class PostProcessBodyMergerTest
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // input & output store
                var inputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, Constants.OperatingStoreSubPath);
                var inputStore = PsiStore.Open(p, Constants.OperatingStoreName, inputStorePath);
                var outputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, @"body-merge-test");
                var outputStore = PsiStore.Create(p, "body-merge-test", Path.Combine(Constants.OperatingDirectory, outputStorePath));

                //create transformation tree and build the relationships
                var transformationSettingPath = Path.Combine(Constants.ResourceLocations, $"transformations-{Constants.StudyType}-{Constants.PartitionIdentifier}.json");
                var transformationTree = new TransformationTreeTracker(p, pathToSettings: transformationSettingPath);
                transformationTree.WorldFrameOutput.Write("world", outputStore);


                // redirect all color streams
                foreach (var colorStreamName in inputStore.AvailableStreams.Where(s => s.Name.EndsWith("color")).Select(s => s.Name))
                {
                    var deviceName = colorStreamName.Split('.')[0];
                    var frameName = Constants.SensorCorrespondMap[deviceName];
                    var calibrationStream = inputStore.OpenStream<IDepthDeviceCalibrationInfo>($"{deviceName}.depth-calibration");
                    // get transformation to global frame
                    var transform = transformationTree.SolveTransformation("world", frameName);
                    inputStore.OpenStream<Shared<EncodedImage>>(colorStreamName).Join(calibrationStream.First(), Reproducible.Nearest<IDepthDeviceCalibrationInfo>()).Select(m => (m.Item1, m.Item2.ColorIntrinsics, transform)).Write($"{frameName}.color", outputStore);
                }

                // create the components
                var merger = new BodyMerger(p);
                var tracker = new BodyTracker(p);
                // var rosPublisher = new ROSWorldSender(p, Constants.LocalRosCoreAddress, Constants.LocalRosClientAddress);
                // link the major components
                merger.PipeTo(tracker);
                tracker.Write("trackedBodies", outputStore);
                // tracker.PipeTo(rosPublisher);

                // add azure bodies into the body mergers
                // Note: We don't use the type name because kinect body's name include versions and it breaks comparison 
                // s.typeName == typeof(List<AzureKinectBody>).QualifyingName doesn't works
                foreach (var azureBodyStreamName in inputStore.AvailableStreams.Where(s => s.Name.StartsWith("azure") && s.Name.Split('.').Last() == "bodies").Select(s => s.Name))
                {
                    var frameName = Constants.SensorCorrespondMap[azureBodyStreamName.Split('.')[0]];
                    // get transformation to global frame
                    var transform = transformationTree.SolveTransformation("world", frameName);
                    // open stream
                    var bodiesOrigin = inputStore.OpenStream<List<AzureKinectBody>>(azureBodyStreamName);
                    var bodies = bodiesOrigin.ChangeToFrame(transform);
                    bodies.Write($"{frameName}.bodies", outputStore);
                    // add to merger
                    var humanBodies = bodies.ChangeToHumanBodies().HumanBodiesSituatedfilter();
                    humanBodies.Write($"{frameName}.humans", outputStore);

                    merger.AddHumanBodyStream(humanBodies);
                }
                /*                foreach (var k2BodyStreamName in inputStore.AvailableStreams.Where(s => s.Name.StartsWith("k2d") && s.Name.Split('.').Last() == "bodies").Select(s => s.Name))
                                {
                                    var frameName = Constants.SensorCorrespondMap[k2BodyStreamName.Split('.')[0]];
                                    // get transformation to global frame
                                    var transform = transformationTree.SolveTransformation("world", frameName);
                                    // open stream
                                    var bodiesOrigin = inputStore.OpenStream<List<KinectBody>>(k2BodyStreamName);
                                    var bodies = bodiesOrigin.ChangeToFrame(transform);
                                    bodies.Write($"{frameName}.bodies", outputStore);
                                    // add to merger
                                    merger.AddHumanBodyStream(bodies.ChangeToHumanBodies());
                                }*/


                p.Diagnostics.Write("diagnostics", outputStore);

                // setting time for debug purposes
                //var replayDescriptor = new ReplayDescriptor(inputStore.MessageOriginatingTimeInterval.Left + TimeSpan.FromSeconds(23.5), TimeSpan.FromSeconds(2));
                var replayDescriptor = ReplayDescriptor.ReplayAll;

                p.Run(replayDescriptor);
            }
        }
    }
}
