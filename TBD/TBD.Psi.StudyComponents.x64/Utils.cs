// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.StudyComponents
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
