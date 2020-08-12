// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisualPipeline
{
    using System;
    using System.Collections.Generic;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.VisualPipeline.Components;

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
            using (var pipeline = Pipeline.Create(true))
            {
                var store = Store.Create(pipeline, "test", @"C:\Data\Stores");

                // Create all the coordinate frames.
                var world = new CoordinateSystem();
                var worldToAzure = new CoordinateSystem(new Point3D(0, 0, 1), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis);

                // var AzureToKinect2 = Utils.CreateCoordinateSystemFrom(0.15f, -0.7f, 0.3f, Convert.ToSingle(MathNet.Spatial.Units.Angle.FromDegrees(45).Radians), 0f, 0f);
                double[,] t =
                {
                    { 0.3913, 0.8957, 0.210, 0.391 },
                    { -0.864, 0.4364, -0.24992, 1.95 },
                    { -0.315, -0.084, 0.945, 0.704 },
                    { 0, 0, 0, 1 },
                };
                var azure1ToAzure2 = new CoordinateSystem(Matrix<double>.Build.DenseOfArray(t));
                var worldToAzure2 = new CoordinateSystem(worldToAzure * azure1ToAzure2);

                // Repeat the frames to the store.
                Generators.Repeat(pipeline, new List<CoordinateSystem> { world, worldToAzure, worldToAzure2 }, TimeSpan.FromSeconds(1)).Write("frames", store);

                var azure1 = new AzureKinectSensor(pipeline, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                        TemporalSmoothing = 0.25f,
                    },
                    DeviceIndex = 1,
                });

                var azure1BodiesInWorld = azure1.Bodies.ChangeToFrame(worldToAzure);
                azure1BodiesInWorld.Write("azure1.bodies", store);
                azure1.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("azure1.color", store);

                var azure2 = new AzureKinectSensor(pipeline, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                        TemporalSmoothing = 0.25f,
                    },
                    DeviceIndex = 0,
                });

                var azure2BodiesInWorld = azure2.Bodies.ChangeToFrame(worldToAzure2);
                azure2BodiesInWorld.Write("azure2.bodies", store);
                azure2.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("azure2.color", store);

                // Merge the who streams
                var merger = new BodyMerger(pipeline, azure1BodiesInWorld);
                merger.AddAzureKinectBodyStream(azure2BodiesInWorld);

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
