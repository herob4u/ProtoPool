using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/* Typed, in order by the numbers they show */
public enum EPoolBallType
{
    Cue = 0,
    Solid_Yellow,
    Solid_Blue,
    Solid_Red,
    Solid_Violet,
    Solid_Orange,
    Solid_Green,
    Solid_Maroon,
    Solid_Black,

    Stripe_Yellow,
    Stripe_Blue,
    Stripe_Red,
    Stripe_Violet,
    Stripe_Orange,
    Stripe_Green,
    Stripe_Maroon,
};

[System.Serializable]
public struct PoolBallDescriptor
{
    public Texture2D BallTexture;
    public EPoolBallType BallType;

    public PoolBallDescriptor(Texture2D texture, EPoolBallType ballType)
    {
        BallTexture = texture;
        BallType = ballType;
    }
}

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
public class PoolBall : NetworkBehaviour
{
    public static Dictionary<EPoolBallType, PoolBallDescriptor> BallTypesDescriptors = new Dictionary<EPoolBallType, PoolBallDescriptor>()
    {
        { EPoolBallType.Cue,                new PoolBallDescriptor(null, EPoolBallType.Cue) },
        { EPoolBallType.Solid_Yellow,       new PoolBallDescriptor(null, EPoolBallType.Solid_Yellow) },
        { EPoolBallType.Solid_Blue,         new PoolBallDescriptor(null, EPoolBallType.Solid_Blue) },
        { EPoolBallType.Solid_Red,          new PoolBallDescriptor(null, EPoolBallType.Solid_Red) },
        { EPoolBallType.Solid_Violet,       new PoolBallDescriptor(null, EPoolBallType.Solid_Violet) },
        { EPoolBallType.Solid_Orange,       new PoolBallDescriptor(null, EPoolBallType.Solid_Orange) },
        { EPoolBallType.Solid_Green,        new PoolBallDescriptor(null, EPoolBallType.Solid_Green) },
        { EPoolBallType.Solid_Maroon,       new PoolBallDescriptor(null, EPoolBallType.Solid_Maroon) },
        { EPoolBallType.Solid_Black,        new PoolBallDescriptor(null, EPoolBallType.Solid_Black) },

        { EPoolBallType.Stripe_Yellow,       new PoolBallDescriptor(null, EPoolBallType.Stripe_Yellow) },
        { EPoolBallType.Stripe_Blue,         new PoolBallDescriptor(null, EPoolBallType.Stripe_Blue) },
        { EPoolBallType.Stripe_Red,          new PoolBallDescriptor(null, EPoolBallType.Stripe_Red) },
        { EPoolBallType.Stripe_Violet,       new PoolBallDescriptor(null, EPoolBallType.Stripe_Violet) },
        { EPoolBallType.Stripe_Orange,       new PoolBallDescriptor(null, EPoolBallType.Stripe_Orange) },
        { EPoolBallType.Stripe_Green,        new PoolBallDescriptor(null, EPoolBallType.Stripe_Green) },
        { EPoolBallType.Stripe_Maroon,       new PoolBallDescriptor(null, EPoolBallType.Stripe_Maroon) }
    };

    public PoolBallDescriptor BallDescriptor;

    public delegate void OnBallLaunchedDelegate(PoolBall self);
    public OnBallLaunchedDelegate OnBallLaunched { get; set; }

    private SphereCollider BallCollider;
    private Rigidbody BallRigidBody;
    private MeshFilter BallMesh;
    private MeshRenderer BallMeshRenderer;

    private bool bIsInPlay = true;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {

        }
        else
        {

        }
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
        PoolBallDescriptor descriptor = BallTypesDescriptors[ballType];
        InitFromDescriptor(descriptor);
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
        if(!BallMeshRenderer)
        {
            Debug.LogWarning("BallMeshRenderer still not initialized");
            return;
        }

        Material ballMat = BallMeshRenderer.material;
        if(!ballMat)
        {
            Debug.LogError("Expected Pool Ball to have a valid material for its MeshRenderer");
            return;
        }

        Debug.LogWarning("@TODO: Use appropriate texture sampler name!");
        //ballMat.SetTexture("diffuse", descriptor.BallTexture);

        BallDescriptor = descriptor;
    }
}
