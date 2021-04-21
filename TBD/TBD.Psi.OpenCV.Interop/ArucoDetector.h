#pragma once

#include <opencv2/core.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include <opencv2/aruco.hpp>
#include <opencv2/calib3d.hpp>
#include "ImageBuffer.h"
#include "ArucoBoard.h"
using namespace System;
using namespace System::Collections::Generic;


namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			public ref class ArucoDetector
			{
			private:
				bool receiveCalibration_ = false;
				cv::Mat1d* distCoeffs_;
				cv::Mat1d* cameraMat_;
				cv::Mat* rvecPtr;
				cv::Mat* tvecPtr;
				ArucoDictionary dictionary_;
				float markerLength_;
			public:

				ArucoDetector(ArucoDictionary dictionary, float markerLength) {
					dictionary_ = dictionary;
					markerLength_ = markerLength;
					distCoeffs_ = new cv::Mat_<double>(8, 1);
					*distCoeffs_ << 0, 0, 0, 0, 0, 0, 0, 0;
					cameraMat_ = new cv::Mat_<double>(3, 3);
					rvecPtr = new cv::Mat_<double>(3, 1);
					tvecPtr = new cv::Mat_<double>(3, 1);
				}

				~ArucoDetector() {
					delete distCoeffs_;
					delete cameraMat_;
					delete rvecPtr;
					delete tvecPtr;
				}

				void SetCameraIntrinsics(array<double>^ intrinsics, array<double>^ radianCoefficients, array<double>^ tangentialCoefficients);
				void SetCameraIntrinsics(array<double>^ intrinsics);

				bool receviveCalbiration() {
					return receiveCalibration_;
				}

				List<Tuple<int, array<double, 2>^>^>^ DetectArucoMarkers(ImageBuffer^ grayImage);
			};
		}
	}
}
