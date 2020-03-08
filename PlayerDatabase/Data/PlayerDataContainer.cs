namespace Abacus.BackEnds.PlayerDatabase.Data
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    /// <summary>
    /// Data structure containing both the secure and insecure data associated
    /// with a character.
    /// </summary>
    /// <typeparam name="T">The type of data to be stored.</typeparam>
    [DataContract]
    internal class PlayerDataContainer<T>
        where T : class
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
    }
}
