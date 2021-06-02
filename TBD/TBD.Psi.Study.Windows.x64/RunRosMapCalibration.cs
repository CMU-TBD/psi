

namespace TBD.Psi.Study
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.StudyComponents;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi.AzureKinect;
    using TBD.Psi.TransformationTree;

    public class RunRosMapCalibration
    {
        public static void Run()
        {
            var matched = new List<string>();

            using (var p = Pipeline.Create(enableDiagnostics: true))
            {

                // baxter face board information
                var podiMarkerSize = 0.063f;
                var podiMarkerDist = 0.0095f;
                var podiXNum = 2;
                var podiYNum = 2;
                var podiDict = TBD.Psi.OpenCV.ArucoDictionary.DICT_4X4_50;
                var podiFirstMaker = 20;
                // baxter tag information
                var baxterInViewDeviceName = 1;
                
                var deliveryPolicy = DeliveryPolicy.SynchronousOrThrottle;

                // Generate the store path
                var outputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, @"podi-board");
                var transformationSettingPath = Path.Combine(Constants.ResourceLocations, $"transformations-{Constants.StudyType}-{Constants.PartitionIdentifier}.json");
                var transformationTree = new TransformationTreeComponent(p, 1000, transformationSettingPath);
                var azureToPsiWorld = transformationTree.QueryTransformation("world", "mainCam");

                // Stores
                var outputStore = PsiStore.Create(p, "podi-board", outputStorePath);

                // components
                // Start the Azure Kinect
                var azure = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    OutputCalibration = true,
                    OutputImu = true,
                    DeviceIndex = baxterInViewDeviceName,
                    Exposure = TimeSpan.FromMilliseconds(10),
                    ColorResolution = ColorResolution.R1440p,
                    WiredSyncMode = WiredSyncMode.Standalone
                });

                var podiBoardDetector = new BoardDetector(p, podiXNum, podiYNum, podiMarkerSize, podiMarkerDist, podiDict, podiFirstMaker);
                var rosListner = new ROSStudyListener(p, "ws://192.168.0.152:9090");
                var podiPose = rosListner.AddCSListener("/psi/marker_in_map");
                azure.DepthDeviceCalibrationInfo.PipeTo(podiBoardDetector.CalibrationIn);
                azure.ColorImage.ToGray().PipeTo(podiBoardDetector.ImageIn);
                podiBoardDetector.DebugImageOut.EncodeJpeg(quality:50).Write("debug", outputStore);
                podiBoardDetector.Out.Write("psi", outputStore);
                podiPose.Write("podi", outputStore);

                podiBoardDetector.Out.Join(podiPose, TimeSpan.FromSeconds(0.03)).Do(m =>
                {
                    Console.WriteLine("matched!");
                    matched.Add(String.Join(",", m.Item1.TransformBy(azureToPsiWorld).Storage.ToRowMajorArray()));
                    matched.Add(String.Join(",", m.Item2.Storage.ToRowMajorArray()));
                });

                p.Diagnostics.Write("diagnostics", outputStore);
                p.RunAsync();
                Console.ReadLine();

                // generate file path
                var mapPath = Path.Combine(Constants.ResourceLocations, $"ros-map-{Constants.StudyType}-{Constants.PartitionIdentifier}.csv");
                File.WriteAllLines(mapPath, matched);
            }
        }
    }
}
