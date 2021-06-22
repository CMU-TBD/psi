

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

    public class Derive2DRecenter
    {
        public static void RayTo2D(double[] result, Ray3D ray)
        {
            result[0] = ray.ThroughPoint.X;
            result[1] = ray.ThroughPoint.Y;
            result[2] = ray.Direction.SignedAngleTo(UnitVector3D.XAxis, UnitVector3D.ZAxis.Negate()).Radians;
        }


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
                    var treeStream = importer.OpenStream<TransformationTree<string>>("world");
                    var bodies = importer.OpenStream<List<HumanBody>>("tracked");

                    // producers
                    IProducer<Dictionary<string, double>> baxterJointState = null;
                    IProducer<CoordinateSystem> centeredPodiPose = null;

                    var humanPositions = bodies.Fuse(treeStream.First(), Available.Nearest<TransformationTree<string>>()).Select(m =>
                    {
                        (var bodies, var tree) = m;
                        var transform = tree.QueryTransformation("world", "baxterBase").Invert();
                        var projectedArr = new List<(uint, double[])>();
                        foreach (var body in bodies)
                        {
                            // change body
                            body.RootPose = body.RootPose.TransformBy(transform);
                            var result = new double[3];
                            if (body.TorsoDirection.HasValue)
                            {
                                RayTo2D(result, body.TorsoDirection.Value);
                            }
                            else
                            {
                                result[0] = body.RootPose.Origin.X;
                                result[1] = body.RootPose.Origin.Y;
                                result[2] = body.RootPose.XAxis.SignedAngleTo(UnitVector3D.XAxis, UnitVector3D.ZAxis).Radians;
                            }
                            projectedArr.Add((body.Id, result));
                        }
                        return projectedArr;
                    });
                    humanPositions.Write("humans2D", exporter);
                    humanPositions.Select(msg =>
                    {
                        var dict = new Dictionary<string, double>();
                        foreach(var m in msg)
                        {
                            dict[m.Item1.ToString()] = Math.Sqrt((m.Item2[0] * m.Item2[0]) + (m.Item2[1] * m.Item2[1]));
                        }
                        return dict;
                    }).Write("humanDist", exporter);

                    // project podi as a rectangle
                    if (importer.HasStream("podi.pose"))
                    {
                        var podiLength = 0.5;
                        var podiWidth = 0.3;

                        var podiPose = importer.OpenStream<CoordinateSystem>("podi.pose");
                        centeredPodiPose = podiPose.Fuse(treeStream.First(), Available.Nearest<TransformationTree<string>>()).Select(m =>
                        {
                            (var cs, var tree) = m;
                            var transform = tree.QueryTransformation("world", "baxterBase").Invert();
                            var result = new double[5];
                            return cs.TransformBy(transform);
                        });
                        centeredPodiPose.Select(cs =>
                        {
                            var result = new double[5];
                            result[0] = cs.Origin.X;
                            result[1] = cs.Origin.Y;
                            result[2] = cs.XAxis.SignedAngleTo(UnitVector3D.XAxis, UnitVector3D.ZAxis).Radians;
                            result[3] = podiLength;
                            result[4] = podiWidth;
                            return ("podi", result);
                        }).Write("podi2D", exporter);
                    }
                    // project baxter's head pose
                    if (importer.HasStream("baxter.joint_states"))
                    {
                        baxterJointState = importer.OpenStream<Dictionary<string, double>>("baxter.joint_states");
                        baxterJointState.Select(jointState =>
                        {
                            // get the headjoint
                            var head_yaw = jointState["head_pan"];
                            return ("baxter", new double[] { 0, 0, -1 * head_yaw, 0.5, 0.5 });
                        }).Write("baxter2D", exporter);
                    }
                    // get analysis values
                    
                },
            "human2DRecentered",
            true,
            "human2DRecentered",
            (session) => $"E:\\Study-Data\\{session.Name.Split('.').First()}\\2d-human-recentered\\{String.Join(".", session.Name.Split('.').Skip(1))}\\"
            );
        }
    }
}
