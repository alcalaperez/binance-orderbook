using System.Threading.Tasks;

public interface IBinanceRestClient
{
    Task<BinanceSnapshot> GetRestSnapshot(string instrument);
}
