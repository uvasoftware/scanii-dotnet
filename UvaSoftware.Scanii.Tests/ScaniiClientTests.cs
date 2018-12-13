using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Serilog;

namespace UvaSoftware.Scanii.Tests
{
  [TestFixture]
  public class ScaniiClientTests
  {
    private readonly ScaniiClient _client;
    private const string Eicar = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
    private const string Finding = "content.malicious.eicar-test-signature";
    private readonly string _eicarFile = Path.GetTempFileName();
    private readonly string _key;
    private readonly string _secret;
    private readonly string _checksum = "cf8bd9dfddff007f75adf4c2be48005cea317c62";
    private readonly string _eicarRemoteChecksum = "bec1b52d350d721c7e22a6d4bb0a92909893a3ae";

    public ScaniiClientTests()
    {
      Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .MinimumLevel.Debug()
        .CreateLogger();

      Log.Logger.Debug("ctor");

      if (Environment.GetEnvironmentVariable("SCANII_CREDS") != null)
      {
        // ReSharper disable once PossibleNullReferenceException
        var creds = Environment.GetEnvironmentVariable("SCANII_CREDS").Split(':');
        _key = creds[0];
        _secret = creds[1];
      }

      _client = new ScaniiClient(_key, _secret);
      using (var output = new StreamWriter(_eicarFile))
      {
        output.WriteLine(Eicar);
      }
    }

    [Test]
    public void ShouldProcessContent()
    {
      // starting with a simple content scan (sync)
      var r = _client.Process(_eicarFile, new Dictionary<string, string>
      {
        {"foo", "bar"}
      });

      Log.Logger.Debug("response: {r}", r);

      Assert.NotNull(r.ResourceId);
      Assert.True(r.Findings.Contains(Finding));
      Assert.AreEqual(1, r.Findings.Count);
      Assert.AreEqual("text/plain", r.ContentType);
      Assert.AreEqual("bar", r.Metadata["foo"]);
      Assert.AreEqual(1, r.Metadata.Count);
      Assert.AreEqual(_checksum, r.Checksum);
      Assert.NotNull(r.ContentLength);
      Assert.NotNull(r.CreationDate);
      Assert.NotNull(r.RawResponse);


      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);
      Assert.NotNull(r.RequestId);

      Assert.Null(r.Message);
      Assert.Null(r.ExpirationDate);
    }

    [Test]
    public void ShouldProcessContentWithoutMetadata()
    {
      // starting with a simple content scan (sync)
      var r = _client.Process(_eicarFile);

      Log.Logger.Debug("response: {r}", r);

      Assert.NotNull(r.ResourceId);
      Assert.True(r.Findings.Contains(Finding));
      Assert.AreEqual(1, r.Findings.Count);
      Assert.AreEqual("text/plain", r.ContentType);
      Assert.AreEqual(0, r.Metadata.Count);
      Assert.AreEqual(_checksum, r.Checksum);
      Assert.NotNull(r.ContentLength);
      Assert.NotNull(r.CreationDate);
      Assert.NotNull(r.RawResponse);


      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);
      Assert.NotNull(r.RequestId);

      Assert.Null(r.Message);
      Assert.Null(r.ExpirationDate);
    }

    [Test]
    public void ShouldProcessAsyncWithoutCallback()
    {
      Log.Logger.Information("submitting content for async processing...");

      var r = _client.ProcessAsync(_eicarFile, new Dictionary<string, string>
      {
        {"foo", "bar"}
      });

      Log.Logger.Debug("response: {r}", r);

      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);

      Assert.AreEqual(0, r.Metadata.Count);
      Assert.AreEqual(0, r.ContentLength);
      Assert.Null(r.CreationDate);
      Assert.Null(r.Checksum);
      Assert.Null(r.ContentType);

      Assert.NotNull(r.RawResponse);
      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.RequestId);

      Assert.Null(r.Message);
      Assert.Null(r.ExpirationDate);


      Log.Logger.Information("request looks good, trying to retrieve result for id: {id}", r.ResourceId);

      Thread.Sleep(1000);

      var finalResult = _client.Retrieve(r.ResourceId);

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("text/plain", finalResult.ContentType);
      Assert.AreEqual("bar", finalResult.Metadata["foo"]);
      Assert.AreEqual(1, finalResult.Metadata.Count);
      Assert.AreEqual(_checksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);
      Assert.NotNull(finalResult.RawResponse);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
      Assert.Null(finalResult.Message);
      Assert.Null(finalResult.ExpirationDate);
    }


    [Test]
    public void ShouldProcessAsyncWithoutCallBackAndMetadata()
    {
      Log.Logger.Information("submitting content for async processing...");

      var r = _client.ProcessAsync(_eicarFile);

      Log.Logger.Debug("response: {r}", r);

      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);

      Assert.AreEqual(0, r.Metadata.Count);
      Assert.AreEqual(0, r.ContentLength);
      Assert.Null(r.CreationDate);
      Assert.Null(r.Checksum);
      Assert.Null(r.ContentType);

      Assert.NotNull(r.RawResponse);
      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.RequestId);

      Assert.Null(r.Message);
      Assert.Null(r.ExpirationDate);


      Log.Logger.Information("request looks good, trying to retrieve result for id: {id}", r.ResourceId);

      Thread.Sleep(1000);

      var finalResult = _client.Retrieve(r.ResourceId);

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("text/plain", finalResult.ContentType);
      Assert.AreEqual(0, finalResult.Metadata.Count);
      Assert.AreEqual(_checksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);
      Assert.NotNull(finalResult.RawResponse);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
      Assert.Null(finalResult.Message);
      Assert.Null(finalResult.ExpirationDate);
    }

    [Test]
    public void ShouldProcessAsyncWithCallback()
    {
      var r = _client.ProcessAsync(_eicarFile, "https://httpbin.org/post");

      Log.Logger.Debug("response: {r}", r);

      Assert.NotNull(r.ResourceId);

      Thread.Sleep(1000);

      var finalResult = _client.Retrieve(r.ResourceId);

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("text/plain", finalResult.ContentType);
      Assert.AreEqual(_checksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);
      Assert.NotNull(finalResult.RawResponse);

      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
      Assert.Null(finalResult.Message);
      Assert.Null(finalResult.ExpirationDate);
    }

    [Test]
    public void ShouldProcessAsyncWithCallbackAndMetadata()
    {
      var r = _client.ProcessAsync(_eicarFile, "https://httpbin.org/post", new Dictionary<string, string>
      {
        {"foo", "bar"}
      });

      Log.Logger.Debug("response: {r}", r);

      Assert.NotNull(r.ResourceId);

      Thread.Sleep(1000);

      var finalResult = _client.Retrieve(r.ResourceId);

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("text/plain", finalResult.ContentType);
      Assert.AreEqual("bar", finalResult.Metadata["foo"]);
      Assert.AreEqual(_checksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);
      Assert.NotNull(finalResult.RawResponse);

      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
      Assert.Null(finalResult.Message);
      Assert.Null(finalResult.ExpirationDate);
    }


    [Test]
    public void ShouldPing()
    {
      Assert.True(_client.Ping());
    }

    [Test]
    public void ShouldFetchContentWithoutCallback()
    {
      var r = _client.Fetch("https://scanii.s3.amazonaws.com/eicarcom2.zip");
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.ResourceLocation);

      Assert.AreEqual(0, r.Metadata.Count);
      Assert.AreEqual(0, r.ContentLength);
      Assert.Null(r.CreationDate);
      Assert.Null(r.Checksum);
      Assert.Null(r.ContentType);

      Assert.NotNull(r.RawResponse);
      Assert.NotNull(r.HostId);
      Assert.NotNull(r.ResourceId);
      Assert.NotNull(r.RequestId);

      Assert.Null(r.Message);
      Assert.Null(r.ExpirationDate);

      Thread.Sleep(1000);

      var finalResult = _client.Retrieve(r.ResourceId);

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("application/zip", finalResult.ContentType);
      Assert.AreEqual(_eicarRemoteChecksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);
      Assert.NotNull(finalResult.RawResponse);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
      Assert.Null(finalResult.Message);
      Assert.Null(finalResult.ExpirationDate);
    }

    [Test]
    public void ShouldFetchContentWithCallback()
    {
      var r = _client.Fetch("https://scanii.s3.amazonaws.com/eicarcom2.zip", "https://httpbin.org/post");

      Thread.Sleep(1000);

      var finalResult = _client.Retrieve(r.ResourceId);

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("application/zip", finalResult.ContentType);
      Assert.AreEqual(_eicarRemoteChecksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);
      Assert.NotNull(finalResult.RawResponse);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
      Assert.Null(finalResult.Message);
      Assert.Null(finalResult.ExpirationDate);
    }

    [Test]
    public void ShouldFetchContentWithCallbackAndMetadata()
    {
      var r = _client.Fetch("https://scanii.s3.amazonaws.com/eicarcom2.zip", "https://httpbin.org/post",
        new Dictionary<string, string>
        {
          {"hello", "world"}
        });

      Thread.Sleep(1000);

      var finalResult = _client.Retrieve(r.ResourceId);

      Assert.NotNull(finalResult.ResourceId);
      Assert.True(finalResult.Findings.Contains(Finding));
      Assert.AreEqual(1, finalResult.Findings.Count);
      Assert.AreEqual("application/zip", finalResult.ContentType);
      Assert.AreEqual("world", finalResult.Metadata["hello"]);
      Assert.AreEqual(1, finalResult.Metadata.Count);
      Assert.AreEqual(_eicarRemoteChecksum, finalResult.Checksum);
      Assert.NotNull(finalResult.ContentLength);
      Assert.NotNull(finalResult.CreationDate);
      Assert.NotNull(finalResult.RawResponse);


      Assert.NotNull(finalResult.HostId);
      Assert.NotNull(finalResult.ResourceId);
      Assert.NotNull(finalResult.RequestId);

      Assert.Null(finalResult.ResourceLocation);
      Assert.Null(finalResult.Message);
      Assert.Null(finalResult.ExpirationDate);
    }

    [Test]
    public void ShouldCreateAuthToken()
    {
      var token = _client.CreateAuthToken(5);
      Assert.NotNull(token.ResourceId);
      Assert.NotNull(token.CreationDate);
      Assert.NotNull(token.ExpirationDate);

      Log.Logger.Information("using auth token to create a new client...");

      var client2 = new ScaniiClient(ScaniiTarget.V21, token.ResourceId);

      Log.Logger.Information("using token to process content");

      var result = client2.Process(_eicarFile);

      Assert.NotNull(result.ResourceId);
      Assert.True(result.Findings.Contains(Finding));
      Assert.AreEqual(1, result.Findings.Count);
      Assert.AreEqual(_checksum, result.Checksum);
      Assert.NotNull(result.ContentLength);
      Assert.NotNull(result.CreationDate);
      Assert.NotNull(result.RawResponse);
    }

    [Test]
    public void ShouldRetrieveAuthToken()
    {
      var token = _client.CreateAuthToken(1);
      var token2 = _client.RetrieveAuthToken(token.ResourceId);

      Assert.AreEqual(token.ResourceId, token2.ResourceId);
      Assert.AreEqual(token.ExpirationDate, token2.ExpirationDate);
      Assert.AreEqual(token.CreationDate, token2.CreationDate);
    }

    [Test]
    public void ShouldDeleteAuthToken()
    {
      var token = _client.CreateAuthToken(1);
      _client.DeleteAuthToken(token.ResourceId);
      var token2 = _client.RetrieveAuthToken(token.ResourceId);

      Assert.AreEqual(token.ResourceId, token2.ResourceId);
      Assert.AreEqual(token.ExpirationDate, token2.ExpirationDate);
      Assert.AreEqual(token.CreationDate, token2.CreationDate);
    }

    [Test]
    public void ShouldPingAllRegions()
    {
      foreach (ScaniiTarget target in Enum.GetValues(typeof(ScaniiTarget)))
      {
        Log.Logger.Information("creating client for target {target}", target);
        Assert.IsTrue(new ScaniiClient(target, _key, _secret).Ping());
      }
    }
  }
}
