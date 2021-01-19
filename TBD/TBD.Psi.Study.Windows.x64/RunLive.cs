namespace TBD.Psi.Study
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;
    using TBD.Psi.StudyComponents;

    public class RunLive
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // general settings
                var azureKinectNum = 1;
                var kinect2Num = 1;
                var mainNum = -1;

                // Code

                var outputStore = PsiStore.Create(p, Constants.LiveSavePath, Constants.LiveOperatingDirectory);

                // create a transformation tree that describe the environment
                var transformationTree = new TransformationTreeTracker(p, pathToSettings: Constants.LiveTransformationSettingsPath);
                transformationTree.WorldFrameOutput.Write("world", outputStore);

                // Create the components that we use live
                var bodyMerger = new BodyMerger(p);
                var bodyTracker = new BodyTracker(p);
                var rosPublisher = new ROSWorldSender(p);

                // connect components
                bodyMerger.PipeTo(bodyTracker);
                bodyTracker.Write("tracked", outputStore);
                bodyTracker.PipeTo(rosPublisher);

                // setup the cameras and add their data
                for (var i = 1; i <= azureKinectNum; i++)
                {
                    var k4a = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputCalibration = true,
                        DeviceIndex = i - 1,
                        Exposure = TimeSpan.FromMilliseconds(10),
                        WiredSyncMode = mainNum > 0 ? (mainNum == (i - 1) ? Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Master : Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Subordinate) : Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Standalone,
                        BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                        {
                            CpuOnlyMode = false,
                            TemporalSmoothing = 0.0f,
                        }
                    });

                    var deviceName = $"azure{(mainNum != i ? i : 0)}";
                    k4a.ColorImage.EncodeJpeg(quality: Constants.JPEGEncodeQuality).Write($"{deviceName}.color", outputStore);
                    k4a.DepthDeviceCalibrationInfo.Write($"{deviceName}.depth-calibration", outputStore);
                    k4a.Bodies.Write($"{deviceName}.bodies", outputStore);
                    k4a.DepthImage.EncodePng().Write($"{deviceName}.depth", outputStore);

                    // Add body to merger
                    var bodyInWorld = k4a.Bodies.ChangeToFrame(transformationTree.SolveTransformation("world", Constants.SensorCorrespondMap[deviceName]));
                    bodyMerger.AddHumanBodyStream(bodyInWorld.ChangeToHumanBodies(), mainNum == i);
                }

                for (var i = 1; i <= kinect2Num; i++)
                {
                    var k2 = new KinectSensor(p, new KinectSensorConfiguration()
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputCalibration = true,
                        OutputBodies = true
                    });

                    var deviceName = $"k2d{i}";
                    k2.ColorImage.EncodeJpeg(quality: Constants.JPEGEncodeQuality).Write($"{deviceName}.color", outputStore);
                    k2.DepthDeviceCalibrationInfo.Write($"{deviceName}.depth-calibration", outputStore);
                    k2.Bodies.Write($"{deviceName}.bodies", outputStore);
                    k2.DepthImage.EncodePng().Write($"{deviceName}.depth", outputStore);

                    // Add body to merger
                    var bodyInWorld = k2.Bodies.ChangeToFrame(transformationTree.SolveTransformation("world", Constants.SensorCorrespondMap[deviceName]));
                    bodyMerger.AddHumanBodyStream(bodyInWorld.ChangeToHumanBodies());
                }

                // Audio recording from main device
                var audioSource = new AudioCapture(p, new AudioCaptureConfiguration()
                {
                    Format = WaveFormat.Create16kHz1Channel16BitPcm()
                });
                audioSource.Write("audio", outputStore);

                // Start Pipeline
                p.Diagnostics.Write("diagnostics", outputStore);
                p.RunAsync();

                Console.WriteLine("Press Enter to stop...");
                Console.ReadLine();
            }
        }
    }
}
