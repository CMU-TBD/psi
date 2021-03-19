

namespace TBD.Psi.Study
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.StudyComponents;
    using Microsoft.Psi.Components;

    public class PostProcessBoardDetectionZipOnly
    {
        public static void Run()
        {
            var start_time = DateTime.Now;
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                
                var deliveryPolicy = DeliveryPolicy.SynchronousOrThrottle;

                // Generate the store path
                var inputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, Constants.OperatingStore);
                var inputStore = PsiStore.Open(p, Constants.OperatingStore.Split('\\').Last().Split('.').First(), inputStorePath);

                // get all the poses
                var poseEmitters = new List<Emitter<CoordinateSystem>>();

                foreach (var poseStream in inputStore.AvailableStreams.Where(s => s.Name.EndsWith(".pose")))
                {
                    var emitter = inputStore.OpenStream<CoordinateSystem>(poseStream.Name).Out;
                    poseEmitters.Add(emitter);
                }

                // zip all of them
                var zipper = new Zip<(CoordinateSystem, string, CoordinateSystem, string)>(p);
                for (var i = 0; i < poseEmitters.Count; i++)
                {
                    for (var j = i + 1; j < poseEmitters.Count; j++)
                    {
                        var name1 = poseEmitters[i].Name.Split('.').First();
                        var name2 = poseEmitters[j].Name.Split('.').First();
                        // join and send to merger
                        var joiner = poseEmitters[i].Join(poseEmitters[j], TimeSpan.FromMilliseconds(5), deliveryPolicy, deliveryPolicy).Select(m =>
                        {
                            var (pose1, pose2) = m;
                            return (pose1, name1, pose2, name2);
                        }, deliveryPolicy);
                        var receiver = zipper.AddInput($"zip-{i}-{j}");
                        joiner.PipeTo(receiver, deliveryPolicy);
                    }
                }

                // collect the information for processing
                var poseCollection = new Dictionary<string, Dictionary<string, (List<CoordinateSystem>, List<CoordinateSystem>)>>();
                zipper.Select(m => m.First(), deliveryPolicy).Do(m =>
                 {
                     if (!poseCollection.ContainsKey(m.Item2))
                     {
                         poseCollection[m.Item2] = new Dictionary<string, (List<CoordinateSystem>, List<CoordinateSystem>)>();
                     }
                     if (!poseCollection[m.Item2].ContainsKey(m.Item4))
                     {
                         var list1 = new List<CoordinateSystem>();
                         var list2 = new List<CoordinateSystem>();
                         poseCollection[m.Item2][m.Item4] = (list1, list2);
                     }
                     poseCollection[m.Item2][m.Item4].Item1.Add(new CoordinateSystem(m.Item1));
                     poseCollection[m.Item2][m.Item4].Item2.Add(new CoordinateSystem(m.Item3));
                 }, deliveryPolicy);

                p.ProposeReplayTime(TimeInterval.LeftBounded(DateTime.UtcNow));
                Generators.Repeat(p, true, 2, TimeSpan.FromSeconds(180));

                p.Run(ReplayDescriptor.ReplayAll);

                var lines = new List<string>();
                var lx = new List<string>();

                //do processing here
                foreach (var item in poseCollection)
                {
                    var name1 = item.Key;
                    foreach (var poses in item.Value)
                    {
                        var name2 = poses.Key;
                        var list1 = poses.Value.Item1;
                        var list2 = poses.Value.Item2;

                        // write to solution
                        for (var i = 0; i < list1.Count; i++)
                        {
                            lx.Add(String.Join(",", list1[i].Storage.ToRowMajorArray()) + $",{name1}");
                            lx.Add(String.Join(",", list2[i].Storage.ToRowMajorArray()) + $",{name2}");
                        }
                    }
                }
                // generate file path
                var csvPath = Path.Combine(Constants.ResourceLocations, $"board-{Constants.StudyType}-{Constants.PartitionIdentifier}.csv");
                int index = 0;
                while (File.Exists(csvPath))
                {
                    csvPath = Path.Combine(Constants.ResourceLocations, $"board-{Constants.StudyType}-{Constants.PartitionIdentifier}-{++index}.csv");
                }
                System.IO.File.WriteAllLines(csvPath, lx);
            }
            Console.WriteLine($"total time:{(DateTime.Now - start_time).TotalSeconds}");
        }
    }
}
