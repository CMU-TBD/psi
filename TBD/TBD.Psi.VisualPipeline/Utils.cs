namespace TBD.Psi.VisualPipeline
{
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    public class Utils
    {

        public static CoordinateSystem CreateCoordinateSystemFrom(float x, float y, float z, float qx, float qy, float qz, float qw)
        {
            return CreateCoordinateSystemFrom(new Point3D(x, y, z), new System.Numerics.Quaternion(qx, qy, qz, qw));
        }

        public static CoordinateSystem CreateCoordinateSystemFrom(float x, float y, float z, float yaw, float pitch, float roll)
        {
            return CreateCoordinateSystemFrom(new Point3D(x, y, z), yaw, pitch, roll);
        }

        public static CoordinateSystem CreateCoordinateSystemFrom(Point3D position, float yaw, float pitch, float roll)
        {
            return CreateCoordinateSystemFrom(position, System.Numerics.Quaternion.CreateFromYawPitchRoll(pitch, roll, yaw));
        }

        public static CoordinateSystem CreateCoordinateSystemFrom(Point3D position, System.Numerics.Quaternion orientation)
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
