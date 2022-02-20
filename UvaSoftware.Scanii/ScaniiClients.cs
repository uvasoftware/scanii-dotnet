using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UvaSoftware.Scanii.Entities;
using UvaSoftware.Scanii.Internal;

namespace UvaSoftware.Scanii
{
  public static class ScaniiClients
  {
    public static IScaniiClient CreateDefault(string key, string secret, ILogger logger = null,
      HttpClient client = null, ScaniiTarget target = null)
    {
      logger ??= NullLogger.Instance;
      client ??= new HttpClient();
      target ??= ScaniiTarget.Auto;
      ;
      return new DefaultScaniiClient(target, key, secret, logger, client);
    }

    public static IScaniiClient CreateDefault(ScaniiAuthToken authToken, ILogger logger = null,
      HttpClient client = null, ScaniiTarget target = null)
    {
      if (authToken == null)
      {
        throw new ArgumentNullException(nameof(authToken));
      }

      logger ??= NullLogger.Instance;
      client ??= new HttpClient();
      target ??= ScaniiTarget.Auto;
      return new DefaultScaniiClient(target, authToken.ResourceId, "", logger, client);
    }
  }
}
