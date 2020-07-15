namespace TBD.Psi.VisualPipeline
{
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Imaging;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Kinect;
    using MathNet.Numerics;
    using System.Numerics;
    using System.Linq;
    using Microsoft.Psi.Calibration;
    using System.Collections.Generic;
    using System;
    using Microsoft.Psi.Components;
    using Microsoft.Azure.Kinect.BodyTracking;
    using TBD.Psi.VisualPipeline.Components;

    public class CollectionPipeline
    {
        public static void Run()
        {
            using (var pipeline = Pipeline.Create(true))
            {
                var store = Store.Create(pipeline, "test", @"C:\Data\Stores");

                // Create all the coordinate frames.
                var World = new CoordinateSystem();
                var WorldToAzure = new CoordinateSystem(new Point3D(0, 0, 1), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis);
                var AzureToKinect2 = Utils.CreateCoordinateSystemFrom(0.15f, -0.7f, 0.3f, Convert.ToSingle(MathNet.Spatial.Units.Angle.FromDegrees(45).Radians), 0f, 0f);
                var WorldToKinect2 = new CoordinateSystem(WorldToAzure * AzureToKinect2);

                // Repeat the frames to the store.
                Generators.Repeat(pipeline, new List<CoordinateSystem> { World, WorldToAzure, WorldToKinect2 }, TimeSpan.FromSeconds(1)).Write("frames", store);

                var kinect = new Microsoft.Psi.Kinect.KinectSensor(pipeline, new KinectSensorConfiguration()
                {
                    OutputBodies = true,
                    OutputColor = true,
                    OutputDepth = true,
                });
                var k2bodiesInWorld = kinect.Bodies.ChangeToFrame(WorldToKinect2);
                k2bodiesInWorld.Write("kinect2_bodies", store);

                kinect.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("kinect2_color", store);

                var azure = new AzureKinectSensor(pipeline, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputInfrared = true,
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                    {
                        CpuOnlyMode = false,
                        TemporalSmoothing = 0.25f
                    }
                });

                var azureBodiesInWorld = azure.Bodies.ChangeToFrame(WorldToAzure);
                azureBodiesInWorld.Write("k4a_bodies", store);
                azure.InfraredImage.Write("k4a_ir", store);
                azure.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("k4a_image", store);

                // Merge the who streams
                var merger = new BodyMerger(pipeline, azureBodiesInWorld);
                merger.AddKinect2BodyStream(k2bodiesInWorld);

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
