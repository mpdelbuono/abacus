namespace FleetPresenceMonitor
{
    using System;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// Entry point for the microservice where the background task takes place.
    /// </summary>
    internal sealed class FleetPresenceMonitor : StatelessService
    {
        public FleetPresenceMonitor(StatelessServiceContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Entry point for the stateless service.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                // Do nothing for now.
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
