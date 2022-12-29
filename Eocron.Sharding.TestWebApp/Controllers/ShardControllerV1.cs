using System.Threading.Channels;
using Eocron.Sharding.TestWebApp.Shards;
using Microsoft.AspNetCore.Mvc;

namespace Eocron.Sharding.TestWebApp.Controllers
{
    [ApiController]
    [Route("api/v1/shards")]
    public class ShardControllerV1 : ControllerBase
    {
        private readonly IShardProvider<string, string, string> _shardProvider;

        public ShardControllerV1(IShardProvider<string, string, string> shardProvider)
        {
            _shardProvider = shardProvider;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchShards(bool? ready, CancellationToken ct)
        {
            return Ok(await SearchShardIds(ready, ct).ConfigureAwait(false));
        }

        [HttpGet("{id}/is_ready")]
        public async Task<IActionResult> IsReady([FromRoute(Name = "id")] string shardId, CancellationToken ct)
        {
            var shard = _shardProvider.FindShardById(shardId);
            if (shard == null)
                return NotFound();
            return Ok(await shard.IsReadyAsync(ct).ConfigureAwait(false));
        }

        [HttpGet("{id}/is_stopped")]
        public async Task<IActionResult> IsStopped([FromRoute(Name = "id")] string shardId, CancellationToken ct)
        {
            var shard = _shardProvider.FindShardById(shardId);
            if (shard == null)
                return NotFound();
            return Ok(await shard.IsStoppedAsync(ct).ConfigureAwait(false));
        }

        [HttpPost("{id}/fetch_errors")]
        public IActionResult FetchErrors([FromRoute(Name = "id")] string shardId, CancellationToken ct)
        {
            var shard = _shardProvider.FindShardById(shardId);
            if (shard == null)
                return NotFound();
            var result = FetchLatest(shard.Errors, 100);
            return Ok(result);
        }

        [HttpPost("{id}/fetch_outputs")]
        public IActionResult FetchOutput([FromRoute(Name = "id")] string shardId, CancellationToken ct)
        {
            var shard = _shardProvider.FindShardById(shardId);
            if (shard == null)
                return NotFound();
            var result = FetchLatest(shard.Outputs, 100);
            return Ok(result);
        }

        [HttpPost("{id}/restart")]
        public async Task<IActionResult> Restart([FromRoute(Name = "id")] string shardId, CancellationToken ct)
        {
            var shard = _shardProvider.FindShardById(shardId);
            if (shard == null)
                return NotFound();
            await shard.RestartAsync(ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> Stop([FromRoute(Name = "id")] string shardId, CancellationToken ct)
        {
            var shard = _shardProvider.FindShardById(shardId);
            if (shard == null)
                return NotFound();
            await shard.StopAsync(ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start([FromRoute(Name = "id")] string shardId, CancellationToken ct)
        {
            var shard = _shardProvider.FindShardById(shardId);
            if (shard == null)
                return NotFound();
            await shard.StartAsync(ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/publish")]
        public async Task<IActionResult> PublishAsync([FromRoute(Name = "id")]string shardId, [FromBody]string[] messages, CancellationToken ct)
        {
            var shard = _shardProvider.FindShardById(shardId);
            if (shard == null)
                return NotFound();
            await shard.PublishAsync(messages, ct).ConfigureAwait(false);
            return NoContent();
        }

        private async Task<List<string>> SearchShardIds(bool? isReady, CancellationToken ct)
        {
            var all = await Task.WhenAll(_shardProvider
                    .GetAllShards()
                    .Select(async x =>
                    {
                        if (isReady == null)
                            return x;
                        var ready = await x.IsReadyAsync(ct).ConfigureAwait(false);
                        return ready == isReady.Value ? x : null;
                    }))
                .ConfigureAwait(false);

            return all.Where(x => x != null).Select(x => x.Id).ToList();
        }

        private static IList<T> FetchLatest<T>(ChannelReader<T> channel, int maxSize)
        {
            var result = new List<T>();
            while (result.Count != maxSize && channel.TryRead(out var item))
            {
                result.Add(item);
            }
            return result;
        }
    }
}