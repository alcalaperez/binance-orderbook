using Microsoft.Extensions.DependencyInjection;

public class ContainerFixture
{
    public ContainerFixture()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<CommandLineArgs>();

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