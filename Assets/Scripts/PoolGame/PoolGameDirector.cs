using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PoolGamePlayer
{
    public Player Player;
    public GameObject CueObject;
    public int Score;

    public PoolGamePlayer(Player player)
    {
        Player = player;
        CueObject = null;
        Score = 0;
    }

    public PoolGamePlayer(Player player, GameObject cueObj, int score)
    {
        Player = player;
        CueObject = cueObj;
        Score = score;
    }
}

[System.Serializable]
public struct PoolGameDefaults
{
    public Mesh DefaultCueMesh;
    public Material DefaultCueMaterial;
    public GameObject DefaultPoolTablePrefab;
    
    public PoolGameDefaults(Mesh cueMesh, Material cueMat, GameObject poolTableObj)
    {
        DefaultCueMesh = cueMesh;
        DefaultCueMaterial = cueMat;
        DefaultPoolTablePrefab = poolTableObj;
    }
}

/* Governs score keeping, and turn switching between players */ 
public class PoolGameDirector : NetworkBehaviour
{
    public delegate void OnPlayerTurnStarted(Player player, int playerIdx);
    public delegate void OnPlayerTurnEnded(Player player, int playerIdx);

    [Range(1, 4)]
    public int NumPlayers = 1;

    public PoolGameDefaults DefaultSettings;

    List<PoolGamePlayer> GamePlayers = new List<PoolGamePlayer>();

    private bool bHasStarted = false;
    private int CurrentPlayerIdx = 0;
    public static PoolGameDirector Instance { get; private set; }
    private GameObject PoolTableObj;
    
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

    // Start is called before the first frame update
    void Start()
    {
        PlayerMgr.Instance.OnPlayerJoined += OnPlayerJoined;
        PlayerMgr.Instance.OnPlayerLeft += OnPlayerLeft;
    }

    public override void OnNetworkSpawn()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(!bHasStarted)
        {
            if(CanStart())
            {
                StartGame();
            }
        }
    }

    void OnPlayerJoined(Player player)
    {
        // Both the client and servers should track the player members
        Debug.Log("PoolGameDirector - player joined");
        GamePlayers.Add(new PoolGamePlayer(player));

        Debug.LogFormat("IsLocalPlayer={0}, IsClient={1}, IsHost={2}, IsServer={3}", IsLocalPlayer, IsClient, IsHost, IsServer);

        // Client also includes if a hosting player
        if (IsClient)
        {
            // Give the player their own camera manager
            player.AddPlayerGameInfo<PoolPlayerCameraMgr>();
        }
    }
    
    void OnPlayerLeft(Player player)
    {
        Debug.Log("PoolGameDirector - player left");

        player.RemovePlayerGameInfo<PoolPlayerCameraMgr>();
    }

    bool CanStart()
    {
        return GamePlayers.Count >= NumPlayers;
    }

    void StartGame()
    {
        bHasStarted = true;

        SpawnPoolGame();

        CurrentPlayerIdx = 0;
        SetGamePlayerActive(GamePlayers[CurrentPlayerIdx], true);
    }

    public void RestartGame()
    {
        DespawnPoolGame();
        StartGame();
    }

    void EndGame()
    {

    }

    void SpawnPoolGame()
    {
        if(IsClient)
        {
            // Spawn client only stuff here...
        }

        if(!IsServer)
        {
            return;
        }

        if(DefaultSettings.DefaultPoolTablePrefab)
        {
            PoolTableObj = Instantiate(DefaultSettings.DefaultPoolTablePrefab);
            PoolTableObj.GetComponent<PoolTable>().OnPoolBallsStopped += OnPoolBallsStoppedHandler;
            PoolTableObj.GetComponent<PoolTable>().OnPoolBallScored += OnPoolBallScored;
            PoolTableObj.GetComponent<PoolTable>().GetCueBall().OnBallLaunched += OnCueBallLaunched;

            PoolTableObj.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Debug.LogError("No valid pool table prefab provided! Aborting.");
            return;
        }

        for(int i = 0; i < GamePlayers.Count; ++i)
        {
            if(GamePlayers[i].Player != null)
            {
                GameObject cueObj = new GameObject(GamePlayers[i].Player.PlayerName + "Cue");
                cueObj.AddComponent<NetworkTransform>();
                cueObj.AddComponent<PoolCue>();
                cueObj.AddComponent<MeshFilter>().mesh = DefaultSettings.DefaultCueMesh;
                cueObj.AddComponent<MeshRenderer>().material = DefaultSettings.DefaultCueMaterial;
                cueObj.AddComponent<NetworkObject>().Spawn();

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

        Destroy(PoolTableObj);

        foreach(PoolGamePlayer player in GamePlayers)
        {
            Debug.LogWarningFormat("Destroying cue {0}", player.CueObject.name);
            Destroy(player.CueObject);
            player.Score = 0;
        }
    }

    void SetGamePlayerActive(PoolGamePlayer gamePlayer, bool active)
    {
        if(IsServer)
        {
            GameObject cueObj = gamePlayer.CueObject;
            cueObj.SetActive(active);
            cueObj.GetComponent<PoolCue>().SetIsServing(active);
            cueObj.GetComponent<PoolCue>().OnAcquireCueBall(PoolTableObj.GetComponent<PoolTable>().GetCueBall());

            // Let the specific player their active state changed.
            ClientRpcParams rpcParams = new ClientRpcParams()
            {
                Send = new ClientRpcSendParams()
                { TargetClientIds = new ulong[] { gamePlayer.Player.NetId  } }

            };

            OnGamePlayerActivatedClientRpc(active, rpcParams);
        }

    }

    [ClientRpc]
    void OnGamePlayerActivatedClientRpc(bool active, ClientRpcParams rpcParams = default)
    {
        Debug.Log("SetGamePlayerActive ClientRPC");
    }

    void EndTurn()
    {
        if(!bHasStarted)
        {
            return;
        }

        if(GamePlayers.Count == 0)
        {
            return;
        }

        // Check for end of game condition...

        // Disable current player
        SetGamePlayerActive(GamePlayers[CurrentPlayerIdx], false);

        CurrentPlayerIdx++;
        if(CurrentPlayerIdx >= GamePlayers.Count)
        {
            CurrentPlayerIdx = 0;
        }

        // Activate next player
        SetGamePlayerActive(GamePlayers[CurrentPlayerIdx], true);

        Debug.Log("Turn End!");
    }

    // Client Side Methods
    public bool TryStartGame()
    {
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
    private void OnCueBallLaunched(PoolBall cueBall)
    {
        GetActivePlayer().GetPlayerGameInfo<PoolPlayerCameraMgr>().EnableTopDownCamera();
        GetActivePoolPlayer().CueObject.GetComponent<PoolCuePositioner>().SetOrbitObject(null);
        GetActivePoolPlayer().CueObject.GetComponent<PoolCue>().SetIsServing(false);
        PoolTableObj.GetComponent<PoolTable>().OnBallLaunched();
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
