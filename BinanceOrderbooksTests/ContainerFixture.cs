using BinanceOrderbooks;
using BinanceOrderbooks.Model;
using BinanceOrderbooks.Services;
using Microsoft.Extensions.DependencyInjection;

public class ContainerFixture
{
    public ContainerFixture()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging();

        serviceCollection
            .AddSingleton(new CommandLineArgs("", 10));

        serviceCollection
            .AddSingleton<BinanceWebsocketBackgroundService>();

        serviceCollection
            .AddHttpClient<IBinanceRestClient, BinanceRestClient>();

        serviceCollection
            .AddSingleton<LiveOrderbook>();

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; private set; }
}