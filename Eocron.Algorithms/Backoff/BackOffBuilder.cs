namespace Eocron.Algorithms.Backoff
{
    public class BackOffBuilder
    {
        internal IBackOffIntervalProvider _provider;


        public IBackOffIntervalProvider Build()
        {
            return _provider;
        }
    }
}