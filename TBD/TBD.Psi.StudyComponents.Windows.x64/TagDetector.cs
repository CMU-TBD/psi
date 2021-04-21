// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.StudyComponents
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.OpenCV;

    /// <summary>
    /// Psi Component to Detect AruCo tags Using OpenCV.
    /// </summary>
    public class TagDetector : IProducer<List<(int, CoordinateSystem)>>
    {
        private Pipeline pipeline;
        private bool receiveCalibration = false;
        private ArucoDetector detector;
        private IDepthDeviceCalibrationInfo calibration;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardDetector"/> class.
        /// </summary>
        /// <param name="p">Pipeline</param>
        /// <param name="markerLength">Length of each markers (in meters).</param>
        /// <param name="dictName">Name of Detectionary Used.</param>
        public TagDetector(Pipeline p, float markerLength, ArucoDictionary dictName)
        {
            this.pipeline = p;
            this.Out = p.CreateEmitter<List<(int, CoordinateSystem)>>(this, nameof(this.Out));
            this.CalibrationIn = p.CreateReceiver<IDepthDeviceCalibrationInfo>(this, this.CalibrationCB, nameof(this.CalibrationIn));
            this.ImageIn = p.CreateReceiver<Shared<Image>>(this, this.ImageCB, nameof(this.ImageIn));
            this.detector = new ArucoDetector(dictName, markerLength);
        }

        /// <summary>
        /// Gets Calibration Receiver.
        /// </summary>
        public Receiver<IDepthDeviceCalibrationInfo> CalibrationIn { get; private set; }

        /// <summary>
        /// Gets Image Receiver.
        /// </summary>
        public Receiver<Shared<Image>> ImageIn { get; private set; }

        /// <summary>
        /// Gets Board Position Emitter.
        /// </summary>
        public Emitter<List<(int, CoordinateSystem)>> Out { get; private set; }

        private void ImageCB(Shared<Image> img, Envelope env)
        {
            if (this.receiveCalibration)
            {
                var buffer = new ImageBuffer(img.Resource.Width, img.Resource.Height, img.Resource.ImageData, img.Resource.Stride);
                var markers = this.detector.DetectArucoMarkers(buffer);
                if (markers.Count > 0)
                {
                    var outputs = markers.Select(marker =>
                    {
                        var mat = Matrix<double>.Build.DenseOfArray(marker.Item2);
                        var kinectBasis = new CoordinateSystem(default, UnitVector3D.ZAxis, UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis.Negate());
                        // We need to convert it from the color frame to the global frame that is useful for us.
                        var initial_solution = new CoordinateSystem(kinectBasis.Transpose() * mat);
                        return (marker.Item1, initial_solution.TransformBy(this.calibration.ColorPose));
                    });
                    this.Out.Post(outputs.ToList(), env.OriginatingTime);
                }
            }
        }

        public void AddCalibrationInfo(IDepthDeviceCalibrationInfo info)
        {
            this.calibration = info;
            var intrinsicArr = new double[4];
            intrinsicArr[0] = info.ColorIntrinsics.FocalLengthXY.X;
            intrinsicArr[1] = info.ColorIntrinsics.FocalLengthXY.Y;
            intrinsicArr[2] = info.ColorIntrinsics.PrincipalPoint.X;
            intrinsicArr[3] = info.ColorIntrinsics.PrincipalPoint.Y;
            this.detector.SetCameraIntrinsics(intrinsicArr, info.ColorIntrinsics.RadialDistortion.AsArray(), info.ColorIntrinsics.TangentialDistortion.AsArray());
            this.receiveCalibration = true;
        }
         
        private void CalibrationCB(IDepthDeviceCalibrationInfo info, Envelope env)
        {
            this.AddCalibrationInfo(info);
        }

    }
}
