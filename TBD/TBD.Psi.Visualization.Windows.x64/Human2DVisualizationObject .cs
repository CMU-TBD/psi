// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// modifed from AzureKinectBody

namespace TBD.Psi.Visualization.Windows
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using System.Linq;
    using HelixToolkit.Wpf;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;
    using TBD.Psi.StudyComponents;
    using MathNet.Spatial.Euclidean;

    [VisualizationObject("Human Body 2D")]
    public class Human2DVisualizationObject : ModelVisual3DVisualizationObject<(uint, double[])>
    {
        private EllipsoidVisual3D circle = new EllipsoidVisual3D() { Fill = new SolidColorBrush(Colors.White), RadiusZ = 0, RadiusX = 0.25, RadiusY = 0.25 };
        private ArrowVisual3D pointer = new ArrowVisual3D() { Fill = new SolidColorBrush(Colors.HotPink), Diameter = 0.05 };

        public Human2DVisualizationObject()
        {

        }

        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private Win3D.Matrix3D ToMatrix3D(CoordinateSystem cs)
        {
            return new Win3D.Matrix3D(
                cs.Storage.At(0, 0), cs.Storage.At(1, 0), cs.Storage.At(2, 0), cs.Storage.At(3, 0),
                cs.Storage.At(0, 1), cs.Storage.At(1, 1), cs.Storage.At(2, 1), cs.Storage.At(3, 1),
                cs.Storage.At(0, 2), cs.Storage.At(1, 2), cs.Storage.At(2, 2), cs.Storage.At(3, 2),
                cs.Storage.At(0, 3), cs.Storage.At(1, 3), cs.Storage.At(2, 3), cs.Storage.At(3, 3)
                );
        }

        public override void UpdateData()
        {
            if (this.CurrentData != default)
            {
                this.pointer.BeginEdit();
                this.pointer.Point1 = new Win3D.Point3D(this.CurrentData.Item2[0], this.CurrentData.Item2[1], 0);
                var theta = this.CurrentData.Item2[2];
                this.pointer.Point2 = new Win3D.Point3D(this.CurrentData.Item2[0] + Math.Cos(theta) * 0.25, this.CurrentData.Item2[1] + Math.Sin(theta) * 0.25, 0);
                this.pointer.EndEdit();

                var cs = new CoordinateSystem();
                cs = cs.RotateCoordSysAroundVector(UnitVector3D.ZAxis, MathNet.Spatial.Units.Angle.FromRadians(theta));
                cs = cs.OffsetBy(new Vector3D(this.CurrentData.Item2[0], this.CurrentData.Item2[1], 0));
                this.circle.BeginEdit();
                this.circle.Transform = new Win3D.MatrixTransform3D(ToMatrix3D(cs));
                this.circle.EndEdit();

            }
            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.pointer, this.Visible && this.CurrentData != default);
            this.UpdateChildVisibility(this.circle, this.Visible && this.CurrentData != default);
        }
    }
}
