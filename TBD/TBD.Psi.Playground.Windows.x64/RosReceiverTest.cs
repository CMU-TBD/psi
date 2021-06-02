using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using RosSharp.Urdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBD.Psi.StudyComponents;

namespace TBD.Psi.Playground.Windows.x64
{
    public class RosReceiverTest
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var rosListner = new ROSStudyListener(p, "ws://172.17.86.92:9090");
                rosListner.AddCSListener("pose").Do(m =>
                {
                    Console.WriteLine("Receive");
                });
                p.RunAsync();
                Console.ReadLine();
            }
        }
    }
}
