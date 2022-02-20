using System;

namespace UvaSoftware.Scanii.Internal
{
  public static class ValidateCredentials
  {
    public static void Validate(string credentials)
    {
      if (credentials == null)
      {
        throw new ArgumentNullException(nameof(credentials));
      }

      if (credentials.Length == 0)
      {
        throw new ArgumentOutOfRangeException(nameof(credentials));
      }
      if (credentials.Contains(":"))
      {
        throw new ArgumentException("credentials must not include the ':' character");
      }

    }
  }
}
