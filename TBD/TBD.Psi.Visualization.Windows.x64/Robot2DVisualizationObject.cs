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

    [VisualizationObject("Robot 2D")]
    public class Robot2DVisualizationObject : ModelVisual3DVisualizationObject<(string, double[])>
    {
        private RectangleVisual3D rectangle = new RectangleVisual3D() { Fill = new SolidColorBrush(Colors.Red) };
        private ArrowVisual3D direction = new ArrowVisual3D() { Fill = new SolidColorBrush(Colors.Black), Diameter = 0.05 };

        public Robot2DVisualizationObject()
        {

        }

        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        public override void UpdateData()
        {
            if (this.CurrentData != default)
            {
                this.rectangle.BeginEdit();
                this.rectangle.Origin = new Win3D.Point3D(this.CurrentData.Item2[0], this.CurrentData.Item2[1], 0);
                var theta = this.CurrentData.Item2[2];
                this.rectangle.LengthDirection = new Win3D.Vector3D(Math.Cos(theta), Math.Sin(theta), 0);
                this.rectangle.Length = this.CurrentData.Item2[3];
                this.rectangle.Width = this.CurrentData.Item2[4];
                this.rectangle.EndEdit();
                // add direction
                this.direction.BeginEdit();
                this.direction.Point1 = new Win3D.Point3D(this.CurrentData.Item2[0], this.CurrentData.Item2[1], 0);
                this.direction.Point2 = new Win3D.Point3D(this.CurrentData.Item2[0] + Math.Cos(theta) * (this.rectangle.Length/2), this.CurrentData.Item2[1] + Math.Sin(theta) * (this.rectangle.Length / 2), 0);
                this.direction.EndEdit();

            }
            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.rectangle, this.Visible && this.CurrentData != default);
            this.UpdateChildVisibility(this.direction, this.Visible && this.CurrentData != default);
        }
    }
}
