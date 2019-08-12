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

    /// <summary>
    /// Creates a new Scanii Client from a key/secret pair
    /// </summary>
    /// <param name="target">the API target to use @see ScaniiTarget</param>
    /// <param name="key">the key to use</param>
    /// <param name="secret">the secret to use</param>
    public ScaniiClient(ScaniiTarget target, string key, string secret)
    {
      Initialize(target, key, secret);
    }

    /// <summary>
    /// Creates a new Scanii Client from a key/secret pair with the default geo-balanced target
    /// </summary>
    /// <param name="key">the key to use</param>
    /// <param name="secret">the secret to use</param>
    public ScaniiClient(string key, string secret)
    {
      Initialize(ScaniiTarget.V21, key, secret);
    }

    /// <summary>
    /// Creates a new Scanii Client from a authentication token
    /// </summary>
    /// <param name="target">the API version and location target @see ScaniiTarget</param>
    /// <param name="token">your authentication token</param>
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

    /// <summary>
    /// Submits a file to be processed (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="path">file path on the local system</param>
    /// <param name="metadata">optional metadata to be added to this file</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    public ScaniiResult Process(string path, Dictionary<string, string> metadata)
    {
      var req = new RestRequest("files", Method.POST);

      // adding payload
      req.AddFile("file", path);

      foreach (var keyValuePair in metadata)
      {
        Log.Logger.Debug("metadata item " + keyValuePair);
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

    /// <summary>
    /// Submits a file to be processed (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="path">file path on the local system</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    public ScaniiResult Process(string path)
    {
      return Process(path, new Dictionary<string, string>());
    }

    /// <summary>
    /// Submits a file to be processed asynchronously (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="path">file path on the local system</param>
    /// <param name="callback">location (URL) to be notified and receive the result</param>
    /// <param name="metadata">optional metadata to be added to this file</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    public ScaniiResult ProcessAsync(string path, string callback, Dictionary<string, string> metadata)
    {
      var req = new RestRequest("files/async", Method.POST);

      // adding payload
      req.AddFile("file", path);

      foreach (var keyValuePair in metadata)
      {
        Log.Logger.Debug("metadata item " + keyValuePair);
        req.AddParameter($"metadata[{keyValuePair.Key}]", keyValuePair.Value);
      }

      if (callback != null)
      {
        req.AddParameter("callback", callback);
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

    /// <summary>
    /// Submits a file to be processed asynchronously (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="path">file path on the local system</param>
    /// <param name="metadata">optional metadata to be added to this file</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    public ScaniiResult ProcessAsync(string path, Dictionary<string, string> metadata = null)
    {
      return ProcessAsync(path, null, metadata ?? new Dictionary<string, string>());
    }

    /// <summary>
    /// Submits a file to be processed asynchronously (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="path">file path on the local system</param>
    /// <param name="callback">location (URL) to be notified and receive the result</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    public ScaniiResult ProcessAsync(string path, string callback)
    {
      return ProcessAsync(path, callback, new Dictionary<string, string>());
    }

    /// <summary>
    /// Fetches the results of a previously processed file (http://docs.scanii.com/v2.1/resources.html#files)
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

    /// <summary>
    /// Pings the scanii service using the credentials provided (http://docs.scanii.com/v2.1/resources.html#ping)
    /// </summary>
    /// <returns>true if ping was successful, false otherwise</returns>
    /// <exception cref="ScaniiException"></exception>
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

    /// <summary>
    /// Makes a fetch call to scanii (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="location">location (URL) of the content to be processed</param>
    /// <returns></returns>
    public ScaniiResult Fetch(string location)
    {
      return Fetch(location, null, new Dictionary<string, string>());
    }

    /// <summary>
    /// Makes a fetch call to scanii (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="location">location (URL) of the content to be processed</param>
    /// <param name="callback">location (URL) to be notified and receive the result</param>
    /// <returns></returns>
    public ScaniiResult Fetch(string location, string callback)
    {
      return Fetch(location, callback, new Dictionary<string, string>());
    }

    /// <summary>
    /// Makes a fetch call to scanii (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="location">location (URL) of the content to be processed</param>
    /// <param name="callback">location (URL) to be notified and receive the result</param>
    /// <param name="metadata">optional metadata to be added to this file</param>
    /// <returns></returns>
    /// <exception cref="ScaniiException"></exception>
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
        Log.Logger.Debug("metadata item " + keyValuePair);
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

    /// <summary>
    /// Creates a new temporary authentication token (http://docs.scanii.com/v2.1/resources.html#auth-tokens)
    /// </summary>
    /// <param name="timeoutInSeconds">How long the token should be valid for</param>
    /// <returns>the new auth token</returns>
    /// <exception cref="ScaniiException"></exception>
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

    /// <summary>
    /// Deletes a previously created authentication token 
    /// </summary>
    /// <param name="id">the id of the token to be deleted</param>
    /// <exception cref="ScaniiException"></exception>
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

    /// <summary>
    /// Retrieves a previously created auth token
    /// </summary>
    /// <param name="id">the id of the token to be retrieved</param>
    /// <returns></returns>
    /// <exception cref="ScaniiException"></exception>
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
