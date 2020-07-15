using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Playground.NETFramework
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Components;
    using OpenCvSharp;
    using OpenCvSharp.Aruco;

    public class OpenCVArucoDetector : IProducer<CoordinateSystem>
    {
        private Connector<DepthDeviceCalibrationInfo> depthDeviceCalibrationConnector;
        private Connector<CameraIntrinsics> cameraIntrinsicsConnector;
        private Connector<Shared<Image>> ImageInConnector;
        private Connector<List<int>> idsOutConnector;

        public OpenCVArucoDetector(Pipeline p)
        {
            this.Out = p.CreateEmitter<CoordinateSystem>(this, nameof(this.Out));
            this.idsOutConnector = p.CreateConnector<List<int>>(nameof(this.IdsOut));
            this.ImageInConnector = p.CreateConnector<Shared<Image>>(nameof(this.ImageInConnector));
            this.depthDeviceCalibrationConnector = p.CreateConnector<DepthDeviceCalibrationInfo>(nameof(this.depthDeviceCalibrationConnector));
            this.cameraIntrinsicsConnector = p.CreateConnector<CameraIntrinsics>(nameof(this.cameraIntrinsicsConnector));

            var detectorParameters = DetectorParameters.Create();
            var dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict5X5_100);



            var grayscaleImg = this.ImageInConnector.ToPixelFormat(PixelFormat.Gray_8bpp);
            var markerDetection = grayscaleImg.Select(m =>
            {
                // TODO make this a shared.
                var mat = new Mat(m.Resource.Height, m.Resource.Width, MatType.MakeType(MatType.CV_8U, m.Resource.Stride / m.Resource.Width), m.Resource.ImageData);
                CvAruco.DetectMarkers(mat, dictionary, out var corners, out var ids, detectorParameters, out var rejectedPoints);
                return (ids, corners);
            }, DeliveryPolicy.LatestMessage);

            grayscaleImg.Do(m =>
            {
                var mat = new Mat(m.Resource.Height, m.Resource.Width, MatType.MakeType(MatType.CV_8U, m.Resource.Stride / m.Resource.Width), m.Resource.ImageData);
                mat.SaveImage("test.png");
            });

            markerDetection.Select(m =>
            {
                return m.ids.ToList();
            }).PipeTo(this.idsOutConnector);
        }

        public Emitter<CoordinateSystem> Out { private set; get; }
        public Emitter<List<int>> IdsOut => this.idsOutConnector.Out;

        public Receiver<Shared<Image>> ImageIn => this.ImageInConnector.In;

        public Receiver<DepthDeviceCalibrationInfo> DepthDeviceCalibrationIn => this.depthDeviceCalibrationConnector.In;

        public Receiver<CameraIntrinsics> CameraIntrinsicsIn => this.cameraIntrinsicsConnector.In;

    }
}
