namespace Abacus.BackEnds.PlayerDatabase.Contracts
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting;

    /// <summary>
    /// Interface to accessing a player database API as a read/write API.
    /// </summary>
    public interface IPlayerDatabaseWriterService : IService
    {
        /// <summary>
        /// Writes data for the character into the database, replacing the data that was previously present.
        /// This data is stored into the insecure table which does not depend upon the character's account hash.
        /// To depend on the account hash (to clear the data after account transfers), see
        /// <see cref="WriteSecurePlayerDataAsync{T}(int, string, T)"/>.
        /// </summary>
        /// <param name="characterId">The ESI character ID of the character for which the data is stored.</param>
        /// <param name="data">The data to store.</param>
        /// <typeparam name="T">The type of data to store.</typeparam>
        /// <returns>A <see cref="Task"/> upon which the operation can be awaited.</returns>
        /// <exception cref="System.Runtime.Serialization.InvalidDataContractException">
        /// The type <typeparamref name="T"/> is not serializable by
        /// <see cref="System.Runtime.Serialization.DataContractSerializer"/>.
        /// </exception>
        /// <exception cref="System.TimeoutException">
        /// Writing to the database timed out. The most common causes for this are that the primary is
        /// unhealthy or the partition is in quorum loss.</exception>
        Task WritePlayerDataAsync<T>(int characterId, T data)
            where T : class;

        /// <summary>
        /// Writes data for the character into the database, replacing
        /// data which was previously present. This data is stored into the secure table
        /// and only accessible via <see cref="IPlayerDatabaseReaderService.GetSecureCharacterDataAsync(int, string)"/>.
        /// The secure table ensures that the appropriate hash is provided, so that if the character
        /// changes accounts, the data is cleared.
        /// </summary>
        /// <param name="characterId">The ESI character ID of the character for which the data is stored.</param>
        /// <param name="secureHash">The secure hash of the ESI character ID and account.</param>
        /// <param name="data">The data to store.</param>
        /// <typeparam name="T">The type of data to store.</typeparam>
        /// <returns>A <see cref="Task"/> upon which the operation can be awaited.</returns>
        /// <exception cref="System.Runtime.Serialization.InvalidDataContractException">
        /// The type <typeparamref name="T"/> is not serializable by
        /// <see cref="System.Runtime.Serialization.DataContractSerializer"/>.
        /// </exception>
        /// <exception cref="System.TimeoutException">
        /// Writing to the database timed out. The most common causes for this are that the primary is
        /// unhealthy or the partition is in quorum loss.</exception>
        Task WriteSecurePlayerDataAsync<T>(int characterId, string secureHash, T data)
            where T : class;
    }
}
