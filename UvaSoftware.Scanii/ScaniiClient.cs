using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using RestSharp;
using RestSharp.Authenticators;
using Serilog;
using UvaSoftware.Scanii.Internal;

namespace UvaSoftware.Scanii
{
  public class ScaniiClient
  {
    private RestClient RestClient { get; set; }
    private Version _version;

    public ScaniiClient(ScaniiTarget target, string key, string secret)
    {
      Initialize(target, key, secret);
    }

    public ScaniiClient(string key, string secret)
    {
      Initialize(ScaniiTarget.V21, key, secret);
    }

    public ScaniiClient(ScaniiTarget target, string token)
    {
      Initialize(target, token, null);
    }

    private void Initialize(ScaniiTarget target, string key, string secret)
    {
      _version = Assembly.GetExecutingAssembly().GetName().Version;

      RestClient = new RestClient(Endpoints.Resolve(target))
      {
        Authenticator = new HttpBasicAuthenticator(key, secret),
        UserAgent = HttpHeaders.Ua + "/v" + _version
      };

      Log.Logger.Information("starting client with version {version} and target {target}", _version, target);
    }

    public ScaniiResult Process(string path, Dictionary<string, string> metadata)
    {
      var req = new RestRequest("files", Method.POST);

      // adding payload
      req.AddFile("file", path);

      foreach (var keyValuePair in metadata)
      {
        Log.Logger.Debug("medata item " + keyValuePair);
        req.AddParameter($"metadata[{keyValuePair.Key}]", keyValuePair.Value);
      }

      var resp = RestClient.Execute(req);

      if (resp.StatusCode != HttpStatusCode.Created)
      {
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {resp.StatusCode} message: {resp.Content}");
      }

      Log.Logger.Information("response: " + resp.Content);
      Log.Logger.Debug("content " + resp.Content);
      Log.Logger.Debug("status code " + resp.StatusCode);

      return ResponseProcessor.Process(resp);
    }

    public ScaniiResult Process(string path)
    {
      return Process(path, new Dictionary<string, string>());
    }

    public ScaniiResult ProcessAsync(string path, Dictionary<string, string> metadata)
    {
      var req = new RestRequest("files/async", Method.POST);

      // adding payload
      req.AddFile("file", path);

      foreach (var keyValuePair in metadata)
      {
        Log.Logger.Debug("medata item " + keyValuePair);
        req.AddParameter($"metadata[{keyValuePair.Key}]", keyValuePair.Value);
      }

      var resp = RestClient.Execute(req);

      if (resp.StatusCode != HttpStatusCode.Accepted)
      {
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {resp.StatusCode} message: {resp.Content}");
      }

      Log.Logger.Information("response: " + resp.Content);
      Log.Logger.Debug("content " + resp.Content);
      Log.Logger.Debug("status code " + resp.StatusCode);

      return ResponseProcessor.Process(resp);
    }


    public ScaniiResult ProcessAsync(string path)
    {
      return ProcessAsync(path, new Dictionary<string, string>());
    }

    /// <summary>
    /// Fetches the results of a previously processed file @see <a href="http://docs.scanii.com/v2.1/resources.html#files">http://docs.scanii.com/v2.1/resources.html#files</a>
    /// </summary>
    /// <param name="id">id of the content/file to be retrieved</param>
    /// <returns>ScaniiResult</returns>
    /// <exception cref="ScaniiException"></exception>
    public ScaniiResult Retrieve(string id)
    {
      var req = new RestRequest("files/{id}", Method.GET);
      req.AddParameter("id", id, ParameterType.UrlSegment);
      var resp = RestClient.Execute(req);

      if (resp.StatusCode != HttpStatusCode.OK)
      {
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {resp.StatusCode} message: {resp.Content}");
      }

      return ResponseProcessor.Process(resp);
    }

    public bool Ping()
    {
      var response = RestClient.Execute(new RestRequest("ping", Method.GET));
      if (response.StatusCode != HttpStatusCode.OK)
      {
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {response.StatusCode}, message: {response.Content}");
      }

      return true;
    }

    public ScaniiResult Fetch(string location)
    {
      return Fetch(location, null, new Dictionary<string, string>());
    }

    public ScaniiResult Fetch(string location, string callback)
    {
      return Fetch(location, callback, new Dictionary<string, string>());
    }

    public ScaniiResult Fetch(string location, string callback, Dictionary<string, string> metadata)
    {
      var req = new RestRequest("files/fetch", Method.POST);

      if (location != null)
      {
        req.AddParameter("location", location);
      }

      if (callback != null)
      {
        req.AddParameter("callback", callback);
      }

      foreach (var keyValuePair in metadata)
      {
        Log.Logger.Debug("medata item " + keyValuePair);
        req.AddParameter($"metadata[{keyValuePair.Key}]", keyValuePair.Value);
      }

      var resp = RestClient.Execute(req);

      if (resp.StatusCode != HttpStatusCode.Accepted)
      {
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {resp.StatusCode} message: {resp.Content}");
      }

      Log.Logger.Information("response: " + resp.Content);
      Log.Logger.Debug("content " + resp.Content);
      Log.Logger.Debug("status code " + resp.StatusCode);

      return ResponseProcessor.Process(resp);
    }

    public ScaniiResult CreateAuthToken(int timeoutInSeconds = 300)
    {
      var req = new RestRequest("auth/tokens", Method.POST);
      req.AddParameter("timeout", timeoutInSeconds);
      var resp = RestClient.Execute(req);

      if (resp.StatusCode != HttpStatusCode.Created)
      {
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {resp.StatusCode} message: {resp.Content}");
      }

      Log.Logger.Information("response: " + resp.Content);
      Log.Logger.Debug("content " + resp.Content);
      Log.Logger.Debug("status code " + resp.StatusCode);

      return ResponseProcessor.Process(resp);
    }

    public void DeleteAuthToken(string id)
    {
      var req = new RestRequest("auth/tokens/{id}", Method.DELETE);
      req.AddParameter("id", id, ParameterType.UrlSegment);

      var resp = RestClient.Execute(req);

      if (resp.StatusCode != HttpStatusCode.NoContent)
      {
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {resp.StatusCode} message: {resp.Content}");
      }

      Log.Logger.Information("response: " + resp.Content);
      Log.Logger.Debug("content " + resp.Content);
      Log.Logger.Debug("status code " + resp.StatusCode);
    }

    public ScaniiResult RetrieveAuthToken(string id)
    {
      var req = new RestRequest("auth/tokens/{id}", Method.GET);
      req.AddParameter("id", id, ParameterType.UrlSegment);

      var resp = RestClient.Execute(req);

      if (resp.StatusCode != HttpStatusCode.OK)
      {
        throw new ScaniiException(
          $"Invalid HTTP response from service, code: {resp.StatusCode} message: {resp.Content}");
      }

      Log.Logger.Information("response: " + resp.Content);
      Log.Logger.Debug("content " + resp.Content);
      Log.Logger.Debug("status code " + resp.StatusCode);

      return ResponseProcessor.Process(resp);
    }
  }
}
