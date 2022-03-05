using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PoolPlayerInput : PlayerComponent
{
    const string AXIS_ACTION = "Action";
    const string AXIS_ACTION_ALT = "ActionAlt";
    const string AXIS_TURN_CUE_X = "TurnCue X";
    const string AXIS_TURN_CUE_Y = "TurnCue Y";
    const string AXIS_MOVE_RACK_X = "MoveRack X";
    const string AXIS_MOVE_RACK_Y = "MoveRack Y";
    const string AXIS_FLIP_RACK = "FlipRack";

    // Duration in seconds to hold the action input for it to register for a swing, otherwise users miscliking can accidently enter and release a swing immediately.
    public float SwingActionDeadTime = 0.1f;

    [Range(0.0f, 1.0f)]
    public float SwingConfirmationThreshold = 0.05f;
    public Vector2 SwingAxisMultiplier = new Vector2(1.0f, 1.0f);
    public Vector2 TurnAxisMultiplier = new Vector2(1.0f, 1.0f);
    private float swingActionTimer = 0.0f;

#if UNITY_EDITOR
    // Dummy object to display where our cursor world position is.
    [SerializeField]
    private GameObject DebugCursorObject = null;
#endif

    protected struct ControlTargetInfo
    {
        const int POOL_CUE_FLAG = (1 << 1);
        const int POOL_RACK_FLAG = (1 << 2);

        public GameObject ControlledObj { get; private set; }
        int TypeFlag; // 0 if controlling nothing

        public void SetControlTarget(GameObject obj)
        {
            if(ControlledObj != obj)
            {
                TypeFlag = 0;
                ControlledObj = obj;
                if(!ControlledObj) { return; }

                if(ControlledObj.GetComponent<PoolCue>())
                {
                    TypeFlag |= POOL_CUE_FLAG;
                }
                else if(ControlledObj.GetComponent<PoolRack>())
                {
                    TypeFlag |= POOL_RACK_FLAG;
                }
            }
        }

        public bool IsPoolRack() { return ControlledObj != null && (TypeFlag & POOL_RACK_FLAG) != 0; }
        public bool IsPoolCue() { return ControlledObj != null && (TypeFlag & POOL_CUE_FLAG) != 0; }
    }
    protected ControlTargetInfo ControlTarget;


    // Sets the object this component tries to control
    public void SetInputTarget(GameObject target)
    {
        ControlTarget.SetControlTarget(target);

        if (target)
        {
            Logger.LogScreen($"Input Target set to {target.name}");
        }
        else
        {
            Logger.LogScreen($"Input Target cleared");
        }

#if UNITY_EDITOR
        DebugCursorObject.SetActive(ControlTarget.IsPoolCue());
#endif
    }

    // Start is called before the first frame update
    void Start()
    {
        // In a networked game, this component must have been only added to the owner of the object.
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsOwner)
        {
            //Debug.LogError("Attempting to add PoolPlayerInput on an object not owned by client. Removing self...");
            Destroy(this);
        }

#if UNITY_EDITOR
        if (DebugCursorObject == null)
        {
            DebugCursorObject = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/DebugCube.prefab");
        }

        DebugCursorObject = Instantiate(DebugCursorObject);
        DebugCursorObject.SetActive(false);
#endif
    }

    // Update is called once per frame
    void Update()
    {
        // Restarts game
        if(Input.GetKeyDown(KeyCode.R))
        {
            if(PoolGameDirector.Instance)
            {
                Debug.Log("Restarting game...");
                PoolGameDirector.Instance.RestartGame();
                return;
            }
        }

        if(Input.GetKeyDown(KeyCode.C))
        {
            if(PlayerMgr.Instance)
            {
                Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();
                PoolPlayerCameraMgr cameraMgr = localPlayer.GetPlayerComponent<PoolPlayerCameraMgr>();

                if(cameraMgr)
                {
                    cameraMgr.ToggleCamera();
                    return;
                }
            }
        }

        if(ControlTarget.IsPoolRack())
        {
            HandleRackInput(ControlTarget.ControlledObj.GetComponent<PoolRack>());
            return;
        }

        if(ControlTarget.IsPoolCue())
        {
            HandleCueInput(ControlTarget.ControlledObj.GetComponent<PoolCue>());
            return;
        }
    }

    void HandleRackInput(PoolRack poolRack)
    {
        // First handle actions in case the placement finished
        if(Input.GetButtonDown(AXIS_ACTION))
        {
            poolRack.FinishPlacement();
            return;
        }

        if(Input.GetAxis(AXIS_FLIP_RACK) != 0)
        {
            Debug.Log("flip rack");
            poolRack.FlipRack();
            return;
        }

        float moveX, moveY;
        moveX = Input.GetAxis(AXIS_MOVE_RACK_X) * Time.deltaTime;
        moveY = Input.GetAxis(AXIS_MOVE_RACK_Y) * Time.deltaTime;

        if(Mathf.Approximately(moveX, 0.0f) && Mathf.Approximately(moveY, 0.0f))
        {
            // Don't waste bandwidth with empty inputs!
            return;
        }

        PoolPlayerCameraMgr cameraMgr = OwningPlayer.GetPlayerComponent<PoolPlayerCameraMgr>();

        Vector3 mouseWorldPos = Vector3.zero;
        if(cameraMgr.GetMouseWorldPosition(ref mouseWorldPos))
        {
            poolRack.MoveRack(mouseWorldPos);
        }
    }

    void HandleCueInput(PoolCue cue)
    {
        if (cue.ServingPoolBall == null)
        {
            return;
        }

        float turnX, turnY;
        turnX = Input.GetAxis(AXIS_TURN_CUE_X) * Time.deltaTime;
        turnY = Input.GetAxis(AXIS_TURN_CUE_Y) * Time.deltaTime;

        if (Input.GetButton(AXIS_ACTION))
        {
            // Just pressed the alt action? User wants to cancel their input
            if(Input.GetButtonDown(AXIS_ACTION_ALT))
            {
                cue.CancelSwing();
                swingActionTimer = 0.0f;
                return;
            }

            if (swingActionTimer >= SwingActionDeadTime)
            {
                cue.AddSwing(GetInputSwingPct(cue, turnX * SwingAxisMultiplier.x, turnY * SwingAxisMultiplier.y));
                return;
            }
            else
            {
                swingActionTimer += Time.deltaTime;
                return;
            }
        }
        else
        {
            swingActionTimer = 0.0f;

            if (cue.GetSwingPct() >= SwingConfirmationThreshold)
            {
                // Sufficently pulled back cue, confirm a swing
                cue.CompleteSwing();
                return;
            }
            else if (cue.GetSwingPct() > 0.0f)
            {
                // The swing delta was small enough to ignore, cancel.
                cue.CancelSwing();
                return;
            }

            PoolPlayerCameraMgr cameraMgr = OwningPlayer.GetPlayerComponent<PoolPlayerCameraMgr>();

            if(cameraMgr && cameraMgr.IsTopDownCameraEnabled())
            {
                Vector3 desiredDir = GetDesiredCueDirection(cue, cameraMgr, turnX * TurnAxisMultiplier.x, turnY * TurnAxisMultiplier.y);
                if(desiredDir.magnitude > 0.0f)
                {
                    cue.SetCueAimServerRpc(desiredDir);
                }
            }
            else
            {
                cue.GetComponent<PoolCuePositioner>().OnOrbit(turnY * TurnAxisMultiplier.y, turnX * TurnAxisMultiplier.x);
            }
        }
    }

    Vector3 GetDesiredCueDirection(PoolCue cue, PoolPlayerCameraMgr cameraMgr, float turnX, float turnY)
    {
        Vector3 mouseWorldPos = Vector3.zero;
        if (cameraMgr.GetMouseWorldPosition(ref mouseWorldPos, true))
        {
            mouseWorldPos.y = cue.transform.position.y; // Keep it level with the cue to retain the appropriate direction

#if UNITY_EDITOR
            DebugCursorObject.transform.position = mouseWorldPos;
#endif
            Vector3 desiredDir = (cue.ServingPoolBall.transform.position - mouseWorldPos).normalized;
            Debug.DrawRay(cue.ServingPoolBall.transform.position, desiredDir, Color.red);

            return desiredDir;
        }

        return Vector3.zero;
    }

    float GetInputSwingPct(PoolCue cue, float dx, float dy)
    {
        PoolPlayerCameraMgr cameraMgr = OwningPlayer.GetPlayerComponent<PoolPlayerCameraMgr>();
        Vector3 cueDir = cue.gameObject.transform.forward;

        float pullAmount = 0.0f;
        if (cameraMgr && cameraMgr.IsTopDownCameraEnabled())
        {
            Vector3 inputRayLocal = new Vector3(dy, 0.0f, -dx);

            Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + cueDir * 30.0f, Color.yellow);
            Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + inputRayLocal * 30.0f, Color.red);

            // Increase pull amount if we are pulling away, hence the negation.
            pullAmount = -Vector3.Dot(cueDir, inputRayLocal);
        }
        else
        {
            pullAmount -= dy;
        }

        return pullAmount;
    }
}
