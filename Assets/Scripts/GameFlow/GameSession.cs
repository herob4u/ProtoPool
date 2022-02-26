using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum ESessionRestriction
{
    Public,
    Private,
    FriendsOnly
};

public interface ISessionHandler
{
    public void OnPlayerJoined(Player player);
    public void OnPlayerLeft(Player player);
    public bool OnJoinRequest(out Result result);
}

// Represents the state of the current online session.
public class GameSession : MonoBehaviour
{
    [System.Serializable]
    public class Properties
    {
        public int PlayerCapacity;
        public int SpectatorCapacity;

        public ESessionRestriction JoinRestriction;        // Who can actually join this session
        public ESessionRestriction VisibilityRestriction;  // Who can discover this session
        public ESessionRestriction SpectateRestriction;
    };

    // Defined on the host/server only.
    private Properties ServerSessionProperties;
    public Properties SessionProperties 
    { get 
        { 
            if(NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
            {
                return ServerSessionProperties;
            }

            return null;
        } 
    }

    // Current number of players in the session
    public int NumPlayers { get; private set; }
    public int NumSpectators { get; private set; }

    private List<ulong> PendingPlayers = new List<ulong>();
    private ISessionHandler SessionHandler;

    private void Awake()
    {
        ServerSessionProperties = new Properties();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(!NetworkManager.Singleton)
        {
            Debug.LogError("GameSession cannot exist without a valid NetworkManager");
            return;
        }

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionRequest;

        PlayerMgr.Instance.OnPlayerJoined += OnPlayerCreated;
        PlayerMgr.Instance.OnPlayerLeft += OnPlayerDestroyed;
    }

    private void OnDestroy()
    {
        if(NetworkManager.Singleton)
        {
            ShutdownSession();

            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionRequest;
        }


        PlayerMgr.Instance.OnPlayerJoined -= OnPlayerCreated;
        PlayerMgr.Instance.OnPlayerLeft -= OnPlayerDestroyed;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = PendingPlayers.Count - 1; i >= 0; --i)
        {
            Player player = PlayerMgr.Instance.GetPlayer(PendingPlayers[i]);
            if (player && player.IsAcknowledged)
            {
                PendingPlayers.RemoveAt(i);
                PlayerJoinSession(player);
            }
        }
    }

    public void InitSession(Properties sessionProperties)
    {
        ServerSessionProperties = sessionProperties;
    }

    public void ShutdownSession()
    {
        NetworkManager networkMgr = NetworkManager.Singleton;
        if(!networkMgr)
        {
            Debug.LogError("NetworkManager cannot be invalid with a live session");
            return;
        }

        // Kick all players
        if(networkMgr.IsServer)
        {
            foreach(ulong clientId in networkMgr.ConnectedClientsIds)
            {
                networkMgr.DisconnectClient(clientId);
            }
        }

        networkMgr.Shutdown();

        // Reset values
        NumPlayers = 0;
        NumSpectators = 0;
        PendingPlayers.Clear();
        SessionHandler = null;
    }

    public void SetSessionHandler(ISessionHandler handler)
    {
        if(SessionHandler != handler)
        {
            SessionHandler = handler;
        }

        // Added a new session handler, notify them of existing players
        if(SessionHandler != null)
        {
            foreach(Player player in PlayerMgr.Instance.GetPlayers())
            {
                // Don't notify about pending players, they will be handled in the Update.
                if(!PendingPlayers.Contains(player.NetId) && player.IsAcknowledged)
                {
                    SessionHandler.OnPlayerJoined(player);
                }
            }
        }
    }

    public bool IsSessionHost()
    {
        if(!NetworkManager.Singleton)
        {
            return true;
        }

        return NetworkManager.Singleton.IsHost;
    }

    // An incoming player is trying to join. Returns true if the player is allowed to join this session.
    public bool OnJoinRequest(ulong clientId)
    {
        Result joinResult = Result.GetSuccess();
        if((NumPlayers + PendingPlayers.Count) >= ServerSessionProperties.PlayerCapacity)
        {
            joinResult = Result.GetFailure();
            return false;
        }

        // Check join restriction...
        // if( ... )

        // We can accept the join from a networking perspective... now ask the game..
        if(SessionHandler != null)
        {
            SessionHandler.OnJoinRequest(out joinResult);
        }

        if(!joinResult.IsSuccess())
        {
            // Propogate showing a message from here
            return false;
        }

        return true;
    }

    protected virtual void PlayerJoinSession(Player player)
    {
        Logger.LogScreen($"Player {player.NetId} joined session!");

        NumPlayers++;
        if(SessionHandler != null)
        {
            SessionHandler.OnPlayerJoined(player);
        }
    }

    protected virtual void PlayerLeaveSession(Player player)
    {
        NumPlayers--;
        if(SessionHandler != null)
        {
            SessionHandler.OnPlayerLeft(player);
        }
    }

    // --------------------PlayerMgr Events ---------------------------
    void OnPlayerCreated(Player player)
    {
        Logger.LogScreen($"Player created, netId = {player.NetId}", 5.0f);
    }

    void OnPlayerDestroyed(Player player)
    {
        Logger.LogScreen($"Player destroyed, netId = {player.NetId}", 5.0f);
    }
    // ---------------------------------------------------------------------


    // --------------------NetworkManager Events ---------------------------

    // This will get called on both host or server. If on host, will be called after OnClientConnected
    void OnServerStarted()
    {

    }

    // This will get called AFTER the player prefab has been created for the client.
    void OnClientConnected(ulong clientNetId)
    {
        Logger.LogScreen($"GameSession: Client {clientNetId} connected", Color.green, 5.0f);

        foreach (var pendingClient in NetworkManager.Singleton.PendingClients)
        {
            Logger.LogScreen($"GameSession: Pending client {pendingClient.Key}, state: {pendingClient.Value.ConnectionState.ToString()}");
        }

        // When a client connects, we put them in a pending list. We remove them from the list the moment
        // they send back a message letting us know they fully discovered us. This is because the host can
        // usually discover the client before the client even recognize they are in an online game.
        Player player = PlayerMgr.Instance.GetPlayer(clientNetId);
        if(player)
        {
            Logger.LogScreen("Client's net object is spawned");

            if(!player.IsAcknowledged)
            {
                Logger.LogScreen("Client is yet to acknowledge their player object, adding to pending.");
                PendingPlayers.Add(clientNetId);
            }
            else
            {
                PlayerJoinSession(player);
            }
        }
        else
        {
            PendingPlayers.Add(clientNetId);
            Logger.LogScreen("Client's net object is NOT spawned", Color.red);
        }

        if(clientNetId != NetworkManager.Singleton.LocalClientId)
        {
            if(GetComponent<GameSessionDebugTest>())
            {
                Logger.LogScreen("Starting game!", Color.green);
                GetComponent<GameSessionDebugTest>().SpawnNetObject();
            }
        }
    }

    void OnClientDisconnected(ulong clientNetId)
    {
    }

    private void OnConnectionRequest(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate approvedDelegate)
    {
        Debug.Log("OnConnectionRequest received");
        NetworkManager networkMgr = NetworkManager.Singleton;

        if(!networkMgr.IsServer)
        {
            Debug.LogException(new System.UnauthorizedAccessException("OnConnectionRequest unexpectendly called on a client!"));
            return;
        }

        Logger.LogScreenFormat("OnConnectioRequest from client {0}", clientId);

        // Check connection ticket...
        bool canJoin = OnJoinRequest(clientId);

        approvedDelegate(true, null, true, null, null);
    }

    // ---------------------------------------------------------------------

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 300, 200, 400));
        GUILayout.Label($"GameSession - IsSessionHost={IsSessionHost()}, NumPlayers={NumPlayers}");
        GUILayout.EndArea();
    }
}
