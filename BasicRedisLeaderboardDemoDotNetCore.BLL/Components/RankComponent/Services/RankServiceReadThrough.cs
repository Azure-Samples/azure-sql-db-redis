using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Models;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Services
{
    public class RankServiceReadThrough : RankServiceAbstract, IRankService
    {
        private IAzureFunctionHttpClient _httpClient;
        private const string _splitCharacter = ":";

        public RankServiceReadThrough(IConnectionMultiplexer redis, ILogger<RankService> logger, IOptions<LeaderboardDemoOptions> options, IAzureFunctionHttpClient httpClient)
             : base(redis, logger, options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public override async Task<List<RankResponseModel>> GetBySymbols(List<string> symbols)
        {
            throw new NotImplementedException();
        }

        public override async Task<(string, string)> GetCompanyBySymbol(string symbol)
        {
            throw new NotImplementedException();
        }

        public override async Task<List<RankResponseModel>> Range(int start, int ent, bool isDesc)
        {
            var data = new List<RankResponseModel>();
            var results = await _httpClient.GetByRange(start, ent, isDesc); 

            return results;
        }
    }
}

