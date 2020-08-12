#include "ArucoBoardDetector.h"
#include <iostream>

namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			// helper function
			static cv::Mat WrapInMat(ImageBuffer^ img)
			{
				cv::Mat ret = cv::Mat(img->Height, img->Width, CV_MAKETYPE(CV_8U, img->Stride / img->Width), (void*)img->Data, cv::Mat::AUTO_STEP);
				return ret;
			}

			void ArucoBoardDetector::SetCameraIntrinsics(array<double>^ intrinsics, array<double>^ radial, array<double>^ tangent)
			{
				*distCoeffs_ << radial[0], radial[1], tangent[0], tangent[1], radial[2], radial[3], radial[4], radial[5];
				SetCameraIntrinsics(intrinsics);
			}

			void ArucoBoardDetector::SetCameraIntrinsics(array<double>^ intrinsics)
			{
				*cameraMat_ << intrinsics[0], 0, intrinsics[2],
					0, intrinsics[1], intrinsics[3],
					0, 0, 1;
				receiveCalibration_ = true;
			}

			array<double, 2>^ ArucoBoardDetector::DetectArucoBoard(ImageBuffer^ grayImage)
			{
				// create temporary variables
				std::vector<int> ids;
				std::vector<std::vector<cv::Point2f> > corners;
				std::vector<std::vector<cv::Point2f> > rejectedCorners;

				cv::Mat img = WrapInMat(grayImage);
				cv::Ptr<cv::aruco::GridBoard> cvBoard = board_->getBoard();

				// Detect and refine the image
				// TODO figure out a way to avoid creating a pointer again
				cv::aruco::detectMarkers(img, cvBoard->dictionary, corners, ids, cv::aruco::DetectorParameters::create(), rejectedCorners);
				cv::aruco::refineDetectedMarkers(img, cvBoard, corners, ids, rejectedCorners);

				// if there is more than 3 detected markers
				if (ids.size() > 3) {
					cv::Mat rvec, tvec;
					cv::Mat obj_points, img_points;
					// get the image points out
					cv::aruco::getBoardObjectAndImagePoints(cvBoard, corners, ids, obj_points, img_points);
					// try to solve the pnp problem
					cv::solvePnP(obj_points, img_points, *cameraMat_, *distCoeffs_, rvec, tvec, false);
					// Convert tvec and rvec to a transformation matrix
					cv::Mat rot;
					cv::Rodrigues(rvec, rot);
					return gcnew array<double, 2>(4, 4)
					{
						{ rot.at<double>(0), rot.at<double>(1), rot.at<double>(2), tvec.at<double>(0)},
						{ rot.at<double>(3), rot.at<double>(4), rot.at<double>(5), tvec.at<double>(1) },
						{ rot.at<double>(6), rot.at<double>(7), rot.at<double>(8), tvec.at<double>(2) },
						{ 0, 0, 0, 1 },
					};
				}
				else {
					return nullptr;
				}
			}
		}
	}
}
