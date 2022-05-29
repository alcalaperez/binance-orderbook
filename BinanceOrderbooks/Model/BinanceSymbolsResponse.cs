using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BinanceOrderbooks.Model
{
    public record BinanceSymbolsResponse
    {
        [JsonPropertyName("symbols")]
        public List<Symbol> Symbols { get; set; } = new();
    }

    public record Symbol
    {
        [JsonPropertyName("symbol")]
        public string SymbolName { get; set; } = "";
    }
}
