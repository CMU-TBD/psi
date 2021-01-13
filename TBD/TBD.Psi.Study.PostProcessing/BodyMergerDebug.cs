using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBD.Psi.VisionComponents;

namespace TBD.Psi.Study.PostProcessing
{
    public class BodyMergerDebug
    {
        public static void Run()
        {
            using(var p = Pipeline.Create(enableDiagnostics:true))
            {
                var store = PsiStore.Create(p, "BodyMergerTest", @"C:\Data\Store\BodyMergerTest");
              
                // open recording streams
                var input = PsiStore.Open(p, "record-pipeline", @"C:\Data\Store\Recording\record-pipeline.0003");

                // create transformation tree
                var transformationTree = new TransformationTreeTracker(p);

                // add transformation to camera1
                transformationTree.UpdateTransformation("world", "mainCam", new double[,]{
                    {0.8660254, 0.5, 0, 0 },
                    {-0.5, 0.8660254, 0, 0 },
                    {0, 0, 1, 1 },
                    {0, 0, 0, 1 },
                });


                // add transformation to sideCamera
                transformationTree.UpdateTransformation("sideCam", "mainCam", new double[,]{
                    { 0.021366686814041747, 0.9997593987944009, -0.004960767768111677, 3.663849404532466 },
                    { -0.9986242695702232, 0.02110424361024798, -0.04800186586975543, 1.806384754318782 },
                    { -0.047885623311484125, 0.005979583923466899, 0.998834927130691, 0.16345326144632713 },
                    { 0.0, 0.0, 0.0, 1.0 },
                });

                // add transformation to topCam
                transformationTree.UpdateTransformation("topCam", "mainCam", new double[,]{
                    { -0.7906092108082582, 0.43011935238484006, -0.4358146607093302, 4.676153322040751 },
                    { -0.4907624861236131, -0.8707444303958708, 0.03092441023432977, 1.6669607250820022 },
                    { -0.3661820011946423, 0.2383306099489041, 0.8995050096372285, 1.3090538580082756 },
                    { 0.0, 0.0, 0.0, 1.0 },
                });

                // save image for debugging
                var imgStream = input.OpenStream<Shared<EncodedImage>>("azure0.color");
                imgStream.Decode().EncodeJpeg(quality: 50).Write("color", store);

                var bodies1Origin = input.OpenStream<List<AzureKinectBody>>("azure1.bodies");
                var bodies1 = bodies1Origin.ChangeToFrame(transformationTree.SolveTransformation("world", "topCam"));
                bodies1.Write("bodies1", store);

                var bodies0Origin = input.OpenStream<List<AzureKinectBody>>("azure0.bodies");
                var bodies0 = bodies0Origin.ChangeToFrame(transformationTree.SolveTransformation("world", "sideCam"));
                bodies0.Write("bodies2", store);

                var bodiesMainOrigin = input.OpenStream<List<AzureKinectBody>>("azure3.bodies");
                var bodiesMain = bodiesMainOrigin.ChangeToFrame(transformationTree.SolveTransformation("world", "mainCam"));
                bodiesMain.Write("bodies0", store);

                var merger = new BodyMerger(p);
                merger.AddHumanBodyStream(bodies0.ChangeToHumanBodies());
                merger.AddHumanBodyStream(bodies1.ChangeToHumanBodies());
                merger.AddHumanBodyStream(bodiesMain.ChangeToHumanBodies(), isMain:true);

                transformationTree.WorldFrameOutput.Write("world", store);

                var tracker = new BodyTracker(p);
                merger.PipeTo(tracker);
                tracker.Write("tracked", store);
                p.Diagnostics.Write("diagnostics", store);

                p.Run();
            }
        }
    }
}
