namespace ReaderFunction.Models
{
    public class RankResponseModel
    {
        public string Symbol { get; set; }
        public string Company { get; set; }
        public string Country { get; set; }
        public int Rank { get; set; }
        public double MarketCap { get; set; }
    }
}
