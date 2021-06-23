

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

    public class ReevaluateTracking
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
                    var mainCamId = 1;
                    var bodyMerger = new BodyMerger(pipeline);
                    var bodyTracker = new BodyTracker(pipeline);

                    for (int i = 1; i <= 3; i++)
                    {
                        var deviceName = $"azure{i}";
                        var humanbodyStream = importer.OpenStream<List<HumanBody>>($"{deviceName}.human-bodies");
                        bodyMerger.AddHumanBodyStream(humanbodyStream, mainCamId == i);
                    }
                    bodyMerger.PipeTo(bodyTracker);
                    bodyTracker.Write("new_tracked", exporter);
                },
            "reevaluateHuman",
            true,
            "reevaluateHuman",
            (session) => $"E:\\Study-Data\\{session.Name.Split('.').First()}\\reevaluateHuman\\\\{String.Join(".", session.Name.Split('.').Skip(1))}\\"
            );
        }
    }
}
