using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding.Jobs
{
    public sealed class CompoundJob : IJob
    {
        private readonly IJob[] _inners;

        public CompoundJob(params IJob[] inners)
        {
            _inners = inners ?? throw new ArgumentNullException(nameof(inners));
        }

        public void Dispose()
        {
            foreach (var inner in _inners)
            {
                inner.Dispose();
            }
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await Task.WhenAll(_inners.Select(x => x.RunAsync(ct))).ConfigureAwait(false);
        }
    }
}