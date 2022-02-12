using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Samples;

public class PoolGamePlayer
{
    public Player Player;
    public GameObject CueObject;
    public int Score;
    public bool IsReady { get; private set; }

    public PoolGamePlayer(Player player)
    {
        Player = player;
        CueObject = null;
        Score = 0;
        IsReady = false;
    }

    public PoolGamePlayer(Player player, GameObject cueObj, int score)
    {
        Player = player;
        CueObject = cueObj;
        Score = score;
        IsReady = false;
    }

    public void SetAsReady()
    {
        IsReady = true;
    }
}

/* Governs score keeping, and turn switching between players 
/* The PoolGameDirector exists only on the server, and is the arbitrer of player turns.
*/
public class PoolGameDirector : NetworkBehaviour
{
    public delegate void OnPlayerTurnStarted(Player player, int playerIdx);
    public delegate void OnPlayerTurnEnded(Player player, int playerIdx);

    public bool OverrideNumPlayers = false;
    [Range(1, 4), EditCondition("OverrideNumPlayers")]
    public int NumPlayers = 1;

    [SerializeField]
    private PoolGameRules GameRules;

    // Lives on server only
    List<PoolGamePlayer> GamePlayers = new List<PoolGamePlayer>();

    private bool bHasStarted = false;
    private int CurrentPlayerIdx = -1;
    public static PoolGameDirector Instance { get; private set; }
    private GameObject PoolTableObj;
    
    public PoolGameRules GetGameRules()
    {
        if(!IsServer)
        {
            Debug.LogError("GameRules only exists on server!");
            return null;
        }

        return GameRules;
    }

    public PoolTable GetPoolTable()
    {
        if(PoolTableObj)
        {
            return PoolTableObj.GetComponent<PoolTable>();
        }

        Debug.LogWarning("Game director did not spawn table yet.");
        return null;
    }

    public Player GetActivePlayer()
    {
        if(bHasStarted)
        {
            return GamePlayers[CurrentPlayerIdx].Player;
        }

        Debug.LogWarning("Game director did not start game yet - no active players available");

        return null;
    }
    
    public PoolGamePlayer GetActivePoolPlayer()
    {
        if(bHasStarted)
        {
            return GamePlayers[CurrentPlayerIdx];
        }

        Debug.LogWarning("Game director did not start game yet - no active pool 2players available");

        return null;
    }

    public PoolGamePlayer GetLocalPoolPlayer()
    {
        if (bHasStarted)
        {
            return GamePlayers.Find(poolPlayer => poolPlayer.Player == PlayerMgr.Instance.GetLocalPlayer());
        }

        Debug.LogWarning("Game director did not start game yet - no active pool 2players available");

        return null;
    }

    void Awake()
    {
        if(Instance != null)
        {
            Debug.LogError("PoolGameDirector already initialized! Make sure only one exists in the scene.");
            return;
        }

        Instance = this;
    }

    public override void OnDestroy()
    {
        EndGame();

        PlayerMgr.Instance.OnPlayerJoined -= OnPlayerJoined;
        PlayerMgr.Instance.OnPlayerLeft -= OnPlayerLeft;

        Instance = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        PlayerMgr.Instance.OnPlayerJoined += OnPlayerJoined;
        PlayerMgr.Instance.OnPlayerLeft += OnPlayerLeft;

        if(IsServer)
        {
            if(!GameRules)
            {
                GameRules = new PoolGameRules();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
    }

    public override void OnNetworkDespawn()
    {
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyReadyServerRpc(ulong playerNetId)
    {
        foreach(PoolGamePlayer player in GamePlayers)
        {
            if(player.Player.NetId == playerNetId)
            {
                player.SetAsReady();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(IsServer)
        {
            ServerUpdate();
        }
        else
        {
            ClientUpdate();
        }
    }

    void ClientUpdate()
    {

    }

    void ServerUpdate()
    {
        if (!bHasStarted)
        {
            if (CanStart())
            {
                StartGame();
            }
        }
    }

    void OnPlayerJoined(Player player)
    {
        Debug.Log("PoolGameDirector - player joined");

        if(IsServer)
        {
            GamePlayers.Add(new PoolGamePlayer(player));
        }

        Debug.LogFormat("Player is {0}", player.IsLocal() ? "local" : "remote");
        Debug.LogFormat("IsClient={0}, IsHost={1}, IsServer={2}", IsClient, IsHost, IsServer);

        if (IsClient)
        {
            if (player.IsLocal())
            {
                // Give the player their own camera manager
                player.AddPlayerGameInfo<PoolPlayerCameraMgr>();

                if(IsSpawned)
                {
                    NotifyReadyServerRpc(player.NetId);
                }
                else
                {
                    Debug.LogWarning("Could not notify ready. NetObject not spawned yet");
                }
            }
        }
    }
    
    void OnPlayerLeft(Player player)
    {
        Debug.Log("PoolGameDirector - player left");

        if(IsServer)
        {
            GamePlayers.RemoveAll(p => { return p.Player == player; });
        }

        if(IsClient)
        {
            if (player.IsLocal())
            {
                player.RemovePlayerGameInfo<PoolPlayerCameraMgr>();
            }
        }
    }

    bool CanStart()
    {
        if(!IsServer)
        {
            return false;
        }

        foreach(PoolGamePlayer gamePlayer in GamePlayers)
        {
            if(!gamePlayer.IsReady)
            {
                return false;
            }
        }

        if(OverrideNumPlayers)
        {
            return GamePlayers.Count >= NumPlayers;
        }

        return (GamePlayers.Count >= GameRules.MinPlayers) && (GamePlayers.Count <= GameRules.MaxPlayers);
    }

    void StartGame()
    {
        if(!IsServer)
        {
            return;
        }

        bHasStarted = true;

        SpawnPoolGame();

        AdvanceTurn();
    }

    public void RestartGame()
    {
        if(!IsServer)
        {
            return;
        }

        DespawnPoolGame();
        StartGame();
    }

    void EndGame()
    {
        DespawnPoolGame();
    }

    void SpawnPoolGame()
    {
        if(!IsServer)
        {
            return;
        }

        PoolGameAssets DefaultSettings = GameRules.GameAssets;
        // Spawn the pool table and its contents first. This gets automatically replicated to clients
        if(DefaultSettings.PoolTablePrefab)
        {
            PoolTableObj = Instantiate(DefaultSettings.PoolTablePrefab);
            PoolTableObj.GetComponent<PoolTable>().OnPoolBallsStopped += OnPoolBallsStoppedHandler;
            PoolTableObj.GetComponent<PoolTable>().OnPoolBallScored += OnPoolBallScored;
            PoolTableObj.GetComponent<PoolTable>().OnPoolBallLaunched += OnPoolBallLaunched;

            PoolTableObj.name = "PoolTable";
            PoolTableObj.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Debug.LogError("No valid pool table prefab provided! Aborting.");
            return;
        }

        // Spawn the cues for each player, making sure that the cue is owned by the respective player.
        // This way, the lifetime of the object is tied to the respective player. The cue is the player's
        // representation in the game, and therefore its events are used to communicate to the director on the server.
        for(int i = 0; i < GamePlayers.Count; ++i)
        {
            Player player = GamePlayers[i].Player;

            if (player != null)
            {
                GameObject cueObj = Instantiate(DefaultSettings.PoolCuePrefab);
                cueObj.name = "Cue" + player.NetId;
                cueObj.GetComponent<NetworkObject>().SpawnWithOwnership(player.NetId);

                GamePlayers[i].CueObject = cueObj;
                GamePlayers[i].Score = 0;
            }
        }

    }

    void DespawnPoolGame()
    {
        if(IsClient)
        {
            // despawn client only stuff here
        }

        if(!IsServer)
        {
            return;
        }

        PoolTableObj.GetComponent<NetworkObject>().Despawn(true);

        foreach(PoolGamePlayer player in GamePlayers)
        {
            Debug.LogWarningFormat("Destroying cue {0}", player.CueObject.name);
            player.CueObject.GetComponent<NetworkObject>().Despawn();
            //Destroy(player.CueObject);
            player.Score = 0;
        }
    }

    void SetGamePlayerActive(PoolGamePlayer gamePlayer, bool active, bool exclusive = false)
    {
        if(IsServer)
        {
            if(gamePlayer == null || gamePlayer.Player == null)
            {
                Debug.LogWarning("SetGamePlayerActive: player is stale?");
                return;
            }

            // The server doesn't own the cue object, so instead, it tells all clients about its active state so that they reflect it locally.
            if(!exclusive)
            {
                SetGamePlayerActive(active, gamePlayer.Player.NetId);
            }
            else
            {
                SetGamePlayerActiveEx(gamePlayer.Player.NetId);
            }
        }

    }

    void SetGamePlayerActive(bool active, ulong playerNetId)
    {
        if(!IsServer)
        {
            return;
        }

        // Do local stuff
        foreach(PoolGamePlayer gamePlayer in GamePlayers)
        {
            if(gamePlayer.Player.NetId == playerNetId)
            {
                gamePlayer.CueObject.GetComponent<PoolCue>().SetCueActive(active);
            }
        }
    }

    /* Exclusive version that makes the desired player the only active one in the game */
    void SetGamePlayerActiveEx(ulong playerNetId)
    {
        if (!IsServer)
        {
            return;
        }

        // Do local stuff
        foreach (PoolGamePlayer gamePlayer in GamePlayers)
        {
            if (gamePlayer.Player.NetId != playerNetId)
            {
                gamePlayer.CueObject.GetComponent<PoolCue>().SetCueActive(false);
            }
            else
            {
                gamePlayer.CueObject.GetComponent<PoolCue>().SetCueActive(true);
            }
        }
    }

    void EndTurn()
    {
        if(!IsServer)
        {
            return;
        }

        if(!bHasStarted)
        {
            return;
        }

        if(GamePlayers.Count == 0)
        {
            return;
        }

        // Check for end of game condition...

        Debug.Log("Turn End!");

        AdvanceTurn();
    }

    public void AdvanceTurn()
    {
        if(!IsServer)
        {
            return;
        }

        // If the turn player changed, make sure to update the active states of the respective cues.
        int prevPlayerIdx = CurrentPlayerIdx;
        CurrentPlayerIdx = GetNextTurnPlayerIdx();

        PoolGamePlayer gamePlayer = GamePlayers[CurrentPlayerIdx];
        if (CurrentPlayerIdx != prevPlayerIdx)
        {
            SetGamePlayerActive(gamePlayer, true, true);
        }

        // Instruct the owning player to target the cue ball.
        PoolBall cueBall = PoolTableObj.GetComponent<PoolTable>().GetCueBall();

        if (cueBall)
        {
            gamePlayer.CueObject.GetComponent<PoolCue>().AcquireBall(cueBall);
        }
        else
        {
            gamePlayer.CueObject.GetComponent<PoolCue>().ResetAcquistion();

            // Throw an error here...

            Debug.LogError("AdvanceTurn failed - could not find cue ball.");
        }
    }

    // Overridable method for determining who gets to play next turn
    protected int GetNextTurnPlayerIdx()
    {
        int nextIdx = CurrentPlayerIdx + 1;
        if (nextIdx >= GamePlayers.Count)
        {
            nextIdx = 0;
        }

        return nextIdx;
    }

    public bool TryStartGame()
    {
        if(!IsServer)
        {
            return false;
        }

        if (CanStart())
        {
            StartGame(); // @todo: server RPC
            return true;
        }
        else
        {
            Debug.LogWarning("Failed to start game");
            return false;
        }
    }

    // ----- Event Handlers -----

    // When the player hits the cue ball, perform some camera cuts, and hide their cue
    private void OnPoolBallLaunched(PoolBall poolBall)
    {
        if(IsServer)
        {
            if(poolBall == null)
            {
                Debug.LogWarning("OnPoolBallLaunched: pool ball is invalid");
            }

            GetActivePoolPlayer().CueObject.GetComponent<PoolCue>().ResetAcquistion();

            OnPoolBallLaunchedClientRpc(poolBall.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    [ClientRpc]
    private void OnPoolBallLaunchedClientRpc(ulong poolBallNetId)
    {
        if(!IsClient)
        {
            return;
        }

        Debug.Log("OnPoolBallLaunchedClientRpc");

        Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();
        if(localPlayer)
        {
            PoolPlayerCameraMgr cameraMgr = localPlayer.GetPlayerGameInfo<PoolPlayerCameraMgr>();
            if(cameraMgr)
            {
                cameraMgr.EnableTopDownCamera();
            }
        }
    }

    // Once the turn has been resolved, end it and go to the next player. Effectively restarts the play loop
    private void OnPoolBallsStoppedHandler()
    {
        Debug.Log("Balls have stopped");
        EndTurn();
    }

    private void OnPoolBallScored(PoolBall ball, PoolGamePlayer byPlayer)
    {
        if(ball.IsCueBall())
        {
            PoolTableObj.GetComponent<PoolTable>().ResetCueBall();
        }
        else
        {
            if(byPlayer == null)
            {
                byPlayer = GetLocalPoolPlayer();
            }

            byPlayer.Score++;
        }
    }
}
