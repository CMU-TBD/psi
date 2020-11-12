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
{ 0.02111307582015721, 0.9997535908665068, -0.004891944359703112, 3.663849404532466 },
{ -0.9986281702706811, 0.021354612500851168, -0.047970782970728584, 1.806384754318782 },
{ -0.04791675668536122, 0.00606119414503281, 0.998836759866975, 0.16345326144632713 },
{ 0.0, 0.0, 0.0, 1.0 },
                };

                var sensorFrame = (new CoordinateSystem(Matrix<double>.Build.DenseOfArray(t2))).Invert().TransformBy(transform);
                list.Add(sensorFrame);

                double[,] t3 =
{
{ -0.7906074630138145, 0.4301133865561143, -0.4358102009513281, 4.676153322040751 },
{ -0.49076508611726105, -0.8707456185633038, 0.030917686625362845, 1.6669607250820022 },
{ -0.36618229022602566, 0.23833703543100465, 0.8995074015262459, 1.3090538580082756 },
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
