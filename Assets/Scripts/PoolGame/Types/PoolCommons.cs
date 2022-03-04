using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Pool
{
    public const int NUM_BALLS = 16;
    public const string DefaultPoolTableAssetPath = "";
    public const string DefaultPoolCueAssetPath = "";
    public const string TextureBundle = "pool/textures";
    public const string MaterialBundle = "pool/materials";
    public const string MeshBundle = "pool/meshes";
}

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
    [ReadOnly] public EPoolBallType BallType;

    public PoolBallDescriptor(Texture2D texture, EPoolBallType ballType)
    {
        BallTexture = texture;
        BallType = ballType;
    }
}

[System.Serializable]
public struct PoolGameDefaults
{
    public Mesh DefaultCueMesh;
    public Material DefaultCueMaterial;
    public GameObject DefaultPoolTablePrefab;
    public GameObject DefaultPoolCuePrefab;

    public PoolGameDefaults(Mesh cueMesh, Material cueMat, GameObject poolTableObj, GameObject poolCueObj)
    {
        DefaultCueMesh = cueMesh;
        DefaultCueMaterial = cueMat;
        DefaultPoolTablePrefab = poolTableObj;
        DefaultPoolCuePrefab = poolCueObj;
    }
}

[System.Serializable]
public struct ImpactEventInfo
{
    public GameObject OtherObject;
    public GameObject Instigator;
    public Vector3 Impulse;
    public ContactPoint ImpactPoint;
}