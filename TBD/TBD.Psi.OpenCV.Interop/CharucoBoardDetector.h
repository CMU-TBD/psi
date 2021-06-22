#pragma once


#include <opencv2/core.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include <opencv2/aruco.hpp>
#include <opencv2/calib3d.hpp>
#include "ImageBuffer.h"
#include "CharucoBoard.h"
#include "Helper.h"

namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			public ref class CharucoBoardDetector
			{
			private:
				bool initialized_ = false;
				bool receiveCalibration_ = false;
				CharucoBoard^ board_;
				cv::Mat1d* distCoeffs_;
				cv::Mat1d* cameraMat_;
				cv::Mat* rvecPtr;
				cv::Mat* tvecPtr;
			public:

				CharucoBoardDetector(CharucoBoard^ board) {
					board_ = board;
					distCoeffs_ = new cv::Mat_<double>(8, 1);
					*distCoeffs_ << 0, 0, 0, 0, 0, 0, 0, 0;
					cameraMat_ = new cv::Mat_<double>(3, 3);
					rvecPtr = new cv::Mat_<double>(3, 1);
					tvecPtr = new cv::Mat_<double>(3, 1);
				}

				~CharucoBoardDetector() {
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

				array<double, 2>^ Detect(ImageBuffer^ grayImage, bool drawAxis);
			};
		}
	}
}
