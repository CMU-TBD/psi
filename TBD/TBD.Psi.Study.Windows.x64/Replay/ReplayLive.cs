namespace TBD.Psi.Study.Replay
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
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Data;
    using TBD.Psi.StudyComponents;
    using TBD.Psi.TransformationTree;

    public class ReplayLive
    {
        public static void Run()
        {
            while (true)
            {
                var participantId = "P1";
                var sessionName = $"{participantId}.delivery.0";
                var startTimeSec = 55.0;
                var durationSec = 10.0;
                using (var p = Pipeline.Create(enableDiagnostics: true))
                {

                    var dataset = Dataset.Load($@"E:\Study-Data\{participantId}\{participantId}.pds");
                    var pickedSession = dataset.Sessions.Where(s => s.Name == sessionName).FirstOrDefault();
                    var pickedPartition = pickedSession.Partitions.Where(m => m.Name == "recording").FirstOrDefault();

                    
                    var inputStore = PsiStore.Open(p, pickedPartition.StoreName, pickedPartition.StorePath);

                    // Send
                    var trackedBodies = inputStore.OpenStream<List<HumanBody>>("tracked");
                    var rosBodyPublisher = new ROSWorldSender(p, Constants.RosCoreAddress, Constants.RosClientAddress, useRealTime:true);

                    trackedBodies.PipeTo(rosBodyPublisher);

                    var replayDescriptor = new ReplayDescriptor(inputStore.MessageOriginatingTimeInterval.Left + TimeSpan.FromSeconds(startTimeSec), TimeSpan.FromSeconds(durationSec));

                    p.Run(replayDescriptor);
                }
            }
        }
    }
}
