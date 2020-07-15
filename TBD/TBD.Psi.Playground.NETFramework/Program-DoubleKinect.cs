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

            using (var p = Pipeline.Create(true))
            {
                var store = Store.Create(p, "test", @"C:\Data\Stores");

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    DeviceIndex = 0,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                    }    
                });

                var k4a2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    DeviceIndex = 1,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                    }
                });



                k4a1.Bodies.Write("b1", store);
                k4a1.ColorImage.EncodeJpeg().Write("img1", store);
                k4a2.ColorImage.EncodeJpeg().Write("img2", store);
                k4a2.Bodies.Write("b2", store);

                p.RunAsync();
                Console.ReadLine();
            }
        }

        private static void k4aErrorMsg(LogMessage obj)
        {
            if (obj.LogLevel != LogLevel.Trace)
            {
                Console.WriteLine(obj);
            }
        }
    }
}
