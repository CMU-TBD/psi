#pragma once


#include <opencv2/core.hpp>
#include <opencv2/aruco/dictionary.hpp>
#include <opencv2/aruco.hpp>
#include "ImageBuffer.h"

namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			public ref class ArucoBoard
			{
			private:
				cv::aruco::GridBoard* boardPtr;
			public:
				
				ArucoBoard(int markersX, int markersY, float markerLength, float markerSeperation, System::String^ dictName, int firstMarker) {
					cv::Ptr<cv::aruco::Dictionary> dictionary = cv::aruco::getPredefinedDictionary(cv::aruco::DICT_4X4_100);
					cv::aruco::GridBoard board = *(cv::aruco::GridBoard::create(markersX, markersY, markerLength, markerSeperation, dictionary, firstMarker));
					boardPtr = new cv::aruco::GridBoard(board);
				}

				~ArucoBoard() {
					delete boardPtr;
				}

				cv::Ptr<cv::aruco::GridBoard> getBoard() {
					return cv::makePtr<cv::aruco::GridBoard>(*boardPtr);
				}
			};
		}
	}
}
