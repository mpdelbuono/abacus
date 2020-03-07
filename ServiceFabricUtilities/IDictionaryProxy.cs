namespace Abacus.ServiceFabricUtilities
{
    using System;
    using System.Threading.Tasks;
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
        /// <returns>The dictionary through which data can be accessed.</returns>
        /// <exception cref="InvalidOperationException">The dictionary is unavailable for opening (for example,
        /// this is a read-only proxy and the dictionary does not exist).</exception>
        Task<IReliableDictionary<TKey, TValue>> OpenDictionaryAsync();
    }
}
