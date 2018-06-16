/**
 * Copyright 2017 The Nakama Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#tool "nuget:?package=NUnit.ConsoleRunner&version=3.8.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var buildDir = Directory("./src/Nakama/bin") + Directory(configuration);

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild("./src/Nakama.sln", settings => settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      XBuild("./src/Nakama.sln", settings => settings.SetConfiguration(configuration));
    }
});

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./src/Nakama.sln");
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./build/msbuild/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
        });
});

Task("Default").IsDependentOn("Run-Unit-Tests");

RunTarget(target);
