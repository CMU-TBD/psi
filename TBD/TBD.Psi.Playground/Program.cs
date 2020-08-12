namespace TBD.Psi.Playground
{
    using System;
    using MathNet.Numerics.LinearAlgebra.Storage;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;

    class Program
    {
        static void Main(string[] args)
        {
            /* using (var p = Pipeline.Create())
             {
                 var store = Store.Create(p, "test", @"C:\Data\Stores");

                 var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                 {
                     OutputColor = true,
                     DeviceIndex = 0
                 });

 *//*                var k4a2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                 {
                     OutputColor = true,
                     DeviceIndex = 1
                 });*//*

                 k4a1.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("cam1", store);
                 *//*                k4a2.ColorImage.Write("cam2", store);
                 */

            /*                var capture = new MediaCapture(p, 1280, 720);
                            capture.Write("Image", store);
            *//*
            p.RunAsync();
            Console.ReadLine();
        }*/
            var cs = new CoordinateSystem(new Point3D(1, 2, 3), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis);
            Console.WriteLine(cs.Invert());
            var cs2 = new CoordinateSystem(new Point3D(1, 2, 3), UnitVector3D.YAxis, UnitVector3D.XAxis.Negate(), UnitVector3D.ZAxis);
            Console.WriteLine(cs2.Invert());
            Console.ReadLine();


        }
    }
}
