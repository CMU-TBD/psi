// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisualPipeline
{
    using System;
    using System.Collections.Generic;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.VisionComponents;

    /// <summary>
    /// Data Collection Pipeline
    /// </summary>
    public class CollectionPipeline
    {
        /// <summary>
        /// Entry point to Pipeline.
        /// </summary>
        public static void Run()
        {
            using (var pipeline = Pipeline.Create(enableDiagnostics: true))
            {
                var store = PsiStore.Create(pipeline, "collection-02", @"C:\Data\Stores");

                // Create all the coordinate frames.
                var world = new CoordinateSystem();
                var worldToAzure = new CoordinateSystem(new Point3D(0, 0, 1.5875), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis);

                // var AzureToKinect2 = Utils.CreateCoordinateSystemFrom(0.15f, -0.7f, 0.3f, Convert.ToSingle(MathNet.Spatial.Units.Angle.FromDegrees(45).Radians), 0f, 0f);
                double[,] t =
                {
                    { 0.8078703900378181, -0.5891752140483332, -0.013780285992902435, 0.032736135237783445 },
                    { 0.5892683041286023, 0.8079544527659226, 0.0011840202089164265, -1.6603675898139039 },
                    { 0.010406663708646178, -0.0090647342387594, 0.9999043463322372, -0.3337323335749119 },
                    { 0.0, 0.0, 0.0, 1.0 },
                };

                var azure1ToAzure2 = new CoordinateSystem(Matrix<double>.Build.DenseOfArray(t));
                var worldToAzure2 = new CoordinateSystem(worldToAzure * azure1ToAzure2);

                // Repeat the frames to the store.
                Generators.Repeat(pipeline, new List<CoordinateSystem> { world, worldToAzure, worldToAzure2 }, TimeSpan.FromSeconds(1)).Write("frames", store);

                // Audio recording from main device
/*                var audioSource = new AudioCapture(pipeline, new AudioCaptureConfiguration()
                {
                    OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm(),
                });
                audioSource.Write("audio", store);*/

                var azure1 = new AzureKinectSensor(pipeline, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    WiredSyncMode = WiredSyncMode.Master,
                    Exposure = TimeSpan.FromTicks(100000),
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                        TemporalSmoothing = 0.2f,
                    },
                    DeviceIndex = 0,
                    SynchronizedImagesOnly = true,
                });

                var azure1BodiesInWorld = azure1.Bodies.ChangeToFrame(worldToAzure);
                azure1BodiesInWorld.Write("azure1.bodies", store);
                azure1.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("azure1.color", store);

                var azure2 = new AzureKinectSensor(pipeline, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    WiredSyncMode = WiredSyncMode.Subordinate,
                    Exposure = TimeSpan.FromTicks(100000),
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                        TemporalSmoothing = 0.2f,
                    },
                    DeviceIndex = 1,
                    SynchronizedImagesOnly = true,
                });

                var azure2BodiesInWorld = azure2.Bodies.ChangeToFrame(worldToAzure2);
                azure2BodiesInWorld.Write("azure2.bodies", store);
                azure2.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("azure2.color", store);

                // Merge the who streams
                var merger = new BodyMerger(pipeline, azure1BodiesInWorld.ChangeToHumanBodies());
                merger.AddHumanBodyStream(azure2BodiesInWorld.ChangeToHumanBodies());

                // Tracking of Bodies across time.
                var tracker = new BodyTracker(pipeline);
                merger.PipeTo(tracker);
                tracker.Out.Write("TrackedBodies", store);

                /*                var rosPub = new ROSWorldSender(pipeline);
                                tracker.PipeTo(rosPub);*/

                // For testing, convert the first body into a single coordinate system.
                /*                merger.Process<List<List<AzureKinectBody>>, CoordinateSystem>((m, e, o) =>
                                {
                                    if (m.Count > 0)
                                    {
                                        o.Post(m[0][0].Joints[JointId.Neck].Pose, e.OriginatingTime);
                                    }
                                }).Write("Merged", store);*/

                pipeline.Diagnostics.Write("diagnostics", store);
                pipeline.RunAsync();

                Console.WriteLine("Press Enter to stop...");
                Console.ReadLine();
            }
        }
    }
}
