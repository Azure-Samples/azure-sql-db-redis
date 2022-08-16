using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Models;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Services
{
    public abstract class RankServiceAbstract 
    {
        protected readonly IDatabase _db;
        private readonly WriteBehind _wb;
        private readonly ILogger<RankService> _logger;
        private const string keyPrefix = "company";
        private readonly IOptions<LeaderboardDemoOptions> _options;

        protected RankServiceAbstract(IConnectionMultiplexer redis, ILogger<RankService> logger, IOptions<LeaderboardDemoOptions> options)
        {
            _db = redis.GetDatabase();
            _wb = new WriteBehind(redis, keyPrefix);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public abstract Task<List<RankResponseModel>> GetBySymbols(List<string> symbols);

        public abstract Task<(string, string)> GetCompanyBySymbol(string symbol);

        public abstract Task<List<RankResponseModel>> Range(int start, int ent, bool isDesc);

        public async Task<bool> Update(string symbol, double amount)
        {
            bool result = false;
            string key = $"{keyPrefix}:{symbol}";
            try
            {
                await _db.SortedSetAddAsync(LeaderboardDemoOptions.RedisKey, key, amount);
                var company = await GetCompanyBySymbol(key);
                var score = await _db.SortedSetScoreAsync(LeaderboardDemoOptions.RedisKey, key);
                var rank = await _db.SortedSetRankAsync(LeaderboardDemoOptions.RedisKey, key);

                HashEntry[] hashEntry = new HashEntry[]
                {
                    new HashEntry("marketcap", score),
                    new HashEntry("rank", rank),
                    new HashEntry("company", company.Item1),
                    new HashEntry("country", company.Item2)
                };

                if (_options.Value.UseWriteBehind)
                {
                    _wb.AddToStream(symbol, hashEntry);
                }

                result = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error happened during update");
            }

            return result;
        }
    }
}

