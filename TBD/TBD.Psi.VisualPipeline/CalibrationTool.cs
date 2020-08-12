
namespace TBD.Psi.VisualPipeline
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TBD.Psi.VisualPipeline.Components;

    class CalibrationTool
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(true))
            {
                var store = Store.Create(p, "test", @"C:\Data\Stores");

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    DeviceIndex = 1
                });

                var k4a1BoardDetector = new BoardDetector(p, 4, 6, 0.0355f, 0.007f, "h");
                var k4a1Gray = k4a1.ColorImage.ToGray();
                k4a1Gray.EncodeJpeg().Write("img1", store);
                k4a1Gray.PipeTo(k4a1BoardDetector.ImageIn, DeliveryPolicy.LatestMessage);
                k4a1.DepthDeviceCalibrationInfo.PipeTo(k4a1BoardDetector.CalibrationIn);

                k4a1BoardDetector.Write("board1pose", store);

                var k4a2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    DeviceIndex = 0
                });

                var k4a2BoardDetector = new BoardDetector(p, 4, 6, 0.0355f, 0.007f, "h");
                var k4a2Gray = k4a2.ColorImage.ToGray();
                k4a2Gray.EncodeJpeg().Write("img2", store);
                k4a2Gray.PipeTo(k4a2BoardDetector.ImageIn, DeliveryPolicy.LatestMessage);
                k4a2.DepthDeviceCalibrationInfo.PipeTo(k4a2BoardDetector.CalibrationIn);

                k4a2BoardDetector.Write("board2pose", store);


                k4a1BoardDetector.Join(k4a2BoardDetector, TimeSpan.FromMilliseconds(50)).Select(m =>
                {
                    var (pose1, pose2) = m;
                    var p2Inv = pose2.Invert();
                    var cs = new CoordinateSystem(pose1 * p2Inv);
                    Console.WriteLine(cs);
                    return cs;
                }).Write("solution", store);

                p.Diagnostics.Write("diagnostics", store);
                p.RunAsync();
                Console.WriteLine("Press Enter to stop...");
                Console.ReadLine();
            }
        }
    }
}
