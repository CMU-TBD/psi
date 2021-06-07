using g3;
using MathNet.Spatial.Euclidean;
using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.StudyComponents
{
    public static class G3Extensions
    {
        public static Quaternionf GetQuaternionf(this CoordinateSystem cs)
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
            return new Quaternionf(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z), Convert.ToSingle(w));
        }

        public static Frame3f ToFrame3f(this CoordinateSystem cs)
        {
            return new Frame3f(new Vector3f(cs.Origin.X, cs.Origin.Y, cs.Origin.Z), cs.GetQuaternionf());
        }
    }
}
