using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

public record BinanceSnapshot
{
    [JsonPropertyName("lastUpdateId")]
    public long LastUpdateId { get; set; }

    [JsonPropertyName("bids")]
    // ["28874.34000000","0.71305000"]
    // Price and volume
    public List<List<decimal>> Bids { get; set; }

    [JsonPropertyName("asks")]
    // ["28874.34000000","0.71305000"]
    // Price and volume
    public List<List<decimal>> Asks { get; set; }
}

public class OrderbookRequest
{
    [JsonPropertyName("method")]
    public string Method;

    [JsonPropertyName("params")]
    public List<string> Params;

    [JsonPropertyName("id")]
    public int Id;
}

public class OrderBookResponse
{
    [JsonProperty(PropertyName = "e")]
    public string EventType;

    [JsonProperty(PropertyName = "E")]
    public long EventTime;

    [JsonProperty(PropertyName = "s")]
    public string Symbol;

    [JsonProperty(PropertyName = "U")]
    public long InitialUpdateId;

    [JsonProperty(PropertyName = "u")]
    public long FinalUpdateId;

    [JsonProperty(PropertyName = "b")]
    public List<List<decimal>> Bids;

    [JsonProperty(PropertyName = "a")]
    public List<List<decimal>> Asks;
}