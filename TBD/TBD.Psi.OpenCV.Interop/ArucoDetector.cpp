#include "ArucoDetector.h"
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

			void ArucoDetector::SetCameraIntrinsics(array<double>^ intrinsics, array<double>^ radial, array<double>^ tangent)
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

			void ArucoDetector::SetCameraIntrinsics(array<double>^ intrinsics)
			{
				*cameraMat_ << intrinsics[0], 0, intrinsics[2],
					0, intrinsics[1], intrinsics[3],
					0, 0, 1;
				receiveCalibration_ = true;
			}

			List<Tuple<int, array<double, 2>^>^>^ ArucoDetector::DetectArucoMarkers(ImageBuffer^ grayImage)
			{
				//// create temporary variables
				std::vector<int> ids;
				std::vector<std::vector<cv::Point2f> > corners;
				std::vector<std::vector<cv::Point2f> > rejectedCorners;
				std::vector<cv::Vec3d> rvecs, tvecs;

				cv::Mat img = WrapInMat(grayImage);
				// detect markers
				// TODO figure out how to generate the predefined dictionary at an earlier point
				cv::aruco::detectMarkers(img, cv::aruco::getPredefinedDictionary((int)dictionary_), corners, ids, cv::aruco::DetectorParameters::create(), rejectedCorners);
				// estiamte poses
				cv::aruco::estimatePoseSingleMarkers(corners, markerLength_, *cameraMat_, *distCoeffs_, rvecs, tvecs);
				List<Tuple<int, array<double, 2>^>^>^ poseList = gcnew List<Tuple<int, array<double, 2>^>^>(ids.size());
				// convert points to list
				for (int i = 0; i < ids.size(); i++)
				{
					cv::Mat rot;
					cv::Rodrigues(rvecs[i], rot);

					array<double, 2>^ mat = gcnew array<double, 2>(4, 4)
					{
						{ rot.at<double>(0), rot.at<double>(1), rot.at<double>(2), tvecs[i][0] },
						{ rot.at<double>(3), rot.at<double>(4), rot.at<double>(5), tvecs[i][1] },
						{ rot.at<double>(6), rot.at<double>(7), rot.at<double>(8), tvecs[i][2] },
						{ 0, 0, 0, 1 },
					};
					poseList->Add(gcnew Tuple<int, array<double, 2>^>(ids[i], mat));
				}
				return poseList;
			}
		}
	}
}
