using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Geometry3Sharp
{
    using Win3D = System.Windows.Media.Media3D;

    public static class Extensions
    {
        public static Win3D.Matrix3D ToMatrix3D(this Frame3f frame)
        {
            var rotMat = frame.Rotation.ToRotationMatrix();
            return new Win3D.Matrix3D(
                rotMat.Row0.x, rotMat.Row1.x, rotMat.Row2.x, 0,
                rotMat.Row0.y, rotMat.Row1.y, rotMat.Row2.y, 0,
                rotMat.Row0.z, rotMat.Row1.z, rotMat.Row2.z, 0,
                frame.Origin.x, frame.Origin.y, frame.Origin.z, 1);
        }

        public static Win3D.Matrix3D ToMatrix3D(this Box3d box)
        {
            return new Win3D.Matrix3D(
                box.AxisX.x, box.AxisX.y, box.AxisX.z, 0,
                box.AxisY.x, box.AxisY.y, box.AxisY.z, 0,
                box.AxisZ.x, box.AxisZ.y, box.AxisZ.z, 0,
                box.Center.x, box.Center.y, box.Center.z, 1);
        }
    }
}
