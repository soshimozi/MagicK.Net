﻿//=================================================================================================
// Copyright 2013-2017 Dirk Lemstra <https://magick.codeplex.com/>
//
// Licensed under the ImageMagick License (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
//
//   http://www.imagemagick.org/script/license.php
//
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
// express or implied. See the License for the specific language governing permissions and
// limitations under the License.
//=================================================================================================

using System.Diagnostics;

namespace ImageMagick.Web
{
  internal static class ImageData
  {
    internal static IImageData Create(IUrlResolver urlResolver, MagickFormatInfo formatInfo)
    {
      IFileUrlResolver fileUrlResolver = urlResolver as IFileUrlResolver;
      if (fileUrlResolver != null)
        return new FileImageData(fileUrlResolver.FileName, formatInfo);

      IStreamUrlResolver streamUrlResolver = urlResolver as IStreamUrlResolver;

      Debug.Assert(streamUrlResolver != null);
      return new StreamImageData(streamUrlResolver, formatInfo);
    }
  }
}
