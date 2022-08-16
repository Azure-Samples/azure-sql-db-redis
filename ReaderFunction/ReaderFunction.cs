using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ReaderFunction.Models;
using System.Linq;
using StackExchange.Redis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace ReaderFunction
{
    public class ReaderFunction
    {
        private readonly IReadThrough _readThrough;
        private readonly ILogger<ReaderFunction> _logger;

        public ReaderFunction(IReadThrough readThrough, ILogger<ReaderFunction> logger)
        {
            _readThrough = readThrough ?? throw new ArgumentNullException(nameof(readThrough));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("GetBySymbol")]
        public async Task<HttpResponseData> GetByKey(
              [HttpTrigger(AuthorizationLevel.Function, "get", Route = "{key}")] HttpRequestData req,
              string key)          
        {
            HttpResponseData response = null;

            try
            {
                var data = await _readThrough.GetByKey(key);

                response = req.CreateResponse(System.Net.HttpStatusCode.OK); 
                await response.WriteAsJsonAsync(data);

                return response;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "There was an error during GetByKey method.");
            }
                      
            response = req.CreateResponse(System.Net.HttpStatusCode.NoContent);
            return response;
        }

        [Function("GetByRange")]
        public async Task<HttpResponseData> GetByRange(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "{start}/{ent}/{isDesc}")] HttpRequestData req,
            int start, int ent, string isDesc)
        {
            HttpResponseData response = null;
            try
            {
                bool isDescending = bool.Parse(isDesc);
                var data = await _readThrough.GetByRange(start, ent, isDescending);

                response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(data);

                return response;

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "There was an error during GetByRange method.");

            }

            response = req.CreateResponse(System.Net.HttpStatusCode.NoContent);
            return response;
        }
    }
}

