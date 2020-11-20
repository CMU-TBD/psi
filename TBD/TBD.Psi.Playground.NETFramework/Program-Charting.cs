using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace TBD.Psi.Playground.NETFramework
{
    public class Charting
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Create(p, "charting", @"C:\Data\Store\Charting");

                var list = new List<CoordinateSystem>();

                // base frame
                list.Add(new CoordinateSystem());
                // transform to robocept cap
                double[,] t1 =
                {
                    {0.8660254, 0.5, 0, 0 },
                    {-0.5, 0.8660254, 0, 0 },
                    {0, 0, 1, 1 },
                    {0, 0, 0, 1 },
                };
                var transform = new CoordinateSystem(Matrix<double>.Build.DenseOfArray(t1));
                list.Add(transform);

                double[,] t2 =
{
{ 0.021366686814041747, 0.9997593987944009, -0.004960767768111677, 3.663849404532466 },
{ -0.9986242695702232, 0.02110424361024798, -0.04800186586975543, 1.806384754318782 },
{ -0.047885623311484125, 0.005979583923466899, 0.998834927130691, 0.16345326144632713 },
{ 0.0, 0.0, 0.0, 1.0 },
                };

                var sensorFrame = (new CoordinateSystem(Matrix<double>.Build.DenseOfArray(t2))).Invert().TransformBy(transform);
                list.Add(sensorFrame);

                double[,] t3 =
{
{ -0.7906092108082582, 0.43011935238484006, -0.4358146607093302, 4.676153322040751 },
{ -0.4907624861236131, -0.8707444303958708, 0.03092441023432977, 1.6669607250820022 },
{ -0.3661820011946423, 0.2383306099489041, 0.8995050096372285, 1.3090538580082756 },
{ 0.0, 0.0, 0.0, 1.0 },
                };

                var sensorFrame2 = (new CoordinateSystem(Matrix<double>.Build.DenseOfArray(t3))).Invert().TransformBy(transform);
                list.Add(sensorFrame2);

                var gen = Generators.Repeat(p, list, 2, TimeSpan.FromSeconds(1));
                gen.Write("world", store);
                p.Run();
            }
        }
    }
}
