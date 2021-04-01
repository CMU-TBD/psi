using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Imaging;
using Microsoft.Azure.Kinect.Sensor;

namespace Woodshop
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start AzureKinect Loggers
            Logger.Initialize();
            Logger.LogMessage += Program.k4aErrorMsg;

            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Create(p, "body", @"C:\Data\body-test");
                var k4a = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputDepth = true,
                    OutputColor = true,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        LiteNetwork = true,
                    }
                });

                k4a.Bodies.Write("body", store);
                k4a.ColorImage.EncodeJpeg(50).Write("color", store);
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
