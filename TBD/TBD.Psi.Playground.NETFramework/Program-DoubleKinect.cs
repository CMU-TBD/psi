namespace TBD.Psi.Playground
{
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using System;
    using TBD.Psi.Imaging.Windows;

    public class DoubleKinect
    {
        public static void Run(string[] args)
        {

            // Start AzureKinect Loggers
            Logger.Initialize();
            Logger.LogMessage += DoubleKinect.k4aErrorMsg;

            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var store = PsiStore.Create(p, "multi-cam-test", @"C:\Data\Store\MultiCamera");

                var kinectNum = 1;

                for(var i = 0; i < kinectNum; i++)
                {
                    var k4a = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        DeviceIndex = i,
                        //Gain = 0,
                        //Exposure = TimeSpan.FromMilliseconds(15),
                        WiredSyncMode = WiredSyncMode.Standalone,
                        BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                        {
                            CpuOnlyMode = false,
                            TemporalSmoothing = 0.0f,
                        }
                    });

                    k4a.ColorImage.EncodeJpeg(quality: 100).Write($"azure{i + 1}.color", store);
                    k4a.ColorImage.Write($"azure{i + 1}.colorOri", store);
                    k4a.ColorImage.EncodeJpegTurbo(quality: 50).Write($"azure{i + 1}.colorTurbo", store);
                    k4a.DepthDeviceCalibrationInfo.Write($"azure{i+1}.depth-Calibration", store);
                    k4a.DepthImage.EncodePng().Write($"azure{i+1}.depth", store);
                    k4a.Bodies.Write($"azure{i + 1}.bodies", store);
                }
           
                p.Diagnostics.Write("diagnostic", store);

                p.RunAsync();
                Console.ReadLine();
            }
        }

        private static void k4aErrorMsg(LogMessage obj)
        {
            
                Console.WriteLine(obj);
            
        }
    }
}
