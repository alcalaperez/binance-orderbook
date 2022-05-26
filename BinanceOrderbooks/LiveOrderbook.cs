using System.Collections.Generic;
using System.Linq;

public class LiveOrderbook
{
    public List<LiveItem> Bids { get; set; } = new List<LiveItem>();
    public List<LiveItem> Asks { get; set; } = new List<LiveItem>();

    public List<LiveItem> GetAskItems(int size)
    {
        return Asks.OrderBy(x => x.Price).Take(size).ToList();
    }

    public List<LiveItem> GetBidItems(int size)
    {
        return Bids.OrderByDescending(x => x.Price).Take(size).ToList();
    }

    public void RemovePriceLevel(string side, decimal price)
    {
        if(side == "bids")
        {
            Bids.RemoveAll(x => x.Price == price);
        } else
        {
            Asks.RemoveAll(x => x.Price == price);
        }
    }

    public void AddOrUpdatePriceLevel(string side, LiveItem itemToAdd)
    {
        if (side == "bids")
        {
            var bidIndex = Bids.FindIndex(x => x.Price == itemToAdd.Price);
            if (bidIndex != -1)
            {
                Bids[bidIndex].Volume = itemToAdd.Volume;
            }
            else
            {
                Bids.Add(itemToAdd);
            }
        }
        else
        {
            var askIndex = Asks.FindIndex(x => x.Price == itemToAdd.Price);
            if (askIndex != -1)
            {
                Asks[askIndex].Volume = itemToAdd.Volume;
            }
            else
            {
                Asks.Add(itemToAdd);
            }
        }
    }
}

public record LiveItem
{
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
}