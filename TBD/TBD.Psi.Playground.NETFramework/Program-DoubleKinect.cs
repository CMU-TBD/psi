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
                var store = PsiStore.Create(p, "test", @"C:\Data\Stores\KinectTest");

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    DeviceIndex = 0,
                });
                /*
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
                */



                //k4a1.Bodies.Write("b1", store);
                k4a1.ColorImage.EncodeJpeg().Write("azure1.color", store);
                // k4a2.ColorImage.EncodeJpeg().Write("azure2.color", store);
                //k4a2.Bodies.Write("b2", store);

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
