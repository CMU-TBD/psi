using g3;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using RosSharp.Urdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBD.Psi.StudyComponents;

namespace TBD.Psi.Playground.Windows.x64
{
    public class TestBox
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Create(p, "box", @"E:\Data\playground");
                var gen = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(100));
                gen.Select(m =>
                {
                    return new Box3d(
                        new Vector3d(0, m / 10.0, 0),
                        new Vector3d(0, 1, 0),
                        new Vector3d(-1, 0, 0),
                        new Vector3d(0, 0, 1),
                        new Vector3d(0.2, 0.1, 0.5)
                    );
                }).Write("box", store);
                p.Run();
            }
        }
    }
}
