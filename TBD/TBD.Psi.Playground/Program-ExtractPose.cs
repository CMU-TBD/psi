namespace TBD.Psi.Playground
{
    using CsvHelper;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using System;
    using System.Globalization;
    using System.IO;

    public class ExtractPose
    {
        public static void Run(string[] args)
        {
            using (var writer = new StreamWriter(@"C:\Data\pose.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Open(p, "test", @"C:\Data\pose_store");
                var pose1 = store.OpenStream<CoordinateSystem>("board1pose");
                var pose2 = store.OpenStream<CoordinateSystem>("board2pose");

                pose1.Join(pose2, TimeSpan.FromMilliseconds(50)).Do(m =>
                {
                    var (cs1, cs2) = m;
                    csv.WriteField(cs1.Values);
                    csv.NextRecord();
                    csv.WriteField(cs2.Values);
                    csv.NextRecord();
                });

                p.Run(ReplayDescriptor.ReplayAll);
            }
        }
    }
}
