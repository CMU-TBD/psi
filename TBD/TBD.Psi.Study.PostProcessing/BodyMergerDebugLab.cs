namespace TBD.Psi.Study.PostProcessing
{
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using System;
    using System.Collections.Generic;
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
                    { 0.003420, -0.914884, -0.403703, 1.804623 },
                    { 0.999756, 0.011944, -0.018599, -2.035176 },
                    { 0.021837, -0.403541, 0.914701, 0.264776 },
                    { 0.000000, 0.000000, 0.000000, 1.000000 },
                 });

                // save image for debugging
                var imgStream = input.OpenStream<Shared<EncodedImage>>("azure1.color");
                imgStream.Write("color1", store);
                var imgStream2 = input.OpenStream<Shared<EncodedImage>>("azure2.color");
                imgStream2.Write("color2", store);

                var bodies1Origin = input.OpenStream<List<AzureKinectBody>>("azure1.bodies");
                //var bodies1 = input.OpenStream<List<AzureKinectBody>>("azure1.bodies");
                var bodies1 = bodies1Origin.ChangeToFrame(transformationTree.SolveTransformation("world", "topCam"), DeliveryPolicy.LatestMessage);
                bodies1.Write("bodies1", store);

                var bodiesMainOrigin = input.OpenStream<List<AzureKinectBody>>("azure2.bodies");
                //var bodiesMain = input.OpenStream<List<AzureKinectBody>>("azure2.bodies");
                var bodiesMain = bodiesMainOrigin.ChangeToFrame(transformationTree.SolveTransformation("world", "mainCam"), DeliveryPolicy.LatestMessage);
                bodiesMain.Write("bodies2", store);

                var merger = new BodyMerger(p, DeliveryPolicy.LatestMessage);
                merger.AddHumanBodyStream(bodies1.ChangeToHumanBodies(DeliveryPolicy.LatestMessage));
                merger.AddHumanBodyStream(bodiesMain.ChangeToHumanBodies(DeliveryPolicy.LatestMessage), isMain: true);

                transformationTree.WorldFrameOutput.Write("world", store);

                var tracker = new BodyTracker(p);
                merger.PipeTo(tracker, DeliveryPolicy.LatestMessage);
                tracker.Write("tracked", store);

                p.Diagnostics.Write("diagnostics", store);
                // Note to self: Cannot run at maximum speed due to the need to use pipeline time to 
                // calibrate missing data.
                p.Run(ReplayDescriptor.ReplayAll);
            }
            Console.WriteLine($"Execution Time:{DateTime.Now - startTime}");
        }
    }
}

