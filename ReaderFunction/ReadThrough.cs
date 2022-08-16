using System;
using System.Linq;
using System.Threading.Tasks;
using ReaderFunction.Models;
using StackExchange.Redis;
using ReaderFunction;
using System.Collections.Generic;

namespace ReaderFunction
{
    public class ReadThrough : IReadThrough
    {
        private readonly IConnectionMultiplexer _redisConnection;
        private IDatabase _db;
        private const string _prefix = "company";

        private const string _splitCharacter = ":";
        private readonly ISqlConnector _sqlConnector;

        public ReadThrough(IConnectionMultiplexer redisConnection, ISqlConnector sqlConnector)
        {
            _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
            _db = _redisConnection.GetDatabase();
            _sqlConnector = sqlConnector ?? throw new ArgumentNullException(nameof(sqlConnector));
        }

        public async Task<RankResponseModel> GetByKey(string key)
        {
            //Check if the data exists in cache            
            
            var company = await GetCompanyByKey(key);
            string symbol = key.Split(_splitCharacter)[1];
            RankResponseModel result = null;

            if (string.IsNullOrEmpty(company.Item1))
            {                
                //Data is not in cache lets load from database
                result = await _sqlConnector.GetDataBySymbol(symbol);

                //Insert results to cache
                await AddToCache(result);                           
            }
            else 
            {
                //Load score from sorted set
                var score = await _db.SortedSetScoreAsync(ReaderFunctionOptions.RedisKey, key);

                  result = new RankResponseModel
                    {
                        Company = company.Item1,
                        Country = company.Item2,
                        Rank = 0,
                        Symbol = symbol,
                        MarketCap = (double)score
                    };       
            }

            return result;
        }

        public async Task<IList<RankResponseModel>> GetByRange(int start, int end, bool isDesc)
        {
            var data = new List<RankResponseModel>();
            IList<RankResponseModel> resultList = null;

            SortedSetEntry[] results = await _db.SortedSetRangeByRankWithScoresAsync(ReaderFunctionOptions.RedisKey, start, end, isDesc? Order.Descending:Order.Ascending);
          
            if(results == null || results.Length == 0)
            {
                //Load from database
                resultList = await _sqlConnector.GetDataByRange(start, end, isDesc);

                //Insert results to cache
                await AddToCache(resultList);
            }else
            {
                var startRank = isDesc ? start + 1 : (results.Count() / 2 - start);
                var increaseFactor = isDesc ? 1 : -1;
                var items = results.ToList();
                resultList = new List<RankResponseModel>();

                for (var i = 0; i < items.Count; i++)
                {
                    var symbol = items[i].Element.ToString().Split(_splitCharacter)[1];
                    var company = await GetCompanyByKey(items[i].Element);

                    resultList.Add(
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
            }

            return resultList;
        }

        public async Task AddToCache(RankResponseModel data)
        {
            var key = $"{_prefix}:{data.Symbol.ToLower()}";

            await _db.SortedSetAddAsync(ReaderFunctionOptions.RedisKey, key, data.MarketCap);

            await _db.HashSetAsync(key, new HashEntry[]
            {
                      new HashEntry(nameof(data.Company).ToLower(), data.Company),
                      new HashEntry(nameof(data.Country).ToLower(), data.Country)
            });
        }

        public async Task AddToCache(IList<RankResponseModel> data)
        {
            foreach(var item in data)
            {
                await AddToCache(item);
            }
        }

        public async Task<(string, string)> GetCompanyByKey(string key)
        {           
            HashEntry[] item = await _db.HashGetAllAsync(key);
            var companyEntry = item.Single(x => x.Name == "company");
            var countryEntry = item.Single(x => x.Name == "country");
            return (companyEntry.Value, countryEntry.Value);
        }
        
    }
}

