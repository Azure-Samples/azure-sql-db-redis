using System;
namespace ReaderFunction
{
    public class ReaderFunctionOptions
    {
        public const string Section = "ReaderFunctionSettings";
        public const string RedisKey = "REDIS_LEADERBOARD";
        public string SQLConnectionString { get; set; }
        public string RedisHost { get; set; }
        public string RedisPort { get; set; }
        public string RedisPassword { get; set; }
        public bool IsACRE { get; set; }

        public string GetRedisEnpoint()
        {
            if (string.IsNullOrEmpty(RedisHost))
            {
                RedisHost = "127.0.0.1";
                RedisPort = "6379";
            }

            if (IsACRE)
            {
                return $"{RedisHost}:{RedisPort},ssl=true,password={RedisPassword}";
            }

            if (RedisPassword != null)
            {
                return $"{RedisPassword}@{RedisHost}:{RedisPort}";
            }
            else
            {
                return $"{RedisHost}:{RedisPort}";
            }
        }
    }
}

