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
    using Microsoft.Psi.Kinect;
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
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var store = PsiStore.Create(p, "cali", @"C:\Data\StoreFolders\Calibration");

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    WiredSyncMode = WiredSyncMode.Standalone,
                    OutputInfrared = true,
                    Exposure = TimeSpan.FromTicks(100000),
                    DeviceIndex = 1,
                });

                var boardMarkerSize = 0.056f;
                var boardMarkerDist = 0.01f;

                // create a calibration tool
                var calibrationMerger = new CalibrationMerger(p, store, 4, 6, boardMarkerSize, boardMarkerDist, "h");

                calibrationMerger.AddSensor(k4a1, "k4a1");

                var k4a2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    WiredSyncMode = WiredSyncMode.Standalone,
                    OutputInfrared = true,
                    Exposure = TimeSpan.FromTicks(100000),
                    DeviceIndex = 0,
                });

                calibrationMerger.AddSensor(k4a2, "k4a2");

                var k4a3 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    WiredSyncMode = WiredSyncMode.Standalone,
                    OutputInfrared = true,
                    Exposure = TimeSpan.FromTicks(100000),
                    DeviceIndex = 2,
                });

                calibrationMerger.AddSensor(k4a3, "k4a3");

                var k21 = new KinectSensor(p, new KinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputCalibration = true,
                });

                calibrationMerger.AddSensor(k21, "k21");

                calibrationMerger.Write("result", store);

                calibrationMerger.Do(m =>
                {
                    var (pose1, n1, pose2, n2) = m;
                    csv.WriteField(pose1.Values);
                    csv.WriteField(n1);
                    csv.NextRecord();
                    csv.WriteField(pose2.Values);
                    csv.WriteField(n2);
                    csv.NextRecord();
                });

                p.Diagnostics.Write("diagnostics", store);
                p.RunAsync();
                Console.WriteLine("Press Enter to stop...");
                Console.ReadLine();
            }
        }
    }
}
