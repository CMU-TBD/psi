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
    /// Psi Component to Detect Charuco Board Using OpenCV.
    /// </summary>
    public class CharucoBoardDetect : IProducer<CoordinateSystem>
    {
        private Pipeline pipeline;
        private bool receiveCalibration = false;
        private CharucoBoardDetector detector;
        private IDepthDeviceCalibrationInfo calibration;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardDetector"/> class.
        /// </summary>
        /// <param name="p">Pipeline</param>
        /// <param name="markersX">Number of markers along X-axis.</param>
        /// <param name="markersY">Number of markers along Y-axis.</param>
        /// <param name="markerLength">Length of each markers (in meters).</param>
        /// <param name="markerSeperation">Length between each markers (in meters).</param>
        /// <param name="dictName">Name of Detectionary Used.</param>
        public CharucoBoardDetect(Pipeline p, int squareX, int squareY, float squareLength, float markerLength, ArucoDictionary dictName)
        {
            this.pipeline = p;
            this.Out = p.CreateEmitter<CoordinateSystem>(this, nameof(this.Out));
            this.DebugImageOut = p.CreateEmitter<Shared<Image>>(this, nameof(this.DebugImageOut));
            this.CalibrationIn = p.CreateReceiver<IDepthDeviceCalibrationInfo>(this, this.CalibrationCB, nameof(this.CalibrationIn));
            this.ImageIn = p.CreateReceiver<Shared<Image>>(this, this.ImageCB, nameof(this.ImageIn));
            this.detector = new CharucoBoardDetector(new CharucoBoard(squareX, squareY, squareLength, markerLength, dictName));
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

        /// <summary>
        /// Gets the debug image emitter.
        /// </summary>
        public Emitter<Shared<Image>> DebugImageOut { get; private set; }

        private void ImageCB(Shared<Image> img, Envelope env)
        {
            if (this.receiveCalibration)
            {
                var buffer = new ImageBuffer(img.Resource.Width, img.Resource.Height, img.Resource.ImageData, img.Resource.Stride);
                var mat = this.detector.Detect(buffer, this.DebugImageOut.HasSubscribers);
                if (mat != null)
                {
                    var cs_mat = Matrix<double>.Build.DenseOfArray(mat);
                    // OpenCV's coordinate system is Z-forward, X-right and Y-down. Change it to the Psi Standard format
                    var kinectBasis = new CoordinateSystem(default, UnitVector3D.ZAxis, UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis.Negate());
                    // We need to convert it from the color frame to the global frame that is useful for us.
                    var initial_solution = new CoordinateSystem(kinectBasis.Transpose() * cs_mat);
                    var transform = initial_solution.TransformBy(this.calibration.ColorPose);
                    this.Out.Post(transform, env.OriginatingTime);
                }
                // also post the debug image
                if (this.DebugImageOut.HasSubscribers)
                {
                    // we assume the lower level already update the image
                    this.DebugImageOut.Post(img, env.OriginatingTime);
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
