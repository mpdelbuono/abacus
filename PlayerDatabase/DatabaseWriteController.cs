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
    internal class DatabaseWriteController : IPlayerDatabaseWriterService
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

        /// <summary>
        /// Time to wait during a write of data.
        /// </summary>
        private static readonly TimeSpan WriteTimeoutDuration = TimeSpan.FromMilliseconds(4000);

        /// <summary>
        /// The <see cref="IDictionaryFactory"/> to use to access the dictionary for writes.
        /// </summary>
        private readonly IDictionaryFactory dictionaryFactory;

        /// <summary>
        /// Callback defining how to merge the current state with the new state.
        /// </summary>
        /// <typeparam name="T">The data type to store.</typeparam>
        /// <param name="input">The current state of the data, or <see langword="null"/>
        /// if the data does not currently exist in the database.</param>
        /// <returns>The new state of the data.</returns>
        /// <exception cref="System.Data.DBConcurrencyException">A concurrent write was detected and
        /// the operation should be aborted.</exception>
        private delegate PlayerDataContainer<T> DataMergeCallback<T>(PlayerDataContainer<T>? input) where T : class, ICloneable, IEquatable<T>; 

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseWriteController"/> class using the specified dictionary.
        /// </summary>
        /// <param name="dictionaryFactory"></param>
        internal DatabaseWriteController(IDictionaryFactory dictionaryFactory)
        {
            this.dictionaryFactory = dictionaryFactory;
        }

        /// <inheritdoc/>
        public Task WritePlayerDataAsync<T>(int characterId, T data, T? previousData, CancellationToken cancellationToken)
            where T : class, ICloneable, IEquatable<T>
        {
            return this.WriteCharacterDataAsync<T>(characterId,
                (oldData) =>
                {
                    // Verify the old data set
                    bool oldDataExists = (oldData?.CharacterData != null);
                    bool newDataExists = (previousData != null);
                    if (oldDataExists != newDataExists)
                    {
                        throw new System.Data.DBConcurrencyException(Properties.Resources.DataTransactionMismatch);
                    }

                    // If there is data, check that they are the same (if there is no data, we know both == null)
                    // BUGBUG: Roslyn compiler incorrectly detects possible null reference here.
                    //         See https://github.com/dotnet/roslyn/issues/42046
                    //         When this issue is fixed, we should remove the override.
                    if (oldDataExists && oldData!.Equals(previousData) == false)
                    {
                        throw new System.Data.DBConcurrencyException(Properties.Resources.DataTransactionMismatch);
                    }

                    // Create the new data set
                    PlayerDataContainer<T> newData = oldData?.Clone() ?? new PlayerDataContainer<T>();
                    newData.CharacterData = (T)data.Clone();
                    return newData;
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public Task WriteSecurePlayerDataAsync<T>(int characterId, string secureHash, T data, T? previousData, CancellationToken cancellationToken) 
            where T : class, ICloneable, IEquatable<T>
        {
            return this.WriteCharacterDataAsync<T>(characterId,
                (oldData) =>
                {
                    // Verify the old data set
                    bool oldDataExists = (oldData?.SecureData?.ContainsKey(secureHash) ?? false);
                    bool newDataExists = (previousData != null);
                    if (oldDataExists != newDataExists)
                    {
                        throw new System.Data.DBConcurrencyException(Properties.Resources.DataTransactionMismatch);
                    }

                    // If there is data, check that they are the same (if there is no data, we know both == null)
                    if (oldDataExists)
                    {
                        // BUGBUG: Roslyn compiler incorrectly detects possible null reference here.
                        //         See https://github.com/dotnet/roslyn/issues/42046
                        //         When this issue is fixed, we should remove the override.
                        T oldValue = oldData!.SecureData![secureHash];
                        if (oldValue.Equals(previousData) == false)
                        {
                            throw new System.Data.DBConcurrencyException(Properties.Resources.DataTransactionMismatch);
                        }
                    }

                    // Checks passed. Create the new data set.
                    PlayerDataContainer<T> newData = oldData?.Clone() ?? new PlayerDataContainer<T>();
                    newData.SecureData ??= new System.Collections.Generic.Dictionary<string, T>();
                    newData.SecureData[secureHash] = (T)data.Clone();
                    return newData;
                },
                cancellationToken);
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
        /// Writes the data associated with the specified character.
        /// </summary>
        /// <typeparam name="T">The type of data to read.</typeparam>
        /// <param name="characterId">The ESI character ID.</param>
        /// <param name="dataMergeCallback">A callback function to use to determine the new state of the data to write.</param>
        /// <param name="cancellationToken">A token that can be canceled to abort the read.</param>
        /// <returns>The data container, or <see langword="null"/> if there is no data for the character.</returns>
        /// <exception cref="FabricNotReadableException">
        /// The replica could not be opened for writing, typically because of quorum loss.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The operation timed out.
        /// </exception>
        /// <exception cref="FabricObjectClosedException">
        /// The replica is closed and this operation should not be attempted.
        /// </exception>
        /// <exception cref="FabricNotPrimaryException">
        /// The replica is not the primary and this operation should be attempted on the primary.
        /// </exception>
        private async Task WriteCharacterDataAsync<T>(
            int characterId,
            DataMergeCallback<T> dataMergeCallback,
            CancellationToken cancellationToken)
            where T : class, ICloneable, IEquatable<T>
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
                            LockMode.Update,
                            ReadTimeoutDuration,
                            cancellationToken).ConfigureAwait(false);

                    // Write the new state to the dictonary
                    bool result;
                    if (value.HasValue)
                    {
                        // Get new state
                        PlayerDataContainer<T> newState = dataMergeCallback(value.Value);
                        result = await dictionary.TryUpdateAsync(
                            tx,
                            characterId,
                            newState,
                            value.Value,
                            WriteTimeoutDuration,
                            cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Get new state from null initial state
                        PlayerDataContainer<T> newState = dataMergeCallback(null);
                        result = await dictionary.TryAddAsync(
                            tx,
                            characterId,
                            newState,
                            WriteTimeoutDuration,
                            cancellationToken).ConfigureAwait(false);
                    }

                    // If the update failed, then the old state is incorrect.
                    if (result == false)
                    {
                        throw new TransactionFaultedException(Properties.Resources.ConcurrentWrite);
                    }

                    // Otherwise, it succeeded, so commit.
                    await tx.CommitAsync().ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    tx.Abort();

                    // Check if we can retry
                    if (attempts >= DictionaryAccessAttempts)
                    {
                        // Ran out of retries. Throw the exception. If it was TransactionFaultedException, wrap
                        // it in TimeoutException for convenience to callers.
                        throw;
                    }

                    // Issue with the transaction or replica. Retry after random delay.
                    await RandomWait(attempts, cancellationToken).ConfigureAwait(false);
                    attempts++;
                }
                catch (TransactionFaultedException ex)
                {
                    tx.Abort();

                    // A concurrent write occurred
                    throw new System.Data.DBConcurrencyException(Properties.Resources.ConcurrentWrite, ex);
                }
                catch (Exception)
                {
                    // Abort any ongoing transaction and rethrow
                    tx.Abort();
                    throw;
                }
            }
        }
    }
}
