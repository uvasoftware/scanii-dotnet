using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace UvaSoftware.Scanii.Tests
{
  [SuppressMessage("ReSharper", "UnusedMember.Local")]
  public class Sample
  {
    static async Task Main(string[] args)
    {
      var client = ScaniiClients.CreateDefault(args[0], args[1]);
      var result = await client.Process("C:\foo.doc");
      Console.WriteLine($"findings: {result.Findings}");
    }  
  }
}
