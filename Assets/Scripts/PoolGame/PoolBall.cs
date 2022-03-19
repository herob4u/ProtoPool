using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/* Represents one of the balls present on the pool table */
public class PoolBall : NetworkBehaviourExt
{
    public PoolBallDescriptor BallDescriptor;

    private SphereCollider BallCollider;
    private Rigidbody BallRigidBody;
    private MeshFilter BallMesh;
    private MeshRenderer BallMeshRenderer;

    public bool bIsMoving { get; private set; }
    public bool bIsLaunched { get; private set; }
    public bool bIsInPlay { get; private set; }

    LaunchEventInfo LaunchInfo;

    public PoolBall()
    {
        bIsLaunched = false;
        bIsInPlay = true;
    }

    protected override void OnReplicaResolved(ulong clientId)
    {
        base.OnReplicaResolved(clientId);

        ClientRpcParams rpcParams = new ClientRpcParams();
        rpcParams.Send = new ClientRpcSendParams();
        rpcParams.Send.TargetClientIds = new ulong[] { clientId };

        // Send the joining player the updated ball visuals.
        InitFromDescriptorClientRpc(BallDescriptor.BallType, ResourceMgr.Instance.GetResourceId<Texture>(BallDescriptor.BallTexture), rpcParams);
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (!GetComponentInParent<NetworkObject>())
        {
            gameObject.AddComponent<NetworkObject>();
            Debug.LogWarning("PoolBall had no NetworkObject in hierarchy. Added automatically.");
        }

        BallCollider = GetComponent<SphereCollider>();
        if (!BallCollider)
        {
            BallCollider = gameObject.AddComponent<SphereCollider>();
            BallCollider.radius = 1.0f;
        }

        BallRigidBody = GetComponent<Rigidbody>();
        if (!BallRigidBody)
        {
            BallRigidBody = gameObject.AddComponent<Rigidbody>();
        }

        BallMesh = GetComponent<MeshFilter>();
        if (!BallMesh)
        {
            BallMesh = gameObject.AddComponent<MeshFilter>();
        }

        BallMeshRenderer = GetComponent<MeshRenderer>();
        if (!BallMeshRenderer)
        {
            BallMeshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
    }

    protected override void ServerUpdate()
    {
        if(BallRigidBody)
        {
            if((bIsLaunched || bIsMoving) && BallRigidBody.IsSleeping())
            {
                OnStopped();
            }
        }
    }

    public void OnLaunch(LaunchEventInfo launchEvent)
    {
        if(IsServer)
        {
            if(BallRigidBody)
            {
                Logger.LogScreen($"Launch velocity = {launchEvent.LaunchVelocity}");

                bIsLaunched = true;
                bIsMoving = true;

                BallRigidBody.velocity = launchEvent.LaunchVelocity;

                PoolTable poolTable = GetPoolTable();
                if (poolTable)
                {
                    poolTable.NotifyBallLaunched(this, launchEvent);
                }
            }
        }
    }

    // Called when the pool ball has stopped moving following
    protected void OnStopped()
    {
        bIsMoving = false;
        bIsLaunched = false;

        PoolTable poolTable = GetPoolTable();
        if (poolTable)
        {
            poolTable.NotifyBallStopped(this);
        }
    }

    public bool IsCueBall()
    {
        return BallDescriptor.BallType == EPoolBallType.Cue;
    }

    public void SetInPlay(bool isInPlay)
    {
        if(bIsInPlay != isInPlay)
        {
            bIsInPlay = isInPlay;
            gameObject.SetActive(isInPlay);
        }
    }

    public void SetBallType(EPoolBallType ballType)
    {
        PoolBallAssetDb assetDb = PoolGameDirector.Instance.GetGameRules().BallAssets;

        if(assetDb)
        {
            InitFromDescriptor(assetDb.Get(ballType));
        }
        else
        {
            Debug.LogWarning("PoolBallAssetDb not set - defaulting to cue ball");
        }
    }

    public EPoolBallType GetBallType()
    {
        return BallDescriptor.BallType;
    }

    public PoolTable GetPoolTable()
    {
        return GetComponentInParent<PoolTable>();
    }

    private void InitFromDescriptor(PoolBallDescriptor descriptor)
    {
        BallDescriptor = descriptor;
        OnDescriptorUpdated();

        InitFromDescriptorClientRpc(BallDescriptor.BallType, ResourceMgr.Instance.GetResourceId<Texture>(BallDescriptor.BallTexture));
    }

    private void OnDescriptorUpdated()
    {
        Logger.LogScreen("OnDescriptorUpdated!");

        if (!BallMeshRenderer)
        {
            Debug.LogWarning("BallMeshRenderer still not initialized");
            return;
        }

        Material ballMat = BallMeshRenderer.material;
        if (!ballMat)
        {
            Debug.LogError("Expected Pool Ball to have a valid material for its MeshRenderer");
            return;
        }

        // Null texture for cue ball.
        ballMat.SetTexture("_MainTex", BallDescriptor.BallTexture);
    }

    [ClientRpc]
    private void InitFromDescriptorClientRpc(EPoolBallType ballType, Hash128 textureId, ClientRpcParams rpcParams = default)
    {
        // Try and get the texture
        Texture2D ballTexture = ResourceMgr.Instance.GetResource<Texture2D>(textureId);
        BallDescriptor = new PoolBallDescriptor(ballTexture, ballType);

        if(GetComponent<NetworkMeshRenderer>() == null)
        {
            OnDescriptorUpdated();
        }
    }
}
