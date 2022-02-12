using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Player : MonoBehaviour
{
    public List<PlayerGameInfo> PlayerGameInfos;

    public string PlayerName { get; private set; }
    public ulong NetId { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        NetId = GetComponent<NetworkObject>().OwnerClientId;

        PlayerMgr.Instance.NotifyPlayerJoined(this);
    }

    private void Awake()
    {
        Debug.LogFormat("Player created - id={0}, isLocal={1}", GetComponent<NetworkObject>().OwnerClientId, GetComponent<NetworkObject>().IsLocalPlayer);
    }

    private void OnDestroy()
    {
        PlayerMgr.Instance.NotifyPlayerLeft(this);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsLocal()
    {
        return GetComponent<NetworkObject>().IsLocalPlayer;
    }

    public T GetPlayerGameInfo<T>() where T : PlayerGameInfo
    {
        return GetComponent<T>();
    }

    public T AddPlayerGameInfo<T>() where T : PlayerGameInfo
    {
        T playerGameInfo = GetComponent<T>();
        if(playerGameInfo)
        {
            Debug.LogWarningFormat("{0} already exists for player, ignoring.", playerGameInfo.GetType().Name);
            return playerGameInfo;
        }

        playerGameInfo = gameObject.AddComponent<T>();
        playerGameInfo.OnAdded(this);

        Debug.LogFormat("Added {0} to player", playerGameInfo.GetType().Name);

        return playerGameInfo;
    }

    public void RemovePlayerGameInfo<T>() where T : PlayerGameInfo
    {
        T playerGameInfo = gameObject.GetComponent<T>();
        if(playerGameInfo)
        {
            playerGameInfo.OnRemoved(this);
            Destroy(playerGameInfo);

            Debug.LogFormat("Removed {0} from player", playerGameInfo.GetType().Name);
        }
    }
}
