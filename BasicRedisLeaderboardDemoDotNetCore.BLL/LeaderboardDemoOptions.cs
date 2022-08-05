namespace BasicRedisLeaderboardDemoDotNetCore.BLL
{
	public class LeaderboardDemoOptions
	{
		public const string Section = "LeaderboardSettings";
        public const string RedisKey = "REDIS_LEADERBOARD";
        public string EventGridUrl { get; set; }
		public string EventGridAccessKey { get; set; }
		public string RedisHost { get; set; }
        public string RedisPort { get; set; }
		public string RedisPassword { get; set; }
		public bool IsACRE { get; set; }
        public bool AllowAdmin { get; set; }
        public bool DeleteAllKeysOnLoad { get; set; }

        public string GetRedisEnpoint()
        {
            if (string.IsNullOrEmpty(RedisHost))
            {
                RedisHost = "127.0.0.1";
                RedisPort = "6379";
            }

            if (IsACRE)
            { 
                return $"{RedisHost}:{RedisPort},ssl=true,password={RedisPassword},allowAdmin={AllowAdmin},syncTimeout=5000,connectTimeout=1000";                
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

