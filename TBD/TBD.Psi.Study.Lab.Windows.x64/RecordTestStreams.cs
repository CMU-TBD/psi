namespace TBD.Psi.Study.Lab
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Imaging;

    public class RecordTestStreams
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var store = PsiStore.Create(p, "calibration-recording", @"C:\Data\Lab-Store\recordings");

                // general settings
                var azureKinectNum = 1;
                var kinect2Num = 1;
                var mainNum = -1;
                var recordBodies = false;

                for (var i = 1; i <= azureKinectNum; i++)
                {
                    var configuration = new AzureKinectSensorConfiguration()
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputCalibration = true,
                        DeviceIndex = i - 1,
                        Exposure = TimeSpan.FromMilliseconds(10),
                        WiredSyncMode = mainNum > 0 ? (mainNum == (i - 1) ? Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Master : Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Subordinate) : Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Standalone,
                    };

                    if (recordBodies)
                    {
                        configuration.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                        {
                            CpuOnlyMode = false,
                            TemporalSmoothing = 0.0f,
                        };
                    }

                    var k4a = new AzureKinectSensor(p, configuration);

                    //k4a.ColorImage.EncodeJpeg(quality: 80).Write($"azure{(mainNum != i ? i : 0)}.color", store);
                    k4a.DepthDeviceCalibrationInfo.Write($"azure{(mainNum != i ? i : 0)}.depth-calibration", store);
                    //k4a.Bodies.Write($"azure{(mainNum != i ? i : 0)}.bodies", store);
                    //k4a.DepthImage.EncodePng().Write($"azure{(mainNum != i ? i : 0)}.depth", store);
                }

                for (var i = 1; i <= kinect2Num; i++)
                {
                    var k2 = new KinectSensor(p, new KinectSensorConfiguration() 
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputCalibration = true,
                        OutputBodies = recordBodies
                    });

                    //k2.ColorImage.EncodeJpeg(quality: 80).Write($"k2d{i}.color", store);
                    k2.DepthDeviceCalibrationInfo.Write($"k2d{i}.depth-calibration", store);
                    //k2.Bodies.Write($"k2d{i}.bodies", store);
                    //k2.DepthImage.EncodePng().Write($"k2d{i}.depth", store);
                    k2.DepthDeviceCalibrationInfo.Do(s => Console.WriteLine($"Receive Calibration:{s}"));
                }

                p.Diagnostics.Write("diagnostics", store);
                p.RunAsync();
                Console.WriteLine("Press to End");
                Console.ReadLine();
                Console.WriteLine("Ending ...");
            }
        }
    }
}
