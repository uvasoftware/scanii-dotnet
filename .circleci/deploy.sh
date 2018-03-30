dotnet pack
dotnet nuget push UvaSoftware.Scanii/bin/Debug/Uvasoftware.Scanii.*.nupkg -k $NUGET_KEY -s https://api.nuget.org/v3/index.json
