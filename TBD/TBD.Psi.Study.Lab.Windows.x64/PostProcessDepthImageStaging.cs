

namespace TBD.Psi.Study.Lab.Windows.x64
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using TBD.Psi.VisionComponents;

    public class PostProcessDepthImageStaging
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // create input & output stores
                var inputStore = PsiStore.Open(p, "calibration-recording", @"C:\Data\Lab-Store\recordings\calibration-recording.0000");
                var outputStore = PsiStore.Create(p, "depth-image-merger", @"C:\Data\Lab-Store\test");

                //create transformation tree and build the relationships
                var transformationTree = new TransformationTreeTracker(p);

                // add transformation to stage right camera
                transformationTree.UpdateTransformation("world", "mainCam", new double[,]{
                                                    {1, 0, 0, 0   },
                                                    {0, 1, 0, 0   },
                                                    {0, 0, 1, 1.5 },
                                                    {0, 0, 0, 1   },
                                                });
                // add transformation to topdown camera
                transformationTree.UpdateTransformation("topCam", "mainCam", new double[,]{
                    { 0.046200, -0.891283, -0.451088, 1.712216 },
                    { 0.997441, 0.065828, -0.027910, -2.077110 },
                    { 0.054570, -0.448644, 0.892043, 0.150837 },
                    { 0.000000, 0.000000, 0.000000, 1.000000 },
                 });
                // add transformation to stage left camera
                transformationTree.UpdateTransformation("topCam", "leftCam", new double[,]{
                    { 0.046137, -0.892165, -0.449346, 1.713954 },
                    { 0.997516, 0.065119, -0.026871, -2.076266 },
                    { 0.053235, -0.446990, 0.892953, 0.156616 },
                    { 0.000000, 0.000000, 0.000000, 1.000000 },
                 });

                var mainCamToWorld = transformationTree.SolveTransformation("world", "mainCam");
                var topCamToWorld = transformationTree.SolveTransformation("world", "topCam");
                var leftCamToWorld = transformationTree.SolveTransformation("world", "leftCam");





                /*

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
                                tracker.Write("tracked", store);*/

                p.Diagnostics.Write("diagnostics", outputStore);
                // Note to self: Cannot run at maximum speed due to the need to use pipeline time to 
                // calibrate missing data.
                p.Run(ReplayDescriptor.ReplayAll);
            }
        }
    }
}
