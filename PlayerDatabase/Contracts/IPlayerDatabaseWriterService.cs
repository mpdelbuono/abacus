namespace Abacus.BackEnds.PlayerDatabase.Contracts
{
    using Microsoft.ServiceFabric.Services.Remoting;

    /// <summary>
    /// Interface to accessing a player database API as a read/write API.
    /// </summary>
    public interface IPlayerDatabaseWriterService : IService
    {
    }
}
