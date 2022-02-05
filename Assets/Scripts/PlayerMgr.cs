using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMgr : MonoBehaviour
{
    public delegate void OnPlayerJoinedDelegate(Player player);
    public delegate void OnPlayerLeftDelegate(Player player);

    public OnPlayerJoinedDelegate OnPlayerJoined;
    public OnPlayerLeftDelegate OnPlayerLeft;

    public List<Player> Players = new List<Player>();

    public static PlayerMgr Instance { get; private set; }
    private static int PlayerIdCounter = 0;

    public virtual void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Only one instance of PlayerMgr can exist!");
            return;
        }

        Debug.Log("Created PlayerMgr");
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public List<Player> GetPlayers()
    {
        return Players;
    }

    private void OnClientDisconnected(ulong localId)
    {
        Debug.LogFormat("Client disconnected: LocalID={0}", localId);
        Player player = Players.Find(p => p.LocalId == localId);
    }

    private void OnClientConnected(ulong localId)
    {
        Debug.LogFormat("Client connected: LocalID={0}", localId);
    }

    private void OnServerStarted()
    {
        Debug.LogFormat("Server started");
    }

    public void NotifyPlayerJoined(Player player)
    {
        if(Players.Contains(player))
        {
            Debug.LogWarningFormat("Player {0} already exists! Ensure clean up code is correct", player.LocalId);
            return;
        }

        Players.Add(player);

        if(OnPlayerJoined != null)
        {
            OnPlayerJoined.Invoke(player);
        }
    }

    public void NotifyPlayerLeft(Player player)
    {
        bool removed = Players.Remove(player);
        if(!removed)
        {
            Debug.LogWarningFormat("Failed to remove player {0}. Player was never added, ensure joining code is correct", player.LocalId);
        }

        if(OnPlayerLeft != null)
        {
            OnPlayerLeft.Invoke(player);
        }
    }

    public Player GetLocalPlayer()
    {
        if (Players.Count == 0)
        {
            Debug.LogWarning("No local player exists!");
            return null;
        }

        return Players[0];
    }

    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
