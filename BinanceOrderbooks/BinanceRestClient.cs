using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class BinanceRestClient : IBinanceRestClient
{
    private readonly HttpClient _client;

    public BinanceRestClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<BinanceSnapshot> GetRestSnapshot(string instrument)
    {
        var response = await _client.GetAsync($"https://api.binance.com/api/v3/depth?symbol={instrument.ToUpper()}");
        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<BinanceSnapshot>(await response.Content.ReadAsStringAsync())!;
        }
        throw new InvalidOperationException("Invalid instrument");
    }
}
