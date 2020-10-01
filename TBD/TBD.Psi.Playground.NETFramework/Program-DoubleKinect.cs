namespace TBD.Psi.Playground
{
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using System;

    public class DoubleKinect
    {
        public static void Run(string[] args)
        {

            // Start AzureKinect Loggers
            Logger.Initialize();
            Logger.LogMessage += DoubleKinect.k4aErrorMsg;

            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var store = PsiStore.Create(p, "multi-cam-body", @"C:\Data\Stores\MultiCamera");

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    DeviceIndex = 0,
                    WiredSyncMode = WiredSyncMode.Subordinate,
                                        BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                                        {
                                            CpuOnlyMode = false,
                                            TemporalSmoothing = 0.2f,
                                        }
                });
                
                var k4a2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    DeviceIndex = 1,
                    WiredSyncMode = WiredSyncMode.Subordinate,
                                        BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                                        {
                                            CpuOnlyMode = false,
                                            TemporalSmoothing = 0.2f,
                                        }
                });

                var k4a3 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    DeviceIndex = 2,
                    WiredSyncMode = WiredSyncMode.Master,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                        TemporalSmoothing = 0.2f,
                    }
                });;


                //k4a1.Bodies.Write("b1", store);
                k4a1.ColorImage.EncodeJpeg().Write("azure1.color", store);
                k4a1.DepthImage.EncodePng().Write("azure1.depth", store);
                k4a1.DepthDeviceCalibrationInfo.Write("azure1.depth-Calibration", store);
                k4a1.AzureKinectSensorCalibration.Write("azure1.calibration", store);
                k4a1.InfraredImage.EncodePng().Write("azure1.infrared", store);
                k4a1.Bodies.Write("azure1.body", store);

                k4a2.ColorImage.EncodeJpeg().Write("azure2.color", store);
                k4a2.DepthImage.EncodePng().Write("azure2.depth", store);
                k4a2.DepthDeviceCalibrationInfo.Write("azure2.depth-Calibration", store);
                k4a2.AzureKinectSensorCalibration.Write("azure2.calibration", store);
                k4a2.InfraredImage.EncodePng().Write("azure2.infrared", store);
                k4a2.Bodies.Write("azure2.body", store);

                k4a3.ColorImage.EncodeJpeg().Write("azure3.color", store);
                k4a3.DepthImage.EncodePng().Write("azure3.depth", store);
                k4a3.DepthDeviceCalibrationInfo.Write("azure3.depth-Calibration", store);
                k4a3.AzureKinectSensorCalibration.Write("azure3.calibration", store);
                k4a3.InfraredImage.EncodePng().Write("azure3.infrared", store);
                k4a3.Bodies.Write("azure3.body", store);
                //k4a2.Bodies.Write("b2", store);

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
