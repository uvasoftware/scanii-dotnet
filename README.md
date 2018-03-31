### Dotnet client for the https://scanii.com content processing service

### How to use this client

This library is installable via Nuget and can be installed via the usual tools, details here: https://www.nuget.org/packages/UvaSoftware.Scanii/

For example, using the dotnet CLI: 
```
dotnet add package UvaSoftware.Scanii --version $LATEST_VERSION
```

### Basic usage:
 
```csharp
 // creating the client
var _client = new ScaniiClient(ScaniiTarget.V21, KEY, SECRET);
 
 // scans a file
var result = _client.Process("file.doc")
Console.WriteLine(result);

```

Please note that you will need a valid scanii.com account and API Credentials. 

More advanced usage examples can be found [here](https://github.com/uvasoftware/scanii-dotnet/blob/master/UvaSoftware.Scanii.Tests/ScaniiClientTests.cs)

General documentation on scanii can be found [here](http://docs.scanii.com)
