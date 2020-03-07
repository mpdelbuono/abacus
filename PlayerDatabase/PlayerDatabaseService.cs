namespace Abacus.BackEnds.PlayerDatabase
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Health;
    using System.Threading;
    using System.Threading.Tasks;
    using Abacus.BackEnds.PlayerDatabase.Contracts;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;

    /// <summary>
    /// Entry point for the service. Service Fabric spins up an instance of this class when the service becomes active.
    /// </summary>
    internal sealed class PlayerDatabaseService : StatefulService, IPlayerDatabaseReaderService, IPlayerDatabaseWriterService
    {
        /// <summary>
        /// The name of the reliable collection storing the database.
        /// </summary>
        private const string ReliableCollectionName = "PlayerDatabase";

        /// <summary>
        /// Number of seconds between heartbeats.
        /// </summary>
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Creates a new instance of the <see cref="PlayerDatabaseService"/> class with the specified
        /// service context.
        /// </summary>
        /// <param name="context">The Service Fabric service context.</param>
        public PlayerDatabaseService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Creates the Service Fabric remoting listeners through which RPCs will be invoked.
        /// </summary>
        /// <returns>The newly created remoting listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners() =>
            new[]
            {
                new ServiceReplicaListener(
                    (c) => new FabricTransportServiceRemotingListener(c, new ReadOnlyController(this.Context, this.Partition, this.StateManager)),
                    listenOnSecondary: true
                )
            };

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // Open the dictionary for use

            // Run while the service is primary. However, because there is no background work to do, we just emit a heartbeat.
            while (cancellationToken.IsCancellationRequested == false)
            {
                this.Partition.ReportPartitionHealth(
                    new HealthInformation("PlayerDatabaseService-RunAsync", "Heartbeat", HealthState.Ok));

                await Task.Delay(HeartbeatInterval, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
