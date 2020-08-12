#pragma once


#include <opencv2/core.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include <opencv2/aruco.hpp>
#include <opencv2/calib3d.hpp>
#include "ImageBuffer.h"
#include "ArucoBoard.h"

namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			public ref class ArucoBoardDetector
			{
			private:
				bool receiveCalibration_ = false;
				ArucoBoard^ board_;
				cv::Mat1d* distCoeffs_;
				cv::Mat1d* cameraMat_;
				cv::aruco::DetectorParameters* detectParameters_;
			public:

				ArucoBoardDetector(ArucoBoard^ board) {
					board_ = board;
					distCoeffs_ = new cv::Mat_<double>(8, 1);
					*distCoeffs_ << 0, 0, 0, 0, 0, 0, 0, 0;
					cameraMat_ = new cv::Mat_<double>(3, 3);
					detectParameters_ = new cv::aruco::DetectorParameters();
				}

				~ArucoBoardDetector() {
					delete distCoeffs_;
					delete cameraMat_;
					delete detectParameters_;
				}

				void SetCameraIntrinsics(array<double>^ intrinsics, array<double>^ radianCoefficients, array<double>^ tangentialCoefficients);
				void SetCameraIntrinsics(array<double>^ intrinsics);

				bool receviveCalbiration() {
					return receiveCalibration_;
				}

				array<double, 2>^ DetectArucoBoard(ImageBuffer^ grayImage);
			};
		}
	}
}
