namespace TBD.Psi.Study.Local
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.VisionComponents;

    public class BodyMergerDebugLab
    {
        public static void Run()
        {
            var startTime = DateTime.Now;
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // create local store
                var store = PsiStore.Create(p, "BodyMergerTest", @"C:\Data\Store\BodyMergerTest");

                // open recording streams
                var input = PsiStore.Open(p, "record-pipeline", @"C:\Data\Store\Recording\record-pipeline.0014");

               // create transformation tree
                var transformationTree = new TransformationTreeTracker(p);

                // add transformation to camera1
                transformationTree.UpdateTransformation("world", "mainCam", new double[,]{
                                    {1, 0, 0, 0 },
                                    {0, 1, 0, 0 },
                                    {0, 0, 1, 1 },
                                    {0, 0, 0, 1 },
                                });


                // add transformation to sideCamera
                transformationTree.UpdateTransformation("topCam", "mainCam", new double[,]{
                                    { 0.0034197863500238235, -0.9148835725148529, -0.4037032992233034, 1.8046225530982707 },
                                    { 0.9997556870900071, 0.011943904257238241, -0.018598636570505468, -2.0351759452849567 },
                                    { 0.02183738062378381, -0.4035410659320236, 0.914700900247692, 0.2647763975581995 },
                                    { 0.0, 0.0, 0.0, 1.0 },
                                });

                // save image for debugging
                var imgStream = input.OpenStream<Shared<EncodedImage>>("azure1.color");
                //imgStream.Write("color1", store);
                var imgStream2 = input.OpenStream<Shared<EncodedImage>>("azure2.color");
                //imgStream2.Write("color2", store);

                //var bodies1Origin = input.OpenStream<List<AzureKinectBody>>("azure1.bodies");
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
