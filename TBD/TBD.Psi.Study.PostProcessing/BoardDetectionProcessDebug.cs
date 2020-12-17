using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Study.PostProcessing
{
    using Microsoft.Psi;
    using TBD.Psi.StudyComponents;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Calibration;
    using System.Threading;
    using MathNet.Spatial.Euclidean;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra.Double;

    class BoardDetectionProcessDebug
    {

        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // input
                var input = PsiStore.Open(p, "BoardDetect", @"C:\Data\Store\BoardDetection\BoardDetect.0014");

                // get the merger
                var mergeStream = input.OpenStream<(CoordinateSystem, string, CoordinateSystem, string)>("result");

                // collect the information for processing
                var poseCollection = new Dictionary<string, Dictionary<string, (List<CoordinateSystem>, List<CoordinateSystem>)>>();
                mergeStream.Do(m =>
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
                    Console.WriteLine(m.Item1);
                    Console.WriteLine(m.Item3);
                    poseCollection[m.Item2][m.Item4].Item1.Add(m.Item1);
                    poseCollection[m.Item2][m.Item4].Item2.Add(m.Item3);
                });

                p.Run(ReplayDescriptor.ReplayAll);


                var lx = new List<string>();

                //do processing here
                foreach (var item in poseCollection)
                {
                    var name1 = item.Key;
                    foreach (var poses in poseCollection[name1])
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
                System.IO.File.WriteAllLines($@"C:\Data\Cal\result2.csv", lx);

            }
        }
    }
}
