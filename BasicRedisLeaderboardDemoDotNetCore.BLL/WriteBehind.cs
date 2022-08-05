using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace BasicRedisLeaderboardDemoDotNetCore.BLL
{
	public class WriteBehind
	{
        private readonly IConnectionMultiplexer _redisConnection;
        private IDatabase _db;
        private string _tableName;


        public WriteBehind(IConnectionMultiplexer redisConnection, string tableName)
        {
            _redisConnection = redisConnection ?? throw new ArgumentException(nameof(redisConnection));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _db = _redisConnection.GetDatabase();
        }

		public void SafeDeleteKey(RedisKey key)
        {
			try
            {
				string newKey = $"__{key}__";
                _db.KeyRename(key, newKey);
                _db.KeyDelete(newKey);
            }
			catch(Exception ex)
            {

            }
        }

        private string GetStreamName(string key)
        {
            return $"_{_tableName}-stream-{key}";
        }

        public void AddToStream(string key, HashEntry[] hashEntries)
        {
            var data = hashEntries.Select(x => new NameValueEntry(x.Name, x.Value)).ToArray();

            if (hashEntries.Length > 0)
            {
                _db.StreamAdd(GetStreamName(key), data);
            }
        }
    }
}

