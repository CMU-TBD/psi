using Microsoft.Psi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Playground.NETFramework
{
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;

    public class CalibrationRecording
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Create(p, "calibration-recording", @"C:\Data\Store\calibration-recording");

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    OutputCalibration = true,
                    DeviceIndex = 1,
                    Exposure = TimeSpan.FromMilliseconds(10),
                    WiredSyncMode = Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Master,
                });

                var k4a2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    OutputCalibration = true,
                    DeviceIndex = 0,
                    Exposure = TimeSpan.FromMilliseconds(10),
                    WiredSyncMode = Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Subordinate,
                });

/*                var k4a3 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    OutputCalibration = true,
                    DeviceIndex = 1,
                    Exposure = TimeSpan.FromMilliseconds(10),
                    WiredSyncMode = Microsoft.Azure.Kinect.Sensor.WiredSyncMode.Subordinate,
                });*/

                k4a1.ColorImage.EncodeJpeg(quality: 80).Write("azure1.color", store);
                k4a1.DepthImage.EncodePng().Write("azure1.depth", store);
                k4a1.DepthDeviceCalibrationInfo.Write("azure1.depth-Calibration", store);
                k4a1.AzureKinectSensorCalibration.Write("azure1.calibration", store);
                k4a1.InfraredImage.EncodePng().Write("azure1.infrared", store);

                k4a2.ColorImage.EncodeJpeg(quality: 80).Write("azure2.color", store);
                k4a2.DepthImage.EncodePng().Write("azure2.depth", store);
                k4a2.DepthDeviceCalibrationInfo.Write("azure2.depth-Calibration", store);
                k4a2.AzureKinectSensorCalibration.Write("azure2.calibration", store);
                k4a2.InfraredImage.EncodePng().Write("azure2.infrared", store);

/*                k4a3.ColorImage.EncodeJpeg(quality: 80).Write("azure3.color", store);
                k4a3.DepthImage.EncodePng().Write("azure3.depth", store);
                k4a3.DepthDeviceCalibrationInfo.Write("azure3.depth-Calibration", store);
                k4a3.AzureKinectSensorCalibration.Write("azure3.calibration", store);
                k4a3.InfraredImage.EncodePng().Write("azure3.infrared", store);*/

                p.RunAsync();
                Console.ReadLine();
            }
        }
    }
}
