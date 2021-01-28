﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a visualization object for a spatial depth camera view as a mesh, from a depth image and position.
    /// Camera intrinsics are determined from manually set focal length properties.
    /// </summary>
    [VisualizationObject("3D Depth Camera View as Mesh")]
    public class SpatialDepthCameraViewAsMeshManualFocalLengthVisualizationObject : ModelVisual3DVisualizationObject<(Shared<DepthImage>, CoordinateSystem)>
    {
        private SpatialDepthCameraViewAsMeshVisualizationObject depthCameraViewAsMesh;

        private Shared<DepthImage> depthImage = null;
        private ICameraIntrinsics intrinsics = null;
        private CoordinateSystem position = null;
        private double focalLengthX = 500;
        private double focalLengthY = 500;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDepthCameraViewAsMeshManualFocalLengthVisualizationObject"/> class.
        /// </summary>
        public SpatialDepthCameraViewAsMeshManualFocalLengthVisualizationObject()
        {
            this.depthCameraViewAsMesh = new SpatialDepthCameraViewAsMeshVisualizationObject();
        }

        /// <summary>
        /// Gets or sets the visualization object for the depth camera view, including the mesh and view frustum.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Spatial Depth Camera View")]
        [Description("The spatial depth camera view, including mesh and frustum.")]
        public SpatialDepthCameraViewAsMeshVisualizationObject DepthCameraViewAsMesh
        {
            get { return this.depthCameraViewAsMesh; }
            set { this.Set(nameof(this.DepthCameraViewAsMesh), ref this.depthCameraViewAsMesh, value); }
        }

        /// <summary>
        /// Gets or sets the focal length in the X dimension.
        /// </summary>
        [DataMember]
        [DisplayName("Focal Length X")]
        [Description("The focal length in the X dimension.")]
        public double FocalLengthX
        {
            get { return this.focalLengthX; }
            set { this.Set(nameof(this.FocalLengthX), ref this.focalLengthX, value); }
        }

        /// <summary>
        /// Gets or sets the focal length in the Y dimension.
        /// </summary>
        [DataMember]
        [DisplayName("Focal Length Y")]
        [Description("The focal length in the Y dimension.")]
        public double FocalLengthY
        {
            get { return this.focalLengthY; }
            set { this.Set(nameof(this.FocalLengthY), ref this.focalLengthY, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.depthImage != null)
            {
                this.depthImage.Dispose();
            }

            this.depthImage = this.CurrentData.Item1?.AddRef();
            this.position = this.CurrentData.Item2;

            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.FocalLengthX) || propertyName == nameof(this.FocalLengthY))
            {
                this.UpdateVisuals();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.intrinsics = this.depthImage?.Resource?.ComputeCameraIntrinsics(this.FocalLengthX, this.FocalLengthY);
            this.depthCameraViewAsMesh.SetCurrentValue(this.SynthesizeMessage((this.depthImage, this.intrinsics, this.position)));
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.depthCameraViewAsMesh.ModelView, this.Visible && this.CurrentData != default);
        }
    }
}
