using System;
using System.Collections.Generic;

namespace UvaSoftware.Scanii.Internal
{
  public static class Endpoints
  {
    private static readonly Dictionary<ScaniiTarget, string> Mapping = new Dictionary<ScaniiTarget, string>
    {
      {ScaniiTarget.V20, "https://api.scanii.com/v2.0"},
      {ScaniiTarget.V21, "https://api.scanii.com/v2.1"},

      {ScaniiTarget.V20Ap1, "https://api-ap1.scanii.com/v2.0"},
      {ScaniiTarget.V20Ap2, "https://api-ap2.scanii.com/v2.0"},

      {ScaniiTarget.V21Ap1, "https://api-ap1.scanii.com/v2.1"},
      {ScaniiTarget.V21Ap2, "https://api-ap2.scanii.com/v2.1"},

      {ScaniiTarget.V20Eu1, "https://api-eu1.scanii.com/v2.0"},
      {ScaniiTarget.V20Eu2, "https://api-eu2.scanii.com/v2.0"},

      {ScaniiTarget.V21Eu1, "https://api-eu1.scanii.com/v2.1"},
      {ScaniiTarget.V21Eu2, "https://api-eu2.scanii.com/v2.1"},

      {ScaniiTarget.V20Us1, "https://api-us1.scanii.com/v2.0"},
      {ScaniiTarget.V21Us1, "https://api-us1.scanii.com/v2.1"}
    };

    public static string Resolve(ScaniiTarget target)
    {
      if (!Mapping.ContainsKey(target))
      {
        throw new ArgumentException($"no mapping found for target ${target}");
      }

      return Mapping[target];
    }
  }
}
