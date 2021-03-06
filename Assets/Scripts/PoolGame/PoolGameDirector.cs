using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PoolGamePlayer : GamePlayer
{
    public GameObject CueObject;
    public int Score;
    public List<EPoolBallType> TurnTally; // Balls scored during the current turn. Resets at end of turn. @todo should keeping tally per turn be the job of the turn controller?
    public bool[] BallTally;

    public void UpdateTally()
    {
        if (TurnTally != null)
        {
            foreach(EPoolBallType ballType in TurnTally)
            {
                BallTally[((int)ballType)] = true;
            }

            TurnTally.Clear();
        }
    }

    public PoolGamePlayer(Player player) : base(player)
    {
        CueObject = null;
        Score = 0;

        TurnTally = new List<EPoolBallType>();
        BallTally = new bool[Pool.NUM_BALLS];
    }

    public PoolGamePlayer(Player player, GameObject cueObj, int score) : base(player)
    {
        CueObject = cueObj;
        Score = score;

        TurnTally = new List<EPoolBallType>();
        BallTally = new bool[Pool.NUM_BALLS];
    }
}


/* Governs score keeping, and turn switching between players 
*/
public class PoolGameDirector : NetworkBehaviour, ISessionHandler, ITurnGameHandler
{
    public bool OverrideNumPlayers = false;
    [Range(1, 4), EditCondition("OverrideNumPlayers")]
    public int NumPlayers = 1;

    public float FastForwardTime = 5.0f;
    [Range(1.0f, 4.0f)]
    public float FastForwardSpeed = 2.0f;

    PoolTurnController TurnController;

    [SerializeField]
    private PoolGameRules GameRules;

    // Lives on server only
    List<PoolGamePlayer> GamePlayers = new List<PoolGamePlayer>();
    List<IGamePlayerObserver> GamePlayerObservers = new List<IGamePlayerObserver>();

    public bool HasGameStarted { get => bHasStarted; }
    public bool IsPositioningRack { get => bIsPositioningRack; }

    private bool bHasStarted = false;
    private bool bIsPositioningRack = false;
    private float FastForwardTimer;

    public static PoolGameDirector Instance { get; private set; }
    private GameObject PoolTableObj;
    private GameObject PoolRackObj;

    public void RegisterObserver(IGamePlayerObserver observer)
    {
        if(!GamePlayerObservers.Contains(observer))
        {
            GamePlayerObservers.Add(observer);
        }
    }

    public void UnregisterObserver(IGamePlayerObserver observer)
    {
        GamePlayerObservers.Remove(observer);
    }

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
            return TurnController.GetTurnPlayer().Player;
        }

        Debug.LogWarning("Game director did not start game yet - no active players available");

        return null;
    }

    public List<PoolGamePlayer> GetPoolPlayers()
    {
        return GamePlayers;
    }

    public PoolGamePlayer GetActivePoolPlayer()
    {
        if(bHasStarted)
        {
            return TurnController.GetTurnPlayer() as PoolGamePlayer;
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

        if(IsServer)
        {
            if(TurnController)
            {
                TurnController.OnTurnStarted -= OnPlayerAcquiredTurn;
            }
        }

        GameSession session = FindObjectOfType<GameSession>();
        if (session)
        {
            session.SetSessionHandler(null);
        }

        Instance = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialize components...
        TurnController = GetComponent<PoolTurnController>();

        if (IsServer)
        {
            if(!GameRules)
            {
                GameRules = new PoolGameRules();
            }

            if(TurnController)
            {
                TurnController.OnTurnStarted += OnPlayerAcquiredTurn;
            }
        }

        GameSession session = FindObjectOfType<GameSession>();
        if(session)
        {
            session.SetSessionHandler(this);
        }
    }

    public override void OnNetworkSpawn()
    {
    }

    public override void OnNetworkDespawn()
    {
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
        else
        {
            // Hande fast forward.
            if(FastForwardTimer > 0)
            {
                FastForwardTimer -= Time.deltaTime;
                if(FastForwardTimer <= 0)
                {
                    GameStatics.SetGameSpeed(FastForwardSpeed);
                    Debug.Log("Speeding up simulation");
                }
            }

        }
    }

    public void OnPlayerJoined(Player player)
    {
        Debug.Log("PoolGameDirector - player joined");

        if(IsServer)
        {
            PoolGamePlayer poolPlayer = new PoolGamePlayer(player);
            GamePlayers.Add(poolPlayer);

            if (bHasStarted)
            {
                DoSpawnPoolPlayer(poolPlayer, GetGameRules().GameAssets);
                SetGamePlayerActive(poolPlayer, false);
            }

            // Notify observers
            foreach(IGamePlayerObserver observer in GamePlayerObservers)
            {
                observer.OnGamePlayerAdded(poolPlayer);
            }
        }

        Debug.LogFormat("Player is {0}", player.IsLocal() ? "local" : "remote");
        Debug.LogFormat("IsClient={0}, IsHost={1}, IsServer={2}", IsClient, IsHost, IsServer);

        if (IsClient)
        {
            if (player.IsLocal())
            {
                // Give the player their own camera manager
                player.AddPlayerComponent<PoolPlayerCameraMgr>();
                player.AddPlayerComponent<PoolPlayerInput>();
            }
        }
    }
    
    public void OnPlayerLeft(Player player)
    {
        Debug.Log("PoolGameDirector - player left");

        if(IsServer)
        {
            foreach (PoolGamePlayer poolPlayer in GamePlayers)
            {
                if (poolPlayer.Player == player)
                {
                    // Notify observers
                    foreach (IGamePlayerObserver observer in GamePlayerObservers)
                    {
                        observer.OnGamePlayerRemoved(poolPlayer);
                    }

                    if(bHasStarted)
                    {
                        DoDespawnPoolPlayer(poolPlayer);
                    }
                }
            }

            // @todo: refactor to avoid double iteration
            GamePlayers.RemoveAll(p => { return p.Player == player; });
        }

        if(IsClient)
        {
            if (player.IsLocal())
            {
                player.RemovePlayerComponent<PoolPlayerInput>();
                player.RemovePlayerComponent<PoolPlayerCameraMgr>();
            }
        }
    }

    public bool OnJoinRequest(out Result result)
    {
        result = Result.GetSuccess();
        return true;
    }

    bool CanStart()
    {
        if(!IsServer)
        {
            return false;
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

        if(TurnController)
        {
            //TurnController.ResetTurns();
            //if(TurnController.GetTurnPlayer() == null)
            //{
            //    // Default to first player who joined
            //    TurnController.SetTurnPlayer(GamePlayers[0]);
            //}
        }

        bHasStarted = true;
        bIsPositioningRack = false;

        SpawnPoolGame();

        PoolRackObj.SetActive(false);
        // Start positoning the rack
        //if(!EnablePoolRack())
        {
            // Start the first turn of the game if positioning is not viable.
            //if (TurnController)
            //{
            //    TurnController.SetTurnState(SimpleTurnController.ETurnState.Advancing);
            //}
        }
    }

    public bool StartPoolRackPlacement()
    {
        return EnablePoolRack();
    }

    bool EnablePoolRack()
    {
        if(!IsServer)
        {
            return false;
        }

        if(!PoolRackObj)
        {
            bIsPositioningRack = false;
            return false;
        }

        if (TurnController)
        {
            PoolRackObj.GetComponent<PoolRack>().SetControllingPlayer(TurnController.GetTurnPlayer() as PoolGamePlayer);
        }

        PoolRackObj.SetActive(true);
        PoolRackObj.GetComponent<PoolRack>().SetupRack(PoolTableObj.GetComponent<PoolTable>());

        bIsPositioningRack = true;

        return true;
    }

    public void DisablePoolRack()
    {
        if(!IsServer)
        {
            return;
        }

        // Update the state of the game object, disable it and replicate to clients.
        if(PoolRackObj)
        {
            PoolRackObj.SetActive(false);
            PoolTableObj.GetComponent<PoolTable>().SetBallsFrozen(true);
        }

        bIsPositioningRack = false;
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

        if(DefaultSettings.PoolRackPrefab)
        {
            PoolRackObj = Instantiate(DefaultSettings.PoolRackPrefab);
            PoolRackObj.GetComponent<PoolRack>().OnRackPlacementStarted += OnRackPlacementStarted;
            PoolRackObj.GetComponent<PoolRack>().OnRackPlacementFinished += OnRackPlacementFinished;
            PoolRackObj.GetComponent<NetworkObject>().Spawn();

        }

        // Spawn the cues for each player, making sure that the cue is owned by the respective player.
        // This way, the lifetime of the object is tied to the respective player. The cue is the player's
        // representation in the game, and therefore its events are used to communicate to the director on the server.
        for (int i = 0; i < GamePlayers.Count; ++i)
        {
            DoSpawnPoolPlayer(GamePlayers[i], DefaultSettings);
        }
    }

    void DoSpawnPoolPlayer(PoolGamePlayer poolPlayer, PoolGameAssets defaultSettings)
    {
        Player player = poolPlayer.Player;

        if (player != null)
        {
            GameObject cueObj = Instantiate(defaultSettings.PoolCuePrefab);
            cueObj.name = "Cue" + player.NetId;
            cueObj.GetComponent<NetworkObject>().SpawnWithOwnership(player.NetId);
            cueObj.GetComponent<PoolCue>().AssignPoolPlayer(poolPlayer);
            cueObj.GetComponent<PoolCue>().SetCueActive(false);
            poolPlayer.CueObject = cueObj;
            poolPlayer.Score = 0;
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

        // Pool Table
        {
            PoolTableObj.GetComponent<PoolTable>().OnPoolBallsStopped -= OnPoolBallsStoppedHandler;
            PoolTableObj.GetComponent<PoolTable>().OnPoolBallScored -= OnPoolBallScored;
            PoolTableObj.GetComponent<PoolTable>().OnPoolBallLaunched -= OnPoolBallLaunched;
            PoolTableObj.GetComponent<NetworkObject>().Despawn(true);
        }

        // Pool Rack
        {
            PoolRackObj.GetComponent<PoolRack>().OnRackPlacementStarted -= OnRackPlacementStarted;
            PoolRackObj.GetComponent<PoolRack>().OnRackPlacementFinished -= OnRackPlacementFinished;
            PoolRackObj.GetComponent<NetworkObject>().Despawn(true);
        }

        foreach (PoolGamePlayer player in GamePlayers)
        {
            DoDespawnPoolPlayer(player);
        }
    }

    void DoDespawnPoolPlayer(PoolGamePlayer poolPlayer)
    {
        Debug.LogWarningFormat("Destroying cue {0}", poolPlayer.CueObject.name);
        poolPlayer.CueObject.GetComponent<NetworkObject>().Despawn();
        poolPlayer.Score = 0;
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

            // @todo: this may be undesirable, it might be our only way of fixing an out of sync client
            /*
            if(gamePlayer.CueObject.activeSelf == active)
            {
                Debug.LogFormat("SetGamePlayerActive not completed - no state change required");
                return;
            }*/

            Debug.LogFormat("Setting GamePlayer Active: player={0}, active={1}, exclusive={2}", gamePlayer.Player.NetId, active.ToString(), exclusive.ToString());
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

    public void CheckWinCondition()
    {
        if(IsServer)
        {
        }
    }

    public void CheckFoulCondition()
    {

    }

    public void CheckLoseCondition()
    {

    }

    public void OnPlayerFouled(PoolGamePlayer poolPlayer)
    {

    }

    public void OnPlayerWon(PoolGamePlayer poolPlayer)
    {

    }

    // ----- Event Handlers -----

    // When the player hits the cue ball, perform some camera cuts, and hide their cue
    private void OnPoolBallLaunched(PoolBall poolBall, LaunchEventInfo launchEvent)
    {
        if(IsServer)
        {
            if(poolBall == null)
            {
                Debug.LogWarning("OnPoolBallLaunched: pool ball is invalid");
            }

            Logger.LogScreen($"{poolBall.name} was launched by {launchEvent.InstigatorObject.name}");

            GetActivePoolPlayer().CueObject.GetComponent<PoolCue>().ResetAcquistion();

            FastForwardTimer = FastForwardTime;

            // Unfreeze to allow the simulation to play out
            PoolTableObj.GetComponent<PoolTable>().SetBallsFrozen(false);

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
            PoolPlayerCameraMgr cameraMgr = localPlayer.GetPlayerComponent<PoolPlayerCameraMgr>();
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

        FastForwardTimer = 0.0f;
        GameStatics.SetGameSpeed(1.0f);

        // Freeze everything so we don't accidentally alter them
        PoolTableObj.GetComponent<PoolTable>().SetBallsFrozen(true);

        //if(!bIsPositioningRack)
        //{
        //    if (TurnController)
        //    {
        //        TurnController.SetTurnState(SimpleTurnController.ETurnState.Ending);
        //    }
        //}

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

    private void OnPlayerRelinquishedTurn(GamePlayer player)
    {

    }

    private void OnPlayerAcquiredTurn(GamePlayer player)
    {
        if(player == null)
        {
            Debug.LogWarning("Received OnPlayerAcquiredTurn, but player is invalid");
            return;
        }

        PoolGamePlayer poolPlayer = player as PoolGamePlayer;
        if(poolPlayer == null)
        {
            Debug.LogWarningFormat("Received OnPlayerAcquiredTurn, but player {0} is not a PoolGamePlayer", poolPlayer.Player.NetId);
            return;
        }

        Debug.Log("OnPlayerAcquiredTurn Turn");
        if (bIsPositioningRack)
        {
            Debug.LogWarning("Rack being positioned, ignoring turn acquistion");
            return;
        }

        // Determine what are we doing this turn - are we cue-ing? placing rack?
        SetGamePlayerActive(poolPlayer, true, true);

        // Instruct the owning player to target the cue ball.
        PoolBall cueBall = PoolTableObj.GetComponent<PoolTable>().GetCueBall();

        if (cueBall)
        {
            poolPlayer.CueObject.GetComponent<PoolCue>().AcquireBall(cueBall);
        }
        else
        {
            poolPlayer.CueObject.GetComponent<PoolCue>().ResetAcquistion();

            // Throw an error here...

            Debug.LogError("OnPlayerAcquiredTurn failed - could not find cue ball.");
        }
    }

    private void OnPlayerContinuedTurn(GamePlayer player)
    {
        if (player == null)
        {
            Debug.LogWarning("Received OnPlayerContinuedTurn, but player is invalid");
            return;
        }

        PoolGamePlayer poolPlayer = player as PoolGamePlayer;
        if (poolPlayer == null)
        {
            Debug.LogWarningFormat("Received OnPlayerContinuedTurn, but player {0} is not a PoolGamePlayer", poolPlayer.Player.NetId);
            return;
        }

        Debug.Log("OnPlayerContinued Turn");
        if (bIsPositioningRack)
        {
            Debug.LogWarning("Rack being positioned, ignoring turn continuation");
            return;
        }

        SetGamePlayerActive(poolPlayer, true, true);

        // Instruct the owning player to target the cue ball.
        // Just like the turn acquistion case, we want to reposition outselves w.r.t the cue ball
        PoolBall cueBall = PoolTableObj.GetComponent<PoolTable>().GetCueBall();

        if (cueBall)
        {
            poolPlayer.CueObject.GetComponent<PoolCue>().AcquireBall(cueBall);
        }
        else
        {
            poolPlayer.CueObject.GetComponent<PoolCue>().ResetAcquistion();

            // Throw an error here...

            Debug.LogError("OnPlayerContinuedTurn failed - could not find cue ball.");
        }
    }

    private void OnRackPlacementStarted()
    {
        if(IsServer)
        {
            Debug.Log("Server: Rack placement started");
            ulong controllingPlayer = PoolRackObj.GetComponent<PoolRack>().GetControllingPlayerNetId();

            foreach(GamePlayer poolPlayer in GamePlayers)
            {
                SetGamePlayerActive(false, poolPlayer.Player.NetId);
            }

            // Notify clients
            OnRackPlacementStartedClientRpc(controllingPlayer);
        }
    }

    [ClientRpc]
    private void OnRackPlacementStartedClientRpc(ulong controllingPlayer)
    {
        Debug.Log("Client: Rack placement started");

        Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();

        // I am the one given control, enable my inputs for the rack
        if(localPlayer.NetId == controllingPlayer)
        {
            // @ WIP: remote player PoolRackObj is still null!!! Need to address it.
            localPlayer.GetPlayerComponent<PoolPlayerInput>().SetInputTarget(PoolRackObj);
        }
    }

    private void OnRackPlacementFinished()
    {
        if(IsServer)
        {
            Debug.Log("Server: Rack placement finished");
            DisablePoolRack();

            //if (TurnController)
            //{
            //    TurnController.SetTurnState(SimpleTurnController.ETurnState.Advancing); // Actually starts the game
            //}

            // Notify players
            OnRackPlacementFinishedClientRpc();
        }
    }

    [ClientRpc]
    private void OnRackPlacementFinishedClientRpc()
    {
        Debug.Log("Client: Rack placement finished");

        Player localPlayer = PlayerMgr.Instance.GetLocalPlayer();
        // @ WIP: remote player PoolRackObj is still null!!! Need to address it.
        localPlayer.GetPlayerComponent<PoolPlayerInput>().SetInputTarget(null);
    }
}
