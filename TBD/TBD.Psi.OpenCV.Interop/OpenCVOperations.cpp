// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/core/cvdef.h>
#include <msclr/marshal_cppstd.h>
#include <opencv2/aruco.hpp>
#include <opencv2/calib3d.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include "ImageBuffer.h"
#include "ArucoBoard.h"

namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			public ref class OpenCVOperations
			{
				// helper function
				static cv::Mat WrapInMat(ImageBuffer^ img)
				{
					cv::Mat ret = cv::Mat(img->Height, img->Width, CV_MAKETYPE(CV_8U, img->Stride / img->Width), (void*)img->Data, cv::Mat::AUTO_STEP);
					return ret;
				}

				static cv::Mat IntrinsicArrayToMat(array<double>^ instrinsicArr)
				{
					cv::Mat_<double> mat(3, 3);
					mat << instrinsicArr[0], 0, instrinsicArr[2],
						0, instrinsicArr[1], instrinsicArr[3],
						0, 0, 1;
					return mat;
				}

			public:
				static ImageBuffer^ ToGray(ImageBuffer^ colorImage, ImageBuffer^ grayImage)
				{
					cv::Mat greyMat = WrapInMat(grayImage);
					cv::Mat colorMat = WrapInMat(colorImage);
					cv::cvtColor(colorMat, greyMat, cv::COLOR_BGR2GRAY);
					return grayImage;
				}

				static void SaveImage(ImageBuffer^ img, System::String^ filename)
				{
					std::string fn = msclr::interop::marshal_as<std::string>(filename);
					cv::Mat matImg = WrapInMat(img);
					cv::imwrite(fn, matImg);
				}

				static int DetectArucoMarker(ImageBuffer^ grayImage, System::String^ dictionary_name)
				{
					cv::Mat matImg = WrapInMat(grayImage);
					cv::Ptr<cv::aruco::Dictionary> dictionary = cv::aruco::getPredefinedDictionary(cv::aruco::DICT_5X5_100);
					std::vector<int> ids;
					std::vector<std::vector<cv::Point2f> > corners;
					cv::aruco::detectMarkers(matImg, dictionary, corners, ids);
					return ids.size();
				}
				static array<double, 2>^ DetectArucoBoard(ImageBuffer^ grayImage, ArucoBoard^ board, array<double>^ intrinsicArr) {
					auto distortionArr = gcnew array<double>(8){0,0,0,0,0,0,0,0};
					return DetectArucoBoard(grayImage, board, intrinsicArr, distortionArr);
				}

				static array<double, 2>^ DetectArucoBoard(ImageBuffer^ grayImage, ArucoBoard^ board, array<double>^ intrinsicArr, array<double>^ distortionArr) {

					cv::Mat intrinsicMat = IntrinsicArrayToMat(intrinsicArr);
					cv::Mat matImg = WrapInMat(grayImage);
					std::vector<int> ids;
					std::vector<std::vector<cv::Point2f> > corners;
					std::vector<std::vector<cv::Point2f> > rejectedCorners;
					cv::Ptr<cv::aruco::GridBoard> cvBoard = board->getBoard();
					// Copy in the distortion matrix
					cv::Mat_<double> distortionMat(8, 1);
					distortionMat << distortionArr[0], distortionArr[1],
					distortionArr[2], distortionArr[3], distortionArr[4],
					distortionArr[5], distortionArr[7], distortionArr[7];


					// Detect and refine the image
					cv::aruco::detectMarkers(matImg, cvBoard->dictionary, corners, ids, cv::aruco::DetectorParameters::create(), rejectedCorners);
					cv::aruco::refineDetectedMarkers(matImg, cvBoard, corners, ids, rejectedCorners);

					// if there is more than 3 detected markers

					if (ids.size() > 3) {
						cv::Mat rvec, tvec;
						cv::Mat obj_points, img_points;
						// get the image points out
						cv::aruco::getBoardObjectAndImagePoints(cvBoard, corners, ids, obj_points, img_points);
						// try to solve the pnp problem
						cv::solvePnP(obj_points, img_points, intrinsicMat, distortionMat, rvec, tvec, false);
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

			};
		}
	}
}
