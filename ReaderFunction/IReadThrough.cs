using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReaderFunction.Models;

namespace ReaderFunction
{
    public interface IReadThrough
    {
        public Task AddToCache(RankResponseModel data);
        public Task<RankResponseModel> GetByKey(string key);
        public Task<IList<RankResponseModel>> GetByRange(int start, int end, bool isDesc);
    }
}

