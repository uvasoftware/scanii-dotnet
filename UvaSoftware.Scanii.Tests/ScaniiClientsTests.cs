using System;
using NUnit.Framework;

namespace UvaSoftware.Scanii.Tests
{
  [TestFixture]
  public class ScaniiClientsTests
  {
    [Test]
    public void ValidateCredentials()
    {
      Assert.Throws<ArgumentNullException>(() => { ScaniiClients.CreateDefault("f", null); });
      Assert.Throws<ArgumentNullException>(() => { ScaniiClients.CreateDefault(null, "f"); });
      Assert.Throws<ArgumentException>(() => { ScaniiClients.CreateDefault("foo:bar", null); });
      Assert.Throws<ArgumentException>(() => { ScaniiClients.CreateDefault("foo","bar:goo"); });
    }
  }
}
