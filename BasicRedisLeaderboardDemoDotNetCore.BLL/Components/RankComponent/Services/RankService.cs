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
    public class RankService : RankServiceAbstract, IRankService
    {
        private const string _splitCharacter = ":";

        public RankService(IConnectionMultiplexer redis, ILogger<RankService> logger, IOptions<LeaderboardDemoOptions> options)
            : base(redis, logger, options)
        {
        }

        public override async Task<List<RankResponseModel>> Range(int start, int ent, bool isDesc)
        {
            var data = new List<RankResponseModel>();            
            var results = await _db.SortedSetRangeByRankWithScoresAsync(LeaderboardDemoOptions.RedisKey, start, ent, isDesc? Order.Descending:Order.Ascending);
            var startRank = isDesc ? start + 1 : (results.Count() / 2 - start);
            var increaseFactor = isDesc ? 1 : -1;
            var items = results.ToList();

            for (var i = 0; i < items.Count; i++)
            {
                var symbol = items[i].Element.ToString().Split(_splitCharacter)[1];
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

        public override async Task<(string, string)> GetCompanyBySymbol(string symbol)
        {           
            HashEntry[] item = await _db.HashGetAllAsync(symbol);
            var companyEntry = item.Single(x => x.Name == "company");
            var countryEntry = item.Single(x => x.Name == "country");
            return (companyEntry.Value, countryEntry.Value);
        }

        public override async Task<List<RankResponseModel>> GetBySymbols(List<string> symbols)
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
    }
}
