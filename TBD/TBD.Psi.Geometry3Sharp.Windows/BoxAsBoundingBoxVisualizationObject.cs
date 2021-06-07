// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// modifed from AzureKinectBody

namespace TBD.Psi.Geometry3Sharp
{
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using g3;
    using Win3D = System.Windows.Media.Media3D;


    [VisualizationObject("Box3d")]
    public class BoxAsBoundingBoxVisualizationObject : ModelVisual3DVisualizationObject<Box3d>
    {
        private readonly BoundingBoxVisual3D box = new BoundingBoxVisual3D() { Diameter = 0.025}; 

        public override void NotifyPropertyChanged(string propertyName)
       {
            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        public override void UpdateData()
        {
            this.updateVisuals();
        }

        public void updateVisuals()
        {
            // update it
            if(this.CurrentData.Volume > 0)
            {
                this.box.Transform = new Win3D.MatrixTransform3D(this.CurrentData.ToMatrix3D());
                this.box.BoundingBox = new Win3D.Rect3D(-this.CurrentData.Extent.x, -this.CurrentData.Extent.y, -this.CurrentData.Extent.z, this.CurrentData.Extent.x * 2, this.CurrentData.Extent.y * 2, this.CurrentData.Extent.z* 2);
            }

            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.box, this.Visible && this.CurrentData.Volume > 0);
        }
    }
}
