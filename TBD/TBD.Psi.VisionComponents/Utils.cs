// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisionComponents
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra.Double;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi.AzureKinect;
    using Quaternion = System.Numerics.Quaternion;

    internal class Utils
    {
        /// <summary>
        /// Convert quaternion to Axis Angle. Implement algorithm from
        /// https://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToAngle/index.htm
        /// </summary>
        /// <param name="quaternion">Quaternion</param>
        /// <returns>Axis and Angle.</returns>
        internal static (Vector3, double) GetAxisAngleFromQuaterion(Quaternion quaternion)
        {
            var angle = 2 * Math.Acos(quaternion.W);
            var axis = new Vector3(0);
            var denominator = Math.Sqrt(1 - (quaternion.W * quaternion.W));
            if (denominator < 0.0001)
            {
                axis.X = quaternion.X;
                axis.Y = quaternion.Y;
                axis.Z = quaternion.Z;
            }
            else
            {
                axis.X = Convert.ToSingle(quaternion.X / denominator);
                axis.Y = Convert.ToSingle(quaternion.Y / denominator);
                axis.Z = Convert.ToSingle(quaternion.Z / denominator);
            }

            return (axis, angle);
        }

        /// <summary>
        /// Calculate the translation and rotation error between two coordinate systems
        /// </summary>
        /// <param name="cs1"></param>
        /// <param name="cs2"></param>
        /// <returns></returns>
        internal static (double, double) CalculateDifference(CoordinateSystem cs1, CoordinateSystem cs2)
        {
            // Calculate the quaternion that rotate Cs1 to Cs2
            var quaternion1 = GetQuaternionFromCoordinateSystem(cs1);
            var quaternion2 = GetQuaternionFromCoordinateSystem(cs2);
            var error_quaternion = quaternion2 * Quaternion.Inverse(quaternion1);
            var (axis, angle) = GetAxisAngleFromQuaterion(error_quaternion);

            // The rotation error is sum of the axis * angle of the quaternion
            // alternative is to compare the error along each frame.
            var rotDiff = angle * Math.Sqrt((axis.X * axis.X) + (axis.Y * axis.Y) + (axis.Z * axis.Z));

            var poseDiff = cs1 - cs2;
            var dist = Math.Sqrt((poseDiff[0, 3] * poseDiff[0, 3]) + (poseDiff[1, 3] * poseDiff[1, 3]));
            return (dist, rotDiff);
        }

        /// <summary>
        /// Using heuristics to decide if the two bodies are the same. Default checks only the neck and chest joints.
        /// </summary>
        /// <param name="body1">Body 1.</param>
        /// <param name="body2">Body 2.</param>
        /// <param name="distTol">Tolerant to distance between key joints (in meters).</param>
        /// <param name="rotTol">Tolerant to rotation between key joints (in radian).</param>
        /// <param name="keyJoints">List of key joints. </param>
        /// <returns>Whether the bodies are the same.</returns>
        internal static bool CompareAzureBodies(HumanBody body1, HumanBody body2, double distTol = 0.5, double rotTol = 0.7, List<JointId> keyJoints = null)
        {
            // Set the key joints if none is passed in
            if (keyJoints == null)
            {
                keyJoints = new List<JointId>() { JointId.Neck, JointId.SpineChest };
            }

            return HumanBody.CompareHumanBodies(body1, body2, distTol, rotTol, keyJoints);
        }

        internal static System.Numerics.Quaternion GetQuaternionFromCoordinateSystem(CoordinateSystem cs)
        {

            var rotMat = cs.GetRotationSubMatrix();
            double w = Math.Sqrt(1 + rotMat.At(0, 0) + rotMat.At(1, 1) + rotMat.At(2, 2)) / 2.0;
            double w4 = 4 * w;
            double x = (rotMat.At(2, 1) - rotMat.At(1, 2)) / w4;
            double y = (rotMat.At(0, 2) - rotMat.At(2, 0)) / w4;
            double z = (rotMat.At(1, 0) - rotMat.At(0, 1)) / w4;

            return new System.Numerics.Quaternion(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z), Convert.ToSingle(w));

        }
    }
}
