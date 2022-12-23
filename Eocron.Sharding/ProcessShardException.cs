using System;

namespace Eocron.Sharding
{
    [Serializable]
    public class ProcessShardException : Exception
    {
        public ProcessShardException(){}

        public ProcessShardException(string message, string shardId, int? processId, int? exitCode) : base(message)
        {
            if (processId != null)
            {
                Data["processId"] = processId.Value;
            }

            if (exitCode != null)
            {
                Data["exitCode"] = exitCode.Value;
            }

            Data["shardId"] = shardId;
        }
    }
}
