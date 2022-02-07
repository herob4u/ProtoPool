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

    public static int NumPoolBalls = 16;

    public BoxCollider PlayBox;
    public GameObject PoolBallPrefab;

    public float PoolBallSpacing = 0.05f;
    public float TableWidth = 10.0f;
    public float TableHeight = 20.0f;

    public OnPoolBallsStoppedDelegate OnPoolBallsStopped { get; set; }
    public OnPoolBallScoredDelegate OnPoolBallScored { get; set; }

    public OnPoolBallLaunchedDelegate OnPoolBallLaunched { get; set; }

    private PoolBall[] PoolBalls = new PoolBall[NumPoolBalls];
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

        InitPoolBalls();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    private void OnTriggerExit(Collider other)
    {
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
        if(!bAreBallsMoving)
        {
            return;
        }

        bool areAllBallsStopped = true;
        foreach(PoolBall ball in PoolBalls)
        {
            if(ball)
            {
                Rigidbody rigidbody = ball.GetComponent<Rigidbody>();
                if(rigidbody)
                {
                    areAllBallsStopped &= rigidbody.IsSleeping();
                }
            }
        }

        if(areAllBallsStopped)
        {
            bAreBallsMoving = false;

            if(OnPoolBallsStopped != null)
            {
                OnPoolBallsStopped.Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        foreach(PoolBall ball in PoolBalls)
        {
            if(ball)
            {
                Debug.LogWarningFormat("Destroying {0}", ball.gameObject.name);
                Destroy(ball.gameObject);
            }
        }
    }

    void InitPoolBalls()
    {
        //Bounds bounds = ScoreBoxCollider.bounds;
        Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;
        Vector3 startPos = bounds.center;
        startPos.y = bounds.max.y + 0.1f;

        // Spawn cue ball first
        GameObject cueBallObj = MakeBall(EPoolBallType.Cue);
        cueBallObj.transform.position = startPos - Vector3.forward * (bounds.extents.magnitude * 0.5f);
        PoolBalls[0] = cueBallObj.GetComponent<PoolBall>();

        // Start spawning remaining balls.
        float spacing = PoolBallSpacing;

        int ballId = 1;
        for(int row = 1; ballId < NumPoolBalls; row++)
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
                        rigidBody.Sleep();
                    }
                    else
                    {
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

        poolBall.SetBallType(ballType);

        Rigidbody rigidbody = ballObj.GetComponent<Rigidbody>();
        if(rigidbody)
        {
            rigidbody.sleepThreshold = 0.012f; //0.000005f; // Roughly approximated from (0.5 * v^2), where v = 1mm/s
        }

        NetworkObject networkObject = ballObj.GetComponent<NetworkObject>();
        if(networkObject)
        {
            networkObject.Spawn();
        }

        // Network object can only be reparented after being spawned.
        ballObj.transform.SetParent(this.transform, true);

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
        if((int) ballType >= NumPoolBalls)
        {
            Debug.LogError("BallType exceeds range of present balls.");
            return null;
        }

        return PoolBalls[(int)ballType];
    }
}
