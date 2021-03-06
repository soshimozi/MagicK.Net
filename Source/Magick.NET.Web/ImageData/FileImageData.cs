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

using System;
using System.IO;

namespace ImageMagick.Web
{
  internal sealed class FileImageData : IImageData
  {
    private readonly string _FileName;

    public FileImageData(string fileName, MagickFormatInfo formatInfo)
    {
      _FileName = fileName;
      FormatInfo = formatInfo;
    }

    public MagickFormatInfo FormatInfo { get; }

    public string ImageId => _FileName;

    public bool IsValid => !string.IsNullOrEmpty(_FileName) && File.Exists(_FileName);

    public DateTime ModifiedTimeUtc => File.GetLastWriteTimeUtc(_FileName);

    public Stream ReadImage()
    {
      return File.OpenRead(_FileName);
    }

    public MagickImage ReadImage(MagickReadSettings settings)
    {
      return new MagickImage(_FileName, settings);
    }

    public void SaveImage(string fileName)
    {
      File.Copy(_FileName, fileName);
    }
  }
}