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
                list.Add(new CoordinateSystem());
                double[,] t1 =
{
{ -0.7640544440268732, -0.5094690182793657, -0.3957540536658895, 3.605137586889407 },
{ 0.4676496447844323, -0.8600016166031593, 0.20427426842670796, -0.5985497762882507 },
{ -0.44443741549939314, -0.02895753534767454, 0.8953495140255282, 0.5265136789507412 },
                    { 0.0, 0.0, 0.0, 1.0 },
                };
                list.Add(new CoordinateSystem(Matrix<double>.Build.DenseOfArray(t1)));
                double[,] t2 =
{
{ -0.9682669502425688, 0.13479699093589156, -0.4281236048937534, 3.7785025457781205 },
{ -0.19326699820852777, -0.9464506655547379, 0.11873133529923119, 1.0013886956147995 },
{ -0.15845182381853345, 0.2933613964134049, 0.8958867389077924, 0.20263601087790348 },
                    { 0.0, 0.0, 0.0, 1.0 },
                };
                list.Add(new CoordinateSystem(Matrix<double>.Build.DenseOfArray(t2)));



                var gen = Generators.Repeat(p, list, 1, TimeSpan.FromSeconds(1));
                gen.Write("world", store);
                p.Run();
            }
        }
    }
}
