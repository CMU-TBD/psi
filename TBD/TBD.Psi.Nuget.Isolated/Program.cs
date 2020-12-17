using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Nuget.Isolated
{
    class Program
    {
        static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // second store
                var store = PsiStore.Create(p, "NugetIsolated", @"C:\Data\Store\NugetTest");
                // open recording streams
                var input = PsiStore.Open(p, "record-pipeline", @"C:\Data\Store\Recording\record-pipeline.0014");


                // save image for debugging
                var imgStream = input.OpenStream<Shared<EncodedImage>>("azure1.color");
                //imgStream.Write("color1", store);
                var imgStream2 = input.OpenStream<Shared<EncodedImage>>("azure2.color");
                //imgStream2.Write("color2", store);


                var bodies1 = input.OpenStream<List<AzureKinectBody>>("azure1.bodies");
                // var bodies1 = bodies1Origin.ChangeToFrame(transformationTree.SolveTransformation("world", "topCam"));
                bodies1.Write("bodies1", store);

                //var bodiesMainOrigin = input.OpenStream<List<AzureKinectBody>>("azure2.bodies");
                var bodiesMain = input.OpenStream<List<AzureKinectBody>>("azure2.bodies");
                // var bodiesMain = bodiesMainOrigin.ChangeToFrame(transformationTree.SolveTransformation("world", "mainCam"));
                bodiesMain.Write("bodies2", store);

                p.Diagnostics.Write("d", store);
                p.Run(ReplayDescriptor.ReplayAll);
            }
            Console.WriteLine($"Execution Time:{DateTime.Now - startTime}");
        }
    }
}
