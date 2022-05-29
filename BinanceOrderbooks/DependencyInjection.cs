using System.IO;
using BinanceOrderbooks.Model;
using BinanceOrderbooks.Services;
using BinanceOrderbooks.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BinanceOrderbooks
{
    public class DependencyInjection
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddJsonFile("appsettings.json");
                    builder.SetBasePath(Directory.GetCurrentDirectory());
                });

            hostBuilder.ConfigureServices((context, services) =>
            {
                services
                    .AddLogging()
                    .AddSingleton(new CommandLineArgs("BTCUSDT", 30))
                    .AddSingleton<BinanceWebsocketBackgroundService>();

                services.AddHttpClient<IBinanceRestClient, BinanceRestClient>();

                services
                    .AddSingleton<LiveOrderbook>()
                    .AddSingleton<TableConfigurator>();
            });

            return hostBuilder;
        }
    }
}