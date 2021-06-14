

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

    public class DeriveVisualize
    {
        public static void Run()
        {
            var datasetPath = Path.Combine(Constants.RootPath, Constants.ParticipantToAnalyze, $"{Constants.ParticipantToAnalyze}.pds");
            var task = Task.Run(() => _Run(datasetPath));
            task.Wait();
        }

        public static async Task _Run(string datasetPath)
        {
            // Open Dataset
            var dataset = Dataset.Load(datasetPath, autoSave: true);
            await dataset.CreateDerivedPartitionAsync(
            (pipeline, importer, exporter) =>
            {
                if (importer.HasStream("podi.pose"))
                {                   
                    // all the other manual conversions
                    var podiURDF = new Robot(@"D:\ROS\tbd_podi_description\urdf\podi.urdf");
                    var podiPose = importer.OpenStream<CoordinateSystem>("podi.pose");
                    podiPose.Select(cs =>
                    {
                        return (cs, podiURDF, new Dictionary<string, double>());
                    }).Write("viz.podi", exporter);
                }
                // get position of baxter
                if (importer.HasStream("world") && importer.HasStream("baxter.joint_states"))
                {
                    var baxterURDF = new Robot(@"D:\ROS\baxter_description\urdf\baxter.urdf");
                    var treeStream = importer.OpenStream<TransformationTree<string>>("world");
                    var jointStream = importer.OpenStream<Dictionary<string, double>>("baxter.joint_states");
                    jointStream.Join(treeStream, TimeSpan.FromMilliseconds(300)).Select( m =>
                    {
                        (var joints, var tree) = m;
                        var transform = tree.QueryTransformation("world", "baxterBase");
                        return (transform, baxterURDF, joints);
                    }).Write("viz.baxter", exporter);
                }
            },
            "viz",
            true,
            "viz",
            (session) => $"E:\\Study-Data\\{session.Name.Split('.').First()}\\viz\\{String.Join(".",session.Name.Split('.').Skip(1).ToArray())}\\"
            );
        }
    }
}
