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
    public bool UpdateEnabled = true;
    public float UpdateInterval = 0.1f;
    private float UpdateTimer = 0.0f;

    private MeshFilter MeshFilter;
    private MeshRenderer MeshRenderer;

    private Mesh CurrentMesh;
    private Texture CurrentTexture;

    // Start is called before the first frame update
    void Start()
    {
        MeshFilter = GetComponent<MeshFilter>();
        MeshRenderer = GetComponent<MeshRenderer>();

        Debug.Assert(MeshFilter && MeshRenderer);

        CurrentMesh = MeshFilter.sharedMesh;
        CurrentTexture = MeshRenderer.material.mainTexture;

        Logger.LogScreen($"{MeshRenderer.material.mainTexture.name}");
    }

    protected override void OnReplicaResolved(ulong clientId)
    {
        base.OnReplicaResolved(clientId);
        SyncMesh(new ulong[]{ clientId});
    }

    // Update is called once per frame
    void Update()
    {
    }

    protected override void ServerUpdate()
    {
        if(UpdateEnabled)
        {
            if(UpdateTimer > 0.0f)
            {
                UpdateTimer -= Time.unscaledDeltaTime;
            }
            else
            {
                UpdateTimer = UpdateInterval;

                // Mesh info has change, do a network sync
                if(CurrentMesh != MeshFilter.sharedMesh
                    || CurrentTexture != MeshRenderer.material.mainTexture)
                {
                    SyncMesh();
                }
            }
        }
    }

    protected override void ClientUpdate()
    {

    }

    public void SyncMesh(ulong[] clientIds = null)
    {
        if(IsServer)
        {
            Hash128 meshId = ResourceMgr.Instance.GetResourceId(CurrentMesh);
            Hash128 textureId = ResourceMgr.Instance.GetResourceId(CurrentTexture);

            if(clientIds != null)
            {
                ClientRpcParams rpcParams = new ClientRpcParams()
                {
                    Send = new ClientRpcSendParams()
                    {
                        TargetClientIds = clientIds
                    }
                };

                SetMeshClientRpc(meshId, textureId, rpcParams);
            }
            else
            {
                SetMeshClientRpc(meshId, textureId);
            }
        }
    }

    [ClientRpc]
    void SetMeshClientRpc(Hash128 meshId, Hash128 mainTextureId, ClientRpcParams rpcParams = default)
    {
        if(MeshFilter)
        {
            Hash128 currMeshId = ResourceMgr.Instance.GetResourceId(MeshFilter.sharedMesh);
            if (currMeshId != meshId)
            {
                MeshFilter.sharedMesh = ResourceMgr.Instance.GetResource<Mesh>(meshId);
            }
        }

        if (MeshRenderer)
        {
            Hash128 currTextureId = ResourceMgr.Instance.GetResourceId(MeshRenderer.material.mainTexture);
            if (currTextureId != mainTextureId)
            {
                if(MeshRenderer.material)
                {
                    MeshRenderer.material.mainTexture = ResourceMgr.Instance.GetResource<Texture>(mainTextureId);
                }
            }
        }
    }
}
