// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisionComponents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;

    /// <summary>
    /// Common Psi Component Extensions.
    /// </summary>
    public static class OperatorExtensions
    {
     
        /// <summary>
        /// Change the Coordinate System of Each Azure Kinect Body.
        /// </summary>
        /// <param name="producer">Procuder of Azure Kinect Bodies.</param>
        /// <param name="wantedToAzure">Transformation from desire to azure frame.</param>
        /// <returns>Azure Kinect Bodies in transformed frame.</returns>
        public static IProducer<List<AzureKinectBody>> ChangeToFrame(this IProducer<List<AzureKinectBody>> producer, CoordinateSystem wantedToAzure)
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
                        pose = new CoordinateSystem(wantedToAzure * pose);
                        m[i].Joints[key] = (pose, confidence);
                    }
                }
                return m;
            });
        }

        public static IProducer<List<HumanBody>> ChangeToFrame(this IProducer<List<HumanBody>> producer, CoordinateSystem wantedToAzure)
        {
            return producer.Select(m =>
            {
                // Go through each body and change Frames
                for (var i = 0; i < m.Count; i++)
                {
                    // just need to update the root
                    m[i].RootPose = new CoordinateSystem(wantedToAzure * m[i].RootPose);
                }
                return m;
            });
        }

        public static IProducer<List<HumanBody>> ChangeToHumanBodies(this IProducer<List<AzureKinectBody>> producer)
        {
            return producer.Select(m =>
            {
                return m.Select(m =>
                {
                    var b = new HumanBody();
                    if (b.FromAzureKinectBody(m))
                    {
                        return b;
                    }
                    return null;
                }).Where(m => m != null).ToList();
            });
        }
        /*
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
                }*/
        /*
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
                }*/
        /*
                /// <summary>
                /// Convert a Kinect2 Bodies to Azure Kinect Bodies.
                /// </summary>
                /// <param name="producer">Emitter of Kinect2 Bodies</param>
                /// <returns>Emitter of Converted Kinect2 Bodies as Azure Kinect Bodies.</returns>
                public static IProducer<List<HumanBody>> ToAzureKinectBodies(this IProducer<List<KinectBody>> producer)
                {
                    return producer.Select(m =>
                    {
                        var k4aBodies = new List<HumanBody>();
                        foreach (var body in m)
                        {
                            // Convert to k4abodies
                            var k4aBody = new HumanBody();
                            k4aBody.TrackingId = Convert.ToUInt32(body.TrackingId % uint.MaxValue);

                            foreach (var correspondent in k4AtoK2JointCorrespondence)
                            {
                                k4aBody.Joints[correspondent.Item1] = ConvertK2JointToAzure(body.Joints[correspondent.Item2]);
                            }

                            k4aBodies.Add(k4aBody);
                        }

                        return k4aBodies;
                    });

                }*/
    }
}
