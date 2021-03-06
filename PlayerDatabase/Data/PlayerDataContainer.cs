﻿namespace Abacus.BackEnds.PlayerDatabase.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Data structure containing both the secure and insecure data associated
    /// with a character.
    /// </summary>
    /// <typeparam name="T">The type of data to be stored.</typeparam>
    [DataContract]
    internal class PlayerDataContainer<T>
        where T : class, System.ICloneable, System.IEquatable<T>
    {
        /// <summary>
        /// The insecure (non-account-bound) data associated with the character.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        internal T? CharacterData { get; set; }

        /// <summary>
        /// The secure (specific to each account) data associated with the character-account pair.
        /// The key in the dictionary references the secure account hash provided by ESI.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        internal Dictionary<string, T>? SecureData { get; set; }

        /// <inheritdoc/>
        internal PlayerDataContainer<T> Clone()
        {
            return new PlayerDataContainer<T>
            {
                // Clone the insecure data
                CharacterData = (T?)this.CharacterData?.Clone(),

                // Clone each entry in the secure dictionary
                SecureData = this.SecureData?.ToDictionary(
                    entry => entry.Key,
                    entry => (T)entry.Value.Clone())
            };
        }
    }
}
