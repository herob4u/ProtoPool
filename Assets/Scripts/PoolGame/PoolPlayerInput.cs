using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolPlayerInput : MonoBehaviour
{
    const string AXIS_ACTION = "Action";
    const string AXIS_ACTION_ALT = "ActionAlt";
    const string AXIS_MOUSE_X = "TurnCue X";
    const string AXIS_MOUSE_Y = "TurnCue Y";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        PoolCue cue = GetComponent<PoolCue>();

        if (Input.GetButtonDown(AXIS_ACTION))
        {
        }
        else if(Input.GetButtonUp(AXIS_ACTION))
        {
        }

        // Restarts game
        if(Input.GetKeyDown(KeyCode.R))
        {
            if(PoolGameDirector.Instance)
            {
                Debug.Log("Restarting game...");
                PoolGameDirector.Instance.RestartGame();
            }
        }

        // Cycle between balls to hit
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(PoolGameDirector.Instance && PoolGameDirector.Instance.GetPoolTable())
            {
                PoolBall cueBall = PoolGameDirector.Instance.GetPoolTable().GetCueBall();
                cue.OnAcquireCueBall(cueBall);
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
                }
            }
        }

        float mouseX, mouseY;
        mouseX = Input.GetAxis(AXIS_MOUSE_X) * Time.deltaTime;
        mouseY = Input.GetAxis(AXIS_MOUSE_Y) * Time.deltaTime;

        if (Input.GetButton(AXIS_ACTION))
        {
            if(cue.GetIsServing())
            {
                //cue.OnSwingInput(mouseY);
                cue.OnDebugHitCue(5.0f);
            }
        }
        else
        {
            if (cue.GetIsServing())
            {
                cue.OnTurnInput(mouseX, mouseY);
            }
        }
    }
}
