using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BasicRedisLeaderboardDemoDotNetCore.BLL.Components.RankComponent.Models;

namespace BasicRedisLeaderboardDemoDotNetCore.BLL
{
    public class AzureFunctionHttpClient : IAzureFunctionHttpClient
    {
        private readonly HttpClient _httpClient;

        public AzureFunctionHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<RankResponseModel>> GetByRange(int start, int ent, bool isDesc)
        {
            var results = await _httpClient.GetAsync($"api/{start}/{ent}/{isDesc}");

            var json = await results.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var data = JsonSerializer.Deserialize<List<RankResponseModel>>(json, jsonOptions);
            return data;
        }
    }
}

