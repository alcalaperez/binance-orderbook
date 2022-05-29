using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class BinanceRestClient : IBinanceRestClient
{
    private readonly HttpClient _client;
    private readonly CommandLineArgs _commandLineArgs;

    public BinanceRestClient(HttpClient client, CommandLineArgs commandLineArgs)
    {
        _client = client;
        _commandLineArgs = commandLineArgs;
    }

    public async Task<BinanceSnapshotResponse> GetRestSnapshot()
    {
        var response = await _client.GetAsync($"https://api.binance.com/api/v3/depth?symbol={_commandLineArgs.Instrument.ToUpper()}&limit=1000");
        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<BinanceSnapshotResponse>(await response.Content.ReadAsStringAsync())!;
        }
        throw new InvalidOperationException("Invalid instrument");
    }
}
