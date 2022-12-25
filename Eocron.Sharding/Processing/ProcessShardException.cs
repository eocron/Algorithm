using System;

namespace Eocron.Sharding.Processing
{
    [Serializable]
    public class ProcessShardException : Exception
    {
        public ProcessShardException()
        {
        }

        public ProcessShardException(string message, string shardId, int? processId, int? exitCode) : base(message)
        {
            if (processId != null) Data["process_id"] = processId.Value;

            if (exitCode != null) Data["exit_code"] = exitCode.Value;

            Data["shard_id"] = shardId;
        }
    }
}