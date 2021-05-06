using System;
using System.Threading;
using System.Threading.Tasks;

namespace UvaSoftware.Scanii.Tests
{
  public class TestUtils
  {
    private const int PollingLimit = 10;

    public static T PollForResult<T>(Func<Task<T>> task)
    {
      var attempt = 0;
      while (true)
      {
        Console.Out.WriteLine($"polling for result {attempt + 1}/{PollingLimit}");
        try
        {
          return task.Invoke().Result;
        }
        catch (AggregateException)
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
