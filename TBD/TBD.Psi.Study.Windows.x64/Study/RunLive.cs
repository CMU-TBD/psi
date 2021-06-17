namespace TBD.Psi.Study.Study
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Data;
    using TBD.Psi.StudyComponents;
    using TBD.Psi.TransformationTree;

    public class RunStudy
    {
        public static void Run()
        {

            // get user setting
            Console.WriteLine("Enter Participant ID:");
            var participantID = Console.ReadLine();
            Console.WriteLine("Enter type:");
            var studyType = Console.ReadLine().ToLower();


            // general settings
            var azureKinectNum = 3;
            var mainBodyKinectNum = 3; // 1 index
            var liteModel = false;

            // create the basePath and any dataset
            var basePath = Path.Combine(Constants.RootPath, participantID);
            var datasetPath = Path.Combine(basePath, $"{participantID}.pds");
            Dataset dataset = null;
            if (!File.Exists(datasetPath))
            {
                dataset = new Dataset($"{participantID}", filename:datasetPath, autoSave:true);
            }
            else
            {
                dataset = Dataset.Load(datasetPath, autoSave: true);
            }




            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var storePath = Path.Combine(basePath, "recording", $"{studyType}");
                var outputStore = PsiStore.Create(p, "recording", storePath);
                // session name must be unique. We going to create new names if the previous name exist, just in case there is some issue with recording
                var sessionIndex = 0;
                var sessionName = $"{participantID}.{studyType}.{sessionIndex}";
                while (dataset.Sessions.Select(s => s.Name).Contains(sessionName))
                {
                    sessionName = $"{participantID}.{studyType}.{++sessionIndex}";
                }
                dataset.AddSessionFromPsiStore(outputStore.Name, outputStore.Path, sessionName:sessionName);


                // create a transformation tree that describe the environment
                var transformationSettingPath = Path.Combine(Constants.ResourceLocations, $"transformations-{Constants.StudyType}-{Constants.PartitionIdentifier}.json");
                var transformationTree = new TransformationTreeComponent(p, 1000, transformationSettingPath);
                transformationTree.Write("world", outputStore);

                // Create the components that we use live
                var bodyStreamValidator = new StreamValidator(p);
                var bodyMerger = new BodyMerger(p);
                var bodyTracker = new BodyTracker(p);
                var stateTracker = new StateTracker(p);
                var azureHeartbeat = Generators.Repeat(p, 0, TimeSpan.FromSeconds(0.25));
                var rosBodyPublisher = new ROSWorldSender(p, Constants.RosCoreAddress, Constants.RosClientAddress);
                var rosAudioPublisher = new ROSAudioSender(p, Constants.RosCoreAddress, Constants.RosClientAddress);
               
                // study ROS Subscriber to link up topics
                var rosListner = new ROSStudyListener(p, Constants.RosBridgeServerAddress);
                rosListner.AddUtteranceListener("/robocept/action_feedback/voice").Write("robocept.utterances", outputStore);
                rosListner.AddUtteranceListener("/podi/action_feedback/voice").Write("podi.utterances", outputStore);
                rosListner.AddUtteranceListener("/robocept/utterance").Write("utterances", outputStore);
                rosListner.AddAudio("/robocept/audio").Write("audio", outputStore);
                rosListner.AddCSListener("/psi/podi_base").Write("podi.pose", outputStore);
                rosListner.AddBaxterState("/robot/joint_states").Write("baxter.joint_states", outputStore);
                
                // connect components
                bodyMerger.PipeTo(bodyTracker);
                bodyTracker.Write("tracked", outputStore);
                bodyTracker.PipeTo(rosBodyPublisher);
                rosListner.AddStringListener("/study/state").PipeTo(stateTracker);
                stateTracker.Write("state", outputStore);
                

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
                        WiredSyncMode = Constants.SensorSyncMode[i],
                        BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                        {
                            LiteNetwork = liteModel,
                            TemporalSmoothing = 0.0f,
                        }
                    });

                    var deviceName = $"azure{i}";
                    k4a.ColorImage.EncodeJpeg(quality: Constants.JPEGEncodeQuality).Write($"{deviceName}.color", outputStore);
                    k4a.DepthDeviceCalibrationInfo.Write($"{deviceName}.depth-calibration", outputStore);
                    k4a.Bodies.Write($"{deviceName}.bodies", outputStore);
                    bodyStreamValidator.AddStream(k4a.Bodies, deviceName);
                    k4a.DepthImage.EncodePng().Write($"{deviceName}.depth", outputStore);

                    // Add body to merger
                    var bodyInWorld = k4a.Bodies.ChangeToFrame(transformationTree.QueryTransformation("world", Constants.SensorCorrespondMap[deviceName]));
                    var humanBodyInWorld = bodyInWorld.ChangeToHumanBodies();
                    humanBodyInWorld.Write($"{deviceName}.human-bodies", outputStore);
                    bodyMerger.AddHumanBodyStream(humanBodyInWorld, mainBodyKinectNum == i);
                }

                // Audio recording from main device
                var audioSource = new AudioCapture(p, new AudioCaptureConfiguration()
                {
                    OptimizeForSpeech = true,
                    Format = WaveFormat.Create16kHz1Channel16BitPcm()
                });
                audioSource.Write("psi_audio", outputStore);
                // send to ROS too.
                audioSource.PipeTo(rosAudioPublisher);


                bodyStreamValidator.Select(m =>
                {
                    Console.WriteLine($"All body stream publishing at originating time:{m}");
                    return m;
                }).Write("info.start_time", outputStore);
                var heartbeatSignal = azureHeartbeat.Fuse(bodyStreamValidator.First(), Available.Nearest<DateTime>()).Select(m =>
                {
                    return m.Item1;
                });
                rosListner.AddEmptySender("/psi/azure_signal", heartbeatSignal);

                // Start Pipeline
                p.Diagnostics.Write("diagnostics", outputStore); 
                p.RunAsync();

                Console.WriteLine("Press Enter to stop...");
                Console.ReadLine();
            }
        }
    }
}
