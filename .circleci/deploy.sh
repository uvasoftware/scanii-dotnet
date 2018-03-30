dotnet pack --configuration Release
dotnet nuget push UvaSoftware.Scanii/bin/Release/Uvasoftware.Scanii.*.nupkg -k $NUGET_KEY -s https://api.nuget.org/v3/index.json
