using System.Text.Json.Serialization;

namespace UvaSoftware.Scanii.Entities
{
  public class ScaniiPendingResult : ScaniiResult
  {
    [JsonPropertyName("id")] public string ResourceId { get; set; }
  }
}
