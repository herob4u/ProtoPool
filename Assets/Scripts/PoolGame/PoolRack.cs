using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/* A component that represents the rack in a pool game. Contains predefined pivot locations for each ball,
* and controls placement and movement of the rack on the table.
*
*   Usage: The server decides to display the rack and set its initial conditions using SetupRack.
*   SetControllingPlayer is then called by the server to decide who should have movement controls of the rack.
*   The controlling client handles input locally ands MoveRack for the desired final position.
*   The server validates the position and replicates to clients via NetworkTransform component.
*   Once done, the client calls FinishPlacement to confirm the final positioning of the rack. Control is relinquished, and the server resumes its flow.
*/
public class PoolRack : NetworkBehaviour
{
    public System.Action OnRackPlacementStarted { get; set; }
    public System.Action OnRackPlacementFinished { get; set; }

    [Range(-5.0f, 5.0f), SerializeField]
    private float RackHeightOffset = 0.05f;

    /* The ball types that this rack will try to position. This can vary if we introduce different ball types per gamemode. Ideally we should always exclude the cue ball */
    private PoolRackSlot[] RackSlots;

    private PoolTable Table;
    private Vector3 PlacementOrigin;
    private float WidthExtent; // The maximum offset for placement, calculated from the table's width and depth.
    private float LengthExtent;
    private float WidthOffset; // The current placement offset relative to the table's center;
    private float LengthOffset;

    private ulong ControllingPlayerNetId = PlayerMgr.INVALID_PLAYER_NET_ID;
    private bool bAreBallsCollected = false;

    public PoolRack SetupRack(PoolTable onTable, float widthOffset = 0.0f, float lengthOffset = 0.0f)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Client cannot issue SetupRack - flow controlled by server");
            return null;
        }

        if (!onTable)
        {
            Debug.LogWarning("Invalid PoolTable passed in for SetupRack");
            return null;
        }

        Table = onTable;

        Vector3 placementPos = onTable.gameObject.transform.position + (Vector3.up * 3.0f); // Arbitrary attempt to put it on "top" of the object. This will be corrected by raycast.
        MeshFilter tableMesh = onTable.GetComponent<MeshFilter>();
        if(tableMesh)
        {
            placementPos = tableMesh.mesh.bounds.center;
            placementPos.y = tableMesh.mesh.bounds.max.y; // top of bounding box.
        }

        PlacementOrigin = placementPos;

        // Compute maximum extents based on dimensions of table
        LengthExtent = onTable.SurfaceLength * 0.5f;
        WidthExtent  = onTable.SurfaceWidth * 0.5f;

        if ((PlacementOrigin + onTable.WidthDirection * widthOffset).magnitude > WidthExtent)
        {
            widthOffset = WidthExtent;
        }

        if((PlacementOrigin + onTable.LengthDirection * lengthOffset).magnitude > LengthExtent)
        {
            lengthOffset = LengthExtent;
        }

        // Apply corrected offset
        WidthOffset  = widthOffset;
        LengthOffset = lengthOffset;

        UpdateRackPosition();
        CollectBallsIntoRack();

        if(OnRackPlacementStarted != null)
        {
            OnRackPlacementStarted.Invoke();
        }

        return this;
    }

    [ClientRpc]
    void OnRackPlacementStartedClientRpc()
    {
        if (OnRackPlacementStarted != null)
        {
            OnRackPlacementStarted.Invoke();
        }
    }

    /* Called by the server after arbitrating the turn, to give the deserving the player the ability to position the rack */
    public void SetControllingPlayer(PoolGamePlayer poolPlayer)
    {
        if(!IsServer)
        {
            return;
        }

        if(poolPlayer != null && poolPlayer.Player != null)
        {
            ControllingPlayerNetId = poolPlayer.Player.NetId;
            Logger.LogScreen($"Player {poolPlayer.Player.NetId} has control over the rack");
        }
        else
        {
            Debug.LogWarning("SetControllingPlayer failed - pool player is invalid!");
        }
    }

    public ulong GetControllingPlayerNetId()
    {
        if(!IsServer)
        {
            return PlayerMgr.INVALID_PLAYER_NET_ID;
        }

        return ControllingPlayerNetId;
    }

    void UpdateRackPosition()
    {
        WidthOffset = Mathf.Clamp(WidthOffset, -WidthExtent, WidthExtent);
        LengthOffset = Mathf.Clamp(LengthOffset, -LengthExtent, LengthExtent);

        Vector3 finalPosition = PlacementOrigin + (Table.LengthDirection * LengthOffset) + (Table.WidthDirection * WidthOffset);

        // Perform raycasts to the ground
        RaycastHit hitInfo;
        int layerMask = (1 << 6); // placement
        float distance = 5.0f;
        if(Physics.Raycast(finalPosition + Vector3.up, Vector3.down, out hitInfo, distance, layerMask, QueryTriggerInteraction.Ignore /* Ignore trigger volumes!*/))
        {
            // Grounds the rack to the surface
            //Debug.LogFormat("Hitting {0}", hitInfo.collider.GetType().Name);
            finalPosition.y = hitInfo.point.y + RackHeightOffset;
        }

        transform.position = finalPosition;
    }

    // @todo: should this remain as private?
    // Called when rack placement is started. Moves the relevant balls into their designated rack slot, and
    // disables physics for stable user placement to occur.
    private void CollectBallsIntoRack()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Client cannot issue CollectBallsIntoRack - flow controlled by server");
            return;
        }

        if (bAreBallsCollected)
        {
            Debug.LogWarning("CollectBallsIntoRack called, but balls are already collected in rack");
            return;
        }

        if (!Table)
        {
            Debug.LogWarning("Failed to collect balls into rack - table is invalid");
            return;
        }

        foreach(PoolRackSlot slot in RackSlots)
        {
            if(slot == null)
            {
                Debug.LogWarning("PoolRackSlot invalid");
                continue;
            }

            // The ball exists in play, position it into the rack
            PoolBall ball = Table.GetBall(slot.BallType);
            if(ball)
            {
                Rigidbody rigidbody = ball.GetComponent<Rigidbody>();

                if(slot.IsPhysicsAsleepPostSpawn && rigidbody)
                {
                    rigidbody.Sleep();
                    rigidbody.detectCollisions = false;
                    rigidbody.isKinematic = true;
                }

                // The rack is a network object
                ball.transform.SetParent(transform, false);
            }
        }

        bAreBallsCollected = true;
    }

    // @todo: should this remain private?
    // Called after rack position is finalized. Releases the attachment of the balls from the rack,
    // and resets their physics state.
    private void ReleaseBallsFromRack()
    {
        if(!IsServer)
        {
            Debug.LogWarning("Client cannot issue ReleaseBallsFromRack - flow controlled by server");
            return;
        }

        if(!bAreBallsCollected)
        {
            Debug.LogWarning("ReleaseBallsFromRack called, but balls are not collected in rack");
            return;
        }

        if (!Table)
        {
            Debug.LogWarning("Failed to release balls from rack - table is invalid");
            return;
        }

        foreach (PoolRackSlot slot in RackSlots)
        {
            if (slot == null)
            {
                Debug.LogWarning("PoolRackSlot invalid");
                continue;
            }

            // The ball exists in play, position it into the rack
            PoolBall ball = Table.GetBall(slot.BallType);
            if (ball)
            {
                Rigidbody rigidbody = ball.GetComponent<Rigidbody>();

                rigidbody.isKinematic = false;
                rigidbody.detectCollisions = true;

                if (!slot.IsPhysicsAsleepPostPlace && rigidbody)
                {
                    rigidbody.WakeUp();
                }
                else if(slot.IsPhysicsAsleepPostPlace && rigidbody)
                {
                    rigidbody.Sleep();
                }

                ball.transform.SetParent(Table.transform, true);
                rigidbody.Sleep();
            }
        }

        bAreBallsCollected = false;
    }

    public void FinishPlacement()
    {
        if(IsClient)
        {
            FinishPlacementServerRpc();
        }
    }

    [ServerRpc]
    void FinishPlacementServerRpc(ServerRpcParams rpcParams=default)
    {
        if(!IsServer)
        {
            return;
        }

        ulong senderNetId = rpcParams.Receive.SenderClientId;
        if(CanPlayerControlRack(senderNetId))
        {
            ReleaseBallsFromRack();
            ControllingPlayerNetId = PlayerMgr.INVALID_PLAYER_NET_ID;

            if(OnRackPlacementFinished != null)
            {
                OnRackPlacementFinished.Invoke();
            }
        }
    }

    [ClientRpc]
    void FinishPlacementClientRpc()
    {
        if(!IsServer)
        {
            // Mainly for local cosmetic reactions to the event.
            if (OnRackPlacementFinished != null)
            {
                OnRackPlacementFinished.Invoke();
            }
        }
    }

    public void MoveRack(Vector3 worldPos)
    {
        if(IsClient)
        {
            MoveRackServerRpc(worldPos);
        }
    }

    [ServerRpc]
    void MoveRackServerRpc(Vector3 worldPos, ServerRpcParams rpcParams = default)
    {
        if(!IsServer)
        {
            return;
        }

        ulong senderNetId = rpcParams.Receive.SenderClientId;
        if (CanPlayerControlRack(senderNetId))
        {
            if (!Table)
            {
                return;
            }

            Vector3 toVector = worldPos - PlacementOrigin;

            float dWidth = Vector3.Dot(toVector, Table.WidthDirection);
            float dLength = Vector3.Dot(toVector, Table.LengthDirection);

            WidthOffset = Mathf.Clamp(dWidth, -WidthExtent, WidthExtent);
            LengthOffset = Mathf.Clamp(dLength, -LengthExtent, LengthExtent);
        }
    }

    public void FlipRack()
    {
        if(IsClient)
        {
            FlipRackServerRpc();
        }
    }

    [ServerRpc]
    void FlipRackServerRpc(ServerRpcParams rpcParams = default)
    {
        if(!IsServer)
        {
            return;
        }

        if(CanPlayerControlRack(rpcParams.Receive.SenderClientId))
        {
            transform.Rotate(new Vector3(0.0f, 180.0f, 0.0f), Space.Self);
        }
    }

    bool CanPlayerControlRack(ulong playerNetId)
    {
        if(IsServer)
        {
            return (ControllingPlayerNetId != PlayerMgr.INVALID_PLAYER_NET_ID) && (ControllingPlayerNetId == playerNetId);
        }

        return false;
    }

    // Start is called before the first frame update
    void Awake()
    {
        RackSlots = GetComponentsInChildren<PoolRackSlot>();
    }

    // Update is called once per frame
    void Update()
    {
        if(NetworkManager.Singleton.IsServer)
        {
            if(Table)
            {
                UpdateRackPosition();
            }

            UpdateBallPositions();
        }
    }

    void UpdateBallPositions()
    {
        foreach (PoolRackSlot slot in RackSlots)
        {
            if (slot == null)
            {
                Debug.LogWarning("PoolRackSlot invalid");
                continue;
            }

            // The ball exists in play, position it into the rack
            PoolBall ball = Table.GetBall(slot.BallType);
            if (ball)
            {
                // The rack is a network object
                ball.transform.position = slot.transform.position;
            }
        }
    }
}