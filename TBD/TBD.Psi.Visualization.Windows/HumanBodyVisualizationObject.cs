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
    using TBD.Psi.VisionComponents;

    [VisualizationObject("Human Body")]
    public class HumanBodyVisualizationObject : ModelVisual3DVisualizationObject<HumanBody>
    {
        private readonly BillboardTextVisual3D billboard;
        private readonly ArrowVisual3D gazeDirection;
        private readonly ArrowVisual3D torsoDirection;
        private readonly UpdatableVisual3DDictionary<JointId, SphereVisual3D> visualJoints;
        private readonly UpdatableVisual3DDictionary<(JointId ChildJoint, JointId ParentJoint), PipeVisual3D> visualBones;

        private Color color = Colors.White;
        private double inferredJointsOpacity = 30;
        private double boneDiameterMm = 20;
        private double jointRadiusMm = 15;
        private bool showBillboard = false;
        private bool showGazeDirection = false;
        private bool showTorsoDirection = false;
        private int polygonResolution = 6;
        private double billboardHeightCm = 100;
        private double directionRayLength = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKinectBodyVisualizationObject"/> class.
        /// </summary>
        public HumanBodyVisualizationObject()
        {
            this.visualJoints = new UpdatableVisual3DDictionary<JointId, SphereVisual3D>(null);
            this.visualBones = new UpdatableVisual3DDictionary<(JointId ChildJoint, JointId ParentJoint), PipeVisual3D>(null);

            this.billboard = new BillboardTextVisual3D()
            {
                Background = Brushes.Gray,
                Foreground = new SolidColorBrush(Colors.White),
                Padding = new Thickness(5),
            };

            this.gazeDirection = new ArrowVisual3D() { Fill = new SolidColorBrush(Colors.HotPink), Diameter = 0.05};
            this.torsoDirection = new ArrowVisual3D() { Fill = new SolidColorBrush(Colors.DeepPink), Diameter = 0.05};

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [Description("Color of the body.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the inferred joints opacity.
        /// </summary>
        [DataMember]
        [Description("Opacity for rendering inferred joints and bones.")]
        public double InferredJointsOpacity
        {
            get { return this.inferredJointsOpacity; }
            set { this.Set(nameof(this.InferredJointsOpacity), ref this.inferredJointsOpacity, value); }
        }

        /// <summary>
        /// Gets or sets the bone diameter.
        /// </summary>
        [DataMember]
        [DisplayName("Bone diameter (mm)")]
        [Description("Diameter of bones (mm).")]
        public double BoneDiameterMm
        {
            get { return this.boneDiameterMm; }
            set { this.Set(nameof(this.BoneDiameterMm), ref this.boneDiameterMm, value); }
        }

        /// <summary>
        /// Gets or sets the joint diameter.
        /// </summary>
        [DataMember]
        [DisplayName("Joint radius (mm)")]
        [Description("Radius of joints (mm).")]
        public double JointRadiusMm
        {
            get { return this.jointRadiusMm; }
            set { this.Set(nameof(this.JointRadiusMm), ref this.jointRadiusMm, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show a billboard with information about the body.
        /// </summary>
        [DataMember]
        [PropertyOrder(0)]
        [Description("Show a billboard with information about the body.")]
        public bool ShowBillboard
        {
            get { return this.showBillboard; }
            set { this.Set(nameof(this.ShowBillboard), ref this.showBillboard, value); }
        }

        /// <summary>
        /// Gets or sets the height at which to draw the billboard (cm).
        /// </summary>
        [DataMember]
        [PropertyOrder(1)]
        [DisplayName("Billboard Height (cm)")]
        [Description("Height at which to draw the billboard (cm).")]
        public double BillboardHeightCm
        {
            get { return this.billboardHeightCm; }
            set { this.Set(nameof(this.BillboardHeightCm), ref this.billboardHeightCm, value); }
        }

        [DataMember]
        [PropertyOrder(2)]
        [Description("Show direction of gaze")]
        public bool ShowGazeDirection
        {
            get { return this.showGazeDirection; }
            set { this.Set(nameof(this.ShowGazeDirection), ref this.showGazeDirection, value); }
        }

        [DataMember]
        [PropertyOrder(3)]
        [Description("Show direction of torso")]
        public bool ShowTorsoDirection
        {
            get { return this.showTorsoDirection; }
            set { this.Set(nameof(this.ShowTorsoDirection), ref this.showTorsoDirection, value); }
        }

        [DataMember]
        [PropertyOrder(4)]
        [Description("Length of Ray (cm)")]
        public double DirectionRayLength
        {
            get { return this.directionRayLength; }
            set { this.Set(nameof(this.DirectionRayLength), ref this.directionRayLength, value); }
        }

        /// <summary>
        /// Gets or sets the number of divisions to use when rendering polygons for joints and bones.
        /// </summary>
        [DataMember]
        [Description("Level of resolution at which to render joint and bone polygons (minimum value is 3).")]
        public int PolygonResolution
        {
            get { return this.polygonResolution; }
            set { this.Set(nameof(this.PolygonResolution), ref this.polygonResolution, value < 3 ? 3 : value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null)
            {
                this.UpdateVisuals();
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Color) ||
                propertyName == nameof(this.InferredJointsOpacity) ||
                propertyName == nameof(this.BoneDiameterMm) ||
                propertyName == nameof(this.JointRadiusMm) ||
                propertyName == nameof(this.PolygonResolution))
            {
                this.UpdateVisuals();
            }
            else if (propertyName == nameof(this.ShowBillboard))
            {
                this.UpdateBillboardVisibility();
            }
            else if (propertyName == nameof(this.BillboardHeightCm))
            {
                this.UpdateBillboard();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
            else if (propertyName == nameof(this.ShowGazeDirection))
            {
                this.UpdateRayVisibility();
            }
            else if (propertyName == nameof(this.ShowTorsoDirection))
            {
                this.UpdateRayVisibility();
            }
            else if (propertyName == nameof(this.DirectionRayLength))
            {
                this.UpdateRays();
            }
        }

        private void UpdateVisuals()
        {
            this.visualJoints.BeginUpdate();
            this.visualBones.BeginUpdate();

            if (this.CurrentData != null)
            {
                var trackedEntitiesBrush = new SolidColorBrush(this.Color);
                var untrackedEntitiesBrush = new SolidColorBrush(
                    Color.FromArgb(
                        (byte)(Math.Max(0, Math.Min(100, this.InferredJointsOpacity)) * 2.55),
                        this.Color.R,
                        this.Color.G,
                        this.Color.B));

                // update the joints
                var jointPoses = this.CurrentData.GetJointPoses().ToDictionary(x => x.Id, x => x.Pose);
                var jointIds = jointPoses.Keys;

                foreach (var jointId in Enum.GetValues(typeof(JointId)))
                {
                    var visualJoint = this.visualJoints[(JointId)jointId];
                    visualJoint.BeginEdit();
                    if (jointIds.Contains((JointId)jointId))
                    {
                        var jointPose = jointPoses[(JointId)jointId];
                        // update the joints
                        var jointPosition = jointPose.Origin;

                        if (visualJoint.Radius != this.JointRadiusMm / 1000.0)
                        {
                            visualJoint.Radius = this.JointRadiusMm / 1000.0;
                        }

                        visualJoint.Fill = trackedEntitiesBrush;
                        visualJoint.Transform = new Win3D.TranslateTransform3D(jointPosition.X, jointPosition.Y, jointPosition.Z);
                        visualJoint.PhiDiv = this.PolygonResolution;
                        visualJoint.ThetaDiv = this.PolygonResolution;

                        visualJoint.Visible = true;
                    }
                    else
                    {
                        visualJoint.Visible = false;
                    }
                    visualJoint.EndEdit();
                }
             
                // update the bones
                foreach (var bone in AzureKinectBody.Bones)
                {
                    var visualBone = this.visualBones[bone];
                    visualBone.BeginEdit();
                    // check if both parent and child exists in the list
                    if (jointIds.Contains(bone.ParentJoint) && jointIds.Contains(bone.ChildJoint))
                    {
                        if (visualBone.Diameter != this.BoneDiameterMm / 1000.0)
                        {
                            visualBone.Diameter = this.BoneDiameterMm / 1000.0;
                        }
                        var joint1Position = this.visualJoints[bone.ParentJoint].Transform.Value;
                        var joint2Position = this.visualJoints[bone.ChildJoint].Transform.Value;

                        visualBone.Point1 = new Win3D.Point3D(joint1Position.OffsetX, joint1Position.OffsetY, joint1Position.OffsetZ);
                        visualBone.Point2 = new Win3D.Point3D(joint2Position.OffsetX, joint2Position.OffsetY, joint2Position.OffsetZ);

                        visualBone.Fill = trackedEntitiesBrush;
                        
                        visualBone.ThetaDiv = this.PolygonResolution;

                        visualBone.Visible = true;
                    }
                    else
                    {
                        visualBone.Visible = false;
                    }
                    visualBone.EndEdit();
                }

                // set billboard position
                this.UpdateBillboard();
                
                // change the rays of the person
                this.UpdateRays();
            }

            this.visualJoints.EndUpdate();
            this.visualBones.EndUpdate();
        }

        private void UpdateRays()
        {
            if (this.CurrentData != null && this.CurrentData.GazeDirection.HasValue)
            {
                var ray = this.CurrentData.GazeDirection.Value;
                this.gazeDirection.Point1 = new Win3D.Point3D(ray.ThroughPoint.X, ray.ThroughPoint.Y, ray.ThroughPoint.Z);
                this.gazeDirection.Point2 = new Win3D.Point3D(ray.ThroughPoint.X + ray.Direction.X * this.directionRayLength, ray.ThroughPoint.Y + ray.Direction.Y * this.directionRayLength, ray.ThroughPoint.Z + ray.Direction.Z * this.directionRayLength);
            }
            
            if (this.CurrentData != null && this.CurrentData.TorsoDirection.HasValue)
            {
                var ray = this.CurrentData.TorsoDirection.Value;
                this.torsoDirection.Point1 = new Win3D.Point3D(ray.ThroughPoint.X, ray.ThroughPoint.Y, ray.ThroughPoint.Z);
                this.torsoDirection.Point2 = new Win3D.Point3D(ray.ThroughPoint.X + ray.Direction.X * this.directionRayLength, ray.ThroughPoint.Y + ray.Direction.Y * this.directionRayLength, ray.ThroughPoint.Z + ray.Direction.Z * this.directionRayLength);

            }
        }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.RootPose.Origin;
                this.billboard.Position = new Win3D.Point3D(origin.X, origin.Y, origin.Z + (this.BillboardHeightCm / 100.0));
                this.billboard.Text = this.CurrentData.ToString();
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.visualJoints, this.Visible && this.CurrentData != default);
            this.UpdateChildVisibility(this.visualBones, this.Visible && this.CurrentData != default);
            this.UpdateBillboardVisibility();
            this.UpdateRayVisibility();
        }

        private void UpdateRayVisibility()
        {
            this.UpdateChildVisibility(this.gazeDirection, this.Visible && this.ShowGazeDirection);
            this.UpdateChildVisibility(this.torsoDirection, this.Visible && this.ShowTorsoDirection);
        }

        private void UpdateBillboardVisibility()
        {
            this.UpdateChildVisibility(this.billboard, this.Visible && this.CurrentData != default && this.ShowBillboard);
        }
    }
}
