using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkBehaviourExt : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if(!IsServer)
        {
            ReplicaResolvedServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReplicaResolvedServerRpc(ServerRpcParams rpcParams = default)
    {
        OnReplicaResolved(rpcParams.Receive.SenderClientId);
    }

    protected virtual void OnReplicaResolved(ulong clientId)
    {

    }
}
