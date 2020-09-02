using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.VisualPipeline
{
    public class RecordingPipeline
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var store = Store.Create(p, "record-compress", @"C:\Data\Stores");

                // start azure1 - main
                var azure1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    WiredSyncMode = WiredSyncMode.Master,
                    Exposure = TimeSpan.FromTicks(100000),
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                        TemporalSmoothing = 0.1f,
                    },
                    DeviceIndex = 1,
                    SynchronizedImagesOnly = true,
                });

                azure1.Bodies.Write("azure1.body", store);
                azure1.ColorImage.EncodeJpeg(quality: 75).Write("azure1.color", store);
                azure1.DepthImage.EncodePng().Write("azure1.depth", store);
                azure1.DepthDeviceCalibrationInfo.Write("azure1.depth-Calibration", store);
                azure1.AzureKinectSensorCalibration.Write("azure1.calibration", store);
                azure1.InfraredImage.EncodePng().Write("azure1.infrared", store);

                // start azure1 - main
                var azure2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    WiredSyncMode = WiredSyncMode.Subordinate,
                    Exposure = TimeSpan.FromTicks(100000),
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                        TemporalSmoothing = 0.1f,
                    },
                    DeviceIndex = 0,
                    SynchronizedImagesOnly = true,
                });

                azure2.Bodies.Write("azure2.body", store);
                azure2.ColorImage.EncodeJpeg(quality: 75).Write("azure2.color", store);
                azure2.DepthImage.EncodePng().Write("azure2.depth", store);
                azure2.DepthDeviceCalibrationInfo.Write("azure2.depth-Calibration", store);
                azure2.AzureKinectSensorCalibration.Write("azure2.calibration", store);
                azure2.InfraredImage.EncodePng().Write("azure2.infrared", store);

                p.RunAsync();
                Console.WriteLine("Press to End");
                Console.ReadLine();
            }
        }
    }
}
