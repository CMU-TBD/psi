

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

    public class Derive2D
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
                    var bodies = importer.OpenStream<List<HumanBody>>("tracked");
                    bodies.Select(m =>
                    {
                        var projectedArr = new List<(uint, double[])>();
                        foreach (var body in m)
                        {
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
                    }).Write("humans2D", exporter);

                    // project podi as a rectangle
                    if (importer.HasStream("podi"))
                    {
                        var podiLength = 0.5;
                        var podiWidth = 0.3;
                        importer.OpenStream<CoordinateSystem>("podi").Select(cs =>
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

                },
            "human2D",
            true,
            "human2D",
            (session) => $"E:\\Study-Data\\{session.Name.Split('.').First()}\\2d-human\\{session.Name.Split('.')[1]}\\"
            );
        }
    }
}
