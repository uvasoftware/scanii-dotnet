using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace UvaSoftware.Scanii.Internal
{
  public static class ResponseProcessor
  {
    public static ScaniiResult Process(IRestResponse response)
    {
      var result = new ScaniiResult(response.Content);
      Log.Logger.Debug("raw response -> {content}", result.RawResponse);

      var location = response.Headers
        .ToList()
        .FirstOrDefault(x => x.Name == HttpHeaders.Location);

      if (location != null)
      {
        result.ResourceLocation = location.Name;
      }

      var hostId = response.Headers
        .ToList()
        .FirstOrDefault(x => x.Name == HttpHeaders.XHostHeader);

      if (hostId != null)
      {
        result.HostId = hostId.Name;
      }

      var requestId = response.Headers
        .ToList()
        .FirstOrDefault(x => x.Name == HttpHeaders.XRequestHeader);

      if (requestId != null)
      {
        result.RequestId = requestId.Name;
      }

      var js = JObject.Parse(response.Content);

      result.ResourceId = js["id"].ToString();

      if (js.ContainsKey("findings"))
      {
        result.ContentType = js["content_type"].ToString();
        result.ContentLength = Convert.ToInt64(js["content_length"].ToString());
        result.Checksum = js["checksum"].ToString();
        foreach (var entry in js["findings"].Values())
        {
          result.Findings.Add(entry.ToString());
        }
      }

      if (js.ContainsKey("message"))
      {
        result.Message = js["message"].ToString();
      }

      if (js.ContainsKey("expiration_date"))
      {
        result.ExpirationDate = js["expiration_date"].ToString();
      }

      if (js.ContainsKey("creation_date"))
      {
        result.CreationDate = js["creation_date"].ToString();
      }

      if (!js.ContainsKey("metadata")) return result;
      foreach (var item in js["metadata"])
      {
        if (item is JProperty prop) result.Metadata.Add(prop.Name, prop.Value.ToString());
        Log.Logger.Debug("parsing metadata {entry}", item);
      }

      return result;
    }
  }
}
