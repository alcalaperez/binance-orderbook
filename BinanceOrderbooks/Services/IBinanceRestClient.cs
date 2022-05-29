using System.Threading.Tasks;

public interface IBinanceRestClient
{
    Task<BinanceSnapshotResponse> GetRestSnapshot();
}
