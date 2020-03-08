namespace Abacus.BackEnds.PlayerDatabase
{
    using System;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Abacus.BackEnds.PlayerDatabase.Contracts;
    using Abacus.BackEnds.PlayerDatabase.Data;
    using Abacus.ServiceFabricUtilities;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    /// <summary>
    /// Controller implementing the read-only interface for the database, which is serviced by
    /// all replicas.
    /// </summary>
    internal class ReadOnlyController : IPlayerDatabaseReaderService
    {
        private const int DictionaryAccessAttempts = 4;

        /// <summary>
        /// Random number generator used for random retry intervals.
        /// </summary>
        private static readonly Random random = new Random();

        /// <summary>
        /// Time to wait during a read of data.
        /// </summary>
        private static readonly TimeSpan ReadTimeoutDuration = TimeSpan.FromMilliseconds(1000);

        private readonly IDictionaryFactory dictionaryFactory;


        internal ReadOnlyController(IDictionaryFactory dictionaryFactory)
        {
            this.dictionaryFactory = dictionaryFactory;
        }

        /// <inheritdoc/>
        public async Task<T?> GetCharacterDataAsync<T>(int characterId, CancellationToken cancellationToken) where T : class
        {
            // Get the data
            PlayerDataContainer<T>? result = await this.LookUpCharacterDataAsync<T>(characterId, cancellationToken).ConfigureAwait(false);
            return result?.CharacterData;
        }

        /// <inheritdoc/>
        public async Task<T?> GetSecureCharacterDataAsync<T>(int characterId, string secureHash, CancellationToken cancellationToken) where T : class
        {
            // Look up the data
            PlayerDataContainer<T>? result = await this.LookUpCharacterDataAsync<T>(characterId, cancellationToken).ConfigureAwait(false);
            
            // Look into the dictionary (if exists) to try to find the account hash
            if (result?.SecureData?.ContainsKey(secureHash) ?? false)
            {
                // No data available
                return result.SecureData[secureHash];
            }

            // No data was available for the character-account combination
            return null;
        }

        /// <summary>
        /// Waits a random amount of time with exponential backoff for the specified attempt number.
        /// </summary>
        /// <param name="attemptNumber">The attempt number, with attempt 0 being the first.</param>
        /// <param name="cancellationToken">Token to be canceled to abort the delay.</param>
        /// <returns>A <see cref="Task"/> upon which to await.</returns>
        private static async Task RandomWait(int attemptNumber, CancellationToken cancellationToken)
        {
            // Compute number of ms to delay as between 10ms and 500 * 2^x, where x is the attempt number
            int msDelay = random.Next(10, 500 * (1 << attemptNumber));
            TimeSpan delay = TimeSpan.FromMilliseconds(msDelay);

            // Wait the determined time
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Looks up the data associated with the specified character.
        /// </summary>
        /// <typeparam name="T">The type of data to read.</typeparam>
        /// <param name="characterId">The ESI character ID.</param>
        /// <param name="cancellationToken">A token that can be canceled to abort the read.</param>
        /// <returns>The data container, or <see langword="null"/> if there is no data for the character.</returns>
        /// <exception cref="FabricNotReadableException">
        /// The replica could not be read, typically because of quorum loss.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The operation timed out.
        /// </exception>
        /// <exception cref="FabricObjectClosedException">
        /// The replica is closed and this operation should not be attempted.
        /// </exception>
        private async Task<PlayerDataContainer<T>?> LookUpCharacterDataAsync<T>(int characterId, CancellationToken cancellationToken)
            where T : class
        {
            // Open the dictionary
            IDictionaryProxy<int, PlayerDataContainer<T>> dictionaryProxy =
                this.dictionaryFactory.CreateDictionary<int, PlayerDataContainer<T>>();

            IReliableDictionary2<int, PlayerDataContainer<T>> dictionary =
                await dictionaryProxy.OpenDictionaryAsync(cancellationToken).ConfigureAwait(false);

            // Look up the character. Retry if necessary.
            int attempts = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var tx = dictionaryProxy.CreateTransaction();

                try
                {
                    // Unpack the data
                    ConditionalValue<PlayerDataContainer<T>> value =
                        await dictionary.TryGetValueAsync(
                            tx,
                            characterId,
                            LockMode.Default,
                            ReadTimeoutDuration,
                            cancellationToken).ConfigureAwait(false);

                    if (value.HasValue)
                    {
                        // Present in dictionary. Return the data, or null if there is none.
                        return value.Value;
                    }
                    else
                    {
                        // No data available for character
                        return null;
                    }
                }
                catch (Exception ex) when (ex is TransactionFaultedException || ex is TimeoutException)
                {
                    // Check if we can retry
                    if (attempts >= DictionaryAccessAttempts)
                    {
                        // Ran out of retries. Throw the exception. If it was TransactionFaultedException, wrap
                        // it in TimeoutException for convenience to callers.
                        if (ex is TransactionFaultedException)
                        {
                            throw new TimeoutException(ex.Message, ex);
                        }

                        throw;
                    }

                    // Issue with the transaction or replica. Retry after random delay.
                    await RandomWait(attempts, cancellationToken).ConfigureAwait(false);
                    attempts++;
                }
            }
        }
    }
}
