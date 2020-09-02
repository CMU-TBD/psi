// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisualPipeline
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using CsvHelper;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.VisualPipeline.Components;

    /// <summary>
    /// Program to run calibration tools.
    /// </summary>
    internal class CalibrationTool
    {
        /// <summary>
        /// Main function of tool.
        /// </summary>
        public static void Run()
        {
            using (var writer = new StreamWriter(@"C:\Data\pose2.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            using (var p = Pipeline.Create(true))
            {
                var store = Store.Create(p, "test", @"C:\Data\Stores");

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    WiredSyncMode = WiredSyncMode.Master,
                    OutputInfrared = true,
                    Exposure = TimeSpan.FromTicks(100000),
                    DeviceIndex = 1,
                });

                var boardMarkerSize = 0.056f;
                var boardMarkerDist = 0.01f;



                var k4a1BoardDetector = new BoardDetector(p, 4, 6, boardMarkerSize, boardMarkerDist, "h");
                var k4a1Gray = k4a1.ColorImage.ToGray();
                k4a1Gray.EncodeJpeg().Write("img1", store);
                k4a1Gray.PipeTo(k4a1BoardDetector.ImageIn, DeliveryPolicy.LatestMessage);
                k4a1.DepthDeviceCalibrationInfo.PipeTo(k4a1BoardDetector.CalibrationIn);

                k4a1BoardDetector.Write("board1pose", store);

                var k4a2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    WiredSyncMode = WiredSyncMode.Subordinate,
                    OutputInfrared = true,
                    Exposure = TimeSpan.FromTicks(100000),
                    DeviceIndex = 0,
                });

                var k4a2BoardDetector = new BoardDetector(p, 4, 6, boardMarkerSize, boardMarkerDist, "h");
                var k4a2Gray = k4a2.ColorImage.ToGray();
                k4a2Gray.EncodeJpeg().Write("img2", store);
                k4a2Gray.PipeTo(k4a2BoardDetector.ImageIn, DeliveryPolicy.LatestMessage);
                k4a2.DepthDeviceCalibrationInfo.PipeTo(k4a2BoardDetector.CalibrationIn);

                k4a2BoardDetector.Write("board2pose", store);

                var joiner = k4a1BoardDetector.Join(k4a2BoardDetector, TimeSpan.FromMilliseconds(50));
                joiner.Do(m =>
                {
                    var (pose1, pose2) = m;
                    csv.WriteField(pose1.Values);
                    csv.NextRecord();
                    csv.WriteField(pose2.Values);
                    csv.NextRecord();
                    Console.WriteLine("one record");
                });
/*                joiner.Select(m =>
                {
                    var (pose1, pose2) = m;
                    var p2Inv = pose2.Invert();
                    var cs = new CoordinateSystem(pose1 * p2Inv);
                    Console.WriteLine(cs);
                    return cs;
                }).Write("solution", store);*/

                p.Diagnostics.Write("diagnostics", store);
                p.RunAsync();
                Console.WriteLine("Press Enter to stop...");
                Console.ReadLine();
            }
        }
    }
}
