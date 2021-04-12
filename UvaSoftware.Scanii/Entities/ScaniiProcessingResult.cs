using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable CollectionNeverUpdated.Global

namespace UvaSoftware.Scanii.Entities
{
  public class ScaniiProcessingResult : ScaniiResult
  {
    [JsonPropertyName("id")] public string ResourceId { get; set; }
    [JsonPropertyName("content_type")] public string ContentType { get; set; }
    [JsonPropertyName("content_length")] public long ContentLength { get; set; }

    [JsonPropertyName("findings")] public List<string> Findings { get; set; } = new List<string>();
    [JsonPropertyName("checksum")] public string Checksum { get; set; }
    [JsonPropertyName("creation_date")] public DateTime CreationDate { get; set; }
    [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; set; }
  }
}
