using BinanceOrderbooks.Model;
using Spectre.Console;

namespace BinanceOrderbooks.Util
{
    public class TableConfigurator
    {
        private readonly LiveOrderbook _liveOrderbook;
        private readonly CommandLineArgs _commandLineArgs;

        public TableConfigurator(LiveOrderbook liveOrderbook, CommandLineArgs commandLineArgs)
        {
            _liveOrderbook = liveOrderbook;
            _commandLineArgs = commandLineArgs;
        }


        public Table SetUpLiveTable()
        {
            var table = new Table().Centered();
            table.AddColumn(new TableColumn("[green]Volume[/]").Centered());
            table.AddColumn(new TableColumn("[green]Bids[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Spread[/]").Centered());
            table.AddColumn(new TableColumn("[red]Asks[/]").Centered());
            table.AddColumn(new TableColumn("[red]Volume[/]").Centered());

            for (int i = 0; i < _commandLineArgs.Size; i++)
            {
                table.AddRow("Loading", "Loading", "", "Loading", "Loading");
            }

            // Refresh table UI
            _ = AnsiConsole.Live(table)
                        .StartAsync(async ctx =>
                        {
                            while (true)
                            {
                                ctx.Refresh();
                                // Wait for the orderbook semaphore to be green and refresh the UI
                                await _liveOrderbook.NewElementsSemaphore.WaitAsync();
                            }
                        });
            return table;
        }
    }
}
