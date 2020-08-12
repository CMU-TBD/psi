namespace TBD.Psi.Playground
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using System;
    using TBD.Psi.OpenCV;

    public static class OpenCVTest
    {
        public static ImageBuffer ToImageBuffer(this Shared<Image> source)
        {
            return new ImageBuffer(source.Resource.Width, source.Resource.Height, source.Resource.ImageData, source.Resource.Stride);
        }

        public static void Run(string[] args)
        {
            using (var p = Pipeline.Create(true))
            {
                var store = Store.Create(p, "test", @"C:\Data\Stores");

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    OutputColor = true,
                    OutputDepth = true,
                    DeviceIndex = 0,
                    BodyTrackerConfiguration =  new AzureKinectBodyTrackerConfiguration()
                });;

                var imgGray = k4a1.ColorImage.ToGray();
                imgGray.EncodeJpeg().Write("img", store);
                k4a1.DepthDeviceCalibrationInfo.Do(m =>
                {
                    Console.WriteLine(m);
                });
                var arucoBoard = new ArucoBoard(4, 6, 0.0355f, 0.007f, "h", 0);
                var arucoBoardDetector = new ArucoBoardDetector(arucoBoard);
                imgGray.Pair(k4a1.DepthDeviceCalibrationInfo).Select(m =>
                {
                    var (img, camInfo) = m;
                    if (!arucoBoardDetector.receviveCalbiration())
                    {
                        var intrinsicArr = new double[4];
                        intrinsicArr[0] = camInfo.ColorIntrinsics.FocalLengthXY.X;
                        intrinsicArr[1] = camInfo.ColorIntrinsics.FocalLengthXY.Y;
                        intrinsicArr[2] = camInfo.ColorIntrinsics.PrincipalPoint.X;
                        intrinsicArr[3] = camInfo.ColorIntrinsics.PrincipalPoint.Y;

                        arucoBoardDetector.SetCameraIntrinsics(intrinsicArr, camInfo.ColorIntrinsics.RadialDistortion.AsArray(), camInfo.ColorIntrinsics.TangentialDistortion.AsArray());
                    }
                    var output = arucoBoardDetector.DetectArucoBoard(img.ToImageBuffer());
                    if (output == null)
                    {
                        return new CoordinateSystem();
                    }
                    var cs_mat = Matrix<double>.Build.DenseOfArray(output);
                    return new CoordinateSystem(cs_mat);
                }, DeliveryPolicy.LatestMessage).Select(m =>
                {
                    if (m.Origin.DistanceTo(Point3D.Origin) == 0)
                    {
                        return m;
                    }
                    // OpenCV's coordinate system is Z-forward, X-right and Y-down. Change it to the Psi Standard format
                    var kinectBasis = new CoordinateSystem(default, UnitVector3D.ZAxis, UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis.Negate());
                    return new CoordinateSystem(kinectBasis.Transpose() * m);
                }).Write("cs", store);
                k4a1.Bodies.Write("body", store);

                /*
                .Process<Shared<Image>, int>(
            (srcImage, env, e) =>
            {
                // Our lambda here is called with each image sample from our stream and calls OpenCV to convert
                // the image into a grayscale image. We then post the resulting gray scale image to our event queue
                // so that the Psi pipeline will send it to the next component.

                // Have Psi allocate a new image. We will convert the current image ('srcImage') into this new image.
                using (var destImage = ImagePool.GetOrCreate(srcImage.Resource.Width, srcImage.Resource.Height, PixelFormat.Gray_8bpp))
                {
                    // Call into our OpenCV wrapper to convert the source image ('srcImage') into the newly created image ('destImage')
                    // Note: since srcImage & destImage are Shared<> we need to access the Microsoft.Psi.Imaging.Image data via the Resource member
                    var destImageBuffer = new ImageBuffer(destImage.Resource.Width, destImage.Resource.Height, destImage.Resource.ImageData, destImage.Resource.Stride);
                    OpenCVOperations.ToGray(srcImage.ToImageBuffer(), destImageBuffer);
                    var output = OpenCVOperations.DetectArucoBoard(destImageBuffer, arucoBoard);
                    e.Post(numTags, env.OriginatingTime);
                }
            }).Write("tags", store);*/
                p.Diagnostics.Write("diagnostic", store);
                p.RunAsync();
                Console.ReadLine();

/*
                mediaCapture.Out.Select(m =>
                {
                    // Have Psi allocate a new image. We will convert the current image ('srcImage') into this new image.
                    using (var destImage = ImagePool.GetOrCreate(m.Resource.Width, m.Resource.Height, PixelFormat.Gray_8bpp))
                    {
                        var imgBuffer = new ImageBuffer(m.Resource.Width, m.Resource.Height, m.Resource.ImageData, m.Resource.Stride);
                        var outBuffer = new ImageBuffer(destImage.Resource.Width, destImage.Resource.Height, destImage.Resource.ImageData, destImage.Resource.Stride);
                        OpenCV.OpenCVOperations.ToGray(imgBuffer, outBuffer);
                        return outBuffer;
                    }
                }).Write("output", store);*/
            }
        }
    }
}
