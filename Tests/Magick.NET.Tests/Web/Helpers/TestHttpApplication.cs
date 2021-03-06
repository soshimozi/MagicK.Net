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

using System.Linq;
using System.Reflection;
using System.Web;

namespace Magick.NET.Tests
{
  [ExcludeFromCodeCoverage]
  public sealed class TestHttpApplication : HttpApplication
  {
    private object BeginRequestEvent;
    private object PostAuthorizeRequestEvent;
    private object PostMapRequestHandlerEvent;

    private object GetValue(FieldInfo[] fields, string name)
    {
      return fields.First(f => f.Name.Contains(name)).GetValue(this);
    }

    public TestHttpApplication()
    {
      var fields = typeof(HttpApplication).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
      BeginRequestEvent = GetValue(fields, "BeginRequest");
      PostAuthorizeRequestEvent = GetValue(fields, "PostAuthorizeRequest");
      PostMapRequestHandlerEvent = GetValue(fields, "PostMapRequest");
    }

    public bool BeginRequestHasEvent => Events[BeginRequestEvent] != null;
    public bool PostAuthorizeRequestHasEvent => Events[PostAuthorizeRequestEvent] != null;
    public bool PostMapRequestHandlerHasEvent => Events[PostMapRequestHandlerEvent] != null;
  }
}