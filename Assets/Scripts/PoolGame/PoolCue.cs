using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PoolCuePositioner))]
[RequireComponent(typeof(PoolPlayerInput))]
public class PoolCue : MonoBehaviour
{
    public delegate void OnCueCompletedDelegate();
    public delegate void OnCueCancelledDelegate();

    public OnCueCompletedDelegate OnCueCompleted;
    public OnCueCancelledDelegate OnCueCancelled;

    [SerializeField]
    private MeshFilter DisplayMesh;

    [Range(0.0f, 1.0f)]
    private float CurrentSwingPct = 0.0f;

    public PoolBall ServingPoolBall { get; private set; }
    private bool bIsServing = false;
    private PoolPlayerCameraMgr CameraMgr = null;

    // Start is called before the first frame update
    void Start()
    {
        Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();
        if(localPlayer != null)
        {
            CameraMgr = localPlayer.GetPlayerGameInfo<PoolPlayerCameraMgr>();
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    bool SetCosmetic(int cosmeticId)
    {
        return false;
    }

    public void SetIsServing(bool isServing)
    {
        bIsServing = isServing;
    }

    public bool GetIsServing() { return bIsServing; }

    public void OnAcquireCueBall(PoolBall cueBall)
    {
        if(cueBall)
        {
            PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
            cuePositioner.SetOrbitObject(cueBall.gameObject);

            ServingPoolBall = cueBall;

            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void OnSwingInput(float delta)
    {
        CurrentSwingPct = Mathf.Clamp(CurrentSwingPct + delta, 0.0f, 1.0f);
        GetComponent<PoolCuePositioner>().SetPullPct(CurrentSwingPct);
    }

    public void OnTurnInput(float dx, float dy)
    {
        PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();

        if (CameraMgr && CameraMgr.IsTopDownCameraEnabled())
        {
            cuePositioner.OnOrbit(0.0f, dx);
        }
        else
        {
            cuePositioner.OnOrbit(dy, dx);
        }
    }

    public void OnDebugHitCue(float launchVelocity)
    {
        if(ServingPoolBall)
        {
            ServingPoolBall.OnLaunch(Vector3.zero, transform.forward * launchVelocity);
        }
    }
}
