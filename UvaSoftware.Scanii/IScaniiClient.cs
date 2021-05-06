using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UvaSoftware.Scanii.Entities;

namespace UvaSoftware.Scanii
{
  /// <summary>
  /// Interface for a Scanii API client
  /// </summary>
  public interface IScaniiClient
  {
    /// <summary>
    ///   Submits a stream to be processed (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="contents">stream of the content to be analyzed</param>
    /// <param name="callback">optional location (URL) to be notified and receive the result</param>
    /// <param name="metadata">optional metadata to be added to this analysis</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    Task<ScaniiProcessingResult> Process(Stream contents, string callback = null,
      Dictionary<string, string> metadata = null);

    /// <summary>
    ///   Submits a file to be processed (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="path">file path on the local system</param>
    /// <param name="callback">optional location (URL) to be notified and receive the result</param>
    /// <param name="metadata">optional metadata to be added to this analysis</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    Task<ScaniiProcessingResult> Process(string path, string callback = null,
      Dictionary<string, string> metadata = null);

    /// <summary>
    ///   Submits a stream to be processed asynchronously (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="contents">stream of the content to be analyzed</param>
    /// <param name="metadata">optional metadata to be added to this analysis</param>
    /// <param name="callback">optional location (URL) to be notified and receive the result</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    Task<ScaniiPendingResult> ProcessAsync(Stream contents, string callback = null,
      Dictionary<string, string> metadata = null);

    /// <summary>
    ///   Submits a file to be processed asynchronously (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="path">file path on the local system</param>
    /// <param name="metadata">optional metadata to be added to this analysis</param>
    /// <param name="callback">optional location (URL) to be notified and receive the result</param>
    /// <returns>processing result</returns>
    /// <exception cref="ScaniiException"></exception>
    Task<ScaniiPendingResult> ProcessAsync(string path, string callback = null,
      Dictionary<string, string> metadata = null);

    /// <summary>
    ///   Fetches the results of a previously processed file (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="id">id of the content/file to be retrieved</param>
    /// <returns>ScaniiResult</returns>
    /// <exception cref="ScaniiException"></exception>
    Task<ScaniiProcessingResult> Retrieve(string id);

    /// <summary>
    ///   Pings the scanii service using the credentials provided (http://docs.scanii.com/v2.1/resources.html#ping)
    /// </summary>
    /// <returns>true if ping was successful, false otherwise</returns>
    /// <exception cref="ScaniiException"></exception>
    Task<bool> Ping();

    /// <summary>
    ///   Makes a fetch call to scanii (http://docs.scanii.com/v2.1/resources.html#files)
    /// </summary>
    /// <param name="location">location (URL) of the content to be processed</param>
    /// <param name="callback">optional location (URL) to be notified and receive the result</param>
    /// <param name="metadata">optional metadata to be added to this file</param>
    /// <returns></returns>
    /// <exception cref="ScaniiException"></exception>
    Task<ScaniiPendingResult> Fetch(string location, string callback = null,
      Dictionary<string, string> metadata = null);

    /// <summary>
    ///   Creates a new temporary authentication token (http://docs.scanii.com/v2.1/resources.html#auth-tokens)
    /// </summary>
    /// <param name="timeoutInSeconds">How long the token should be valid for</param>
    /// <returns>the new auth token</returns>
    /// <exception cref="ScaniiException"></exception>
    Task<ScaniiAuthToken> CreateAuthToken(int timeoutInSeconds = 300);

    /// <summary>
    ///   Deletes a previously created authentication token
    /// </summary>
    /// <param name="id">the id of the token to be deleted</param>
    /// <exception cref="ScaniiException"></exception>
    Task DeleteAuthToken(string id);

    /// <summary>
    ///   Retrieves a previously created auth token
    /// </summary>
    /// <param name="id">the id of the token to be retrieved</param>
    /// <returns></returns>
    /// <exception cref="ScaniiException"></exception>
    Task<ScaniiAuthToken> RetrieveAuthToken(string id);
  }
}
