using System;

namespace Eocron.Algorithms.Backoff
{
    public interface IBackOffIntervalProvider
    {
        TimeSpan GetNext(BackOffContext context);
    }
}