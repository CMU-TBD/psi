using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Azure.Kinect;
using Microsoft.Psi.AzureKinect;
namespace Woodshop
{
    class Program
    {
        static void Main(string[] args)
        {
            using(var p = Pipeline.Create())
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
    }
}
