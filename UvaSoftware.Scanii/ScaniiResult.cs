using System.Collections.Generic;

namespace UvaSoftware.Scanii
{
  public class ScaniiResult
  {
    public string RawResponse { get; }
    public string ResourceId { get; set; }
    public string ContentType { get; set; }
    public long ContentLength { get; set; }
    public string ResourceLocation { get; set; }
    public string RequestId { get; set; }
    public string HostId { get; set; }
    public readonly List<string> Findings = new List<string>();
    public string Checksum { get; set; }
    public string Message { get; set; }
    public string ExpirationDate { get; set; }
    public string CreationDate { get; set; }
    public readonly Dictionary<string, string> Metadata = new Dictionary<string, string>();

    public override string ToString()
    {
      return
        $"{nameof(Findings)}: {Findings}, {nameof(Metadata)}: {Metadata}, {nameof(RawResponse)}: {RawResponse}, {nameof(ResourceId)}: {ResourceId}, {nameof(ContentType)}: {ContentType}, {nameof(ContentLength)}: {ContentLength}, {nameof(ResourceLocation)}: {ResourceLocation}, {nameof(RequestId)}: {RequestId}, {nameof(HostId)}: {HostId}, {nameof(Checksum)}: {Checksum}, {nameof(Message)}: {Message}, {nameof(ExpirationDate)}: {ExpirationDate}, {nameof(CreationDate)}: {CreationDate}";
    }

    public ScaniiResult(string rawResponse)
    {
      RawResponse = rawResponse;
    }
  }
}
