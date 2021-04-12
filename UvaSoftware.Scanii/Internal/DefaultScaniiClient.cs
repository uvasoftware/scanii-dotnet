using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UvaSoftware.Scanii.Entities;
using System.Text.Json;

namespace UvaSoftware.Scanii.Internal
{
  public class DefaultScaniiClient : IScaniiClient
  {
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public DefaultScaniiClient(ScaniiTarget target, string key, string secret, ILogger logger, HttpClient httpClient)
    {
      _logger = logger;
      _httpClient = httpClient;
      ConfigureClient(target, key, secret);
    }

    public async Task<ScaniiProcessingResult> Process(Stream contents, string callback = null,
      Dictionary<string, string> metadata = null)
    {
      var formDataContent = new MultipartFormDataContent {{new StreamContent(contents), "file"}};

      if (metadata != null)
        foreach (var keyValuePair in metadata)
          formDataContent.Add(new StringContent(keyValuePair.Value), $"metadata[{keyValuePair.Key}]");

      if (callback != null) formDataContent.Add(new StringContent(callback), "callback");

      using var response = _httpClient.PostAsync("/v2.1/files", formDataContent).Result;
      _logger.LogDebug("status code {Code}", response.StatusCode);

      if (response.StatusCode == HttpStatusCode.Created)
      {
        return DecorateEntity(
          await JsonSerializer.DeserializeAsync<ScaniiProcessingResult>(await response.Content.ReadAsStreamAsync()),
          response);
      }

      var responseBody = await response.Content.ReadAsStringAsync();
      throw new ScaniiException(
        $"Invalid HTTP response from service, code: {response.StatusCode} message: {responseBody}");
    }

    public Task<ScaniiProcessingResult> Process(string path, string callback = null,
      Dictionary<string, string> metadata = null)
    {
      using var stream = File.Open(path, FileMode.Open);
      return Process(stream, callback, metadata);
    }

    public async Task<ScaniiPendingResult> ProcessAsync(Stream contents, string callback = null,
      Dictionary<string, string> metadata = null)
    {
      var req = new MultipartFormDataContent {{new StreamContent(contents), "file"}};

      if (callback != null) req.Add(new StringContent(callback), "callback");

      if (metadata != null)
        foreach (var keyValuePair in metadata)
          req.Add(new StringContent(keyValuePair.Value), $"metadata[{keyValuePair.Key}]");

      using var response = await _httpClient.PostAsync("/v2.1/files/async", req);
      _logger.LogDebug("status code {Code}", response.StatusCode);

      if (response.StatusCode == HttpStatusCode.Accepted)
        return DecorateEntity(
          await JsonSerializer.DeserializeAsync<ScaniiPendingResult>(await response.Content.ReadAsStreamAsync()),
          response);

      var responseBody = await response.Content.ReadAsStringAsync();
      throw new ScaniiException(
        $"Invalid HTTP response from service, code: {response.StatusCode} message: {responseBody}");
    }

    public Task<ScaniiPendingResult> ProcessAsync(string path, string callback = null,
      Dictionary<string, string> metadata = null)
    {
      using var stream = File.Open(path, FileMode.Open);
      return ProcessAsync(stream, callback, metadata);
    }

    public async Task<ScaniiProcessingResult> Retrieve(string id)
    {
      using var response = await _httpClient.GetAsync($"/v2.1/files/{id}");

      if (response.StatusCode != HttpStatusCode.OK)
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {response.StatusCode} message: {response.Content.ReadAsStringAsync()}");

      var body = await response.Content.ReadAsStringAsync();
      _logger.LogDebug(body);
      return DecorateEntity(
        JsonSerializer.Deserialize<ScaniiProcessingResult>(body),
        response);
    }

    public async Task<bool> Ping()
    {
      using var response = await _httpClient.GetAsync("/v2.1/ping");
      if (response.StatusCode != HttpStatusCode.OK)
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {response.StatusCode}, message: {response.Content.ReadAsStringAsync()}");

      return true;
    }

    public async Task<ScaniiPendingResult> Fetch(string location, string callback = null,
      Dictionary<string, string> metadata = null)
    {
      if (location == null) throw new ArgumentNullException(nameof(location));

      var parameters = new Dictionary<string, string> {{"location", location}};

      if (callback != null) parameters.Add("callback", callback);

      if (metadata != null)
        foreach (var keyValuePair in metadata)
          parameters.Add($"metadata[{keyValuePair.Key}]", keyValuePair.Value);

      using var response = await _httpClient.PostAsync("/v2.1/files/fetch", new FormUrlEncodedContent(parameters));
      _logger.LogDebug("status code {Code}", response.StatusCode);

      if (response.StatusCode == HttpStatusCode.Accepted)
        return DecorateEntity(
          await JsonSerializer.DeserializeAsync<ScaniiPendingResult>(await response.Content.ReadAsStreamAsync()),
          response);

      var responseBody = await response.Content.ReadAsStringAsync();
      throw new ScaniiException(
        $"Invalid HTTP response from service, code: {response.StatusCode} message: {responseBody}");
    }

    public async Task<ScaniiAuthToken> CreateAuthToken(int timeoutInSeconds = 300)
    {
      var parameters = new Dictionary<string, string> {{"timeout", timeoutInSeconds.ToString()}};

      var req = new FormUrlEncodedContent(parameters);
      using var response = await _httpClient.PostAsync("/v2.1/auth/tokens", req);

      if (response.StatusCode == HttpStatusCode.Created)
        return DecorateEntity(
          await JsonSerializer.DeserializeAsync<ScaniiAuthToken>(await response.Content.ReadAsStreamAsync()),
          response);

      var responseBody = await response.Content.ReadAsStringAsync();
      throw new ScaniiException(
        $"Invalid HTTP response from service, code: {response.StatusCode} message: {responseBody}");
    }

    public async Task DeleteAuthToken(string id)
    {
      _logger.LogInformation("deleting auth token {Token}", id);
      using var response = await _httpClient.DeleteAsync($"/v2.1/auth/tokens/{id}");

      if (response.StatusCode == HttpStatusCode.NoContent) return;

      var responseBody = await response.Content.ReadAsStringAsync();
      throw new ScaniiException(
        $"Invalid HTTP response from service, code: {response.StatusCode} message: {responseBody}");
    }

    public async Task<ScaniiAuthToken> RetrieveAuthToken(string id)
    {
      using var response = await _httpClient.GetAsync($"/v2.1/auth/tokens/{id}");
      if (response.StatusCode != HttpStatusCode.OK)
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {response.StatusCode} message: {response.Content.ReadAsStringAsync()}");

      return DecorateEntity(
        await JsonSerializer.DeserializeAsync<ScaniiAuthToken>(await response.Content.ReadAsStreamAsync()),
        response);
    }

    private void ConfigureClient(ScaniiTarget target, string key, string secret)
    {
      var version = Assembly.GetExecutingAssembly().GetName().Version;

      _httpClient.BaseAddress = target.Endpoint;
      _httpClient.DefaultRequestHeaders.Add("User-Agent", $"{HttpHeaders.UserAgent}/v{version}");
      _httpClient.DefaultRequestHeaders.Add("Authorization",
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}")));

      _logger.LogInformation("starting client with version {Version} and target {@Target}", version, target);
    }

    private static T DecorateEntity<T>(T entity, HttpResponseMessage response) where T : ScaniiResult
    {
      entity.StatusCode = response.StatusCode.GetHashCode();
      if (response.Headers.Contains(HttpHeaders.XHostHeader))
        entity.HostId = response.Headers.GetValues(HttpHeaders.XHostHeader).First();

      if (response.Headers.Contains(HttpHeaders.Location))
        entity.ResourceLocation = response.Headers.GetValues(HttpHeaders.Location).First();

      if (response.Headers.Contains(HttpHeaders.XRequestHeader))
        entity.RequestId = response.Headers.GetValues(HttpHeaders.XRequestHeader).First();

      return entity;
    }
  }
}
