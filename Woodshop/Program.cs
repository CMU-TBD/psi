using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
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
                var k4a = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputDepth = true,
                    OutputColor = true,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false
                    }
                });

                k4a.Bodies.Do(m => { });
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
