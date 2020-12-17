using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Study.PostProcessing
{
    using Microsoft.Psi;
    using TBD.Psi.StudyComponents;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Calibration;
    using System.Threading;
    using MathNet.Spatial.Euclidean;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra.Double;

    class BoardDetectionProcess
    {

        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // board information
                var boardMarkerSize = 0.115f;
                //var boardMarkerSize = 0.077f;
                var boardMarkerDist = 0.023f;
                //var boardMarkerDist = 0.00151f;

                var store = PsiStore.Create(p, "BoardDetect", @"C:\Data\Store\BoardDetection");
                var camNum = 4;

                // create a calibration tool
                var calibrationMerger = new CalibrationMerger(p, store, 2, 3, boardMarkerSize, boardMarkerDist, "DICT_4X4_100");

                // open different streams
                var input = PsiStore.Open(p, "calibration-recording", @"C:\Data\Store\calibration-recording\calibration-recording.0006");

                for(var i = 0; i <= camNum; i++)
                {
                    if (input.AvailableStreams.Any(m => m.Name == $"azure{i}.color"))
                    {
                        var color = input.OpenStream<Shared<EncodedImage>>($"azure{i}.color");
                        var calibration = input.OpenStream<IDepthDeviceCalibrationInfo>($"azure{i}.depth-Calibration");
                        calibrationMerger.AddSavedStreams(color.Decode().Out, calibration.Out, $"color{i}");
                    }
                }

                calibrationMerger.Write("result", store);
                p.Diagnostics.Write("diagnotics", store);

                // collect the information for processing
                var poseCollection = new Dictionary<string, Dictionary<string, (List<CoordinateSystem>, List<CoordinateSystem>)>>();
                calibrationMerger.Do(m =>
                {
                    if (!poseCollection.ContainsKey(m.Item2))
                    {
                        poseCollection[m.Item2] = new Dictionary<string, (List<CoordinateSystem>, List<CoordinateSystem>)>();
                    }
                    if (!poseCollection[m.Item2].ContainsKey(m.Item4))
                    {
                        var list1 = new List<CoordinateSystem>();
                        var list2 = new List<CoordinateSystem>();
                        poseCollection[m.Item2][m.Item4] = (list1, list2);
                    }
                    poseCollection[m.Item2][m.Item4].Item1.Add(m.Item1);
                    poseCollection[m.Item2][m.Item4].Item2.Add(m.Item3);
                });

                p.Run();

                var lines = new List<string>();
                var lx = new List<string>();

                //do processing here
                foreach (var item in poseCollection)
                {
                    var name1 = item.Key;
                    foreach(var poses in item.Value)
                    {
                        var name2 = poses.Key;
                        var list1 = poses.Value.Item1;
                        var list2 = poses.Value.Item2;

                        // write to solution
                        for (var i = 0; i < list1.Count; i++)
                        {
                            lx.Add(String.Join(",",list1[i].Storage.ToRowMajorArray()) + $",{name1}");
                            lx.Add(String.Join(",",list2[i].Storage.ToRowMajorArray()) + $",{name2}");
                        }
                    }
                }
                System.IO.File.WriteAllLines($@"C:\Data\Cal\result.csv", lx);
            }
        }
    }
}
