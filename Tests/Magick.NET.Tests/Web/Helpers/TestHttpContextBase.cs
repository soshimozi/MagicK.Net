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

using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace Magick.NET.Tests
{
  [ExcludeFromCodeCoverage]
  public sealed class TestHttpContextBase : HttpContextBase
  {
    private Dictionary<object, object> _Items;
    private HttpRequestBase _Request;

    public TestHttpContextBase()
      : this("https://www.imagemagick.org")
    {
    }

    public TestHttpContextBase(string url)
    {
      _Items = new Dictionary<object, object>();
      _Request = new TestHttpRequest(url);
    }

    public override IHttpHandler Handler { get; set; }

    public override IDictionary Items => _Items;

    public override HttpRequestBase Request => _Request;

    public IHttpHandler RemapedHandler { get; private set; }

    public override void RemapHandler(IHttpHandler handler)
    {
      RemapedHandler = handler;
    }
  }
}
