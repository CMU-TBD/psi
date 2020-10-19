namespace TBD.Psi.Playground
{
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using System;

    public class SingleKinect
    {
        public static void Run(string[] args)
        {

            // Start AzureKinect Loggers
            Logger.Initialize();
            Logger.LogMessage += SingleKinect.k4aErrorMsg;
            int deviceIndex = 1;

            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var store = PsiStore.Create(p, "single-cam-body", @"C:\Data\Stores\SingleCam");

                var k4a = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    DeviceIndex = deviceIndex,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        TemporalSmoothing = 0.0f
                    },
                });
                
                k4a.ColorImage.EncodeJpeg(quality: 50).Write("color", store);
                k4a.DepthImage.EncodePng().Write("depth", store);
                k4a.DepthDeviceCalibrationInfo.Write("depth-Calibration", store);
                k4a.AzureKinectSensorCalibration.Write("calibration", store);
                k4a.InfraredImage.EncodePng().Write("infrared", store);
                k4a.Bodies.Write("body", store);

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
