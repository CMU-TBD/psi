namespace TBD.Psi.VisualPipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Kinect;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Kinect;

    public static class OperatorExtensions
    {
        public static IProducer<List<AzureKinectBody>> ChangeToFrame(this IProducer<List<AzureKinectBody>> producer, CoordinateSystem WantedToAzure)
        {
            return producer.Select(m =>
            {
                // Go through each body and change Frames
                for (var i = 0; i < m.Count; i++)
                {
                    foreach (var key in m[i].Joints.Keys.ToList())
                    {
                        var (pose, confidence) = m[i].Joints[key];
                        // update Pose
                        pose = new CoordinateSystem(WantedToAzure * pose);
                        m[i].Joints[key] = (pose, confidence);
                    }
                }
                return m;
            });
        }


        public static IProducer<List<KinectBody>> ChangeToFrame(this IProducer<List<KinectBody>> producer, CoordinateSystem WantedToKinect)
        {
            return producer.Select(m =>
            {
                // Go through each body and change Frames
                for (var i = 0; i < m.Count; i++)
                {
                    foreach (var key in m[i].Joints.Keys.ToList())
                    {
                        var (pose, trackingState) = m[i].Joints[key];
                        // update Pose
                        pose = new CoordinateSystem(WantedToKinect * pose);
                        m[i].Joints[key] = (pose, trackingState);
                    }
                }
                return m;
            });
        }

        public static (CoordinateSystem, JointConfidenceLevel) ConvertK2JointToAzure(ValueTuple<CoordinateSystem, TrackingState> tuple)
        {
            var level = JointConfidenceLevel.None;
            if (tuple.Item2 == TrackingState.Tracked)
            {
                level = JointConfidenceLevel.Medium;
            }
            else if (tuple.Item2 == TrackingState.Inferred)
            {
                level = JointConfidenceLevel.Low;
            }
            return (tuple.Item1, level);
        }

        public static List<(JointId, Microsoft.Kinect.JointType)> K4AtoK2JointCorrespondence = new List<(JointId, JointType)>
        {
            // Head
            (JointId.Head, JointType.Head),
            (JointId.Neck, JointType.Neck),

            // Body
            (JointId.SpineChest, JointType.SpineMid),
            (JointId.Pelvis, JointType.SpineBase),

            // Left Arm
            (JointId.ShoulderLeft, JointType.ShoulderLeft),
            (JointId.ElbowLeft, JointType.ElbowLeft),
            (JointId.WristLeft, JointType.WristLeft),
            (JointId.ThumbLeft, JointType.ThumbLeft),

            // Right Arm
            (JointId.ShoulderRight, JointType.ShoulderRight),
            (JointId.ElbowRight, JointType.ElbowRight),
            (JointId.WristRight, JointType.WristRight),
            (JointId.ThumbRight, JointType.ThumbRight),

            // Left Leg
            (JointId.HipLeft, JointType.HipLeft),
            (JointId.KneeLeft, JointType.KneeLeft),
            (JointId.AnkleLeft, JointType.AnkleLeft),
            (JointId.FootLeft, JointType.FootLeft),

            // Right Legs
            (JointId.HipRight, JointType.HipRight),
            (JointId.KneeRight, JointType.KneeRight),
            (JointId.AnkleRight, JointType.AnkleRight),
            (JointId.FootRight, JointType.FootRight),
        };

        public static IProducer<List<AzureKinectBody>> ToAzureKinectBodies(this IProducer<List<KinectBody>> producer)
        {
            return producer.Select(m =>
            {
                var k4aBodies = new List<AzureKinectBody>();
                foreach (var body in m)
                {
                    //Convert to k4abodies
                    var k4aBody = new AzureKinectBody();
                    k4aBody.TrackingId = Convert.ToUInt32(body.TrackingId % UInt32.MaxValue);

                    foreach (var correspondent in K4AtoK2JointCorrespondence)
                    {
                        k4aBody.Joints[correspondent.Item1] = ConvertK2JointToAzure(body.Joints[correspondent.Item2]);
                    }
                    k4aBodies.Add(k4aBody);
                }
                return k4aBodies;
            });

        }
    }
}
