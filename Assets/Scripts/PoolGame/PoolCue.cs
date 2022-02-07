using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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
[RequireComponent(typeof(PoolPlayerInput))]
public class PoolCue : NetworkBehaviour
{
    public delegate void OnCueCompletedDelegate();
    public delegate void OnCueCancelledDelegate();

    public OnCueCompletedDelegate OnCueCompleted;
    public OnCueCancelledDelegate OnCueCancelled;

    [SerializeField]
    private MeshFilter DisplayMesh;

    [Range(0.0f, 1.0f)]
    private float CurrentSwingPct = 0.0f;

    // @todo: Add a struct here for the stats of the cue. This influences how strong the swings are, accurate, etc.
    // ...

    public PoolBall ServingPoolBall { get; private set; }
    private NetworkVariable<bool> bIsServing = new NetworkVariable<bool>();
    private PoolPlayerCameraMgr CameraMgr = null;

    // Start is called before the first frame update
    void Start()
    {

    }

    public virtual void NetworkStart()
    {
        bIsServing.Value = false;

        if(IsOwner)
        {
            if(GetComponent<PoolPlayerInput>() == null)
            {
                gameObject.AddComponent<PoolPlayerInput>();
            }

            Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();
            if (localPlayer != null)
            {
                CameraMgr = localPlayer.GetPlayerGameInfo<PoolPlayerCameraMgr>();
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
                gameObject.SetActive(active);

                PoolPlayerInput poolPlayerInput = GetComponent<PoolPlayerInput>();
                if (IsOwner && poolPlayerInput != null)
                {
                    poolPlayerInput.enabled = active;
                }

                SetCueActiveClientRpc(active);
            }
        }
    }

    [ClientRpc]
    private void SetCueActiveClientRpc(bool active)
    {
        if(!IsServer)
        {
            if (active != gameObject.activeSelf)
            {
                gameObject.SetActive(active);

                PoolPlayerInput poolPlayerInput = GetComponent<PoolPlayerInput>();
                if (IsOwner && poolPlayerInput != null)
                {
                    poolPlayerInput.enabled = active;
                }
            }
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
        if(IsServer)
        {
            bIsServing.Value = isServing;
        }
    }

    public bool GetIsServing() { return bIsServing.Value; }

    // Instructs this cue to target a specified ball
    public void AcquireBall(PoolBall poolBall)
    {
        if(!poolBall)
        {
            return;
        }

        // Server can force us to acquire a ball too if needed
        if(IsOwner || IsServer)
        {
            // Send the server the request for acquistion
            AcquireBallServerRpc(poolBall.NetworkObjectId);
        }
    }

    public void ResetAcquistion()
    {
        if (!ServingPoolBall)
        {
            return;
        }

        if(IsOwner || IsServer)
        {
            Debug.Log("ResetAcquistion");

            ResetAcquistionServerRpc();
        }
    }

    [ServerRpc]
    private void AcquireBallServerRpc(ulong ballObjNetId)
    {
        if (IsServer)
        {
            // Can decide to verify to allow this or not...

            NetworkObject ballNetObj = null;
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ballObjNetId, out ballNetObj);

            if(!ballNetObj)
            {
                Debug.LogWarning("Server received AcquireBall with invalid net id. Make sure the object has a NetworkObject component.");
                ResetAcquistion();
                return;
            }

            PoolBall poolBall = ballNetObj.GetComponent<PoolBall>();
            if(!poolBall)
            {
                Debug.LogWarningFormat("Server AcquireBall failed. Object {0} with netID={1} is not a PoolBall.", ballNetObj.gameObject.name, ballObjNetId);
                ResetAcquistion();
                return;
            }

            // Hosts are both a client and server, they will finish setting the cue in the client RPC, so this is redundant.
            ServingPoolBall = poolBall;
            PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
            if(cuePositioner)
            {
                cuePositioner.SetOrbitObject(poolBall.gameObject);
            }


            // Notify clients so they update their state
            AcquireBallClientRpc(ballObjNetId);
        }
    }

    [ClientRpc]
    private void AcquireBallClientRpc(ulong ballObjNetId)
    {
        if(IsServer)
        {
            Debug.Log("Server called AcquireBallClientRpc");
        }

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
        PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
        if (cuePositioner)
        {
            cuePositioner.SetOrbitObject(poolBall.gameObject);
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    [ClientRpc]
    private void ResetAcquistionClientRpc()
    {
        if(IsClient)
        {
            ServingPoolBall = null;

            PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
            if (cuePositioner)
            {
                cuePositioner.SetOrbitObject(null);
            }

            Cursor.lockState = CursorLockMode.None;
        }
    }

    [ServerRpc]
    private void ResetAcquistionServerRpc()
    {
        if(IsServer)
        {
            // Can decide to verify to allow this or not...

            ServingPoolBall = null;

            PoolCuePositioner cuePositioner = GetComponent<PoolCuePositioner>();
            if (cuePositioner)
            {
                cuePositioner.SetOrbitObject(null);
            }

            ResetAcquistionClientRpc();
        }
    }

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

    public void CompleteSwing()
    {
        if(!IsOwner)
        {
            return;
        }

        if(ServingPoolBall)
        {
            PoolHitNetData hitData;
            hitData.PlayerNetId = OwnerClientId;
            hitData.HitOrigin = gameObject.transform.position;
            hitData.HitDirection = gameObject.transform.forward;
            hitData.SwingPct = CurrentSwingPct;

            OnHitServerRpc(hitData);
        }

        ServingPoolBall = null;

        CurrentSwingPct = 0.0f;
    }

    public void CancelSwing()
    {
        if (!IsOwner)
        {
            return;
        }

        CurrentSwingPct = 0.0f;
    }

    public float GetSwingPct()
    {
        return CurrentSwingPct;
    }

    [ServerRpc]
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
