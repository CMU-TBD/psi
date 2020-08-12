// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "ImageBuffer.h"

namespace TBD
{
	namespace Psi
	{
		namespace OpenCV
		{
			ImageBuffer::ImageBuffer(int width, int height, System::IntPtr data, int stride)
			{
				this->Width = width;
				this->Height = height;
				this->Data = data;
				this->Stride = stride;
			}
		}
	}
}
