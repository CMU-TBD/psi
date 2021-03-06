#include "CharucoBoardDetector.h"
#include <iostream>

namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			void CharucoBoardDetector::SetCameraIntrinsics(array<double>^ intrinsics, array<double>^ radial, array<double>^ tangent)
			{
				if (radial->Length > 2)
				{
					*distCoeffs_ << radial[0], radial[1], tangent[0], tangent[1], radial[2], radial[3], radial[4], radial[5];
				}
				else
				{
					*distCoeffs_ << radial[0], radial[1], tangent[0], tangent[1];
				}
				SetCameraIntrinsics(intrinsics);
			}

			void CharucoBoardDetector::SetCameraIntrinsics(array<double>^ intrinsics)
			{
				*cameraMat_ << intrinsics[0], 0, intrinsics[2],
					0, intrinsics[1], intrinsics[3],
					0, 0, 1;
				receiveCalibration_ = true;
			}

			array<double, 2>^ CharucoBoardDetector::Detect(ImageBuffer^ grayImage, bool drawAxis)
			{
				// create temporary variables
				std::vector<int> ids;
				std::vector<std::vector<cv::Point2f> > corners;
				std::vector<std::vector<cv::Point2f> > rejectedCorners;

				cv::Mat img = WrapInMat(grayImage);
				cv::Ptr<cv::aruco::CharucoBoard> cvBoard = board_->getBoard();

				// Detect and refine the image
				// TODO figure out a way to avoid creating a pointer again
				cv::aruco::detectMarkers(img, cvBoard->dictionary, corners, ids, cv::aruco::DetectorParameters::create(), rejectedCorners);
				// cv::aruco::refineDetectedMarkers(img, cvBoard, corners, ids, rejectedCorners);
				// if we can see the whole board & all the points
				if (drawAxis)
				{
					cv::aruco::drawDetectedMarkers(img, corners, ids);
				}

				if (ids.size() > 0) {
					cv::Mat obj_points, img_points;
					std::vector<cv::Point2f> ccorners;
					std::vector<int> cids;
					cv::aruco::interpolateCornersCharuco(corners, ids, img, cvBoard, ccorners, cids, *cameraMat_, *distCoeffs_);
					if (cids.size() > 0)
					{
						if (cv::aruco::estimatePoseCharucoBoard(ccorners, cids, cvBoard, *cameraMat_, *distCoeffs_, *rvecPtr, *tvecPtr, false))
						{
							initialized_ = true;
							// Convert tvec and rvec to a transformation matrix
							double p1 = tvecPtr->at<double>(0);
							double p2 = tvecPtr->at<double>(1);
							double p3 = tvecPtr->at<double>(2);
							cv::Mat rot;
							cv::Rodrigues(*rvecPtr, rot);
							if (drawAxis)
							{
								cv::aruco::drawAxis(img, *cameraMat_, *distCoeffs_, *rvecPtr, *tvecPtr, 0.1);
							}
							return gcnew array<double, 2>(4, 4)
							{
								{ rot.at<double>(0), rot.at<double>(1), rot.at<double>(2), tvecPtr->at<double>(0)},
								{ rot.at<double>(3), rot.at<double>(4), rot.at<double>(5), tvecPtr->at<double>(1) },
								{ rot.at<double>(6), rot.at<double>(7), rot.at<double>(8), tvecPtr->at<double>(2) },
								{ 0, 0, 0, 1 },
							};
						}
					}
				}
				else {
					return nullptr;
				}
			}
		}
	}
}
