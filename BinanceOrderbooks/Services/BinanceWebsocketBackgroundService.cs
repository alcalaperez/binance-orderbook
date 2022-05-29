using System;
using System.Threading;
using Newtonsoft.Json;

public class BinanceWebsocketBackgroundService
{
    private readonly CommandLineArgs _commandLineArgs;
    private readonly IBinanceRestClient _binanceRestClient;
    private readonly LiveOrderbook _liveOrderbook;
    private bool initialMessage = true;

    public BinanceWebsocketBackgroundService(CommandLineArgs commandLineArgs, IBinanceRestClient binanceRestClient, LiveOrderbook liveOrderbook)
    {
        _commandLineArgs = commandLineArgs;
        _binanceRestClient = binanceRestClient;
        _liveOrderbook = liveOrderbook;
    }

    public void Start()
    {
        var finishMessageEvent = new ManualResetEvent(false);

        try
        {
            using var client = new WebSocket4Net.WebSocket($"wss://stream.binance.com:9443/ws/{_commandLineArgs.Instrument.ToLower()}@depth");
            client.Closed += (sender, args) =>
            {
                Console.WriteLine("Socket Closed");
                finishMessageEvent.Set();
            };

            client.Opened += (sender, args) =>
            {
                //Console.WriteLine("Socket Opened");
            };

            client.MessageReceived += (sender, args) =>
            {
                var response = JsonConvert.DeserializeObject<OrderBookResponse>(@args.Message)!;
                var added = _liveOrderbook.buffer.Writer.WriteAsync(response);

                if (initialMessage)
                {
                    _liveOrderbook.StoreSnapshot();
                    initialMessage = false;
                }
            };

            client.Error += (sender, args) =>
            {
                Console.WriteLine(args.Exception);
            };

            client.Open();

            finishMessageEvent.WaitOne();

            client.Close();

            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.ToString());
        }
    }
}