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
using System.Drawing;

namespace ImageMagick
{
  /// <content>
  /// Contains code that is not compatible with .NET Core.
  /// </content>
  public sealed partial class MagickFactory : IMagickFactory
  {
    /// <summary>
    /// Initializes a new instance that implements <see cref="IMagickImage"/>.
    /// </summary>
    /// <param name="bitmap">The bitmap to use.</param>
    /// <exception cref="MagickException">Thrown when an error is raised by ImageMagick.</exception>
    /// <returns>A new <see cref="IMagickImage"/> instance.</returns>
    public IMagickImage CreateImage(Bitmap bitmap)
    {
      return new MagickImage(bitmap);
    }
  }
}
