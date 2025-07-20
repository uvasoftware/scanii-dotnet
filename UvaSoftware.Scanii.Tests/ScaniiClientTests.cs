using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace UvaSoftware.Scanii.Tests
{
  [TestFixture]
  public class ScaniiClientTests
  {
    [OneTimeSetUp]
    public void Setup()
    {
      _eicarFile = Path.GetTempFileName();
      Console.WriteLine($"using temp file {_eicarFile}");
      using var output = new StreamWriter(_eicarFile);
      output.WriteLine(Eicar);
      output.Close();
      // calculating checksum (oddly complex on dotnet): 
      var sha1 = SHA1.Create().ComputeHash(File.ReadAllBytes(_eicarFile));
      _checksum = BitConverter.ToString(sha1).ToLower().Replace("-", "");
      _logger.LogDebug("using temp file {E}, with sha1 {S}", _eicarFile, _checksum);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
      File.Delete(Path.GetTempFileName());
    }

    private readonly IScaniiClient _client;
    private readonly ILogger _logger;
    private const string Eicar = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
    private const string Finding = "content.malicious.eicar-test-signature";
    private string _eicarFile;
    private readonly string _key;
    private readonly string _secret;
    private string _checksum;
    private const string EicarRemoteChecksum = "bec1b52d350d721c7e22a6d4bb0a92909893a3ae";

    public ScaniiClientTests()
    {
      var serilogLogger = new LoggerConfiguration()
        .WriteTo.Console()
        .MinimumLevel.Is(LogEventLevel.Debug)
        .CreateLogger();

      var provider = new SerilogLoggerProvider(serilogLogger);
      _logger = provider.CreateLogger("ScaniiClientTests");

      _logger.LogDebug("ctor");


      if (Environment.GetEnvironmentVariable("SCANII_CREDS") != null)
      {
        // ReSharper disable once PossibleNullReferenceException
        var credentials = Environment.GetEnvironmentVariable("SCANII_CREDS").Split(':');
        _key = credentials[0];
        _secret = credentials[1];
      }

      Debug.Assert(_secret != null, nameof(_secret) + " != null");
      Debug.Assert(_key != null, nameof(_key) + " != null");

      _client = ScaniiClients.CreateDefault(_key, _secret, _logger, new HttpClient());
    }

    [Test]
    public async Task ShouldCreateAuthToken()
    {
      var token = await _client.CreateAuthToken(5);
      Assert.That(token.RequestId, Is.Not.Null);
      Assert.That(token.CreationDate, Is.Not.Null);
      Assert.That(token.ExpirationDate, Is.Not.Null);

      _logger.LogInformation("using auth token to create a new client...");
    }

    [Test]
    public async Task ShouldCreatedUsableAuthTokens()
    {
      var token = await _client.CreateAuthToken(5);
      var client2 = ScaniiClients.CreateDefault(token);

      _logger.LogInformation("using token to process content");

      var result = await client2.Process(_eicarFile);

      Assert.That(result.ResourceId, Is.Not.Null);
      Assert.That(result.Findings.Contains(Finding));
      Assert.That(result.Findings.Count, Is.EqualTo(1));
      Assert.That(result.Checksum, Is.EqualTo(_checksum));
      Assert.That(result.ContentLength, Is.Not.Null);
      Assert.That(result.CreationDate, Is.Not.Null);
    }

    [Test]
    public async Task ShouldDeleteAuthToken()
    {
      var token = await _client.CreateAuthToken(1);
      await _client.DeleteAuthToken(token.ResourceId);
      var token2 = await _client.RetrieveAuthToken(token.ResourceId);

      Assert.That(token.ResourceId, Is.EqualTo(token2.ResourceId));
      Assert.That(token.ExpirationDate, Is.EqualTo(token2.ExpirationDate));
      Assert.That(token.CreationDate, Is.EqualTo(token2.CreationDate));
    }

    [Test]
    public async Task ShouldFetchContentWithCallback()
    {
      var r = await _client.Fetch("https://scanii.s3.amazonaws.com/eicarcom2.zip", "https://httpbin.org/post");

      var finalResult = TestUtils.PollForResult(() => _client.Retrieve(r.ResourceId));

      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.Findings.Contains(Finding));
      Assert.That(1, Is.EqualTo(finalResult.Findings.Count));
      Assert.That("application/zip", Is.EqualTo(finalResult.ContentType));
      Assert.That(EicarRemoteChecksum, Is.EqualTo(finalResult.Checksum));
      Assert.That(finalResult.ContentLength, Is.Not.Null);
      Assert.That(finalResult.CreationDate, Is.Not.Null);


      Assert.That(finalResult.HostId, Is.Not.Null);
      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.RequestId, Is.Not.Null);
      Assert.That(finalResult.ResourceLocation, Is.Null);
    }

    [Test]
    public async Task ShouldFetchContentWithCallbackAndMetadata()
    {
      var r = await _client.Fetch(
        "https://scanii.s3.amazonaws.com/eicarcom2.zip",
        "https://httpbin.org/post",
        new Dictionary<string, string>
        {
          {"hello", "world"}
        });

      var finalResult = TestUtils.PollForResult(() => _client.Retrieve(r.ResourceId));

      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.Findings.Contains(Finding));
      Assert.That(finalResult.Findings.Count, Is.EqualTo(1));
      Assert.That(finalResult.ContentType, Is.EqualTo("application/zip"));
      Assert.That(finalResult.Metadata["hello"], Is.EqualTo("world"));
      Assert.That(finalResult.Metadata.Count, Is.EqualTo(1));
      Assert.That(finalResult.Checksum, Is.EqualTo(EicarRemoteChecksum));
      Assert.That(finalResult.ContentLength, Is.Not.Null);
      Assert.That(finalResult.CreationDate, Is.Not.Null);


      Assert.That(finalResult.HostId, Is.Not.Null);
      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.RequestId, Is.Not.Null);
      Assert.That(finalResult.StatusCode, Is.EqualTo(200));
      Assert.That(finalResult.ResourceLocation, Is.Null);
    }

    [Test]
    public async Task ShouldFetchContentWithoutCallback()
    {
      var r = await _client.Fetch("https://scanii.s3.amazonaws.com/eicarcom2.zip");
      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.ResourceLocation, Is.Not.Null);
      Assert.That(r.HostId, Is.Not.Null);
      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.RequestId, Is.Not.Null);

      var finalResult = TestUtils.PollForResult(() => _client.Retrieve(r.ResourceId));

      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.Findings.Contains(Finding));
      Assert.That(finalResult.Findings.Count, Is.EqualTo(1));
      Assert.That(finalResult.ContentType, Is.EqualTo("application/zip"));
      Assert.That(finalResult.Checksum, Is.EqualTo(EicarRemoteChecksum));
      Assert.That(finalResult.ContentLength, Is.Not.Null);
      Assert.That(finalResult.CreationDate, Is.Not.Null);


      Assert.That(finalResult.HostId, Is.Not.Null);
      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.RequestId, Is.Not.Null);

      Assert.That(finalResult.ResourceLocation, Is.Null);
    }


    [Test]
    public async Task ShouldPing()
    {
      Assert.That(await _client.Ping(), Is.True);
    }

    [Test]
    public async Task ShouldPingAllRegions()
    {
      foreach (var target in ScaniiTarget.All())
      {
        _logger.LogInformation("creating client for target {Target}", target.Endpoint);
        var client = ScaniiClients.CreateDefault(_key, _secret, target: target);
        Assert.That(await client.Ping(), Is.True);
      }
    }

    [Test]
    public async Task ShouldProcessAsyncWithCallback()
    {
      var r = await _client.ProcessAsync(_eicarFile, "https://httpbin.org/post");

      _logger.LogDebug("response: {@R}", r);

      Assert.That(r.ResourceId, Is.Not.Null);

      var finalResult = TestUtils.PollForResult(() => _client.Retrieve(r.ResourceId));

      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.Findings.Contains(Finding));
      Assert.That(finalResult.Findings.Count, Is.EqualTo(1));
      Assert.That("text/plain", Is.EqualTo(finalResult.ContentType));
      Assert.That(_checksum, Is.EqualTo(finalResult.Checksum));
      Assert.That(finalResult.ContentLength, Is.Not.Null);
      Assert.That(finalResult.CreationDate, Is.Not.Null);

      Assert.That(finalResult.HostId, Is.Not.Null);
      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.RequestId, Is.Not.Null);

      Assert.That(finalResult.ResourceLocation, Is.Null);
    }

    [Test]
    public async Task ShouldProcessAsyncWithCallbackAndMetadata()
    {
      var r = await _client.ProcessAsync(_eicarFile, "https://httpbin.org/post", new Dictionary<string, string>
      {
        {"foo", "bar"}
      });

      _logger.LogDebug("response: {@R}", r);

      Assert.That(r.ResourceId, Is.Not.Null);

      var finalResult = TestUtils.PollForResult(() => _client.Retrieve(r.ResourceId));

      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.Findings.Contains(Finding));
      Assert.That(1, Is.EqualTo(finalResult.Findings.Count));
      Assert.That("text/plain", Is.EqualTo(finalResult.ContentType));
      Assert.That("bar", Is.EqualTo(finalResult.Metadata["foo"]));
      Assert.That(_checksum, Is.EqualTo(finalResult.Checksum));
      Assert.That(finalResult.ContentLength, Is.Not.Null);
      Assert.That(finalResult.CreationDate, Is.Not.Null);

      Assert.That(finalResult.HostId, Is.Not.Null);
      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.RequestId, Is.Not.Null);

      Assert.That(finalResult.ResourceLocation, Is.Null);
    }

    [Test]
    public async Task ShouldProcessAsyncWithoutCallback()
    {
      _logger.LogInformation("submitting content for async processing...");

      var r = await _client.ProcessAsync(_eicarFile, metadata: new Dictionary<string, string>
      {
        {"foo", "bar"}
      });

      _logger.LogDebug("response: {@R}", r);

      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.ResourceLocation, Is.Not.Null);

      Assert.That(r.HostId, Is.Not.Null);
      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.RequestId, Is.Not.Null);


      _logger.LogInformation("request looks good, trying to retrieve result for id: {Id}", r.ResourceId);

      var finalResult = TestUtils.PollForResult(() => _client.Retrieve(r.ResourceId));

      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.Findings.Contains(Finding));
      Assert.That(1, Is.EqualTo(finalResult.Findings.Count));
      Assert.That("text/plain", Is.EqualTo(finalResult.ContentType));
      Assert.That("bar", Is.EqualTo(finalResult.Metadata["foo"]));
      Assert.That(1, Is.EqualTo(finalResult.Metadata.Count));
      Assert.That(_checksum, Is.EqualTo(finalResult.Checksum));
      Assert.That(finalResult.ContentLength, Is.Not.Null);
      Assert.That(finalResult.CreationDate, Is.Not.Null);


      Assert.That(finalResult.HostId, Is.Not.Null);
      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.RequestId, Is.Not.Null);

      Assert.That(finalResult.ResourceLocation, Is.Null);
    }

    [Test]
    public async Task ShouldProcessAsyncWithoutCallBackAndMetadata()
    {
      _logger.LogInformation("submitting content for async processing...");

      var r = await _client.ProcessAsync(_eicarFile);

      _logger.LogDebug("response: {@R}", r);

      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.ResourceLocation, Is.Not.Null);

      Assert.That(r.HostId, Is.Not.Null);
      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.RequestId, Is.Not.Null);

      _logger.LogInformation("request looks good, trying to retrieve result for id: {Id}", r.ResourceId);


      var finalResult = TestUtils.PollForResult(() => _client.Retrieve(r.ResourceId));


      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.Findings.Contains(Finding));
      Assert.That(1, Is.EqualTo(finalResult.Findings.Count));
      Assert.That("text/plain", Is.EqualTo(finalResult.ContentType));
      Assert.That(0, Is.EqualTo(finalResult.Metadata.Count));
      Assert.That(_checksum, Is.EqualTo(finalResult.Checksum));
      Assert.That(finalResult.ContentLength, Is.Not.Null);
      Assert.That(finalResult.CreationDate, Is.Not.Null);


      Assert.That(finalResult.HostId, Is.Not.Null);
      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.RequestId, Is.Not.Null);

      Assert.That(finalResult.ResourceLocation, Is.Null);
    }

    [Test]
    public async Task ShouldProcessContent()
    {
      // starting with a simple content scan (sync)
      var r = await _client.Process(_eicarFile, metadata: new Dictionary<string, string>
      {
        {"foo", "bar"}
      });

      _logger.LogDebug("response: {r}", r);

      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.Findings.Contains(Finding));
      Assert.That(1, Is.EqualTo(r.Findings.Count));
      Assert.That("text/plain", Is.EqualTo(r.ContentType));
      Assert.That("bar", Is.EqualTo(r.Metadata["foo"]));
      Assert.That(1, Is.EqualTo(r.Metadata.Count));
      Assert.That(_checksum, Is.EqualTo(r.Checksum));
      Assert.That(r.ContentLength, Is.Not.Null);
      Assert.That(r.CreationDate, Is.Not.Null);


      Assert.That(r.HostId, Is.Not.Null);
      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.ResourceLocation, Is.Not.Null);
      Assert.That(r.RequestId, Is.Not.Null);
      Assert.That(r.StatusCode, Is.Not.Null);
    }

    [Test]
    public async Task ShouldProcessContentWithoutMetadata()
    {
      // starting with a simple content scan (sync)
      var r = await _client.Process(_eicarFile);

      _logger.LogDebug("response: {@R}", r);

      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.Findings.Contains(Finding));
      Assert.That(1, Is.EqualTo(r.Findings.Count));
      Assert.That("text/plain", Is.EqualTo(r.ContentType));
      Assert.That(0, Is.EqualTo(r.Metadata.Count));
      Assert.That(_checksum, Is.EqualTo(r.Checksum));
      Assert.That(r.ContentLength, Is.Not.Null);
      Assert.That(r.CreationDate, Is.Not.Null);


      Assert.That(r.HostId, Is.Not.Null);
      Assert.That(r.ResourceId, Is.Not.Null);
      Assert.That(r.ResourceLocation, Is.Not.Null);
      Assert.That(r.RequestId, Is.Not.Null);
    }

    [Test]
    public async Task ShouldBeSimple()
    {
      var r = await _client.Process(_eicarFile);
      if (r.Findings.Count == 0) Console.WriteLine("Content is safe!");
    }

    [Test]
    public async Task ShouldRetrieveAuthToken()
    {
      var token = await _client.CreateAuthToken(1);
      var token2 = await _client.RetrieveAuthToken(token.ResourceId);

      Assert.That(token.ResourceId, Is.EqualTo(token2.ResourceId));
      Assert.That(token.ExpirationDate, Is.EqualTo(token2.ExpirationDate));
      Assert.That(token.CreationDate, Is.EqualTo(token2.CreationDate));
    }

    [Test]
    [Ignore("triage test")]
    public async Task ShouldTriage()
    {
      var r = await _client.Fetch(
        "",
        "https://httpbin.org/post",
        new Dictionary<string, string>
        {
          {"hello", "world"}
        });
      _logger.LogInformation("here");
      var finalResult = TestUtils.PollForResult(() => _client.Retrieve(r.ResourceId));

      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.Findings.Contains(Finding));
      Assert.That(1, Is.EqualTo(finalResult.Findings.Count));
      Assert.That("application/zip", Is.EqualTo(finalResult.ContentType));
      Assert.That("world", Is.EqualTo(finalResult.Metadata["hello"]));
      Assert.That(1, Is.EqualTo(finalResult.Metadata.Count));
      Assert.That(EicarRemoteChecksum, Is.EqualTo(finalResult.Checksum));
      Assert.That(finalResult.ContentLength, Is.Not.Null);
      Assert.That(finalResult.CreationDate, Is.Not.Null);


      Assert.That(finalResult.HostId, Is.Not.Null);
      Assert.That(finalResult.ResourceId, Is.Not.Null);
      Assert.That(finalResult.RequestId, Is.Not.Null);

      Assert.That(finalResult.ResourceLocation, Is.Null);
    }
  }
}
