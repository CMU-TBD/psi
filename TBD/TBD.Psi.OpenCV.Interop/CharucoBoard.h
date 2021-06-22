#pragma once


#include <opencv2/core.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include <opencv2/aruco.hpp>
#include <opencv2/aruco/charuco.hpp>
#include "ImageBuffer.h"
#include "ArucoBoard.h"

namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			public ref class CharucoBoard
			{
			private:
				cv::aruco::CharucoBoard* boardPtr;
			public:

				CharucoBoard(int squareX, int squareY, float squareLength, float markerLength, ArucoDictionary dictionaryName) {
					cv::Ptr<cv::aruco::Dictionary> dictionary = cv::aruco::getPredefinedDictionary((int)dictionaryName);
					cv::aruco::CharucoBoard board = *(cv::aruco::CharucoBoard::create(squareX, squareY, squareLength, markerLength, dictionary));
					boardPtr = new cv::aruco::CharucoBoard(board);
				}

				~CharucoBoard() {
					delete boardPtr;
				}

				cv::Ptr<cv::aruco::CharucoBoard> getBoard() {
					return cv::makePtr<cv::aruco::CharucoBoard>(*boardPtr);
				}
			};
		}
	}
}
