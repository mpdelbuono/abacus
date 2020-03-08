namespace Abacus.BackEnds.PlayerDatabase
{
    using System;
    using System.Collections.Concurrent;
    using System.Fabric;
    using Abacus.ServiceFabricUtilities;
    using Microsoft.ServiceFabric.Data;

    /// <summary>
    /// Implementation of <see cref="IDictionaryFactory"/> which provides a Service Fabric-based
    /// dictionary proxy.
    /// </summary>
    internal class ServiceFabricReadOnlyDictionaryProxyFactory : IDictionaryFactory
    {
        /// <summary>
        /// The set of <see cref="IDictionaryProxy{TKey, TValue}"/> objects that have already
        /// been constructed, mapped to the TKey/TValue pair of its type parameters.
        /// </summary>
        private readonly ConcurrentDictionary<(Type, Type), object> constructedProxies
            = new ConcurrentDictionary<(Type, Type), object>();

        /// <summary>
        /// The Service Fabric state manager to use to read the data.
        /// </summary>
        private readonly IReliableStateManager stateManager;

        /// <summary>
        /// The partition in which to find the data.
        /// </summary>
        private readonly IServicePartition partition;

        /// <summary>
        /// The name of the dictionary to load.
        /// </summary>
        private readonly string dictionaryName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFabricReadOnlyDictionaryProxyFactory"/> class
        /// which uses the specified Service Fabric state manager and partition to look up the specified dictionary.
        /// </summary>
        /// <param name="stateManager">The Service Fabric state manager.</param>
        /// <param name="partition">The partition in which to find the data.</param>
        /// <param name="dictionaryName">The dictionary containing the data.</param>
        internal ServiceFabricReadOnlyDictionaryProxyFactory(
            IReliableStateManager stateManager,
            IServicePartition partition,
            string dictionaryName)
        {
            // Check parameter sanity
            if (string.IsNullOrEmpty(dictionaryName))
            {
                throw new ArgumentException(Properties.Resources.DictionaryNameBlank, nameof(dictionaryName));
            }

            this.stateManager = stateManager;
            this.partition = partition;
            this.dictionaryName = dictionaryName;
        }

        /// <inheritdoc/>
        public IDictionaryProxy<TKey, TValue> CreateDictionary<TKey, TValue>()
            where TKey : IComparable<TKey>, IEquatable<TKey>
        {
            // Construct the proxy if necessary
            object proxy = this.constructedProxies.GetOrAdd(
                (typeof(TKey), typeof(TValue)),
                (key) =>
                    new ReadOnlyDictionaryProxy<TKey, TValue>(
                        this.stateManager,
                        this.partition,
                        this.dictionaryName));

            // Return it. Type should match because we control it entirely.
            return (IDictionaryProxy<TKey, TValue>)proxy;
        }
    }
}
