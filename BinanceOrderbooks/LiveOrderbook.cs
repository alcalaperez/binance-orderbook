using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using BinanceOrderbooks.Model;

public class LiveOrderbook
{
    private readonly ConcurrentDictionary<decimal, decimal> Bids = new();
    private readonly ConcurrentDictionary<decimal, decimal> Asks = new();
    public Channel<OrderBookResponse> buffer;
    private readonly IBinanceRestClient _binanceRestClient;
    private long lastUpdated;

    public LiveOrderbook(IBinanceRestClient binanceRestClient)
    {
        _binanceRestClient = binanceRestClient;
        buffer = Channel.CreateUnbounded<OrderBookResponse>();
    }

    public IEnumerable<KeyValuePair<decimal, decimal>> GetAskItems(int size)
    {
        return Asks.OrderBy(x => x.Key).Take(size);
    }

    public IEnumerable<KeyValuePair<decimal, decimal>> GetBidItems(int size)
    {
        return Bids.OrderByDescending(x => x.Key).Take(size);

    }

    public void RemovePriceLevel(OrderBookSide side, decimal price)
    {
        if (side == OrderBookSide.Bids)
        {
            Bids.Remove(price, out var liveItem);
        }
        else
        {
            Asks.Remove(price, out var liveItem);
        }
    }

    public void UpsertPriceLevel(OrderBookSide side, LiveItem itemToAdd)
    {
        if (side == OrderBookSide.Bids)
        {
            Bids.AddOrUpdate(itemToAdd.Price, itemToAdd.Volume, (price, volume) => itemToAdd.Volume);
        }
        else
        {
            Asks.AddOrUpdate(itemToAdd.Price, itemToAdd.Volume, (price, volume) => itemToAdd.Volume);
        }
    }

    public async void StoreSnapshot()
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

        await ProcessBufferAsync();
    }

    public async Task ProcessBufferAsync()
    {
        while (await buffer.Reader.WaitToReadAsync())
        {
            while (buffer.Reader.TryRead(out var orderBookResponse))
            {
                if(orderBookResponse.FinalUpdateId > lastUpdated)
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
            }
        }
    }
}
