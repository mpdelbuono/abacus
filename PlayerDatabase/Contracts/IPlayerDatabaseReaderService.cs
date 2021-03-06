﻿namespace Abacus.BackEnds.PlayerDatabase.Contracts
{
    using Microsoft.ServiceFabric.Services.Remoting;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to accessing a player database API as a read-only API.
    /// </summary>
    public interface IPlayerDatabaseReaderService : IService
    {
        /// <summary>
        /// Gets the data associated with the character. If no data is stored,
        /// then <see langword="null"/> is returned. Only the insecure data is returned, i.e.,
        /// the data written by <see cref="IPlayerDatabaseWriterService.WritePlayerDataAsync{T}(int, T)"/>.
        /// </summary>
        /// <param name="characterId">The EVE ID of the character.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> with which to abort the read.</param>
        /// <returns>The data stored on the character, or <see langword="null"/> if no
        /// data is stored.</returns>
        /// <remarks>
        /// This data is only the data which is stored generically, without
        /// reference to which account owns the character. To access the secure
        /// (account-bound) information, use <see cref="GetSecureCharacterDataAsync(int, string)"/>.
        /// </remarks>
        /// <exception cref="System.Fabric.FabricNotReadableException">
        /// The replica could not be read, typically because of quorum loss.
        /// </exception>
        /// <exception cref="System.TimeoutException">
        /// The operation timed out.
        /// </exception>
        /// <exception cref="System.Fabric.FabricObjectClosedException">
        /// The replica is closed and this operation should not be attempted.
        /// </exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException">
        /// The data that is stored is incompatible with type <typeparamref name="T"/>.</exception>
        Task<T?> GetCharacterDataAsync<T>(int characterId, CancellationToken cancellationToken)
            where T : class, ICloneable, IEquatable<T>;

        /// <summary>
        /// Gets the data associated with the player, including any secure data associated
        /// with the player, stored by <see cref="IPlayerDatabaseWriterService.WriteSecurePlayerDataAsync{T}(int, string, T)"/>.
        /// If no data is stored, then <see langword="null"/> is returned.
        /// Secure data for other accounts that owned the character is not returned.
        /// </summary>
        /// <param name="characterId">The EVE ID of the character.</param>
        /// <param name="secureHash">The ESI secure hash of the player.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> with which to abort the read.</param>
        /// <returns>The data stored on the character, or <see langword="null"/> if no
        /// data is stored.</returns>
        /// <remarks>This data is a superset of that data that would be returned
        /// from <see cref="GetPlayerDataAsync(int)"/>. The <paramref name="secureHash"/>
        /// is guaranteed unique for the combination of a character and the player's
        /// login account. This means if the character is transferred to another account,
        /// the hash will change.</remarks>
        /// <exception cref="System.Fabric.FabricNotReadableException">
        /// The replica could not be read, typically because of quorum loss.
        /// </exception>
        /// <exception cref="System.TimeoutException">
        /// The operation timed out.
        /// </exception>
        /// <exception cref="System.Fabric.FabricObjectClosedException">
        /// The replica is closed and this operation should not be attempted.
        /// </exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException">
        /// The data that is stored is incompatible with type <typeparamref name="T"/>.</exception>
        Task<T?> GetSecureCharacterDataAsync<T>(int characterId, string secureHash, CancellationToken cancellationToken)
            where T : class, ICloneable, IEquatable<T>;
    }
}
