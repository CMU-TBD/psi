namespace TBD.Psi.Study
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
    using TBD.Psi.StudyComponents;
    using TBD.Psi.TransformationTree;

    public class ReplayLive
    {
        public static void Run()
        {
            while (true)
            {
                using (var p = Pipeline.Create(enableDiagnostics: true))
                {
                    var inputPath = Path.Combine(Constants.LiveOperatingDirectory, Constants.PartitionIdentifier, Constants.LiveFolderName, Constants.ReplayLiveStore);
                    var inputStore = PsiStore.Open(p, Constants.LiveStoreName, inputPath);

                    // Send
                    var trackedBodies = inputStore.OpenStream<List<HumanBody>>("tracked");
                    var rosBodyPublisher = new ROSWorldSender(p, Constants.RosCoreAddress, Constants.RosClientAddress);


                    trackedBodies.PipeTo(rosBodyPublisher);

                    var replayDescriptor = new ReplayDescriptor(inputStore.MessageOriginatingTimeInterval.Left + TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(1));

                    p.Run(replayDescriptor);
                }
            }
        }
    }
}
