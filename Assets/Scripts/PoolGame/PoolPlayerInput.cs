using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PoolPlayerInput : MonoBehaviour
{
    const string AXIS_ACTION = "Action";
    const string AXIS_ACTION_ALT = "ActionAlt";
    const string AXIS_TURN_CUE_X = "TurnCue X";
    const string AXIS_TURN_CUE_Y = "TurnCue Y";

    // Duration in seconds to hold the action input for it to register for a swing, otherwise users miscliking can accidently enter and release a swing immediately.
    public float SwingActionDeadTime = 0.1f;

    [Range(0.0f, 1.0f)]
    public float SwingConfirmationThreshold = 0.05f;

    private float swingActionTimer = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        // In a networked game, this component must have been only added to the owner of the object.
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsOwner)
        {
            Debug.LogError("Attempting to add PoolPlayerInput on an object not owned by client. Removing self...");
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        PoolCue cue = GetComponent<PoolCue>();

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

        // Cycle between balls to hit
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(PoolGameDirector.Instance && PoolGameDirector.Instance.GetPoolTable())
            {
                PoolBall cueBall = PoolGameDirector.Instance.GetPoolTable().GetCueBall();
                cue.AcquireBall(cueBall);
                return;
            }

        }

        if(Input.GetKeyDown(KeyCode.C))
        {
            if(PlayerMgr.Instance)
            {
                Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();
                PoolPlayerCameraMgr cameraMgr = localPlayer.GetPlayerGameInfo<PoolPlayerCameraMgr>();

                if(cameraMgr)
                {
                    cameraMgr.ToggleCamera();
                    return;
                }
            }
        }

        if(cue.GetIsServing())
        {
            HandleCueInput();
        }
    }

    void HandleCueInput()
    {
        PoolCue cue = GetComponent<PoolCue>();

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
                cue.OnSwingInput(turnY);
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

            cue.OnTurnInput(turnX, turnY);
        }
    }
}
