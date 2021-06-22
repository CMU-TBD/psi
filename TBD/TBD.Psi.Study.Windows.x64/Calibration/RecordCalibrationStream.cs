namespace TBD.Psi.Study.Calibration
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.StudyComponents;

    public class RecordCalibrationStream
    {
        public static void Run()
        {
            // Create a calibration stream
            var datasetIdentifier = Constants.CalibrationDatasetIdentifier;
            var datasetRootPath = Path.Combine(Constants.RootPath, "calibration", datasetIdentifier);
            var dataset = new Dataset(datasetIdentifier, Path.Combine(datasetRootPath, "calibration.pds"), autoSave: true);
            var storePath = "";
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                //create the recording store
                var store = PsiStore.Create(p,"calibration-recording", datasetRootPath);
                storePath = store.Path;
                // general settings
                var azureKinectNum = 3;
                var kinect2Num = 0;

                // Validation components
                var bodyStreamValidator = new StreamValidator(p);

                for (var i = 1; i <= azureKinectNum; i++)
                {
                    var configuration = new AzureKinectSensorConfiguration()
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputCalibration = true,
                        OutputImu = true,
                        DeviceIndex = i - 1,
                        Exposure = TimeSpan.FromMilliseconds(12),
                        ColorResolution = ColorResolution.R1440p,
                        WiredSyncMode = Constants.SensorSyncMode[i]
                    };

                    var k4a = new AzureKinectSensor(p, configuration);
                    var deviceName = $"azure{i}";
                    k4a.ColorImage.EncodeJpeg(quality: Constants.JPEGEncodeQuality).Write($"{deviceName}.color", store);
                    k4a.DepthDeviceCalibrationInfo.Write($"{deviceName}.depth-calibration", store);
                    k4a.DepthImage.EncodePng().Write($"{deviceName}.depth", store);
                }

                for (var i = 1; i <= kinect2Num; i++)
                {
                    var k2 = new KinectSensor(p, new KinectSensorConfiguration() 
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputCalibration = true,
                    });
                    var deviceName = $"k2d{i}";
                    k2.ColorImage.EncodeJpeg(quality: Constants.JPEGEncodeQuality).Write($"{deviceName}.color", store);
                    k2.DepthDeviceCalibrationInfo.Write($"{deviceName}.depth-calibration", store);                    
                    k2.DepthImage.EncodePng().Write($"{deviceName}.depth", store);
                }

                p.Diagnostics.Write("diagnostics", store);
                // Run for 30 seconds
                p.RunAsync();
                p.WaitAll(30000); 
                Console.WriteLine("Ending ...");
            }
            // add it to 
            dataset.AddSessionFromPsiStore("calibration-recording", storePath, datasetIdentifier, "recording");
        }
    }
}
