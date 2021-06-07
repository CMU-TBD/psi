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
                var store = PsiStore.Create(p, "state", @"E:\Data\playground");
                var rosListner = new ROSStudyListener(p, "ws://172.17.86.94:9090");
                var stateTracker = new StateTracker(p);
                rosListner.AddStringListener("/study/state").PipeTo(stateTracker);
                stateTracker.Write("state", store);
                p.RunAsync();
                Console.ReadLine();
            }
        }
    }
}
