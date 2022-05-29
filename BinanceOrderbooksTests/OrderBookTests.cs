using System.Linq;
using System.Threading.Tasks;
using BinanceOrderbooks;
using BinanceOrderbooks.Model;
using BinanceOrderbooks.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinanceOrderbooksTests
{
    public class OrderBookTests : IClassFixture<ContainerFixture>
    {
        private readonly LiveOrderbook liveOrderBook; 
        private readonly IBinanceRestClient binanceRestClient;
        private readonly CommandLineArgs args;

        public OrderBookTests(ContainerFixture fixture)
        {
            var serviceProvider = fixture.ServiceProvider;
            liveOrderBook = serviceProvider.GetService<LiveOrderbook>();
            binanceRestClient = serviceProvider.GetService<IBinanceRestClient>();
            args = serviceProvider.GetService<CommandLineArgs>();

        }

        [Theory]
        [InlineData("BTCUSDT")]
        [InlineData("ETHUSDT")]
        [InlineData("ETCUSDT")]
        [InlineData("BCHUSDT")]
        public async Task TestSnapshotRestClient(string instrument)
        {

            args.Instrument = instrument;
            var response = await binanceRestClient.GetRestSnapshot();

            response.Should().NotBeNull();
            response.Asks.Should().NotBeNullOrEmpty();
            response.Bids.Should().NotBeNullOrEmpty();

            response.LastUpdateId.Should().BeGreaterThan(0);

            foreach (var ask in response.Asks)
            {
                decimal.Parse(ask[0]).Should().BeGreaterThan(0);
                decimal.Parse(ask[1]).Should().BeGreaterThan(0);
            }

            foreach (var bid in response.Bids)
            {
                decimal.Parse(bid[0]).Should().BeGreaterThan(0);
                decimal.Parse(bid[1]).Should().BeGreaterThan(0);
            }
        }

        [Fact]
        public async Task TestSymbolsRestClient()
        {

            var response = await binanceRestClient.GetSymbols();

            response.Should().NotBeNull();
            response.Symbols.Should().NotBeNullOrEmpty();

            foreach (var symbol in response.Symbols)
            {
                symbol.SymbolName.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void TestLiveOrderbook()
        {
            // Initial state
            var asks = liveOrderBook.GetAskItems(10);
            var bids = liveOrderBook.GetBidItems(10);

            asks.Should().BeEmpty();
            bids.Should().BeEmpty();

            // Add bids
            liveOrderBook.UpsertPriceLevel(OrderBookSide.Bids, new LiveItem { Price = 200, Volume = 1});
            liveOrderBook.UpsertPriceLevel(OrderBookSide.Bids, new LiveItem { Price = 100, Volume = 2 });
            liveOrderBook.UpsertPriceLevel(OrderBookSide.Bids, new LiveItem { Price = 300, Volume = 3 });

            bids = liveOrderBook.GetBidItems(10);
            bids.Count().Should().Be(3);

            // Check bids order (higher to lower)
            bids.First().Key.Should().Be(300);
            bids.First().Value.Should().Be(3);
            bids.Last().Key.Should().Be(100);
            bids.Last().Value.Should().Be(2);

            // Add asks
            liveOrderBook.UpsertPriceLevel(OrderBookSide.Asks, new LiveItem { Price = 100, Volume = 1 });
            liveOrderBook.UpsertPriceLevel(OrderBookSide.Asks, new LiveItem { Price = 200, Volume = 2 });
            liveOrderBook.UpsertPriceLevel(OrderBookSide.Asks, new LiveItem { Price = 300, Volume = 3 });

            asks = liveOrderBook.GetAskItems(10);
            asks.Count().Should().Be(3);

            // Check asks order (lower to higher)
            asks.First().Key.Should().Be(100);
            asks.First().Value.Should().Be(1);
            asks.Last().Key.Should().Be(300);
            asks.Last().Value.Should().Be(3);

            // Update the volume of an existing bid price
            liveOrderBook.UpsertPriceLevel(OrderBookSide.Bids, new LiveItem { Price = 300, Volume = 10 });
            bids = liveOrderBook.GetBidItems(10);
            bids.First().Key.Should().Be(300);
            bids.First().Value.Should().Be(10);

            // Update the volume of an existing ask price
            liveOrderBook.UpsertPriceLevel(OrderBookSide.Asks, new LiveItem { Price = 100, Volume = 40 });
            asks = liveOrderBook.GetAskItems(10);
            asks.First().Key.Should().Be(100);
            asks.First().Value.Should().Be(40);

            // Remove the bid level
            liveOrderBook.RemovePriceLevel(OrderBookSide.Bids, 300);
            bids = liveOrderBook.GetBidItems(10);
            bids.Count().Should().Be(2);
            bids.First().Key.Should().Be(200);
            bids.First().Value.Should().Be(1);

            // Remove the ask level
            liveOrderBook.RemovePriceLevel(OrderBookSide.Asks, 100);
            asks = liveOrderBook.GetAskItems(10);
            asks.Count().Should().Be(2);
            asks.First().Key.Should().Be(200);
            asks.First().Value.Should().Be(2);
        }
    }
}
