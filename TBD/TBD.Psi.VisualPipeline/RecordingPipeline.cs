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
                var store = PsiStore.Create(p, "record-pipeline", @"C:\Data\Store\Recording");

                var kinectNum = 3;
                var mainNum = 2;

                for (var i = 1; i <= kinectNum; i++)
                {
                    var k4a = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputCalibration = true,
                        DeviceIndex = i - 1,
                        Exposure = TimeSpan.FromMilliseconds(10),
                        WiredSyncMode = mainNum == (i - 1) ? Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Master : Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Subordinate,
                        BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                        {
                            CpuOnlyMode = false,
                            TemporalSmoothing = 0.0f,
                        },
                    });

                    k4a.ColorImage.EncodeJpeg(quality: 80).Write($"azure{(mainNum != i ? i : 0)}.color", store);
                    k4a.DepthDeviceCalibrationInfo.Write($"azure{(mainNum != i ? i : 0)}.depth-Calibration", store);
                    k4a.Bodies.Write($"azure{(mainNum != i ? i : 0)}.bodies", store);
                    k4a.InfraredImage.EncodePng().Write($"azure{(mainNum != i ? i : 0)}.infrared", store);
                }

                p.RunAsync();
                Console.WriteLine("Press to End");
                Console.ReadLine();
                Console.WriteLine("Here");

            }
        }
    }
}
