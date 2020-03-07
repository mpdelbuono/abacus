namespace Abacus.BackEnds.PlayerDatabase
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Text;
    using Abacus.BackEnds.PlayerDatabase.Contracts;
    using Microsoft.ServiceFabric.Data;

    /// <summary>
    /// Controller implementing the read-only interface for the database, which is serviced by
    /// all replicas.
    /// </summary>
    internal class ReadOnlyController : IPlayerDatabaseReaderService
    {

        private readonly StatefulServiceContext context;
        private readonly IStatefulServicePartition partition;
        private readonly IReliableStateManager stateManager;


        internal ReadOnlyController(StatefulServiceContext context, IStatefulServicePartition partition, IReliableStateManager stateManager)
        {
            this.context = context;
            this.partition = partition;
            this.stateManager = stateManager;
        }
    }
}
