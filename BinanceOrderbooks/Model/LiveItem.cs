namespace BinanceOrderbooks.Model
{
    public record LiveItem
    {
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }
}