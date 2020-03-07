using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;

// Use Service Fabric Remoting V2
[assembly: FabricTransportServiceRemotingProvider(
    RemotingListenerVersion = RemotingListenerVersion.V2,
    RemotingClientVersion = RemotingClientVersion.V2)]