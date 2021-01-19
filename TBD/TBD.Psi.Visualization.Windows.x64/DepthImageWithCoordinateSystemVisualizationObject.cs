// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// Modified from DepthImageWithIntrinsicsAsPointCloudVisualizationObject.cs
// Copyright (c) CMU

namespace TBD.Psi.Visualization.Windows
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Views.Visuals3D;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a depth image 3D point cloud visualization object.
    /// </summary>
    [VisualizationObject("3D Point Cloud With Origin CoordinateSystem")]
    public class DepthImageWithCoordinateSystemVisualizationObject : ModelVisual3DVisualizationObject<(Shared<EncodedDepthImage>, CoordinateSystem)>
    {
        private readonly DepthImagePointCloudVisual3D depthImagePointCloudVisual3D;
        private readonly CameraIntrinsicsVisual3D cameraIntrinsicsVisual3D;

        private CameraIntrinsics intrinsics;
        private Color pointCloudColor = Colors.Gray;
        private double pointSize = 1.0;
        private int sparsity = 3;
        private Color frustumColor = Colors.DimGray;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageAsPointCloudVisualizationObject"/> class.
        /// </summary>
        public DepthImageWithCoordinateSystemVisualizationObject()
        {
            this.depthImagePointCloudVisual3D = new DepthImagePointCloudVisual3D()
            {
                Color = this.pointCloudColor,
                Size = this.pointSize,
                Sparsity = this.sparsity,
            };

            this.cameraIntrinsicsVisual3D = new CameraIntrinsicsVisual3D()
            {
                Color = this.frustumColor,
            };
        }

        /// <summary>
        /// Gets or sets the point cloud color.
        /// </summary>
        [DataMember]
        [DisplayName("Point Color")]
        [Description("The color of a point in the cloud.")]
        public Color PointCloudColor
        {
            get { return this.pointCloudColor; }
            set { this.Set(nameof(this.PointCloudColor), ref this.pointCloudColor, value); }
        }

        /// <summary>
        /// Gets or sets the point size.
        /// </summary>
        [DataMember]
        [DisplayName("Point Size")]
        [Description("The size of a point in the cloud.")]
        public double PointSize
        {
            get { return this.pointSize; }
            set { this.Set(nameof(this.PointSize), ref this.pointSize, value); }
        }

        /// <summary>
        /// Gets or sets the point cloud sparsity.
        /// </summary>
        [DataMember]
        [DisplayName("Point Cloud Sparsity")]
        [Description("The sparsity (in pixels) of the point cloud.")]
        public int Sparsity
        {
            get { return this.sparsity; }
            set { this.Set(nameof(this.Sparsity), ref this.sparsity, value); }
        }

        /// <summary>
        /// Gets or sets the frustum color.
        /// </summary>
        [DataMember]
        [DisplayName("Frustum Color")]
        [Description("The color of the rendered frustum.")]
        public Color FrustumColor
        {
            get { return this.frustumColor; }
            set { this.Set(nameof(this.FrustumColor), ref this.frustumColor, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.PointCloudColor))
            {
                this.depthImagePointCloudVisual3D.Color = this.PointCloudColor;
            }
            else if (propertyName == nameof(this.PointSize))
            {
                this.depthImagePointCloudVisual3D.Size = this.PointSize;
            }
            else if (propertyName == nameof(this.Sparsity))
            {
                this.depthImagePointCloudVisual3D.Sparsity = this.Sparsity;
            }
            else if (propertyName == nameof(this.FrustumColor))
            {
                this.cameraIntrinsicsVisual3D.Color = this.FrustumColor;
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.ComputeIntrinsics();
            if (this.intrinsics != null)
            {
                this.depthImagePointCloudVisual3D.UpdatePointCloud(this.CurrentData.Item1?.Resource?.Decode(new DepthImageFromStreamDecoder()), this.intrinsics, this.CurrentData.Item2);
                this.cameraIntrinsicsVisual3D.Intrinsics = this.intrinsics;
                this.cameraIntrinsicsVisual3D.Position = this.CurrentData.Item2;
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.depthImagePointCloudVisual3D, this.Visible && this.CurrentData != default && this.intrinsics != null);
            this.UpdateChildVisibility(this.cameraIntrinsicsVisual3D, this.Visible && this.intrinsics != null);
        }

        private void ComputeIntrinsics()
        {
            if (this.CurrentData.Item1 == null || this.CurrentData.Item1.Resource == null || this.CurrentData.Item2 == null)
            {
                this.intrinsics = null;
            }
            else
            {
                var width = this.CurrentData.Item1.Resource.Width;
                var height = this.CurrentData.Item1.Resource.Height;
                var transform = Matrix<double>.Build.Dense(3, 3);
                transform[0, 0] = 500;
                transform[1, 1] = 500;
                transform[2, 2] = 1;
                transform[0, 2] = width / 2.0;
                transform[1, 2] = height / 2.0;
                this.intrinsics = new CameraIntrinsics(width, height, transform);
            }
        }
    }
}
