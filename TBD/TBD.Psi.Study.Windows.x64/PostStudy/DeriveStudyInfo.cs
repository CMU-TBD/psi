

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

    public class DeriveStudyInfo
    {
        public static void Run()
        {
            var datasetPath = Path.Combine(Constants.RootPath, Constants.ParticipantToAnalyze, $"{Constants.ParticipantToAnalyze}.pds");
/*            Task.Run(() => FindInteractor(datasetPath)).Wait();
*/            Task.Run(() => CalculateMemberInformation(datasetPath)).Wait();
        }

        private static async Task FindInteractor(string datasetPath)
        {
            // Open Dataset
            var dataset = Dataset.Load(datasetPath, autoSave: true);
            await dataset.CreateDerivedPartitionAsync(
                (pipeline, importer, exporter) =>
                {
                    var bodies = importer.OpenStream<List<HumanBody>>("tracked");
                    var studyState = importer.OpenStream<Tuple<string, TimeSpan>>("state").Process<Tuple<string, TimeSpan>, string>((m,e,o) =>
                    {
                        o.Post(m.Item1, e.OriginatingTime - m.Item2);
                    });

                    int interactPerson = -1; 
                    bodies.Fuse(studyState, Available.Nearest<string>()).Process<(List<HumanBody>, string), int>( (m,e,o) =>
                    {
                        if (m.Item2.StartsWith("start-") && interactPerson == -1)
                        {
                            // study started. Let's find the first person
                            if(m.Item1.Select(b => b.Id).Count() > 0)
                            {
                                interactPerson = (int) m.Item1.Select(b => b.Id).FirstOrDefault();
                                o.Post(interactPerson, pipeline.StartTime);
                            }
                        }
                    }).Write("interactor", exporter);
                },
            "StudyState",
            true,
            "StudyState",
            (session) => $"E:\\Study-Data\\{session.Name.Split('.').First()}\\StudyState\\{session.Name.Split('.')[1]}\\"
            );
        }

        private static double CalculateXYDistance(CoordinateSystem cs)
        {
            return Math.Sqrt(cs.Origin.X * cs.Origin.X + cs.Origin.Y * cs.Origin.Y);
        }

        private static double CalculateYaw(CoordinateSystem cs)
        {
            var val = cs.XAxis.SignedAngleTo(UnitVector3D.XAxis, UnitVector3D.ZAxis.Negate()).Radians;
            if (val < -(Math.PI/2))
            {
                val += Math.PI;
            }
            return val;
            //return cs.XAxis.SignedAngleTo(UnitVector3D.XAxis, UnitVector3D.ZAxis.Negate()).Radians;
        }

        private static double CalculateYaw(Ray3D ray)
        {
            var val = ray.Direction.SignedAngleTo(UnitVector3D.XAxis, UnitVector3D.ZAxis.Negate()).Radians;
            if (val < -(Math.PI / 2))
            {
                val += Math.PI;
            }
            return val;
        }


        private static async Task CalculateMemberInformation(string datasetPath)
        {
            // Open Dataset
            var dataset = Dataset.Load(datasetPath, autoSave: true);
            await dataset.CreateDerivedPartitionAsync(
                (pipeline, importer, exporter) =>
                {
                    var treeStream = importer.OpenStream<TransformationTree<string>>("world");
                    var bodies = importer.OpenStream<List<HumanBody>>("tracked");
                    // var interatorInfo = importer.OpenStream<int>("interactor");
                    var podiStream = importer.OpenStream<CoordinateSystem>("podi.pose");
                    var baxterJointStream = importer.OpenStream<Dictionary<string, double>>("baxter.joint_states");

                    var studyState = importer.OpenStream<Tuple<string, TimeSpan>>("state").Process<Tuple<string, TimeSpan>, string>((m, e, o) =>
                    {
                        o.Post(m.Item1, e.OriginatingTime - m.Item2);
                    });

                    int interactPerson = -1;
                    bool started = false;
                    var interatorInfo = bodies.Fuse(studyState, Available.Nearest<string>()).Process<(List<HumanBody>, string), int>((m, e, o) =>
                    {
                        if (m.Item2.StartsWith("1-to-1"))
                        {
                            started = true;
                        }
                        if (started && interactPerson == -1)
                        {
                            // study started. Let's find the first person we see
                            if (m.Item1.Select(b => b.Id).Count() > 0)
                            {
                                interactPerson = (int)m.Item1.Select(b => b.Id).FirstOrDefault();
                                o.Post(interactPerson, pipeline.StartTime);
                            }
                        }
                    });

                    // get the streams with only the interactor
                    var interatorStream = bodies.Fuse(interatorInfo.First(), Available.Nearest<int>()).Select(m =>
                    {
                        (var bodies, var iid) = m;
                        return bodies.Where(b => b.Id == iid).FirstOrDefault();
                    }).Process<HumanBody, HumanBody>((m, e, o) =>
                    {
                        if (m != null)
                        {
                            o.Post(m, e.OriginatingTime);
                        }
                    });
                    // now we can merger all of them
                    var infoStream = interatorStream.Join(podiStream.Join(baxterJointStream, TimeSpan.FromMilliseconds(50)), TimeSpan.FromMilliseconds(50))
                        .Fuse(treeStream.First(), Available.Nearest<TransformationTree<string>>())
                        .Select(m =>
                        {
                            (var humanBody, var podiBase, var jointState, var tree) = m;
                            var transform = tree.QueryTransformation("world", "baxterBase").Invert();

                            humanBody.RootPose = humanBody.RootPose.TransformBy(transform);
                            return (humanBody, podiBase.TransformBy(transform), jointState["head_pan"]);
                        });
                    infoStream.Select(m =>
                    {
                        (var hb, var podi, var roboPan) = m;
                        var distDict = new Dictionary<string, double>();
                        distDict["interactor"] = CalculateXYDistance(hb.RootPose);
                        distDict["podi"] = CalculateXYDistance(podi);
                        return distDict;
                    }).Write("distance", exporter);
                    infoStream.Select(m =>
                    {
                        (var hb, var podi, var roboPan) = m;
                        var distDict = new Dictionary<string, double>();
                        distDict["interactor"] = hb.RootPose.Origin.X;
                        // distDict["podi"] = podi.Origin.X;
                        return distDict;
                    }).Write("X", exporter);
                    infoStream.Select(m =>
                    {
                        (var hb, var podi, var roboPan) = m;
                        var distDict = new Dictionary<string, double>();
                        distDict["interactor"] = hb.RootPose.Origin.Y;
                        //distDict["podi"] = podi.Origin.Y;
                        return distDict;
                    }).Write("Y", exporter);
                    infoStream.Select(m =>
                    {
                        (var hb, var podi, var roboPan) = m;
                        var yawDict = new Dictionary<string, double>();
                        yawDict["interactor"] = CalculateYaw(hb.RootPose);
                        if (hb.TorsoDirection.HasValue)
                        {
                            yawDict["interactor-torso"] = CalculateYaw(hb.TorsoDirection.Value);
                        }
                        if (hb.GazeDirection.HasValue)
                        {
                            yawDict["interactor-gaze"] = CalculateYaw(hb.GazeDirection.Value);
                        }
                        yawDict["podi"] = CalculateYaw(podi);
                        yawDict["baxter"] = (-1 * roboPan) - (Math.PI/2);
                        return yawDict;
                    }).Write("angle", exporter);
                    pipeline.Diagnostics.Write("studyinfo-diagnostics", exporter);
                },
            "StudyInfo",
            true,
            "StudyInfo",
            (session) => $"E:\\Study-Data\\{session.Name.Split('.').First()}\\StudyInfo\\{String.Join(".",session.Name.Split('.').Skip(1))}\\",
            replayDescriptor:ReplayDescriptor.ReplayAll,
            enableDiagnostics:true
            );
        }


    }
}
