using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReaderFunction.Models;

namespace ReaderFunction
{
    public class SqlConnector : ISqlConnector
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SqlConnector> _logger;

        public SqlConnector(AppDbContext context, ILogger<SqlConnector> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RankResponseModel> GetDataBySymbol(string symbol)
        {
            RankResponseModel data = null;

            try
            {
               if(string.IsNullOrEmpty(symbol))
               {
                    throw new ApplicationException("Please enter a symbol to search");
               }

                data = await _context.Companies.FirstAsync(x => x.Symbol == symbol);

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception during GetDataBySymbol");
            }

            return data;
        }

        public async Task<IList<RankResponseModel>> GetDataByRange(int start, int end, bool isDesc)
        {
            IList<RankResponseModel> data = null;

            try
            {
                data = await _context.Companies.Skip(start).Take(end + 1).ToListAsync();

                if(isDesc)
                {
                    data = data.OrderByDescending(x => x.MarketCap).ToList();
                }                
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception during GetDataByRange");
            }

            return data;
        }
    }
}

