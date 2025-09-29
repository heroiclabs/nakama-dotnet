Release Instructions
===

This document outlines the release of the Nakama and Satori .NET clients to Github and [Nuget](https://www.nuget.org/packages/NakamaClient/).

Our current monorepo strategy is to maintain the Nakama and Satori clients in the same repo per language. Clients are published together under Github under the same tag and version, even if only one has changed. Clients are released independently to Nuget.

1. Update and tidy up the CHANGELOG.

2. Run the test suite for the codebase. See the README for instructions.

3. Create a tag for the new release. This tag applies to both Nakama and Satori .NET:

   ```shell
   git add CHANGELOG
   git commit -m "Nakama .NET <version> release."
   git tag -a <version> -m "<version>"
   git push origin <version> master
   ```

4. Build Nakama

   ```shell
   dotnet build -c Release ./Nakama/Nakama.csproj
   ```

5. Build Satori

   ```shell
   dotnet build -c Release ./Satori/Satori.csproj
   ```

6. Create a release on GitHub: https://github.com/heroiclabs/nakama-dotnet/releases/new

7. Copy the CHANGELOG section to the release notes. Upload the release DLLs to be part of the GitHub release. Publish it.

8. Package and push the releases to Nuget. Don't put a `v` prefix before the version number.

   ```shell
   dotnet pack -p:AssemblyVersion=<package-version> -p:PackageVersion=<package-version> -c Release ./Nakama/Nakama.csproj
   ```

   ```shell
   dotnet nuget push ./Nakama/bin/Release/NakamaClient.<package-version>.nupkg -k "somekey" -s https://api.nuget.org/v3/index.json
   ```

   ```shell
   dotnet pack -p:AssemblyVersion=<package-version> -p:PackageVersion=<package-version> -c Release ./Satori/Satori.csproj
   ```

   ```shell
   dotnet nuget push ./Satori/bin/Release/SatoriClient.<package-version>.nupkg -k "somekey" -s https://api.nuget.org/v3/index.json
   ```

9. Update CHANGELOG with section for new unreleased changes.

   ```shell
   git add CHANGELOG.md
   git commit -m "Set new development version."
   git push origin master
   ```
