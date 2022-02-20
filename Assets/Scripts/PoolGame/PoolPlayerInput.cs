using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// @TODO: consider promoting this to a PlayerComponent!
public class PoolPlayerInput : PlayerComponent
{
    const string AXIS_ACTION = "Action";
    const string AXIS_ACTION_ALT = "ActionAlt";
    const string AXIS_TURN_CUE_X = "TurnCue X";
    const string AXIS_TURN_CUE_Y = "TurnCue Y";
    const string AXIS_MOVE_RACK_X = "MoveRack X";
    const string AXIS_MOVE_RACK_Y = "MoveRack Y";

    // Duration in seconds to hold the action input for it to register for a swing, otherwise users miscliking can accidently enter and release a swing immediately.
    public float SwingActionDeadTime = 0.1f;

    [Range(0.0f, 1.0f)]
    public float SwingConfirmationThreshold = 0.05f;
    public Vector2 SwingAxisMultiplier = new Vector2(1.0f, 1.0f);
    public Vector2 TurnAxisMultiplier = new Vector2(1.0f, 1.0f);
    private float swingActionTimer = 0.0f;

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
                ControlledObj = obj;
                if(!ControlledObj) { return; }

                TypeFlag = 0;
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

        public bool IsPoolRack() { return ControlledObj != null && (TypeFlag &= POOL_RACK_FLAG) != 0; }
        public bool IsPoolCue() { return ControlledObj != null && (TypeFlag &= POOL_CUE_FLAG) != 0; }
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
                cue.OnSwingInput(turnX * SwingAxisMultiplier.x, turnY * SwingAxisMultiplier.y);
                //cue.OnDebugHitCue(5.0f);
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
                cue.CompleteSwing();
                return;
            }
            else if (cue.GetSwingPct() > 0.0f)
            {
                cue.CancelSwing();
                return;
            }

            cue.OnTurnInput(turnX * TurnAxisMultiplier.x, turnY * TurnAxisMultiplier.y);
        }
    }
}
