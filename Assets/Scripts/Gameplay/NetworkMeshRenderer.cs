using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.IO;

// A component that allows changes in a mesh to be replicated across the network
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class NetworkMeshRenderer : NetworkBehaviourExt
{
    private MeshFilter Mesh;
    private MeshRenderer MeshRenderer;
    private bool bHasMeshChanged;


    // Start is called before the first frame update
    void Start()
    {
        Mesh = GetComponent<MeshFilter>();
        MeshRenderer = GetComponent<MeshRenderer>();
        bHasMeshChanged = false;

        Logger.LogScreen($"{MeshRenderer.material.mainTexture.name}");
    }

    protected override void OnReplicaResolved(ulong clientId)
    {
        base.OnReplicaResolved(clientId);
    }

    // Update is called once per frame
    void Update()
    {
        if(IsServer)
        {
            ServerUpdate();
        }
        else
        {
            ClientUpdate();
        }
    }

    void ServerUpdate()
    {
        
    }

    void ClientUpdate()
    {

    }

    [ClientRpc]
    void SetMeshClientRpc(Hash128 meshId, Hash128 mainTextureId)
    {

    }
}
