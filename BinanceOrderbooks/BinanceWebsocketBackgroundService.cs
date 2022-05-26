using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

public class BinanceWebsocketBackgroundService : BackgroundService
{
    private readonly CommandLineArgs _commandLineArgs;
    private readonly IBinanceRestClient _binanceRestClient;
    private readonly LiveOrderbook _liveOrderbook;

    public BinanceWebsocketBackgroundService(CommandLineArgs commandLineArgs, IBinanceRestClient binanceRestClient, LiveOrderbook liveOrderbook)
    {
        _commandLineArgs = commandLineArgs;
        _binanceRestClient = binanceRestClient;
        _liveOrderbook = liveOrderbook;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var firstMessageEvent = new ManualResetEvent(false);
        var finishMessageEvent = new ManualResetEvent(false);

        try
        {
            using (var client = new WebSocket4Net.WebSocket($"wss://stream.binance.com:9443/ws/{_commandLineArgs.Instrument.ToLower()}@depth"))
            {
                client.Closed += (sender, args) =>
                {
                    Console.WriteLine("Socket Closed");
                };

                client.Opened += (sender, args) =>
                {
                    //Console.WriteLine("Socket Opened");
                };

                client.MessageReceived += (sender, args) =>
                {
                    var response = JsonConvert.DeserializeObject<OrderBookResponse>(@args.Message)!;
                    foreach (var ask in response.Asks)
                    {
                        if(ask[1] == 0)
                        {
                            _liveOrderbook.RemovePriceLevel("ask", ask[0]);
                        } else
                        {
                            _liveOrderbook.AddOrUpdatePriceLevel("ask", new LiveItem { Price = ask[0], Volume = ask[1] });
                        }
                    }

                    foreach (var bid in response.Bids)
                    {
                        if (bid[1] == 0)
                        {
                            _liveOrderbook.RemovePriceLevel("bids", bid[0]);
                        }
                        else
                        {
                            _liveOrderbook.AddOrUpdatePriceLevel("bids", new LiveItem { Price = bid[0], Volume = bid[1] });
                        }
                    }
                };

                client.DataReceived += (sender, args) =>
                {
                    Console.WriteLine(args.Data);
                };
                client.Error += (sender, args) =>
                {
                    Console.WriteLine(args.Exception);
                };

                client.Open();

                firstMessageEvent.WaitOne();

                var snapshot = await _binanceRestClient.GetRestSnapshot(_commandLineArgs.Instrument);

                foreach(var ask in snapshot.Asks)
                {
                    _liveOrderbook.AddOrUpdatePriceLevel("ask", new LiveItem { Price = ask[0] , Volume = ask[1] });
                }

                foreach (var bid in snapshot.Bids)
                {
                    _liveOrderbook.AddOrUpdatePriceLevel("bids", new LiveItem { Price = bid[0], Volume = bid[1] });
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.ToString());
        }
        //return Task.CompletedTask;
    }
}