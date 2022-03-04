using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// The pool table, and to an effect, the container to most objects (i.e balls, cues)
public class PoolTable : NetworkBehaviour
{
    // Called whenever the balls have come to rest.
    public delegate void OnPoolBallsStoppedDelegate();
    public delegate void OnPoolBallLaunchedDelegate(PoolBall ball);
    public delegate void OnPoolBallScoredDelegate(PoolBall ball, PoolGamePlayer byPlayer);

    [SerializeField] private BoxCollider PlayBox;
    [SerializeField] private GameObject PoolBallPrefab;
    [SerializeField, Tooltip("Optional object that can be placed to explicitly indicate where the rack should be placed")]          private GameObject RackPivotHelper;
    [SerializeField, Tooltip("Optional object that can be placed to explicitly indicate where the cue ball should be placed")]      private GameObject CueBallPivotHelper;
    [SerializeField, Tooltip("Optional object that can be placed to explicitly indicate where the origin of the table surface is")] private GameObject SurfaceOriginHelper;

    [SerializeField] private float PoolBallSpacing = 0.05f;
    [SerializeField] private float Width = 10.0f;
    [SerializeField] private float Length = 20.0f;
    [SerializeField] private float Height = 5.0f;

    // By default, we assume that the mesh of the table is such that the length is oriented towards the forward direction of the object.
    // Reverse this if needed so that code that operates on the table surface can adapt.
    [SerializeField] private bool bLengthIsForward = true;

    public float SurfaceWidth { get => Width; }
    public float SurfaceLength { get => Length; }
    public float SurfaceHeight { get => Height; }

    public Vector3 LengthDirection { get { return bLengthIsForward ? transform.forward : transform.right; } }
    public Vector3 WidthDirection { get { return bLengthIsForward ? transform.right : transform.forward; } }

    public bool AreBallsMoving { get => bAreBallsMoving; }

    public OnPoolBallsStoppedDelegate OnPoolBallsStopped { get; set; }
    public OnPoolBallScoredDelegate OnPoolBallScored { get; set; }

    public OnPoolBallLaunchedDelegate OnPoolBallLaunched { get; set; }

    private PoolBall[] PoolBalls = new PoolBall[Pool.NUM_BALLS];
    private bool bAreBallsMoving = false;

    // Start is called before the first frame update
    void Awake()
    {
        // If no networking, just spawn immediately, no spawning dependencies.
        if(GetComponent<NetworkObject>() == null)
        {
            InitPoolBalls();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(IsServer)
        {
            InitPoolBalls();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            foreach (PoolBall ball in PoolBalls)
            {
                if (ball)
                {
                    Debug.LogWarningFormat("Destroying {0}", ball.gameObject.name);
                    ball.GetComponent<NetworkObject>().Despawn(true);
                    //Destroy(ball.gameObject);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(!IsServer)
        {
            return;
        }

        PoolBall ball = other.GetComponent<PoolBall>();

        if(!ball)
        {
            return;
        }

        if(ball.IsCueBall())
        {
            // ... Reset its position!
            ResetCueBall();
        }
        else
        {
            // Ball is disabled from play.
            ball.gameObject.SetActive(false);
            OnPoolBallScored.Invoke(ball, null);
        }
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
        if (!bAreBallsMoving)
        {
            return;
        }

        bool areAllBallsStopped = true;
        foreach (PoolBall ball in PoolBalls)
        {
            if (ball)
            {
                Rigidbody rigidbody = ball.GetComponent<Rigidbody>();
                if (rigidbody)
                {
                    areAllBallsStopped &= rigidbody.IsSleeping();
                }
            }
        }

        if (areAllBallsStopped)
        {
            bAreBallsMoving = false;

            if (OnPoolBallsStopped != null)
            {
                OnPoolBallsStopped.Invoke();
            }
        }
    }

    void ClientUpdate()
    {

    }

    public override void OnDestroy()
    {
    }

    public Vector3 GetIdealRackPosition()
    {
        if(RackPivotHelper)
        {
            return RackPivotHelper.transform.position;
        }

        float absoluteOffset = 0.25f * Length; // Rack should be place at the 75% mark, or 25% off the origin in the length direction.
        return GetSurfaceOrigin() + (LengthDirection * absoluteOffset);
    }

    // The center point of the rectangle that makes up the playable surface. This can either be inferred from
    // the bounding box, or by a user created collider.
    public Vector3 GetSurfaceOrigin()
    {
        if(SurfaceOriginHelper)
        {
            return SurfaceOriginHelper.transform.position;
        }

        Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;
        Vector3 origin = bounds.center;
        origin.y = bounds.max.y + 0.1f;

        return origin; 
    }

    void InitPoolBalls()
    {
        //Bounds bounds = ScoreBoxCollider.bounds;
        Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;
        Vector3 startPos = GetSurfaceOrigin();

        // Spawn cue ball first
        GameObject cueBallObj = MakeBall(EPoolBallType.Cue);
        PoolBalls[0] = cueBallObj.GetComponent<PoolBall>();

        if(CueBallPivotHelper)
        {
            cueBallObj.transform.position = CueBallPivotHelper.transform.position;
        }
        else
        {
            cueBallObj.transform.position = startPos - Vector3.forward * (bounds.extents.magnitude * 0.5f);
        }

        // Start spawning remaining balls.
        float spacing = PoolBallSpacing;

        int ballId = 1;
        for(int row = 1; ballId < Pool.NUM_BALLS; row++)
        {
            for(int i = 0; i < row; i++, ballId++)
            {
                EPoolBallType ballType = (EPoolBallType)ballId;

                GameObject ball = MakeBall(ballType);//PoolBallPrefab ? GameObject.Instantiate<GameObject>(PoolBallPrefab) : new GameObject();
                Vector3 ballPos = startPos;
                ballPos.x += spacing * i;

                ball.transform.position = ballPos;

                PoolBalls[ballId] = ball.GetComponent<PoolBall>();
            }

            startPos.z += spacing;
            startPos.x -= spacing / 2.0f;
        }

        bAreBallsMoving = true;
    }

    public void NotifyBallLaunched(PoolBall ball)
    {
        bAreBallsMoving = true;

        if(OnPoolBallLaunched != null)
        {
            OnPoolBallLaunched.Invoke(ball);
        }
    }

    public void SetBallsFrozen(bool frozen)
    {
        foreach(PoolBall ball in PoolBalls)
        {
            if(ball)
            {
                Rigidbody rigidBody = ball.GetComponent<Rigidbody>();
                if(rigidBody)
                {
                    if(frozen)
                    {
                        rigidBody.velocity = Vector3.zero;
                        rigidBody.freezeRotation = true;
                        rigidBody.Sleep();
                    }
                    else
                    {
                        rigidBody.freezeRotation = false;
                        rigidBody.WakeUp();
                    }
                }
            }
        }
    }

    private GameObject MakeBall(EPoolBallType ballType)
    {
        //Debug.LogFormat("Making ball type: {0}", ballType.ToString());
        // We define our pool balls such that they are owned/contained by the pool table. That way, the balls in play can readily access the pool table as a parent when needed.
        GameObject ballObj = PoolBallPrefab ? Instantiate<GameObject>(PoolBallPrefab) : new GameObject();

        PoolBall poolBall = ballObj.GetComponent<PoolBall>();
        if(!poolBall)
        {
            poolBall = ballObj.AddComponent<PoolBall>();
        }

        Rigidbody rigidbody = ballObj.GetComponent<Rigidbody>();
        if(rigidbody)
        {
            //rigidbody.sleepThreshold = 0.012f; //0.000005f; // Roughly approximated from (0.5 * v^2), where v = 1mm/s
        }

        NetworkObject networkObject = ballObj.GetComponent<NetworkObject>();
        if(networkObject)
        {
            networkObject.Spawn();
        }

        // Network object can only be reparented after being spawned.
        // RPCs can only be called after spawn, so make sure to do networked initializations afterwards too.
        ballObj.transform.SetParent(transform, true);
        poolBall.SetBallType(ballType);

        return ballObj;
    }

    public void ResetCueBall()
    {
        Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;
        Vector3 startPos = bounds.center;
        startPos.y = bounds.max.y + 0.1f;

        // Spawn cue ball first
        GameObject cueBallObj = PoolBalls[0].gameObject;
        if(cueBallObj)
        {
            cueBallObj.GetComponent<Rigidbody>().velocity = Vector3.zero;
            cueBallObj.transform.position = startPos - Vector3.forward * (bounds.extents.magnitude * 0.5f);
        }

    }

    public PoolBall GetCueBall()
    {
        return GetBall(EPoolBallType.Cue);
    }

    public PoolBall GetBall(EPoolBallType ballType)
    {
        if((int) ballType >= Pool.NUM_BALLS)
        {
            Debug.LogError("BallType exceeds range of present balls.");
            return null;
        }

        return PoolBalls[(int)ballType];
    }
}
