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
      Console.WriteLine($"findings: {result.Findings}");
    }  
  }
}
