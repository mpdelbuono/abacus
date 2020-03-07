namespace Abacus.ServiceFabricUtilities
{
    using System;
    using System.Collections.Generic;
    using System.Security;

    /// <summary>
    /// Provider for Service Fabric configuration settings.
    /// </summary>
    public interface IServiceFabricConfigurationProvider
    {
        /// <summary>
        /// Gets a configuration value from the Service Fabric configuration settings.
        /// </summary>
        /// <param name="sectionName">The section name to look in.</param>
        /// <param name="parameterName">The parameter name to look up.</param>
        /// <returns>The value of the configuration entry.</returns>
        /// <exception cref="KeyNotFoundException">The <paramref name="sectionName"/> or 
        /// <paramref name="parameterName"/> was not found in the config package.,</exception>
        /// <exception cref="InvalidOperationException">The parameter is encrypted. You
        /// must use <see cref="GetSecureConfigValue(string, string)"/> to decrypt.</exception>

        string GetConfigValue(string sectionName, string parameterName);

        /// <summary>
        /// Gets a configuration value from the Service Fabric configuration settings. If
        /// the parameter is encrypted, it is decrypted.
        /// </summary>
        /// <param name="sectionName">The section name to look in.</param>
        /// <param name="parameterName">The parameter name to look up.</param>
        /// <returns>The value of the configuration entry.</returns>
        /// <exception cref="KeyNotFoundException">The <paramref name="sectionName"/> or 
        /// <paramref name="parameterName"/> was not found in the config package.,</exception>
        SecureString GetSecureConfigValue(string sectionName, string parameterName);
    }
}
