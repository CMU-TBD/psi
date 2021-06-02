// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.StudyComponents
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra.Double;
    using MathNet.Spatial.Euclidean;
    using MathNet.Spatial.Units;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi.AzureKinect;
    using Quaternion = System.Numerics.Quaternion;
 

    public class Utils
    {
        /// <summary>
        /// Convert quaternion to Axis Angle. Implement algorithm from
        /// https://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToAngle/index.htm
        /// </summary>
        /// <param name="quaternion">Quaternion</param>
        /// <returns>Axis and Angle (degrees).</returns>
        internal static (Vector3, double) GetAxisAngleFromQuaterion(Quaternion quaternion)
        {
            // check for the singularity conditions
            if (quaternion.IsIdentity)
            {
                return (new Vector3(1, 0, 0), 0);
            }

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

        public static System.Numerics.Quaternion GetQuaternionFromCoordinateSystem(CoordinateSystem cs)
        {

            var rotMat = cs.GetRotationSubMatrix();
            double trace = rotMat.At(0, 0) + rotMat.At(1, 1) + rotMat.At(2, 2);
            double w, x, y, z;
            if (trace > 0)
            {
                double s = 0.5 / Math.Sqrt(trace + 1);
                w = 0.25 / s;
                x = (rotMat.At(2, 1) - rotMat.At(1, 2)) * s;
                y = (rotMat.At(0, 2) - rotMat.At(2, 0)) * s;
                z = (rotMat.At(1, 0) - rotMat.At(0, 1)) * s;
            }
            else if (rotMat.At(0, 0) > rotMat.At(1, 1) && rotMat.At(0, 0) > rotMat.At(2, 2))
            {
                double s = Math.Sqrt(1 + rotMat.At(0, 0) - rotMat.At(1, 1) - rotMat.At(2, 2)) * 2;
                w = (rotMat.At(2, 1) - rotMat.At(1, 2)) / s;
                x = 0.25 * s;
                y = (rotMat.At(0, 1) + rotMat.At(1, 0)) / s;
                z = (rotMat.At(0, 2) + rotMat.At(2, 0)) / s;
            }
            else if (rotMat.At(1, 1) > rotMat.At(2, 2))
            {
                double s = Math.Sqrt(1 + rotMat.At(1, 1) - rotMat.At(0, 0) - rotMat.At(2, 2)) * 2;
                w = (rotMat.At(0, 2) - rotMat.At(2, 0)) / s;
                x = (rotMat.At(0, 1) + rotMat.At(1, 0)) / s;
                y = 0.25 * s;
                z = (rotMat.At(1, 2) + rotMat.At(2, 1)) / s;
            }
            else
            {
                double s = Math.Sqrt(1 + rotMat.At(2, 2) - rotMat.At(0, 0) - rotMat.At(1, 1)) * 2;
                w = (rotMat.At(1, 0) - rotMat.At(0, 1)) / s;
                x = (rotMat.At(0, 2) + rotMat.At(2, 0)) / s;
                y = (rotMat.At(1, 2) + rotMat.At(2, 1)) / s;
                z = 0.25 * s;
            }

            return new System.Numerics.Quaternion(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z), Convert.ToSingle(w));
        }

        internal static Quaternion Normalize(Quaternion q)
        {
            var norm = q.Length();
            return new Quaternion(q.X / norm, q.Y / norm, q.Z / norm, q.W / norm);
        }

        internal static CoordinateSystem ConstructCoordinateSystem(Vector3D origin, Quaternion q)
        {
            var transMat = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix.CreateIdentity(4);
            double sqw = q.W * q.W;
            double sqx = q.X * q.X;
            double sqy = q.Y * q.Y;
            double sqz = q.Z * q.Z;

            transMat.At(0, 0, sqx - sqy - sqz + sqw);
            transMat.At(1, 1, -sqx + sqy - sqz + sqw);
            transMat.At(2, 2, -sqx - sqy + sqz + sqw);

            double tmp1 = q.X * q.Y;
            double tmp2 = q.Z * q.W;

            transMat.At(1, 0, (tmp1 + tmp2) * 2);
            transMat.At(0, 1, (tmp1 - tmp2) * 2);

            tmp1 = q.X * q.Z;
            tmp2 = q.Y * q.W;
            transMat.At(2, 0, 2* (tmp1 - tmp2));
            transMat.At(0, 2, 2* (tmp1 + tmp2));
            tmp1 = q.Y * q.Z;
            tmp2 = q.X * q.W;
            transMat.At(2, 1, 2 * (tmp1 + tmp2));
            transMat.At(1, 2, 2 * (tmp1 - tmp2));

            transMat.At(0, 3, origin.X);
            transMat.At(1, 3, origin.Y);
            transMat.At(2, 3, origin.Z);

            //(var rotVec, var rotVal) = GetAxisAngleFromQuaterion(q);
            //var rotMat = Matrix3D.RotationAroundArbitraryVector(UnitVector3D.Create(rotVec.X, rotVec.Y, rotVec.Z), Angle.FromDegrees(rotVal));

            //var cs = new CoordinateSystem()
            return new CoordinateSystem(transMat);
        }
    }
}
