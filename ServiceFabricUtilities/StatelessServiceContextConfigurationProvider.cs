namespace Abacus.ServiceFabricUtilities
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Globalization;
    using System.Security;

    using Resources = Properties.Resources;

    /// <summary>
    /// Implementation of a <see cref="IServiceFabricConfigurationProvider"/> for a
    /// <see cref="StatelessServiceContext"/>'s configuration package..
    /// </summary>
    public class StatelessServiceContextConfigurationProvider : IServiceFabricConfigurationProvider
    {
        /// <summary>
        /// The name of the artifact package for configuration entries.
        /// </summary>
        private const string ConfigPackageName = "Config";

        /// <summary>
        /// The <see cref="ConfigurationPackage"/> in which to find configuration values.
        /// </summary>
        private readonly ConfigurationPackage configPackage;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatelessServiceContextConfigurationProvider"/> class
        /// with the specified <paramref name="context"/> used to look up configuration values.
        /// </summary>
        /// <param name="context">The <see cref="StatelessServiceContext"/> in which to find configuration values.</param>
        public StatelessServiceContextConfigurationProvider(StatelessServiceContext context)
        {
            this.configPackage = context.CodePackageActivationContext.GetConfigurationPackageObject(ConfigPackageName);
        }

        /// <inheritdoc/>
        public string GetConfigValue(string sectionName, string parameterName)
        {
            // Find the section
            if (this.configPackage.Settings.Sections.Contains(sectionName) == false)
            {
                throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.ConfigSectionNotFound, sectionName));
            }

            ConfigurationSection section = this.configPackage.Settings.Sections[sectionName];

            // Find the parameter
            if (section.Parameters.Contains(parameterName) == false)
            {
                throw new KeyNotFoundException($"");
            }

            if (section.Parameters[parameterName].IsEncrypted)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.CannotDecryptToString, parameterName));
            }

            return section.Parameters[parameterName].Value;
        }

        /// <inheritdoc/>
        public SecureString GetSecureConfigValue(string sectionName, string parameterName)
        {
            // Find the section
            if (this.configPackage.Settings.Sections.Contains(sectionName) == false)
            {
                throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.ConfigSectionNotFound, sectionName));
            }

            ConfigurationSection section = this.configPackage.Settings.Sections[sectionName];

            // Find the parameter
            if (section.Parameters.Contains(parameterName) == false)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.CannotDecryptToString, parameterName));
            }

            if (section.Parameters[parameterName].IsEncrypted)
            {
                return section.Parameters[parameterName].DecryptValue();
            }
            else
            {
                // Convert to secure string. Note this is not secure, but we were never secure because
                // the config parameter is not encrypted.
                var result = section.Parameters[parameterName].Value;
                var returnValue = new SecureString();
                foreach (char c in result)
                {
                    returnValue.AppendChar(c);
                }

                return returnValue;
            }
        }
    }
}
