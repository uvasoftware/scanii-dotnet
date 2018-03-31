#!/bin/bash
echo "************************************"
echo "Nuget PACK"
echo "************************************"
dotnet pack --configuration Release

echo "************************************"
echo "Nuget PUSH"
echo "************************************"
dotnet nuget push UvaSoftware.Scanii/bin/Release/Uvasoftware.Scanii.*.nupkg -k ${NUGET_KEY} -s https://api.nuget.org/v3/index.json
