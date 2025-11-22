#!/bin/bash

set -e

if [ -z "$1" ]; then
  echo "Usage: $0 <ApiKey>"
  exit 1
fi

ApiKey="$1"
Version="0.0.1"

find . \( -name bin -o -name obj -o -name x64 -o -name packages \) -type d -exec rm -rf {} +

dotnet build . -c Release -t:CorruptionTolerantStream:Rebuild

Now=$(date -u +"%Y%m%dT%H%M%SZ")
dotnet pack "./CorruptionTolerantStream/CorruptionTolerantStream.csproj" -c Release -p:VersionPrefix=$Version -p:VersionSuffix="$Now"

cp "./CorruptionTolerantStream/bin/Release/CorruptionTolerantStream.$Version-$Now.nupkg" "$OFFLINE_PACKAGES_DIRECTORY"

echo "Successfully copied the NuGet package to $OFFLINE_PACKAGES_DIRECTORY"

dotnet nuget push "./CorruptionTolerantStream/bin/Release/CorruptionTolerantStream.$Version-$Now.nupkg" --api-key "$ApiKey" --source "github"
