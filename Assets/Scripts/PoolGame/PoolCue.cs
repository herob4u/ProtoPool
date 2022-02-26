using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public struct PoolHitNetData : INetworkSerializable
{
    public ulong PlayerNetId;
    public Vector3 HitOrigin;
    public Vector3 HitDirection;
    public float SwingPct;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerNetId);
        serializer.SerializeValue(ref HitOrigin);
        serializer.SerializeValue(ref HitDirection);
        serializer.SerializeValue(ref SwingPct);
    }
}
[RequireComponent(typeof(PoolCuePositioner))]
//[RequireComponent(typeof(PoolPlayerInput))]
public class PoolCue : NetworkBehaviour
{
    public delegate void OnCueCompletedDelegate();
    public delegate void OnCueCancelledDelegate();

    public OnCueCompletedDelegate OnCueCompleted;
    public OnCueCancelledDelegate OnCueCancelled;

    [SerializeField]
    private MeshFilter DisplayMesh;

    [Range(0.0f, 1.0f)]
    private NetworkVariable<float> CurrentSwingPct = new NetworkVariable<float>(0.0f);
    public float SwingPct { get => CurrentSwingPct.Value; }

    // @todo: Add a struct here for the stats of the cue. This influences how strong the swings are, accurate, etc.
    // ...

    public PoolBall ServingPoolBall { get; private set; }
    private PoolPlayerCameraMgr CameraMgr = null;

    [SerializeField]
    private GameObject DebugCursorObject;

    public enum ECueState
    {
        None,
        Turn,
        Swing,
        BackSwing
    };

    public ECueState CueState { get => cueState.Value; }
    private NetworkVariable<ECueState> cueState = new NetworkVariable<ECueState>(ECueState.None);

    // Start is called before the first frame update
    void Start()
    {
        DebugCursorObject = Instantiate(DebugCursorObject);
        DebugCursorObject.SetActive(false);
        //gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if(IsOwner && IsClient)
        {
            Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();
            if (localPlayer != null)
            {
                CameraMgr = localPlayer.GetPlayerComponent<PoolPlayerCameraMgr>();
                if(CameraMgr)
                {
                    CameraMgr.TargetCueObject = gameObject;
                }
            }
        }
    }
    
    // Called on the clients to hide the cue object, and disable its inputs if it's our own cue
    public void SetCueActive(bool active)
    {
        if(IsServer)
        {
            if (active != gameObject.activeSelf)
            {
                if(!IsClient)
                {
                    gameObject.SetActive(active);
                }

                SetCueActiveClientRpc(active);
            }
        }
    }

    [ClientRpc]
    private void SetCueActiveClientRpc(bool active)
    {
        if(IsClient)
        {
            Debug.Log("SetCueActiveClientRpc called");
            if (active != gameObject.activeSelf)
            {
                gameObject.SetActive(active);

                if(IsOwner && active)
                {
                    Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();
                    PoolPlayerInput playerInput = localPlayer.GetPlayerComponent<PoolPlayerInput>();

                    if (playerInput)
                    {
                        playerInput.SetInputTarget(gameObject);
                    }
                    else
                    {
                        Logger.LogScreen("Client: SetCueActive did not set input target. PoolPlayerInput is null!");
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if( IsServer)
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
        // Performing a backswing, wait for it to finish and commit the hit
        if (cueState.Value == ECueState.BackSwing)
        {
            PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
            if (cuePositioner.GetPullPct() == 0.0f)
            {
                OnBackSwingCompleted();
            }
            else
            {
                cuePositioner.SetPullPct(cuePositioner.GetPullPct() - ((1.0f / 0.125f) * Time.deltaTime));
            }
        }
    }

    void ClientUpdate()
    {

    }

    bool SetCosmetic(int cosmeticId)
    {
        return false;
    }

    // Instructs this cue to target a specified ball - can only be called by the server
    public void AcquireBall(PoolBall poolBall)
    {
        if(!poolBall)
        {
            return;
        }

        if(IsServer)
        {
            DoAcquireBall(poolBall);
            AcquireBallClientRpc(poolBall.NetworkObjectId);
        }
    }

    // Instructs this cue to clear its targetting and reset its position - can only be called by the server
    public void ResetAcquistion()
    {
        if (!ServingPoolBall)
        {
            return;
        }

        if(IsServer)
        {
            Debug.Log("ResetAcquistion");

            ServingPoolBall = null;

            PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
            if (cuePositioner)
            {
                cuePositioner.SetOrbitObject(null);
            }

            ResetAcquistionClientRpc();
        }
    }

    [ClientRpc]
    private void AcquireBallClientRpc(ulong ballObjNetId)
    {
        NetworkObject ballNetObj = null;
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ballObjNetId, out ballNetObj);

        if (!ballNetObj)
        {
            Debug.LogWarning("Client received AcquireBall with invalid net id. Make sure the object has a NetworkObject component.");
            ResetAcquistion();
            return;
        }

        PoolBall poolBall = ballNetObj.GetComponent<PoolBall>();
        if (!poolBall)
        {
            Debug.LogWarningFormat("Client AcquireBall failed. Object {0} with netID={1} is not a PoolBall.", ballNetObj.gameObject.name, ballObjNetId);
            ResetAcquistion();
            return;
        }

        ServingPoolBall = poolBall;
        Debug.LogFormat("Client: acquired serving ball {0}", ServingPoolBall.name);
    }

    private void DoAcquireBall(PoolBall poolBall)
    {
        ServingPoolBall = poolBall;
        PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
        if (cuePositioner)
        {
            cuePositioner.SetOrbitObject(poolBall.gameObject);
        }

        //if(!IsClient)
        //{
        //    Cursor.lockState = CursorLockMode.Locked;
        //}
    }

    [ClientRpc]
    private void ResetAcquistionClientRpc()
    {
        if(IsClient)
        {
            ServingPoolBall = null;

            /*
            PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
            if (cuePositioner)
            {
                cuePositioner.SetOrbitObject(null);
            }
            */

            Cursor.lockState = CursorLockMode.None;
        }
    }

    void SetCueState(ECueState state)
    {
        if(!IsServer)
        {
            Debug.LogWarning("Only server controls the state of the cue");
            return;
        }

        if(cueState.Value != state)
        {
            cueState.Value = state;
        }
    }

    public void AddSwing(float pct)
    {
        if(IsClient && IsOwner)
        {
            GetComponent<PoolCuePositioner>().SetPullPct(pct); // Set the position locally. Will get fixed by server afterwards.
            AddSwingServerRpc(pct);
        }
    }

    [ServerRpc]
    void AddSwingServerRpc(float pct, ServerRpcParams rpcParams = default)
    {
        if (IsServer)
        {
            if(rpcParams.Receive.SenderClientId != OwnerClientId)
            {
                return;
            }

            //Debug.LogFormat("Server: adding {0} swing pct", pct);
            if (cueState.Value == ECueState.BackSwing)
            {
                return;
            }

            SetCueState(ECueState.Swing);

            // Verify input is from correct user...
            float pullAmount = Mathf.Clamp(pct, -1.0f, 1.0f);
            CurrentSwingPct.Value = Mathf.Clamp(CurrentSwingPct.Value + pullAmount, 0.0f, 1.0f);

            GetComponent<PoolCuePositioner>().SetPullPct(CurrentSwingPct.Value);
        }
    }

    [ServerRpc]
    public void SetCueSwingPctServerRpc(float swingPct, ServerRpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId == OwnerClientId)
        {
            CurrentSwingPct.Value = Mathf.Clamp(swingPct, 0.0f, 1.0f);
            GetComponent<PoolCuePositioner>().SetPullPct(CurrentSwingPct.Value);
        }
    }

    public void SetCueAim(Vector3 aimDirection)
    {
        if(IsClient && IsOwner)
        {
            // Set locally, will then be corrected by server.
            PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
            if(aimDirection.magnitude > 0.0f)
            {
                cuePositioner.SetOrbitDirection(aimDirection);
            }

            SetCueAimServerRpc(aimDirection);
        }
    }

    [ServerRpc]
    public void SetCueAimServerRpc(Vector3 aimDirection, ServerRpcParams rpcParams = default)
    {
        if(!IsServer)
        {
            return;
        }

        if (cueState.Value == ECueState.BackSwing)
        {
            Debug.LogWarning("Server: rejecting cue aim, cue is in backswing");
            return;
        }

        SetCueState(ECueState.Turn);

        PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();

        if (aimDirection.magnitude <= 0.0f)
        {
            Debug.LogWarning("Server: rejecting cue aim, aim direction invalid");
            return;
        }

        float pitch, yaw;
        PoolCuePositioner.DirectionToOrientation(aimDirection, out pitch, out yaw);

        // Client provided an out of bound orientation, ignore or choose to clamp
        if(!cuePositioner.IsValidOrientation(pitch, yaw))
        {
            //Debug.LogWarning("Server: rejecting cue aim, out of bound orientation");
            return;
        }

        cuePositioner.SetOrbitDirection(aimDirection, true);
    }

    public void CompleteSwing()
    {
        if(!IsOwner)
        {
            return;
        }

        CompleteSwingServerRpc();
    }

    [ServerRpc]
    void CompleteSwingServerRpc(ServerRpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId == OwnerClientId)
        {
            SetCueState(ECueState.BackSwing);
        }
    }

    // Called by the server - occurs when a backswing is completed in Update(). Launches the ball.
    void OnBackSwingCompleted()
    {
        if(!IsServer)
        {
            return;
        }

        if (ServingPoolBall)
        {
            PoolHitNetData hitData;
            hitData.PlayerNetId = OwnerClientId;
            hitData.HitOrigin = gameObject.transform.position;
            hitData.HitDirection = gameObject.transform.forward;
            hitData.SwingPct = CurrentSwingPct.Value;

            OnHitServerRpc(hitData);
        }

        ServingPoolBall = null;
        ResetAcquistionClientRpc();

        CurrentSwingPct.Value = 0.0f;

        SetCueState(ECueState.None);
    }

    public void CancelSwing()
    {
        if (!IsOwner)
        {
            return;
        }

        CancelSwingServerRpc();
    }

    [ServerRpc]
    void CancelSwingServerRpc(ServerRpcParams rpcParams=default)
    {
        if(!IsServer)
        {
            return;
        }

        if(rpcParams.Receive.SenderClientId == OwnerClientId)
        {
            CurrentSwingPct.Value = 0.0f;
            GetComponent<PoolCuePositioner>().SetPullPct(CurrentSwingPct.Value);

            SetCueState(ECueState.Turn);
        }
    }

    public float GetSwingPct()
    {
        return CurrentSwingPct.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    void OnHitServerRpc(PoolHitNetData hitData)
    {
        // Clients send this message to the server, letting it know they confirmed a hit. The server now tries to simulate the hit.
        if(IsServer)
        {
            if(ServingPoolBall)
            {
                LaunchNetData launchData = new LaunchNetData(hitData.HitOrigin, hitData.HitDirection, hitData.SwingPct * 5.0f);
                ServingPoolBall.OnLaunch(launchData);
            }
            else
            {
                Debug.LogWarning("Server received hit event, but no ball is being served.");
            }
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
