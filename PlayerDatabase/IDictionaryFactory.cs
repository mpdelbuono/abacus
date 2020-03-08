namespace Abacus.BackEnds.PlayerDatabase
{
    using System;
    using Abacus.ServiceFabricUtilities;

    /// <summary>
    /// Interface to a mechanism to create an <see cref="IDictionaryProxy{TKey,TValue}"/>
    /// based on the provided types. This allows to create a type-generic interface
    /// through which to create a type-specific dictionary.
    /// </summary>
    internal interface IDictionaryFactory
    {
        /// <summary>
        /// Creates a new <see cref="IDictionaryProxy{TKey, TValue}"/> given
        /// the provided <typeparamref name="TKey"/> and <typeparamref name="TValue"/>
        /// parameters.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <returns>The <see cref="IDictionaryProxy{TKey, TValue}"/> to use
        /// for the specified types.</returns>
        IDictionaryProxy<TKey, TValue> CreateDictionary<TKey, TValue>()
            where TKey : IComparable<TKey>, IEquatable<TKey>;
    }
}
