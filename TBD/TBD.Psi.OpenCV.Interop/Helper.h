#pragma once
#include <opencv2/core.hpp>
#include "ImageBuffer.h"

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

			static ImageBuffer^ MatUnWrap(cv::Mat mat)
			{
				return gcnew ImageBuffer(mat.rows, mat.cols, System::IntPtr((void*)mat.data), mat.step);
			}

			static bool compreIdArrays(std::vector<int> ids, std::vector<int> boardIds)
			{
				bool missing = false;
				for (auto bid : boardIds) {
					missing = (std::find(ids.begin(), ids.end(), bid) == ids.end());
				}
				return !missing;
			}

		}
	}
}