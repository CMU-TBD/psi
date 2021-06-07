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
        private EllipsoidVisual3D circle = new EllipsoidVisual3D() {RadiusZ = 0, RadiusX = 0.25, RadiusY = 0.25 };
        private ArrowVisual3D pointer = new ArrowVisual3D() { Diameter = 0.05 };

        private Color baseColor = Colors.White;
        private Color arrowColor = Colors.Yellow;
        private int radius = 250;

        public Human2DVisualizationObject()
        {
        }

        public override void NotifyPropertyChanged(string propertyName)
       {
            if (propertyName == nameof(this.ArrowColor) ||
                propertyName == nameof(this.BaseColor) ||
                propertyName == nameof(this.Radius))
            {
                this.updateVisuals();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        /// <summary>
        /// Gets or sets the base color.
        /// </summary>
        [DataMember]
        [Description("Color of the human base.")]
        public Color BaseColor
        {
            get { return this.baseColor; }
            set { this.Set(nameof(this.BaseColor), ref this.baseColor, value); }
        }

        /// <summary>
        /// Gets or sets the arrow color.
        /// </summary>
        [DataMember]
        [Description("Color of the human arrow.")]
        public Color ArrowColor
        {
            get { return this.arrowColor; }
            set { this.Set(nameof(this.ArrowColor), ref this.arrowColor, value); }
        }

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        [DataMember]
        [Description("Radius of the human. (mm)")]
        public int Radius
        {
            get { return this.radius; }
            set { this.Set(nameof(this.Radius), ref this.radius, value); }
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
            this.updateVisuals();
        }

        public void updateVisuals()
        {
            if (this.CurrentData != default)
            {
                this.pointer.BeginEdit();
                this.pointer.Point1 = new Win3D.Point3D(this.CurrentData.Item2[0], this.CurrentData.Item2[1], 0);
                var theta = this.CurrentData.Item2[2];
                this.pointer.Point2 = new Win3D.Point3D(this.CurrentData.Item2[0] + Math.Cos(theta) * (this.radius/1000.0), this.CurrentData.Item2[1] + Math.Sin(theta) * (this.radius / 1000.0), 0);
                this.pointer.Fill = new SolidColorBrush(this.arrowColor);
                this.pointer.EndEdit();

                var cs = new CoordinateSystem();
                cs = cs.RotateCoordSysAroundVector(UnitVector3D.ZAxis, MathNet.Spatial.Units.Angle.FromRadians(theta));
                cs = cs.OffsetBy(new Vector3D(this.CurrentData.Item2[0], this.CurrentData.Item2[1], 0));
                this.circle.BeginEdit();
                this.circle.Transform = new Win3D.MatrixTransform3D(ToMatrix3D(cs));
                this.circle.RadiusX = (this.radius / 1000.0);
                this.circle.RadiusY = (this.radius / 1000.0);
                this.circle.Fill = new SolidColorBrush(this.baseColor);
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
