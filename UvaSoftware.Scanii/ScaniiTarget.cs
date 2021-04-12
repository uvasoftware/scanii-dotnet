using System;
using System.Collections.Generic;

namespace UvaSoftware.Scanii
{
  public class ScaniiTarget
  {
    public static readonly ScaniiTarget Auto = new ScaniiTarget("https://api.scanii.com");
    public static readonly ScaniiTarget Us1 = new ScaniiTarget("https://api-us1.scanii.com");
    public static readonly ScaniiTarget Eu1 = new ScaniiTarget("https://api-eu1.scanii.com");
    public static readonly ScaniiTarget Eu2 = new ScaniiTarget("https://api-eu2.scanii.com");
    public static readonly ScaniiTarget Ap1 = new ScaniiTarget("https://api-ap1.scanii.com");
    public static readonly ScaniiTarget Ap2 = new ScaniiTarget("https://api-ap2.scanii.com");

    // ReSharper disable once MemberCanBePrivate.Global
    public ScaniiTarget(string endpoint)
    {
      Endpoint = new Uri(endpoint);
    }

    public Uri Endpoint { get; }

    public static IEnumerable<ScaniiTarget> All()
    {
      return new List<ScaniiTarget>
      {
        Auto, Ap1, Ap2, Us1, Eu1, Eu2
      };
    }
  }
}
