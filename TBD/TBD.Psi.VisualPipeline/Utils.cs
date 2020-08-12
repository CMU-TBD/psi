// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisualPipeline
{
    using System;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    internal class Utils
    {
        internal static CoordinateSystem CreateCoordinateSystemFrom(float x, float y, float z, float qx, float qy, float qz, float qw)
        {
            return CreateCoordinateSystemFrom(new Point3D(x, y, z), new System.Numerics.Quaternion(qx, qy, qz, qw));
        }

        internal static CoordinateSystem CreateCoordinateSystemFrom(float x, float y, float z, float yaw, float pitch, float roll)
        {
            return CreateCoordinateSystemFrom(new Point3D(x, y, z), yaw, pitch, roll);
        }

        internal static CoordinateSystem CreateCoordinateSystemFrom(Point3D position, float yaw, float pitch, float roll)
        {
            return CreateCoordinateSystemFrom(position, System.Numerics.Quaternion.CreateFromYawPitchRoll(pitch, roll, yaw));
        }

        // Implementation of https://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/
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

        internal static CoordinateSystem CreateCoordinateSystemFrom(Point3D position, System.Numerics.Quaternion orientation)
        {
            var rotation = Matrix4x4.CreateFromQuaternion(orientation);
            var transformationMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { rotation.M11, rotation.M21, rotation.M31, position.X},
                { rotation.M12, rotation.M22, rotation.M32, position.Y},
                { rotation.M13, rotation.M23, rotation.M33, position.Z},
                { 0,                  0,                  0,                 1 },
            });
            return new CoordinateSystem(transformationMatrix);
        }
    }
}
