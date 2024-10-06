namespace Eocron.Algorithms.Backoff
{
    public class BackOffBuilder
    {
        internal IBackOffIntervalProvider Provider;


        public IBackOffIntervalProvider Build()
        {
            return Provider;
        }
    }
}