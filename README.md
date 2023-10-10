### Dotnet client for the https://scanii.com content processing service

### How to use this client

This library is installable via Nuget and can be installed via the usual tools, details here: https://www.nuget.org/packages/UvaSoftware.Scanii/

For example, using the dotnet CLI: 
```
dotnet add package UvaSoftware.Scanii --version $LATEST_VERSION
```

### Basic usage:
 
```c#
using System;
using System.Threading.Tasks;
using UvaSoftware.Scanii;

namespace Acme
{
  public class Sample
  {
    static async Task Main(string[] args)
    {
      var client = ScaniiClients.CreateDefault(args[0], args[1]);
      var result = await client.Process("C:\foo.doc");
      
      if (r.Findings.Count == 0)
      {
        Console.WriteLine("Content is safe!")
      }
    }  
  }
}
```

Please note that you will need a valid scanii.com account and API Credentials. 

More advanced usage examples can be found [here](https://github.com/uvasoftware/scanii-dotnet/blob/master/UvaSoftware.Scanii.Tests/ScaniiClientTests.cs)

General documentation on scanii can be found [here](http://docs.scanii.com)
