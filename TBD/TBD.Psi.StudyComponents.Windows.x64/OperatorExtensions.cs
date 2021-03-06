﻿
namespace TBD.Psi.StudyComponents
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Kinect;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Kinect;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class OperatorExtensions
    {
        private static List<(JointId, JointType)> k4AtoK2JointCorrespondence = new List<(JointId, JointType)>
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

        /// <summary>
        /// Change the Coordinate System of Each Kinect2 Body.
        /// </summary>
        /// <param name="producer">Procuder of Kinect2 Bodies.</param>
        /// <param name="wantedToKinect">Transformation from desire to kinect2 frame.</param>
        /// <returns>Kinect2 Bodies in transformed frame.</returns>
        public static IProducer<List<KinectBody>> ChangeToFrame(this IProducer<List<KinectBody>> producer, CoordinateSystem wantedToKinect)
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
                        pose = new CoordinateSystem(wantedToKinect * pose);
                        m[i].Joints[key] = (pose, trackingState);
                    }
                }

                return m;
            });
        }

        private static (CoordinateSystem, JointConfidenceLevel) ConvertK2JointToAzure(ValueTuple<CoordinateSystem, TrackingState> tuple)
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

        /// <summary>
        /// Convert a Kinect2 Bodies to Human Body
        /// </summary>
        /// <param name="producer">Emitter of Kinect2 Bodies</param>
        /// <returns>Emitter of Converted Kinect2 Bodies as Human Bodies.</returns>
        public static IProducer<List<HumanBody>> ChangeToHumanBodies(this IProducer<List<KinectBody>> producer)
        {
            return producer.Select(m =>
            {
                var humanBodies = new List<HumanBody>();
                foreach (var body in m)
                {
                    // we first convert to azurekinect body then back to k2. 
                    // TODO: If this is too slow, change it to be direct in the future
                    var k4aBody = new AzureKinectBody();
                    k4aBody.TrackingId = Convert.ToUInt32(body.TrackingId % uint.MaxValue);

                    foreach (var correspondent in k4AtoK2JointCorrespondence)
                    {
                        k4aBody.Joints[correspondent.Item1] = ConvertK2JointToAzure(body.Joints[correspondent.Item2]);
                    }

                    // now we convert to human body
                    var humanBody = new HumanBody();
                    if (humanBody.FromAzureKinectBody(k4aBody))
                    {
                        humanBodies.Add(humanBody);
                    }
                }

                return humanBodies;
            });

        }
    }
}
