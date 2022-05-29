using System;
using System.Threading;
using BinanceOrderbooks.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BinanceOrderbooks.Services
{
    public class BinanceWebsocketBackgroundService
    {
        private readonly CommandLineArgs _commandLineArgs;
        private readonly LiveOrderbook _liveOrderbook;
        private readonly ILogger<BinanceWebsocketBackgroundService> _logger;
        private bool initialMessage = true;

        public BinanceWebsocketBackgroundService(CommandLineArgs commandLineArgs, LiveOrderbook liveOrderbook, ILogger<BinanceWebsocketBackgroundService> logger)
        {
            _commandLineArgs = commandLineArgs;
            _liveOrderbook = liveOrderbook;
            _logger = logger;
        }

        public void Start()
        {
            var finishMessageEvent = new ManualResetEvent(false);

            try
            {
                using var client = new WebSocket4Net.WebSocket($"wss://stream.binance.com:9443/ws/{_commandLineArgs.Instrument.ToLower()}@depth");
                client.Closed += (sender, args) =>
                {
                    _logger.LogInformation("Websocket Closed");
                    finishMessageEvent.Set();
                };

                client.Opened += (sender, args) =>
                {
                    _logger.LogInformation("Websocket Opened");
                };

                client.MessageReceived += (sender, args) =>
                {
                    var response = JsonConvert.DeserializeObject<OrderBookResponse>(@args.Message)!;
                    _logger.LogDebug(response.ToString());
                    var added = _liveOrderbook.Buffer.Writer.WriteAsync(response);

                    if (initialMessage)
                    {
                        _logger.LogInformation("First message received from websocket");
                        _liveOrderbook.StoreInitialSnapshot();
                        initialMessage = false;
                    }
                };

                client.Error += (sender, args) =>
                {
                    _logger.LogError("Websocket error ", args.Exception);
                };

                client.Open();

                finishMessageEvent.WaitOne();

                client.Close();

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError("Websocket error ", ex);
            }
        }
    }
}