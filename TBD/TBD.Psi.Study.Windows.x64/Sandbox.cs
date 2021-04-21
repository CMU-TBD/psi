
namespace TBD.Psi.Study
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.StudyComponents;

    public class Sandbox
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var tagDetector = new TagDetector(p, 0.045f, OpenCV.ArucoDictionary.DICT_APRILTAG_16h5);
                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    DeviceIndex = 2,
                    OutputColor = true
                });
                k4a1.ColorImage.ToGray().PipeTo(tagDetector.ImageIn);
                k4a1.DepthDeviceCalibrationInfo.PipeTo(tagDetector.CalibrationIn);
                tagDetector.Do(m =>
                {
                    foreach (var n in m)
                    {
                        Console.WriteLine(n.Item2);
                    }
                });
                p.RunAsync();
                Console.ReadLine();
            }
        }
    }
}
