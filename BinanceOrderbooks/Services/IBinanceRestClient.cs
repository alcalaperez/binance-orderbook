using System.Threading.Tasks;
using BinanceOrderbooks.Model;

namespace BinanceOrderbooks.Services
{
    public interface IBinanceRestClient
    {
        Task<BinanceSnapshotResponse> GetRestSnapshot();
        Task<BinanceSymbolsResponse> GetSymbols();
    }
}