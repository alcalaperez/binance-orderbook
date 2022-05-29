using System.Collections.Generic;
using System.Text.Json.Serialization;

public record BinanceSnapshotResponse
{
    [JsonPropertyName("lastUpdateId")]
    public long LastUpdateId { get; set; }

    [JsonPropertyName("bids")]
    // ["28874.34000000","0.71305000"]
    // Price and volume
    public List<List<string>> Bids { get; set; }

    [JsonPropertyName("asks")]
    // ["28874.34000000","0.71305000"]
    // Price and volume
    public List<List<string>> Asks { get; set; }
}