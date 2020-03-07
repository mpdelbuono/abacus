namespace Abacus.FrontEnd.FleetPresenceApi
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Abacus.ServiceFabricUtilities;

    /// <summary>
    /// Implementation of the service entry point. The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class FleetPresenceServerService : StatelessService
    {
        /// <summary>
        /// Configuration section name for the server configuration.
        /// </summary>
        private const string ServerSectionName = "Server";

        /// <summary>
        /// Configuration parameter name for the Azure Key Vault to connect to.
        /// </summary>
        private const string KeyVaultParameterName = "KeyVaultUri";

        /// <summary>
        /// Configuration parameter name for the SSL certificate identifier in the Azure Key Vault.
        /// </summary>
        private const string SslCertificateParameterName = "SslCertificateIdentifier";

        /// <summary>
        /// Initializes as new instance of the <see cref="FleetPresenceServerService"/> class for the
        /// service specified by <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The context in which this service instance is running.</param>
        public FleetPresenceServerService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Creates listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder() 
                                    .UseKestrel(opt =>
                                    {
                                        int port = serviceContext.CodePackageActivationContext.GetEndpoint("ServiceEndpoint").Port;
                                        opt.Listen(IPAddress.IPv6Any, port, listenOptions =>
                                        {
                                            X509Certificate2? cert = GetCertificateFromStore(new StatelessServiceContextConfigurationProvider(serviceContext));

                                            if (cert != null)
                                            {
                                                listenOptions.UseHttps(cert);
                                            }
                                        });
                                    })
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatelessServiceContext>(serviceContext))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };
        }

        /// <summary>
        /// Finds the ASP .NET Core HTTPS development certificate in the current environment.
        /// </summary>
        /// <param name="configPackage">The configuration package from which to look up the service configuration.</param>
        /// <returns>The ASP .NET Core HTTPS development certificate, or <see langword="null"/> if no certificate was found and the system is in development mode.</returns>
        /// <exception cref="KeyNotFoundException">The certificate was not found and this is not a development environment.</exception>
        private static X509Certificate2? GetCertificateFromStore(IServiceFabricConfigurationProvider configPackage)
        {
            string aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            if (string.Equals(aspNetCoreEnvironment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                const string aspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
                const string CNName = "CN=localhost";
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certCollection = store.Certificates;
                    var currentCerts = certCollection.Find(X509FindType.FindByExtension, aspNetHttpsOid, true);
                    currentCerts = currentCerts.Find(X509FindType.FindByIssuerDistinguishedName, CNName, true);
                    return currentCerts.Count == 0 ? null : currentCerts[0];
                }
            }
            else
            {
                // Use MSI to access KeyVault to get the certificate
                var tokenProvider = new AzureServiceTokenProvider();
                using (var keyVaultClient = new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(
                            tokenProvider.KeyVaultTokenCallback)))
                {
                    CertificateBundle bundle =
                        keyVaultClient.GetCertificateAsync(
                            configPackage.GetConfigValue(ServerSectionName, KeyVaultParameterName),
                            configPackage.GetConfigValue(ServerSectionName, SslCertificateParameterName))
                            .GetAwaiter()
                            .GetResult();
                    return new X509Certificate2(bundle.Cer);
                }
            }
        }
    }
}
