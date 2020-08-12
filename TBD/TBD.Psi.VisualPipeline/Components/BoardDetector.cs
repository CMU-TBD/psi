namespace TBD.Psi.VisualPipeline.Components
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using TBD.Psi.OpenCV;
    using Microsoft.Psi.Imaging;
    using MathNet.Numerics.LinearAlgebra;

    class BoardDetector : IProducer<CoordinateSystem>
    {
        private bool receiveCalibration = false;
        private ArucoBoardDetector detector;

        public BoardDetector(Pipeline p, int markersX, int markersY, float markerLength, float markerSeperation, string dictName, int firstMarker = 0)
        {
            this.Out = p.CreateEmitter<CoordinateSystem>(this, nameof(this.Out));
            this.CalibrationIn = p.CreateReceiver<IDepthDeviceCalibrationInfo>(this, this.calibrationCB, nameof(this.CalibrationIn));
            this.ImageIn = p.CreateReceiver<Shared<Image>>(this, this.ImageCB, nameof(this.ImageIn));
            this.detector = new ArucoBoardDetector(new ArucoBoard(markersX, markersY, markerLength, markerSeperation, dictName, firstMarker));
        }

        private void ImageCB(Shared<Image> img, Envelope env)
        {
            if (this.receiveCalibration)
            {
                var buffer = new ImageBuffer(img.Resource.Width, img.Resource.Height, img.Resource.ImageData, img.Resource.Stride);
                var mat = detector.DetectArucoBoard(buffer);
                if (mat != null)
                {
                    var cs_mat = Matrix<double>.Build.DenseOfArray(mat);
                    // OpenCV's coordinate system is Z-forward, X-right and Y-down. Change it to the Psi Standard format
                    var kinectBasis = new CoordinateSystem(default, UnitVector3D.ZAxis, UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis.Negate());
                    this.Out.Post(new CoordinateSystem(kinectBasis.Transpose() * cs_mat), env.OriginatingTime);
                }
            }
        }

        private void calibrationCB(IDepthDeviceCalibrationInfo info, Envelope env)
        {
            var intrinsicArr = new double[4];
            intrinsicArr[0] = info.ColorIntrinsics.FocalLengthXY.X;
            intrinsicArr[1] = info.ColorIntrinsics.FocalLengthXY.Y;
            intrinsicArr[2] = info.ColorIntrinsics.PrincipalPoint.X;
            intrinsicArr[3] = info.ColorIntrinsics.PrincipalPoint.Y;
            this.detector.SetCameraIntrinsics(intrinsicArr, info.ColorIntrinsics.RadialDistortion.AsArray(), info.ColorIntrinsics.TangentialDistortion.AsArray());
            receiveCalibration = true;
        }

        public Receiver<IDepthDeviceCalibrationInfo> CalibrationIn { get; private set; }
        public Receiver<Shared<Image>> ImageIn { get; private set; }

        public Emitter<CoordinateSystem> Out { get; private set; }
    }
}
