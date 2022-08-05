using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Models;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Services
{
    public class RankService : IRankService
    {
        private readonly IDatabase _db;
        private readonly WriteBehind _wb;
        private readonly ILogger<RankService> _logger;
        private const string keyPrefix = "company";

        public RankService(IConnectionMultiplexer redis, ILogger<RankService> logger)
        {
            _db = redis.GetDatabase();
            _wb = new WriteBehind(redis, keyPrefix);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        public async Task<List<RankResponseModel>> Range(int start, int ent, bool isDesc)
        {
            var data = new List<RankResponseModel>();            
            var results = await _db.SortedSetRangeByRankWithScoresAsync(LeaderboardDemoOptions.RedisKey, start,ent, isDesc? Order.Descending:Order.Ascending);
            var startRank = isDesc ? start + 1 : (results.Count() / 2 - start);
            var increaseFactor = isDesc ? 1 : -1;
            var items = results.ToList();

            for (var i = 0; i < items.Count; i++)
            {
                var symbol = items[i].Element.ToString().Split(":")[1];
                var company = await GetCompanyBySymbol(items[i].Element);

                data.Add(
                    new RankResponseModel
                    {
                        Company = company.Item1,
                        Country = company.Item2,
                        Rank = startRank,
                        Symbol = symbol,
                        MarketCap = items[i].Score,
                    });
                startRank += increaseFactor;
            }

            return data;
        }

        public async Task<(string, string)> GetCompanyBySymbol(string symbol)
        {           
            HashEntry[] item = await _db.HashGetAllAsync(symbol);
            var companyEntry = item.Single(x => x.Name == "company");
            var countryEntry = item.Single(x => x.Name == "country");
            return (companyEntry.Value, countryEntry.Value);
        }

        public async Task<List<RankResponseModel>> GetBySymbols(List<string> symbols)
        {
            var results = new List<RankResponseModel>();
            for (var i = 0;i< symbols.Count; i++)
            {
                var score = await _db.SortedSetScoreAsync(LeaderboardDemoOptions.RedisKey, symbols[i]);
                var company = await GetCompanyBySymbol(symbols[i]);
                results.Add(
                     new RankResponseModel
                     {
                         Company = company.Item1,
                         Country = company.Item2,
                         Rank = i+1,
                         Symbol = symbols[i],
                         MarketCap = (double)score
                     });
            }
                
            return results;
        }

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
                _wb.AddToStream(symbol, hashEntry);

                result = true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error happened during update");
            }

            return result;
        }
    }
}
