using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class DependencyInjection
{
    public static IHostBuilder CreateHostBuilder(string[] args, string instrument, int size)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddJsonFile("appsettings.json");
                builder.SetBasePath(Directory.GetCurrentDirectory());
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(new CommandLineArgs { Instrument = instrument, Size = size });
                services.AddSingleton<BinanceWebsocketBackgroundService>();
                services.AddHttpClient<IBinanceRestClient, BinanceRestClient>();
                services.AddSingleton<LiveOrderbook>();
            });

        return hostBuilder;
    }
}