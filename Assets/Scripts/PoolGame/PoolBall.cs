using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public struct LaunchNetData
{
    public Vector3 ImpactPoint;
    public Vector3 ImpactDirection;
    public float ImpactForce;
    public Vector3 OverrideVelocity;

    public LaunchNetData(Vector3 overrideVelocity)
    {
        ImpactPoint = Vector3.zero;
        ImpactDirection = Vector3.zero;
        ImpactForce = 0.0f;
        OverrideVelocity = overrideVelocity;
    }

    public LaunchNetData(Vector3 impactPoint, Vector3 impactDir, float impactForce)
    {
        ImpactPoint = impactPoint;
        ImpactDirection = impactDir;
        ImpactForce = impactForce;
        OverrideVelocity = Vector3.zero;
    }
}

/* Represents one of the balls present on the pool table */
public class PoolBall : NetworkBehaviourExt
{
    public PoolBallDescriptor BallDescriptor;

    public delegate void OnBallLaunchedDelegate(PoolBall self);
    public OnBallLaunchedDelegate OnBallLaunched { get; set; }

    private SphereCollider BallCollider;
    private Rigidbody BallRigidBody;
    private MeshFilter BallMesh;
    private MeshRenderer BallMeshRenderer;

    private bool bIsInPlay = true;

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

    // Update is called once per frame
    void Update()
    {

    }

    [ServerRpc]
    void OnLaunch_ServerRpc(LaunchNetData launchData)
    {
        if(!IsServer)
        {
            return;
        }

        Debug.LogWarning("OnLaunch_ServerRpc: WIP, don't expect this to be called on a client");
        return;

        //OnLaunch(launchData);
    }

    public void OnLaunch(LaunchNetData launchData)
    {
        if (IsServer)
        {
            if (BallRigidBody)
            {
                BallRigidBody.velocity = launchData.OverrideVelocity.magnitude == 0 ? launchData.ImpactForce * launchData.ImpactDirection.normalized : launchData.OverrideVelocity;

                if(OnBallLaunched != null)
                {
                    OnBallLaunched.Invoke(this);
                }

                PoolTable poolTable = GetPoolTable();
                if(poolTable)
                {
                    poolTable.NotifyBallLaunched(this);
                }
            }
        }
        else
        {
            OnLaunch_ServerRpc(launchData);
        }
    }

    public void OnLaunch(Vector3 impactPoint, Vector3 impactVelocity)
    {
        LaunchNetData launchData;
        launchData.OverrideVelocity = impactVelocity;
        launchData.ImpactForce = 0.0f;
        launchData.ImpactPoint = Vector3.zero;
        launchData.ImpactDirection = Vector3.zero;

        OnLaunch(launchData);
    }

    public bool IsCueBall()
    {
        return BallDescriptor.BallType == EPoolBallType.Cue;
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
