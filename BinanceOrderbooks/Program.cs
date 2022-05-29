using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

class Program
{
    static async Task Main(string[] args)
    {
        var instrument = AnsiConsole.Ask<string>("Insert the instrument");
        var size = AnsiConsole.Ask<int>("Enter the size of the orderbook");

        // Set up injection
        var host = DependencyInjection.CreateHostBuilder(args, instrument, size).Build();

        // Run the websocket process in the background
        var binanceWebsocketBackgroundService = host.Services.GetRequiredService<BinanceWebsocketBackgroundService>();
        _ = Task.Run(() => binanceWebsocketBackgroundService.Start());

        // Prepare the UI table
        var liveOrderbook = host.Services.GetRequiredService<LiveOrderbook>();
        var liveTable = SetUpLiveTable(size);

        while (true)
        {
            lock (liveOrderbook)
            {
                var bids = liveOrderbook.GetBidItems(size);
                var asks = liveOrderbook.GetAskItems(size);
                /*int i = 0;

                foreach (var bid in bids)
                {
                    liveTable.UpdateCell(i, 0, bid.Value.ToString());
                    liveTable.UpdateCell(i, 1, bid.Key.ToString());
                    i++;
                }

                i = 0;

                foreach (var ask in asks)
                {
                    liveTable.UpdateCell(i, 3, ask.Key.ToString());
                    liveTable.UpdateCell(i, 4, ask.Value.ToString());
                    i++;
                }*/

                for (int i = 0; i < size; i++)
                {
                    if(bids.Count() -1 < i || asks.Count() - 1 < i)
                    {
                        break;
                    }
                    lock (bids)
                    {
                        lock(asks)
                        {
                            var bid = bids.ElementAt(i);
                            var ask = asks.ElementAt(i);
                            liveTable.UpdateCell(i, 0, new Markup($"[green]{bid.Value}[/]"));
                            liveTable.UpdateCell(i, 1, new Markup($"[green]{bid.Key}[/]"));
                            // Adjust the spread on the top of the orderbook
                            var topSpread = i == 0 ? (ask.Key - bid.Key).ToString() : "";
                            liveTable.UpdateCell(i, 2, new Markup($"[bold]{topSpread}[/]"));
                            liveTable.UpdateCell(i, 3, new Markup($"[red]{ask.Key}[/]"));
                            liveTable.UpdateCell(i, 4, new Markup($"[red]{ask.Value}[/]"));
                        }
                    }                    
                }
            }
            await Task.Delay(1000);
        }
    }

    private static Table SetUpLiveTable(int size)
    {
        var table = new Table().Centered();
        table.AddColumn(new TableColumn("[green]Volume[/]").Centered());
        table.AddColumn(new TableColumn("[green]Bids[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Spread[/]").Centered());
        table.AddColumn(new TableColumn("[red]Asks[/]").Centered());
        table.AddColumn(new TableColumn("[red]Volume[/]").Centered());

        for (int i = 0; i < size; i++)
        {
            table.AddRow("Loading", "Loading", "", "Loading", "Loading");
        }


        _ = AnsiConsole.Live(table)
                    .StartAsync(async ctx =>
                    {
                        while (true)
                        {
                            ctx.Refresh();
                            await Task.Delay(100);
                        }
                    });
        return table;
    }
}