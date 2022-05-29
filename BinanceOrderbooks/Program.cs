using System.Linq;
using System.Threading.Tasks;
using BinanceOrderbooks.Model;
using BinanceOrderbooks.Services;
using BinanceOrderbooks.Util;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BinanceOrderbooks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Set up injection
            AnsiConsole.WriteLine("Loading services...");
            var host = DependencyInjection.CreateHostBuilder(args).Build();
            var selectedArgs = host.Services.GetRequiredService<CommandLineArgs>();
            var binanceRestClient = host.Services.GetRequiredService<IBinanceRestClient>();
            var binanceWebsocketBackgroundService = host.Services.GetRequiredService<BinanceWebsocketBackgroundService>();
            var liveOrderbook = host.Services.GetRequiredService<LiveOrderbook>();
            var tableConfigurator = host.Services.GetRequiredService<TableConfigurator>();

            // Load all available symbols
            var symbolsResponse = await binanceRestClient.GetSymbols();

            var symbol = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a symbol:")
                    .PageSize(40)
                    .MoreChoicesText("[grey](Move up and down to reveal more symbols)[/]")
                    .AddChoices(symbolsResponse.Symbols.Select(x => x.SymbolName).OrderBy(x => x)));

            AnsiConsole.WriteLine($"Selected {symbol}");

            var size = AnsiConsole.Ask("Enter the size of the orderbook", 30);

            // Set up selected arguments
            selectedArgs.Instrument = symbol;
            selectedArgs.Size = size;

            // Run the websocket process in the background
            _ = Task.Run(() => binanceWebsocketBackgroundService.Start());

            // Prepare the UI after we get some elements
            await liveOrderbook.NewElementsSemaphore.WaitAsync();
            var liveTable = tableConfigurator.SetUpLiveTable();

            // Update table information in an infinite loop
            while (true)
            {
                var bids = liveOrderbook.GetBidItems(size);
                var asks = liveOrderbook.GetAskItems(size);
                var lowestSize = bids.Count() >= asks.Count() ? asks.Count() : bids.Count();

                for (int i = 0; i < lowestSize; i++)
                {
                    var bid = bids.ElementAt(i);
                    var ask = asks.ElementAt(i);
                    // Adjust the spread only on the top of the orderbook
                    var topSpread = i == 0 ? (ask.Key - bid.Key).ToString() : "";

                    liveTable
                        .UpdateCell(i, 0, new Markup($"[green]{bid.Value}[/]"))
                        .UpdateCell(i, 1, new Markup($"[green]{bid.Key}[/]"))
                        .UpdateCell(i, 2, new Markup($"[bold]{topSpread}[/]"))
                        .UpdateCell(i, 3, new Markup($"[red]{ask.Key}[/]"))
                        .UpdateCell(i, 4, new Markup($"[red]{ask.Value}[/]"));
                }

                // Wait for the orderbook semaphore to be green and update the table
                await liveOrderbook.NewElementsSemaphore.WaitAsync();
            }
        }
    }
}