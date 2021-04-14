using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UvaSoftware.Scanii.Tests
{
  [TestFixture]
  public class ScaniiClientTests
  {
    private readonly IScaniiClient _client;
    private readonly ILogger _logger;
    private const string Eicar = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
    private const string Finding = "content.malicious.eicar-test-signature";
    private readonly string _eicarFile = Path.GetTempFileName();
    private readonly string _key;
    private readonly string _secret;
    private const int PollingLimit = 10;
    private const string Checksum = "cf8bd9dfddff007f75adf4c2be48005cea317c62";
    private const string EicarRemoteChecksum = "bec1b52d350d721c7e22a6d4bb0a92909893a3ae";

    public ScaniiClientTests()
    {
      var serilogLogger = new LoggerConfiguration()
        .WriteTo.Console()
        .MinimumLevel.Is(LevelConvert.ToSerilogLevel(LogLevel.Debug))
        .CreateLogger();

      var provider = new SerilogLoggerProvider(serilogLogger);
      _logger = provider.CreateLogger("ScaniiClientTests");

      _logger.LogDebug("ctor");

      if (Environment.GetEnvironmentVariable("SCANII_CREDS") != null)
      {
        // ReSharper disable once PossibleNullReferenceException
        var creds = Environment.GetEnvironmentVariable("SCANII_CREDS").Split(':');
        _key = creds[0];
        _secret = creds[1];
      }

      Debug.Assert(_secret != null, nameof(_secret) + " != null");
      Debug.Assert(_key != null, nameof(_key) + " != null");

      _client = ScaniiClients.CreateDefault(_key, _secret, _logger, new HttpClient());
      using var output = new StreamWriter(_eicarFile);
      output.WriteLine(Eicar);
    }

    [Test]
    public async Task ShouldCreateAuthToken()
    {
      var token = await _client.CreateAuthToken(5);
      Assert.NotNull(token.RequestId);
      Assert.NotNull(token.CreationDate);
      Assert.NotNull(token.ExpirationDate);

      _logger.LogInformation("using auth token to create a new client...");
    }

    [Test]
    public async Task ShouldCreatedUsableAuthTokens()
    {
      var token = await _client.CreateAuthToken(5);
      var client2 = ScaniiClients.CreateDefault(token.ResourceId);

      _logger.LogInformation("using token to process content");

      var result = await client2.Process(_eicarFile);

      Assert.NotNull(result.ResourceId);
      Assert.True(result.Findings.Contains(Finding));
      Assert.AreEqual(1, result.Findings.Count);
      Assert.AreEqual(Checksum, result.Checksum);
      Assert.NotNull(result.ContentLength);
      Assert.NotNull(result.CreationDate);
    }

    [Test]
    public async Task ShouldDeleteAuthToken()
    {
      var token = await _client.CreateAuthToken(1);
      await _client.DeleteAuthToken(token.ResourceId);
      var token2 = await _client.RetrieveAuthToken(token.ResourceId);

      Assert.AreEqual(token.ResourceId, token2.ResourceId);
      Assert.AreEqual(token.ExpirationDate, token2.ExpirationDate);
      Assert.AreEqual(token.CreationDate, token2.CreationDate);
    }

    [Test]
    public async Task ShouldFetchContentWithCallback()
    {
      var r = await _client.Fetch("https://scanii.s3.amazonaws.com/eicarcom2.zip", "https://httpbin.org/post");

      var finalResult = await PollForResult((async () => await _client.Retrieve(r.ResourceId))).Result;

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("application/zip", finalResult.ContentType);
      Assert.AreEqual(EicarRemoteChecksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);
      Assert.Null(finalResult.ResourceLocation);
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

      var finalResult = await PollForResult(async () => await _client.Retrieve(r.ResourceId)).Result;

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("application/zip", finalResult.ContentType);
      Assert.AreEqual("world", finalResult.Metadata["hello"]);
      Assert.AreEqual(1, finalResult.Metadata.Count);
      Assert.AreEqual(EicarRemoteChecksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);
      Assert.AreEqual(200, finalResult.StatusCode);
      Assert.Null(finalResult.ResourceLocation);
    }

    [Test]
    public async Task ShouldFetchContentWithoutCallback()
    {
      var r = await _client.Fetch("https://scanii.s3.amazonaws.com/eicarcom2.zip");
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);
      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.RequestId);

      var finalResult = await PollForResult(async () => await _client.Retrieve(r.ResourceId)).Result;

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("application/zip", finalResult.ContentType);
      Assert.AreEqual(EicarRemoteChecksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
    }


    [Test]
    public async Task ShouldPing()
    {
      Assert.True(await _client.Ping());
    }

    [Test]
    public async Task ShouldPingAllRegions()
    {
      foreach (var target in ScaniiTarget.All())
      {
        _logger.LogInformation("creating client for target {Target}", target.Endpoint);
        var client = ScaniiClients.CreateDefault(_key, _secret, target: target);
        Assert.IsTrue(await client.Ping());
      }
    }

    [Test]
    public async Task ShouldProcessAsyncWithCallback()
    {
      var r = await _client.ProcessAsync(_eicarFile, "https://httpbin.org/post");

      _logger.LogDebug("response: {@R}", r);

      Assert.NotNull(r.ResourceId);

      var finalResult = await PollForResult(async () => await _client.Retrieve(r.ResourceId)).Result;

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("text/plain", finalResult.ContentType);
      Assert.AreEqual(Checksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);

      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
    }

    [Test]
    public async Task ShouldProcessAsyncWithCallbackAndMetadata()
    {
      var r = await _client.ProcessAsync(_eicarFile, "https://httpbin.org/post", new Dictionary<string, string>
      {
        {"foo", "bar"}
      });

      _logger.LogDebug("response: {@R}", r);

      Assert.NotNull(r.ResourceId);

      var finalResult = await PollForResult(async () => await _client.Retrieve(r.ResourceId)).Result;

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("text/plain", finalResult.ContentType);
      Assert.AreEqual("bar", finalResult.Metadata["foo"]);
      Assert.AreEqual(Checksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);

      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
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

      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);

      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.RequestId);


      _logger.LogInformation("request looks good, trying to retrieve result for id: {Id}", r.ResourceId);

      var finalResult = await PollForResult(async () => await _client.Retrieve(r.ResourceId)).Result;

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("text/plain", finalResult.ContentType);
      Assert.AreEqual("bar", finalResult.Metadata["foo"]);
      Assert.AreEqual(1, finalResult.Metadata.Count);
      Assert.AreEqual(Checksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
    }

    [Test]
    public async Task ShouldProcessAsyncWithoutCallBackAndMetadata()
    {
      _logger.LogInformation("submitting content for async processing...");

      var r = await _client.ProcessAsync(_eicarFile);

      _logger.LogDebug("response: {@R}", r);

      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);

      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.RequestId);

      _logger.LogInformation("request looks good, trying to retrieve result for id: {Id}", r.ResourceId);


      var finalResult = await PollForResult(async () => await _client.Retrieve(r.ResourceId)).Result;


      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("text/plain", finalResult.ContentType);
      Assert.AreEqual(0, finalResult.Metadata.Count);
      Assert.AreEqual(Checksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
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

      Assert.NotNull(r.ResourceId);
      Assert.True(r.Findings.Contains(Finding));
      Assert.AreEqual(1, r.Findings.Count);
      Assert.AreEqual("text/plain", r.ContentType);
      Assert.AreEqual("bar", r.Metadata["foo"]);
      Assert.AreEqual(1, r.Metadata.Count);
      Assert.AreEqual(Checksum, r.Checksum);
      Assert.NotNull(r.ContentLength);
      Assert.NotNull(r.CreationDate);


      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);
      Assert.NotNull(r.RequestId);
      Assert.NotNull(r.StatusCode);
    }

    [Test]
    public async Task ShouldProcessContentWithoutMetadata()
    {
      // starting with a simple content scan (sync)
      var r = await _client.Process(_eicarFile);

      _logger.LogDebug("response: {@R}", r);

      Assert.NotNull(r.ResourceId);
      Assert.True(r.Findings.Contains(Finding));
      Assert.AreEqual(1, r.Findings.Count);
      Assert.AreEqual("text/plain", r.ContentType);
      Assert.AreEqual(0, r.Metadata.Count);
      Assert.AreEqual(Checksum, r.Checksum);
      Assert.NotNull(r.ContentLength);
      Assert.NotNull(r.CreationDate);


      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);
      Assert.NotNull(r.RequestId);
    }

    [Test]
    public async Task ShouldRetrieveAuthToken()
    {
      var token = await _client.CreateAuthToken(1);
      var token2 = await _client.RetrieveAuthToken(token.ResourceId);

      Assert.AreEqual(token.ResourceId, token2.ResourceId);
      Assert.AreEqual(token.ExpirationDate, token2.ExpirationDate);
      Assert.AreEqual(token.CreationDate, token2.CreationDate);
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
      var finalResult = await PollForResult(async () => await _client.Retrieve(r.ResourceId)).Result;

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("application/zip", finalResult.ContentType);
      Assert.AreEqual("world", finalResult.Metadata["hello"]);
      Assert.AreEqual(1, finalResult.Metadata.Count);
      Assert.AreEqual(EicarRemoteChecksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
    }

    private static async Task<T> PollForResult<T>(Func<T> function)
    {
      var attempt = 0;
      while (true)
      {
        await Console.Out.WriteLineAsync($"polling for result {attempt + 1}/{PollingLimit}");
        try
        {
          return function.Invoke();
        }
        catch (ScaniiException e)
        {
          attempt += 1;
          if (attempt > PollingLimit)
            throw;
          Thread.Sleep(attempt * 500);
        }
      }
    }
  }
}
