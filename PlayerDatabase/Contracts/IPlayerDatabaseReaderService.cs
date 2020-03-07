namespace Abacus.BackEnds.PlayerDatabase.Contracts
{
    using Microsoft.ServiceFabric.Services.Remoting;

    /// <summary>
    /// Interface to accessing a player database API as a read-only API.
    /// </summary>
    public interface IPlayerDatabaseReaderService : IService
    {
    }
}
