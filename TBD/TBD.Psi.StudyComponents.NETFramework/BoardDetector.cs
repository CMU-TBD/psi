// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.StudyComponents
{
    using System;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.OpenCV;

    /// <summary>
    /// Psi Component to Detect AruCo Board Using OpenCV.
    /// </summary>
    internal class BoardDetector : IProducer<CoordinateSystem>
    {
        private Pipeline pipeline;
        private bool receiveCalibration = false;
        private ArucoBoardDetector detector;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardDetector"/> class.
        /// </summary>
        /// <param name="p">Pipeline</param>
        /// <param name="markersX">Number of markers along X-axis.</param>
        /// <param name="markersY">Number of markers along Y-axis.</param>
        /// <param name="markerLength">Length of each markers (in meters).</param>
        /// <param name="markerSeperation">Length between each markers (in meters).</param>
        /// <param name="dictName">Name of Detectionary Used.</param>
        /// <param name="firstMarker">Index of first marker.</param>
        public BoardDetector(Pipeline p, int markersX, int markersY, float markerLength, float markerSeperation, ArucoDictionary dictName, int firstMarker = 0)
        {
            this.pipeline = p;
            this.Out = p.CreateEmitter<CoordinateSystem>(this, nameof(this.Out));
            this.CalibrationIn = p.CreateReceiver<IDepthDeviceCalibrationInfo>(this, this.CalibrationCB, nameof(this.CalibrationIn));
            this.ImageIn = p.CreateReceiver<Shared<Image>>(this, this.ImageCB, nameof(this.ImageIn));
            this.detector = new ArucoBoardDetector(new ArucoBoard(markersX, markersY, markerLength, markerSeperation, dictName, firstMarker));
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
        public Emitter<CoordinateSystem> Out { get; private set; }

        private void ImageCB(Shared<Image> img, Envelope env)
        {
            if (this.receiveCalibration)
            {
                var buffer = new ImageBuffer(img.Resource.Width, img.Resource.Height, img.Resource.ImageData, img.Resource.Stride);
                var mat = this.detector.DetectArucoBoard(buffer);
                if (mat != null)
                {
                    var cs_mat = Matrix<double>.Build.DenseOfArray(mat);
                    // OpenCV's coordinate system is Z-forward, X-right and Y-down. Change it to the Psi Standard format
                    var kinectBasis = new CoordinateSystem(default, UnitVector3D.ZAxis, UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis.Negate());
                    this.Out.Post(new CoordinateSystem(kinectBasis.Transpose() * cs_mat), env.OriginatingTime);
                }
            }

        }

        public void AddCalibrationInfo(IDepthDeviceCalibrationInfo info)
        {
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
