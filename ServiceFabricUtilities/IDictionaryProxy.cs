namespace Abacus.ServiceFabricUtilities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    /// <summary>
    /// Proxy class through which to access a dictionary.
    /// </summary>
    public interface IDictionaryProxy<TKey, TValue>
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        /// <summary>
        /// Opens the dictionary represented by this proxy.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> with which to cancel the operation.</param>
        /// <returns>The dictionary through which data can be accessed.</returns>
        /// <exception cref="InvalidOperationException">The dictionary is unavailable for opening (for example,
        /// this is a read-only proxy and the dictionary does not exist).</exception>
        Task<IReliableDictionary2<TKey, TValue>> OpenDictionaryAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Begins a transaction that ends when the returned value is disposed.
        /// </summary>
        /// <returns>The transaction object.</returns>
        ITransaction CreateTransaction();
    }
}
