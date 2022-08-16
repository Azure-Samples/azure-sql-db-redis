using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReaderFunction.Models;

namespace ReaderFunction
{
    public interface ISqlConnector
    {
        public Task<RankResponseModel> GetDataBySymbol(string symbol);

        public Task<IList<RankResponseModel>> GetDataByRange(int start, int end, bool isDesc);
    }
}

