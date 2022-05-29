using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BinanceOrderbooks.Model;
using Microsoft.Extensions.Logging;

namespace BinanceOrderbooks.Services
{
    public class BinanceRestClient : IBinanceRestClient
    {
        private readonly HttpClient _client;
        private readonly CommandLineArgs _commandLineArgs;
        private readonly ILogger<IBinanceRestClient> _logger;

        public BinanceRestClient(HttpClient client, CommandLineArgs commandLineArgs, ILogger<IBinanceRestClient> logger)
        {
            _client = client;
            _commandLineArgs = commandLineArgs;
            _logger = logger;
        }

        public async Task<BinanceSnapshotResponse> GetRestSnapshot()
        {
            _logger.LogInformation("Getting initial snapshot for {symbol}...", _commandLineArgs.Instrument.ToUpper());
            var response = await _client.GetAsync($"https://api.binance.com/api/v3/depth?symbol={_commandLineArgs.Instrument.ToUpper()}&limit=1000");
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<BinanceSnapshotResponse>(await response.Content.ReadAsStringAsync())!;
            }
            throw new InvalidOperationException("Invalid instrument");
        }

        public async Task<BinanceSymbolsResponse> GetSymbols()
        {
            _logger.LogInformation("Loading symbols...");
            var response = await _client.GetAsync("https://api.binance.com/api/v3/exchangeInfo");
            return JsonSerializer.Deserialize<BinanceSymbolsResponse>(await response.Content.ReadAsStringAsync())!;
        }
    }
}
