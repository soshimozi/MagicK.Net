#==================================================================================================
# Copyright 2013-2017 Dirk Lemstra <https://magick.codeplex.com/>
#
# Licensed under the ImageMagick License (the "License"); you may not use this file except in 
# compliance with the License. You may obtain a copy of the License at
#
#   http://www.imagemagick.org/script/license.php
#
# Unless required by applicable law or agreed to in writing, software distributed under the
# License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
# express or implied. See the License for the specific language governing permissions and
# limitations under the License.
#==================================================================================================
function AddFileElement($xml, $src, $target)
{
  $files = $xml.package.files

  if (!($files))
  {
    $files = $xml.CreateElement("files", $xml.DocumentElement.NamespaceURI)
    [void]$xml.package.AppendChild($files)
  }

  $file = $xml.CreateElement("file", $xml.DocumentElement.NamespaceURI)

  $srcAtt = $xml.CreateAttribute("src")
  $srcAtt.Value = $src
  [void]$file.Attributes.Append($srcAtt)

  $targetAtt = $xml.CreateAttribute("target")
  $targetAtt.Value = $target
  [void]$file.Attributes.Append($targetAtt)

  [void]$files.AppendChild($file)
}

function CheckStrongNames($builds)
{
  foreach ($build in $builds)
  {
    $path = FullPath "Source\Magick.NET\bin\Release$($build.Quantum)\$($build.Platform)$($build.Suffix)\Magick.NET-$($build.Quantum)-$($build.Platform).dll"
    sn -Tp $path
    CheckExitCode "$path does not represent a strongly named assembly"

    if ($build.Framework -ne "v4.0")
    {
      continue
    }

    $path = FullPath "Source\Magick.NET.Web\bin\Release$($build.Quantum)\$($build.Platform)$($build.Suffix)\Magick.NET.Web-$($build.Quantum)-$($build.Platform).dll"
    sn -Tp $path
    CheckExitCode "$path does not represent a strongly named assembly"
  }
}

function GetImageMagickVersion()
{
  $versionPath = FullPath "ImageMagick\Include\MagickCore\version.h"
  $lines = [IO.File]::ReadAllLines($versionPath, [System.Text.Encoding]::UTF8)

  $version = "0.0.0"
  $addendum = "--0"
  foreach ($line in $lines)
  {
    if ($line.StartsWith("#define MagickLibVersionText  "))
    {
      $version = $line.Replace("""","").SubString(30);
    }
    elseif ($line.StartsWith("#define MagickLibAddendum  "))
    {
      $addendum = $line.Replace("""","").SubString(27);
    }
  }

  if (("$version" -eq "0.0.0") -or ("$addendum" -eq "--0"))
  {
    Write-Error "Unable to find ImageMagickVersion"
    Exit
  }

  return "$version$addendum"
}

function CreateNuGetPackage($id, $version, $build)
{
  $path = FullPath "Publish\NuGet\Magick.NET.nuspec"
  $xml = [xml](Get-Content $path)

  $platform = $($build.Platform)
  if ($platform -eq "x86")
  {
    $platform = "Win32"
  }

  $imVersion = GetImageMagickVersion
  $xml.package.metadata.releaseNotes = "Magick.NET linked with ImageMagick " + $imVersion

  AddFileElement $xml "..\..\Source\Magick.NET\bin\Release$($build.Quantum)\$($build.Platform).net20\Magick.NET-$($build.Quantum)-$($build.Platform).dll" "lib\net20"
  AddFileElement $xml "..\..\Source\Magick.NET\bin\Release$($build.Quantum)\$($build.Platform).net20\Magick.NET-$($build.Quantum)-$($build.Platform).xml" "lib\net20"

  AddFileElement $xml "..\..\Source\Magick.NET\bin\Release$($build.Quantum)\$($build.Platform)$($build.Suffix)\Magick.NET-$($build.Quantum)-$($build.Platform).dll" "lib\$($build.FrameworkName)"
  AddFileElement $xml "..\..\Source\Magick.NET\bin\Release$($build.Quantum)\$($build.Platform)$($build.Suffix)\Magick.NET-$($build.Quantum)-$($build.Platform).xml" "lib\$($build.FrameworkName)"

  if ($build.Platform -ne "AnyCPU")
  {
    AddFileElement $xml "..\..\Source\Magick.NET.Native\bin\Release$($build.Quantum)\$($platform)\Magick.NET-$($build.Quantum)-$($build.Platform).Native.dll" "build\$($build.FrameworkName)\$($build.Platform)"
    AddFileElement $xml "Magick.NET.targets" "build\$($build.FrameworkName)\$id.targets"
  }

  AddFileElement $xml "..\Readme.txt" "Readme.txt"
  AddFileElement $xml "..\..\Copyright.txt" "Copyright.txt"

  WriteNuGetPackage $id $version $xml

  $webId = $id + ".Web"
  $path = FullPath "Publish\NuGet\Magick.NET.Web.nuspec"
  $xml = [xml](Get-Content $path)

  AddFileElement $xml "..\..\Source\Magick.NET.Web\bin\Release$($build.Quantum)\$($build.Platform)$($build.Suffix)\Magick.NET.Web-$($build.Quantum)-$($build.Platform).dll" "lib\$($build.FrameworkName)"
  AddFileElement $xml "..\..\Source\Magick.NET.Web\bin\Release$($build.Quantum)\$($build.Platform)$($build.Suffix)\Magick.NET.Web-$($build.Quantum)-$($build.Platform).xml" "lib\$($build.FrameworkName)"

  AddFileElement $xml "..\Readme.Web.txt" "Readme.txt"
  AddFileElement $xml "..\..\Copyright.txt" "Copyright.txt"

  WriteNuGetPackage $webId $version $xml
}

function SetVersion($content, $startMatch, $endMatch, $version)
{
  $start = $content.IndexOf($startMatch)
  if ($start -eq -1)
  {
    Write-Error "Unable to find startMatch"
    Exit
  }

  $start += $startMatch.Length

  $newContent = $content.Substring(0, $start)
  $newContent += $version

  $start = $content.IndexOf($endMatch, $start)
  if ($start -eq -1)
  {
    Write-Error "Unable to find endMatch"
    Exit
  }

  $newContent += $content.Substring($start)
  return $newContent
}

function UpdateAssemblyInfo($fileName, $version)
{
  $path = FullPath $fileName
  $content = [IO.File]::ReadAllText($path, [System.Text.Encoding]::Default)
  $content = SetVersion $content "AssemblyFileVersion(`"" "`"" $version
  [IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::Default)
}

function UpdateAssemblyInfos($version)
{
  UpdateAssemblyInfo "Source\Magick.NET\Properties\AssemblyInfo.cs" $version
  UpdateAssemblyInfo "Source\Magick.NET\Core\Properties\AssemblyInfo.Core.cs" $version
  UpdateAssemblyInfo "Source\Magick.NET.Web\Properties\AssemblyInfo.cs" $version
}

function UpdateCoreProject($directory, $version)
{
  $path = FullPath "Publish\Magick.NET.Core\src\$directory\$directory.csproj"

  $content = [IO.File]::ReadAllText($path, [System.Text.Encoding]::Default)
  $content = SetVersion $content "`<VersionPrefix`>" "`<" $version
  [IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::Default)
}

function UpdateCoreProjects($version)
{
  UpdateCoreProject "Magick.NET.Core-Q8" $version
  UpdateCoreProject "Magick.NET.Core-Q8.Native" $version
  UpdateCoreProject "Magick.NET.Core-Q16" $version
  UpdateCoreProject "Magick.NET.Core-Q16.Native" $version
  UpdateCoreProject "Magick.NET.Core-Q16-HDRI" $version
  UpdateCoreProject "Magick.NET.Core-Q16-HDRI.Native" $version
}

function UpdateResourceFile($fileName, $version)
{
  $content = [IO.File]::ReadAllText($fileName, [System.Text.Encoding]::Unicode)
  $content = SetVersion $content "FILEVERSION " `r $version.Replace('.', ',')
  $content = SetVersion $content "PRODUCTVERSION " `r $version.Replace('.', ',')
  $content = SetVersion $content "`"FileVersion`", `""  "`"" $version
  $content = SetVersion $content "`"ProductVersion`", `"" "`"" $version
  $content = SetVersion $content "`"LegalCopyright`", `"" "`"" "Copyright � Dirk Lemstra $((Get-Date).year)"

  [IO.File]::WriteAllText($fileName, $content, [System.Text.Encoding]::Unicode)
}

function UpdateResourceFiles($builds, $version)
{
  foreach ($build in $builds)
  {
    if ($build.Framework -ne "v4.0")
    {
      continue
    }

    $platform = $($build.Platform)
    if ($platform -eq "AnyCPU")
    {
      continue
    }

    if ($platform -eq "x86")
    {
      $platform = "Win32"
    }

    $fileName = FullPath "Source\Magick.NET.Native\Resources\Release$($build.Quantum)\$platform\Magick.NET.rc"
    UpdateResourceFile $filename $version

    $fileName = FullPath "Source\Magick.NET.Native\Resources\Debug$($build.Quantum)\$platform\Magick.NET.rc"
    UpdateResourceFile $filename $version
  }
}

function WriteNuGetPackage($id, $version, $xml)
{
  $xml.package.metadata.id = $id
  $xml.package.metadata.title = $id
  $xml.package.metadata.version = $version

  $dir = FullPath "Publish\NuGet"
  $nuspecFile = "$dir\$id.nuspec"
  if (Test-Path $nuspecFile)
  {
    Remove-Item $nuspecFile
  }

  $xml.Save($nuspecFile)

  .\Tools\Programs\NuGet.exe pack $nuspecFile -NoPackageAnalysis -OutputDirectory $dir
  CheckExitCode "Failed to create NuGet package"

  Remove-Item $nuspecFile
}
