
namespace TBD.Psi.Study
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.StudyComponents;
    using TBD.Psi.TransformationTree;
    using TBD.Psi.RosSharpBridge;
    using MathNet.Numerics.LinearAlgebra;

    public class Sandbox2
    {
        public static void Run()
        {
            var frameName = "mainCam";
            var deviceId = 1;

            var lines = new List<string>();
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var transformationSettingPath = Path.Combine(Constants.ResourceLocations, $"transformations-{Constants.StudyType}-{Constants.PartitionIdentifier}.json");
                var transformationTree = new TransformationTreeComponent(p, 1000, transformationSettingPath);
                var outputStorePath = Path.Combine(Constants.OperatingDirectory, Constants.PartitionIdentifier, "sandbox");
                var outputStore = PsiStore.Create(p, "sandbox", Path.Combine(Constants.OperatingDirectory, outputStorePath));

                var boardDetector = new BoardDetector(p, 2, 2, 0.063f, 0.0095f, OpenCV.ArucoDictionary.DICT_4X4_50, 20);

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    DeviceIndex = deviceId,
                    OutputColor = true,
                    OutputCalibration = true,
                    OutputDepth = true,
                    CameraFPS = Microsoft.Azure.Kinect.Sensor.FPS.FPS15,
                    Exposure = TimeSpan.FromMilliseconds(10),
                    ColorResolution = Microsoft.Azure.Kinect.Sensor.ColorResolution.R3072p
                });
                k4a1.ColorImage.ToGray().PipeTo(boardDetector.ImageIn);
                k4a1.ColorImage.ToGray().EncodeJpeg().Write("img", outputStore);
                k4a1.DepthImage.EncodePng().Write("depth", outputStore);
                k4a1.DepthDeviceCalibrationInfo.PipeTo(boardDetector.CalibrationIn);

                boardDetector.Out.Write("position", outputStore);
                boardDetector.Select(m => 
                {
                    return m.TransformBy(transformationTree.QueryTransformation("world", frameName));
                }).Do(m =>

                {
                    lines.Add(String.Join(",", m.Storage.ToRowMajorArray()));
                });
                boardDetector.DebugImageOut.EncodeJpeg().Write("debugImg", outputStore);

                p.RunAsync();
                Console.ReadLine();

                /*                var numOfPoints = 0;

                                tagDetector.Select(m =>
                                {
                                    return m.Select(x => (x.Item1, x.Item2.TransformBy(transformationTree.QueryTransformation("world", frameName)))).ToList();
                                }).Process<List<(int, CoordinateSystem)>, CoordinateSystem>( (m,e,o) =>
                                {
                                    if (m.Count > 0)
                                    {
                                        lines.Add(String.Join(",", m[0].Item2.Storage.ToRowMajorArray()));
                                        Console.WriteLine(m[0].Item2);
                                        o.Post(m[0].Item2, e.OriginatingTime);
                                        numOfPoints++;
                                    }

                                });
                                p.RunAsync();

                                while (numOfPoints < 100)
                                {
                                    Thread.Sleep(100);
                                }*/
            }
            // generate file path
            var csvPath = Path.Combine(Constants.ResourceLocations, $"{frameName}-{Constants.StudyType}-{Constants.PartitionIdentifier}-{deviceId}.csv");
            System.IO.File.WriteAllLines(csvPath, lines);
        }
    }
}

