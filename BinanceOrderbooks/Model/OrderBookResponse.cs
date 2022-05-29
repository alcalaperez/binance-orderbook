using System.Collections.Generic;
using Newtonsoft.Json;

public record OrderBookResponse
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