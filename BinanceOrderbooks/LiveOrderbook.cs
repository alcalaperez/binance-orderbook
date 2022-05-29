using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BinanceOrderbooks.Model;
using BinanceOrderbooks.Services;
using Microsoft.Extensions.Logging;

namespace BinanceOrderbooks
{
    public class LiveOrderbook
    {
        private readonly ConcurrentDictionary<decimal, decimal> Bids;
        private readonly ConcurrentDictionary<decimal, decimal> Asks;
        private readonly IBinanceRestClient _binanceRestClient;
        private readonly ILogger<LiveOrderbook> _logger;

        // Store updates from websocket and consume them
        public Channel<OrderBookResponse> Buffer { get; set; }
        // Comunicate the UI that there are new elements to show
        public SemaphoreSlim NewElementsSemaphore { get; set; }

        private long lastUpdated;

        public LiveOrderbook(IBinanceRestClient binanceRestClient, ILogger<LiveOrderbook> logger)
        {
            Bids = new ConcurrentDictionary<decimal, decimal>();
            Asks = new ConcurrentDictionary<decimal, decimal>();
            _binanceRestClient = binanceRestClient;
            _logger = logger;
            Buffer = Channel.CreateUnbounded<OrderBookResponse>();
            NewElementsSemaphore = new SemaphoreSlim(0, 1);
        }

        public IEnumerable<KeyValuePair<decimal, decimal>> GetAskItems(int size)
        {
            // ToArray is thread safe for ConcurrentDictionary so the collection can be ordered safely
            return Asks.ToArray().OrderBy(x => x.Key).Take(size);
        }

        public IEnumerable<KeyValuePair<decimal, decimal>> GetBidItems(int size)
        {
            // ToArray is thread safe for ConcurrentDictionary so the collection can be ordered safely
            return Bids.ToArray().OrderByDescending(x => x.Key).Take(size);
        }

        public void RemovePriceLevel(OrderBookSide side, decimal price)
        {
            if (side == OrderBookSide.Bids)
            {
                _logger.LogDebug("Removing bid level at price {price}", price);
                Bids.Remove(price, out var liveItem);
            }
            else
            {
                _logger.LogDebug("Removing ask level at price {price}", price);
                Asks.Remove(price, out var liveItem);
            }
        }

        public void UpsertPriceLevel(OrderBookSide side, LiveItem itemToAdd)
        {
            if (side == OrderBookSide.Bids)
            {
                _logger.LogDebug("Adding or updating bid level at price {price} and volume {volume}", itemToAdd.Price, itemToAdd.Volume);
                Bids.AddOrUpdate(itemToAdd.Price, itemToAdd.Volume, (price, volume) => itemToAdd.Volume);
            }
            else
            {
                _logger.LogDebug("Adding or updating ask level at price {price} and volume {volume}", itemToAdd.Price, itemToAdd.Volume);
                Asks.AddOrUpdate(itemToAdd.Price, itemToAdd.Volume, (price, volume) => itemToAdd.Volume);
            }
        }

        /*
         * Get a snapshot from binance since we only get L2 updates from the websocket
         * This gets triggered by the websocket when the first update arrives and only gets executed once
         */
        public async void StoreInitialSnapshot()
        {
            var snapshot = await _binanceRestClient.GetRestSnapshot();

            lastUpdated = snapshot.LastUpdateId;

            foreach (var ask in snapshot.Asks)
            {
                UpsertPriceLevel(OrderBookSide.Asks, new LiveItem { Price = decimal.Parse(ask[0]), Volume = decimal.Parse(ask[1]) });
            }

            foreach (var bid in snapshot.Bids)
            {
                UpsertPriceLevel(OrderBookSide.Bids, new LiveItem { Price = decimal.Parse(bid[0]), Volume = decimal.Parse(bid[1]) });
            }

            _logger.LogDebug("Initial snapshot has been processed with result {result}", snapshot.ToString());
            // We have new elements (initial ones) so the semaphore is green
            NewElementsSemaphore.Release();

            // Long running process
            await ProcessBufferAsync();
        }

        /*
         * Process all the buffer elements that are being added from the websocket
         * Gets running on the background
         */
        public async Task ProcessBufferAsync()
        {
            while (await Buffer.Reader.WaitToReadAsync())
            {
                while (Buffer.Reader.TryRead(out var orderBookResponse))
                {
                    if (orderBookResponse.FinalUpdateId > lastUpdated)
                    {
                        var asks = orderBookResponse.Asks;
                        var bids = orderBookResponse.Bids;

                        foreach (var ask in asks)
                        {
                            if (ask[1] == 0)
                            {
                                RemovePriceLevel(OrderBookSide.Asks, ask[0]);
                            }
                            else
                            {
                                UpsertPriceLevel(OrderBookSide.Asks, new LiveItem { Price = ask[0], Volume = ask[1] });
                            }
                        }

                        foreach (var bid in bids)
                        {
                            if (bid[1] == 0)
                            {
                                RemovePriceLevel(OrderBookSide.Bids, bid[0]);
                            }
                            else
                            {
                                UpsertPriceLevel(OrderBookSide.Bids, new LiveItem { Price = bid[0], Volume = bid[1] });
                            }
                        }
                    }
                    _logger.LogDebug("Update has been processed: {result}", orderBookResponse.ToString());
                }
                _logger.LogDebug("Batch updates have been processed, updating UI...");
                // We finished the batch of updates so the semaphore is green (update UI)
                NewElementsSemaphore.Release();
            }
        }
    }
}