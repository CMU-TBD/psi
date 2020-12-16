using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.VisualPipeline
{
    public class CalibrationRecording
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Create(p, "calibration-recording", @"C:\Data\Store\calibration-recording");

                var kinectNum = 2;
                var mainNum = -1;

                for (var i = 1; i <= kinectNum; i++)
                {
                    var k4a = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputCalibration = true,
                        DeviceIndex = i - 1,
                        Exposure = TimeSpan.FromMilliseconds(10),
                        WiredSyncMode = mainNum > 0 ?(mainNum == (i - 1) ? Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Master : Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Subordinate) : Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Standalone,
                    });

                    k4a.ColorImage.EncodeJpeg(quality: 80).Write($"azure{(mainNum != i ? i : 0)}.color", store);
                    k4a.DepthDeviceCalibrationInfo.Write($"azure{(mainNum != i ? i : 0)}.depth-Calibration", store);
                }

                p.RunAsync();
                Console.ReadLine();
            }
        }
    }
}
