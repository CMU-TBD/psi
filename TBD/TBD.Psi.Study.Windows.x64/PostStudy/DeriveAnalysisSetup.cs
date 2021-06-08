

namespace TBD.Psi.Study.PostStudy
{
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TBD.Psi.StudyComponents;
    using MathNet.Spatial.Euclidean;
    using TBD.Psi.TransformationTree;
    using RosSharp.Urdf;
    using Win3D = System.Windows.Media.Media3D;
    using g3;

    public class DeriveAnalysisSetup
    {
        public static void Run()
        {
            var task = Task.Run(_Run);
            task.Wait();
        }

        public static async Task _Run()
        {
            // Open Dataset
            var dataset = Dataset.Load(Constants.DatasetPath);
            await dataset.CreateDerivedPartitionAsync(
            (pipeline, importer, exporter) =>
            {
                if (importer.HasStream("podi"))
                {
                 
                    // all the other manual conversions
                    var podiURDF = new Robot(@"D:\ROS\tbd_podi_description\urdf\podi.urdf");
                    var podiPose = importer.OpenStream<CoordinateSystem>("podi");
                    podiPose.Select(cs =>
                    {
                        var podiHeight = 0.6;
                        var originalFrame = cs.ToFrame3f();
                        originalFrame.Translate(new Vector3f(0, 0, podiHeight / 2));
                        return new Box3d(originalFrame, new Vector3f(0.3, 0.15, podiHeight/2));
                    }).Write("pose.box", exporter);
                }
                // get position of baxter
/*                if (importer.HasStream("world") && importer.HasStream("baxter.joint_states"))
                {
                    var baxterURDF = new Robot(@"D:\ROS\baxter_description\urdf\baxter.urdf");
                    var treeStream = importer.OpenStream<TransformationTree<string>>("world");
                    jointStream.Join(treeStream, TimeSpan.FromMilliseconds(300)).Select( m =>
                    {
                        (var joints, var tree) = m;
                        var transform = tree.QueryTransformation("world", "baxterBase");
                        return (transform, baxterURDF, joints);
                    }).Write("pose.baxter", exporter);
                }*/
            },
            "analysisSetup",
            true,
            "analysisSetup",
            (session) => $"E:\\Study-Data\\{session.Name.Split('.').First()}\\analysisSetup\\{session.Name.Split('.')[1]}\\"
            );
            dataset.Save(Constants.DatasetPath);
        }
    }
}
