using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Abacus.ServiceFabricUtilities
{
    /// <summary>
    /// Proxy implementation for accessing a dictionary in a read-only manner.
    /// </summary>
    public class ReadOnlyDictionaryProxy<TKey, TValue> : IDictionaryProxy<TKey, TValue>
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        /// <summary>
        /// Amount of time to wait between retries opening the dictionary. If all durations
        /// are exhausted, the attempt fails.
        /// </summary>
        private static readonly IEnumerable<TimeSpan> RetryDurations = new TimeSpan[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(8)
        };

        /// <summary>
        /// The <see cref="IReliableStateManager"/> from which to read the data.
        /// </summary>
        private readonly IReliableStateManager stateManager;

        /// <summary>
        /// The <see cref="IServicePartition"/> on which to report faults.
        /// </summary>
        private readonly IServicePartition partition;

        /// <summary>
        /// The name of the dictionary represented by this proxy.
        /// </summary>
        private readonly string dictionaryName;

        /// <summary>
        /// The already-opened dictionary that can be reused.
        /// </summary>
        private IReliableDictionary2<TKey, TValue>? cachedDictionary = null;

        public ReadOnlyDictionaryProxy(IReliableStateManager stateManager, IServicePartition partition, string dictionaryName)
        {
            this.stateManager = stateManager;
            this.partition = partition;
            this.dictionaryName = dictionaryName;
        }

        /// <summary>
        /// Gets the dictionary, opening it if necessary.
        /// </summary>
        /// <returns>The reliable state dictionary.</returns>
        /// <remarks>This task may take some time to complete. In addition to needing to possibly needing to communicate with
        /// replicas, the call may also need to perform retries in the event that the service is not in a healthy state.</remarks>
        public async Task<IReliableDictionary2<TKey, TValue>> OpenDictionaryAsync(CancellationToken cancellationToken)
        {
            // Use the cached dictionary if possible
            if (this.cachedDictionary != null)
            {
                return this.cachedDictionary;
            }

            // Look it up instead
            IEnumerator<TimeSpan> retryDuration = RetryDurations.GetEnumerator();
            do
            {
                // Try to open the dictionary
                cancellationToken.ThrowIfCancellationRequested();
                var dictionary = await this.stateManager.TryGetAsync<IReliableDictionary2<TKey, TValue>>(this.dictionaryName).ConfigureAwait(false);
                if (dictionary.HasValue)
                {
                    this.cachedDictionary = dictionary.Value;
                    return this.cachedDictionary;
                }
            } while (await WaitNext(retryDuration, cancellationToken).ConfigureAwait(false));

            // Failed to open the dictionary
            throw new FabricNotReadableException($"Failed to open dictionary {this.dictionaryName}");
        }

        /// <inheritdoc/>
        public ITransaction CreateTransaction() => this.stateManager.CreateTransaction();

        /// <summary>
        /// Reads the next wait time from the enumerator and waits for that duration.
        /// If the enumerator has no more entries, no wait occurs.
        /// </summary>
        /// <param name="enumerator">The enumerator to enumerate over.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> with which to abort the wait.</param>
        /// <returns><see langword="false"/> if there are no more elements in
        /// <paramref name="enumerator"/>, or <see langword="true"/> otherwise.</returns>
        private static async Task<bool> WaitNext(IEnumerator<TimeSpan> enumerator, CancellationToken cancellationToken)
        {
            // Check if there are more entries
            bool result = enumerator.MoveNext();

            // If there are more entries, delay
            if (result)
            {
                await Task.Delay(enumerator.Current, cancellationToken).ConfigureAwait(false);
            }

            // Return whether or not to break out.
            return result;
        }
    }
}
