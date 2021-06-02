using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using RosSharp.Urdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Playground.Windows.x64
{
    public class URDFSave
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var podi = new Robot(@"D:\ROS\tbd_podi_description\urdf\podi.urdf");
                var baxter = new Robot(@"D:\ROS\baxter_description\urdf\baxter.urdf");
                var store = PsiStore.Create(p, "urdf", @"E:\Data\playground");
                var gen = Generators.Range(p, 1, 50, TimeSpan.FromSeconds(0.1));

                gen.Select(_ =>
                {
                    return (new CoordinateSystem(new Point3D(-5, 0, 0), UnitVector3D.YAxis,UnitVector3D.XAxis.Negate(), UnitVector3D.ZAxis), podi.links[0]);
                }).Write("podi_base", store);

                gen.Select(m =>
                {
                    return (new CoordinateSystem(new Point3D(m*0.1,0,0), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis), podi, new Dictionary<string, double>());
                }).Write("podi", store);

                gen.Select(m =>
                {
                    return (new CoordinateSystem(new Point3D(0, 3, 0), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis), baxter, new Dictionary<string, double>());
                }).Write("baxter", store);


                p.Run();
            }
        }
    }
}
