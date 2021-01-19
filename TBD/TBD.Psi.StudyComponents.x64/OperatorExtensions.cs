// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.StudyComponents
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
        public static IProducer<List<AzureKinectBody>> ChangeToFrame(this IProducer<List<AzureKinectBody>> producer, CoordinateSystem wantedToAzure, DeliveryPolicy deliveryPolicy = null)
        {
            return producer.Select(m =>
            {
                // Go through each body and change Frames
                for (var i = 0; i < m.Count; i++)
                {
                    foreach (var key in m[i].Joints.Keys.ToList())
                    {
                        // update the pose
                        m[i].Joints[key] = (m[i].Joints[key].Pose.TransformBy(wantedToAzure), m[i].Joints[key].Confidence);
                    }
                }
                return m;
            }, deliveryPolicy);
        }

        public static IProducer<List<HumanBody>> ChangeToFrame(this IProducer<List<HumanBody>> producer, CoordinateSystem wantedToAzure, DeliveryPolicy deliveryPolicy = null)
        {
            return producer.Select(m =>
            {
                // Go through each body and change Frames
                for (var i = 0; i < m.Count; i++)
                {
                    // just need to update the root
                    m[i].RootPose = m[i].RootPose.TransformBy(wantedToAzure);
                }
                return m;
            }, deliveryPolicy);
        }

        public static IProducer<List<HumanBody>> ChangeToHumanBodies(this IProducer<List<AzureKinectBody>> producer, DeliveryPolicy deliveryPolicy = null)
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
            }, deliveryPolicy);
        }
    }
}
