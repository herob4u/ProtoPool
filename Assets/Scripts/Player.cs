using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public List<PlayerComponent> PlayerGameInfos;

    public string PlayerName { get; private set; }
    public ulong NetId { get; private set; }
    public bool IsAcknowledged { get => bIsAcknowledged.Value; }
    private NetworkVariable<bool> bIsAcknowledged = new NetworkVariable<bool>(false);

    // Start is called before the first frame update
    void Start()
    {
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        NetId = GetComponent<NetworkObject>().OwnerClientId;
        Logger.LogScreen($"Player NetworkSpawn called, NetId={NetId}");
        SetAcknowledged();
        PlayerMgr.Instance.NotifyPlayerJoined(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerMgr.Instance.NotifyPlayerLeft(this);
    }

    private void Awake()
    {
        Debug.LogFormat("Player created - id={0}, isLocal={1}", GetComponent<NetworkObject>().OwnerClientId, GetComponent<NetworkObject>().IsLocalPlayer);
    }



    // Update is called once per frame
    void Update()
    {
        
    }

    // This will be called immediately on the server since the netobject will be spawned right after instantiation.
    // On the client, this will be called when the prefab is created, and a message is sent to the server indicating
    // that the clients have discovered this player.
    void SetAcknowledged()
    {
        if(IsClient)
        {
            if (IsLocalPlayer)
            {
                SetAcknowledgedServerRpc();
            }
        }
    }

    [ServerRpc]
    void SetAcknowledgedServerRpc()
    {
        if(!IsServer) { return; }

        if(bIsAcknowledged.Value == true)
        {
            Debug.LogWarning("Player already acknowledged!");
            return;
        }

        bIsAcknowledged.Value = true;
        Logger.LogScreen($"Player {NetId} acknowledged", Color.green);
    }

    public bool IsLocal()
    {
        return GetComponent<NetworkObject>().IsLocalPlayer;
    }

    public T GetPlayerComponent<T>() where T : PlayerComponent
    {
        return GetComponent<T>();
    }

    public T AddPlayerComponent<T>() where T : PlayerComponent
    {
        T playerComponent = GetComponent<T>();
        if(playerComponent)
        {
            Debug.LogWarningFormat("{0} already exists for player, ignoring.", playerComponent.GetType().Name);
            return playerComponent;
        }

        playerComponent = gameObject.AddComponent<T>();
        playerComponent.OnAdded(this);

        Debug.LogFormat("Added {0} to player", playerComponent.GetType().Name);

        return playerComponent;
    }

    public void RemovePlayerComponent<T>() where T : PlayerComponent
    {
        T playerComponent = gameObject.GetComponent<T>();
        if(playerComponent)
        {
            playerComponent.OnRemoved(this);
            Destroy(playerComponent);

            Debug.LogFormat("Removed {0} from player", playerComponent.GetType().Name);
        }
    }
}
