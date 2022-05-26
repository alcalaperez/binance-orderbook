using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // Ask for the instument
        Console.WriteLine("Insert the instrument");
        var instrument = Console.ReadLine();
        var size = AnsiConsole.Ask<int>("Enter the size of the orderbook");

        var host = CreateHostBuilder(args, instrument).Build();

        Task.Factory.StartNew(() => host.RunAsync());

        var liveOrderbook = host.Services.GetRequiredService<LiveOrderbook>();
        var table = new Table().Centered();
        table.AddColumn("Volume");
        table.AddColumn("Bids");
        table.AddColumn("Spread");
        table.AddColumn("Asks");
        table.AddColumn("Volume");

        for (int i = 0; i < size; i++)
        {
            table.AddRow("Loading", "Loading", "Loading", "Loading", "Loading");
        }


        AnsiConsole.Live(table)
                    .StartAsync(async ctx =>
                    {
                        while (true)
                        {
                            ctx.Refresh();
                            await Task.Delay(100);
                        }
                    });

        while (true)
        {
            if(liveOrderbook.Asks.Count >= size && liveOrderbook.Bids.Count >= size)
            {
                var bids = liveOrderbook.GetBidItems(size);
                var asks = liveOrderbook.GetAskItems(size);

                for (int i = 0; i < size; i++)
                {
                    table.UpdateCell(i, 0, bids[i].Volume.ToString());
                    table.UpdateCell(i, 1, bids[i].Price.ToString());
                    // Adjust the spread on the top of the orderbook
                    if(i == 0)
                    {
                        table.UpdateCell(i, 2, (asks[i].Price - bids[i].Price).ToString());
                    } else
                    {
                        table.UpdateCell(i, 2, "");
                    }
                    table.UpdateCell(i, 3, asks[i].Price.ToString());
                    table.UpdateCell(i, 4, asks[i].Volume.ToString());
                }
            }
            
            await Task.Delay(100);
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, string instrument)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.SetBasePath(Directory.GetCurrentDirectory());
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(new CommandLineArgs { Instrument = instrument });
                services.AddHostedService<BinanceWebsocketBackgroundService>();
                services.AddHttpClient<IBinanceRestClient, BinanceRestClient>();
                services.AddSingleton<LiveOrderbook>();
            });

        return hostBuilder;
    }
}