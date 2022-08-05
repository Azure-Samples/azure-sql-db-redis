using System;
namespace SQLSweeperFunction.Models
{
    public class RankResponseModel
    {
        public int Id { get; set; }
        public string Company { get; set; }
        public string Country { get; set; }
        public int Rank { get; set; }
        public string Symbol { get; set; }
        public double MarketCap { get; set; }
        public DateTime LastModified { get; set; }

    }
}

