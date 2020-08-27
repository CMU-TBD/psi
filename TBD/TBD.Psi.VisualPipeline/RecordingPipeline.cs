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
    public class RecordingPipeline
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var store = Store.Create(p, "record", @"C:\Data\Stores");
                var azureKinect = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        TemporalSmoothing = 0.1f,
                    },
                    OutputColor = true,
                    OutputDepth = true,
                    OutputInfrared = true,
                    OutputCalibration = true,
                });

                azureKinect.Bodies.Write("body", store);
                azureKinect.ColorImage.EncodeJpeg().Write("color", store);
                azureKinect.DepthImage.EncodePng().Write("depth", store);
                azureKinect.DepthDeviceCalibrationInfo.Write("depth-Calibration", store);
                azureKinect.AzureKinectSensorCalibration.Write("calibration", store);
                azureKinect.InfraredImage.EncodePng().Write("infrared", store);

                p.RunAsync();
                Console.WriteLine("Press to End");
                Console.ReadLine();
            }
        }
    }
}
