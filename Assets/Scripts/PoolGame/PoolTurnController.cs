using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurnEvents
{
    public class TurnPlayerGone : TurnState.Event
    {
        public GamePlayer GamePlayer;
        public TurnPlayerGone(GamePlayer gamePlayer) { GamePlayer = gamePlayer; }
    }
}

namespace TurnStates
{
    // The entry state, this waits for the pool table to settle and then assigns the first turn player.
    public class Start : TurnState
    {
        protected override void DoEnter()
        {
        }

        protected override void DoExit()
        {
        }

        protected override void DoUpdate(float scaledDt)
        {
        }

        public override bool CanExit()
        {
            PoolGameDirector director = PoolGameDirector.Instance;
            if (!director.HasGameStarted)
            {
                // Cannot start the turn until the game has been spawned and started
                return false;
            }

            if (director.GetGameRules() && (OwningTurnController.NumTurnPlayers() < director.GetGameRules().MinPlayers))
            {
                // Cannot start the turn if the minimum players are not present
                return false;
            }

            PoolTable poolTable = director.GetPoolTable();
            if (poolTable && poolTable.AreBallsMoving)
            {
                // Cannot start turn until all balls are settled.
                return false;
            }

            return true;
        }
    }

    // Preconditions before entering the turn happen here.
    public class PrePlay : TurnState
    {
        protected override void DoEnter()
        {
        }

        protected override void DoExit()
        {
            OwningTurnController.SetTurnPlayer(OwningTurnController.GetNextTurnPlayer());
        }

        protected override void DoUpdate(float scaledDt)
        {
        }

        public override bool CanExit()
        {
            return true;
        }
    }

    public class Rack : TurnState
    {
        bool bIsPlacingRack = false;

        protected override void DoEnter()
        {
            //bIsPlacingRack = true;
            bIsPlacingRack = PoolGameDirector.Instance.StartPoolRackPlacement();
            Logger.LogScreen($"Started placing rack: {bIsPlacingRack}", Color.green, 5.0f);
        }

        protected override void DoExit()
        {
            Logger.LogScreen("Finished placing rack", Color.green, 5.0f);
        }

        protected override void DoUpdate(float scaledDt)
        {
            bIsPlacingRack = PoolGameDirector.Instance.IsPositioningRack;
        }

        public override bool CanExit()
        {
            return !bIsPlacingRack;
        }
    }

    // Play loop - player is aiming and trying to hit cue
    public class InPlay : TurnState
    {
        PoolGamePlayer TurnPlayer;
        bool bPlayerGone;
        bool bBallLaunched;

        public override bool CanExit()
        {
            return bPlayerGone || bBallLaunched;
        }

        public override void OnEvent(Event turnEvent)
        {
            if(turnEvent.GetType() == typeof(TurnEvents.TurnPlayerGone))
            {
                GamePlayer gonePlayer = (turnEvent as TurnEvents.TurnPlayerGone).GamePlayer;
                if(gonePlayer == TurnPlayer)
                {
                    // The player currently assigned the turn has left! End the turn.
                    bPlayerGone = true;
                }
            }
        }

        protected override void DoEnter()
        {
            bPlayerGone = false;
            bBallLaunched = false;

            // Listen to launch event as that would signify turn ended
            PoolTable table = PoolGameDirector.Instance.GetPoolTable();
            if(table)
            {
                table.OnPoolBallLaunched += OnPoolBallLaunched;
            }
            else
            {
                Debug.LogError("No valid PoolTable found. InPlay turn state cannot meet its exit condition!");
            }

            PoolGamePlayer newTurnPlayer = OwningTurnController.GetTurnPlayer() as PoolGamePlayer;

            PoolTurnController turnController = OwningTurnController as PoolTurnController;
            if(turnController)
            {
                // Avoid giving the same player back-to-back turns unless it's single player
                if (newTurnPlayer == TurnPlayer && OwningTurnController.NumTurnPlayers() > 1)
                {
                    newTurnPlayer = turnController.GetNextTurnPlayer(TurnPlayer) as PoolGamePlayer;
                }

                // Notify that the turn has started
                if(turnController.OnTurnStarted != null)
                {
                    turnController.OnTurnStarted.Invoke(newTurnPlayer);
                }
            }

            TurnPlayer = newTurnPlayer;
        }

        protected override void DoExit()
        {
            // Disconnect launch event - make no assumption on whether we will re-enter this state soon enough.
            PoolTable table = PoolGameDirector.Instance.GetPoolTable();
            if (table)
            {
                table.OnPoolBallLaunched -= OnPoolBallLaunched;
            }
            else
            {
                Debug.LogWarning("Failed to disconnect OnPoolBallLaunched delegate - no valid PoolTable found");
            }
        }

        protected override void DoUpdate(float scaledDt)
        {
        }

        void OnPoolBallLaunched(PoolBall ball, LaunchEventInfo launchEvent)
        {
            Debug.Assert(ball.IsCueBall());
            bBallLaunched = true;
        }
    }

    // Waiting for balls to come to rest
    public class WaitForRest : TurnState
    {
        protected override void DoEnter()
        {
            Logger.LogScreen("Waiting for balls to come to rest", Color.green, 5.0f);
        }

        protected override void DoExit()
        {
            // Now that the balls finished moving, freeze them completely
            PoolTable poolTable = PoolGameDirector.Instance.GetPoolTable();
            if (poolTable)
            {
                poolTable.SetBallsFrozen(true);
            }

            Logger.LogScreen("Balls came to rest", Color.green, 5.0f);
        }

        protected override void DoUpdate(float scaledDt)
        {
        }

        public override bool CanExit()
        {
            PoolTable poolTable = PoolGameDirector.Instance.GetPoolTable();
            if (poolTable)
            {
                return !poolTable.AreBallsMoving;
            }

            return true;
        }
    }

    // Pauses between turn ends are done here - facilitates UI displays
    public class EndTurn : TurnState
    {
        float fakeTimer = 0.0f;

        protected override void DoEnter()
        {
            Logger.LogScreen("PreTurnEnd - waiting for UI", Color.green, 5.0f);
            fakeTimer = 3.0f;
        }

        protected override void DoExit()
        {
            Logger.LogScreen("PreTurnEnd - finished", Color.green, 5.0f);
            OwningTurnController.SetTurnPlayer(OwningTurnController.GetNextTurnPlayer());
        }

        protected override void DoUpdate(float scaledDt)
        {
            fakeTimer -= scaledDt;
        }

        public override bool CanExit()
        {
            return fakeTimer <= 0.0f;
        }
    }
}

/* Server only object that arbitrates turns among players in a sequential fashion by joining order */
public class PoolTurnController : StatefulTurnController, IGamePlayerObserver
{
    public int CurrentPlayerIdx = -1;
    public List<PoolGamePlayer> PoolPlayers { get => PoolGameDirector.Instance.GetPoolPlayers(); }
    public System.Action<PoolGamePlayer> OnTurnStarted;

    public override void OnNetworkSpawn()
    {
        PoolGameDirector director = PoolGameDirector.Instance;
        if (director)
        {
            director.RegisterObserver(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        PoolGameDirector director = PoolGameDirector.Instance;
        if (director)
        {
            director.UnregisterObserver(this);
        }
    }

    // IGamePlayerObserver Interface
    public void OnGamePlayerAdded(GamePlayer player)
    {

    }

    public void OnGamePlayerRemoved(GamePlayer player)
    {
        // The turn belongs to the player leaving, we need to relinquish turn to the next player
        if(player == GetTurnPlayer())
        {
            StateMachine.GetCurrentState().OnEvent(new TurnEvents.TurnPlayerGone(player));
        }
    }


    // ITurnController Interface
    public override void SetTurnPlayer(GamePlayer turnPlayer)
    {
        if(StateMachine.GetCurrentState().GetType() == typeof(TurnStates.InPlay))
        {
            Debug.LogWarning("Cannot set turn player, turn is underway");
            return;
        }

        int playerIdx = PoolPlayers.IndexOf(turnPlayer as PoolGamePlayer);
        if (playerIdx < 0)
        {
            Debug.LogWarning("Cannot set turn player, player does not exist");
            return;
        }

        CurrentPlayerIdx = playerIdx;
    }

    public override GamePlayer GetTurnPlayer()
    {
        if (CurrentPlayerIdx >= 0)
        {
            return PoolPlayers[CurrentPlayerIdx];
        }

        Debug.LogWarning("Turn controller did not set first turn player - no active game players available");

        return null;
    }

    public override GamePlayer GetNextTurnPlayer()
    {
        int nextIdx = GetNextTurnPlayerIdx();
        return PoolPlayers[nextIdx];
    }

    public override int NumTurnPlayers()
    {
        return PoolPlayers.Count;
    }

    public GamePlayer GetNextTurnPlayer(GamePlayer relativeToPlayer)
    {
        int currIdx = PoolPlayers.IndexOf(relativeToPlayer as PoolGamePlayer);
        if(currIdx < 0)
        {
            Debug.LogWarning("Could not find next turn player relatively - provided player does not exist!");
            return null;
        }

        int nextIdx = GetNextTurnPlayerIdx(currIdx);
        return PoolPlayers[nextIdx];
    }

    protected int GetNextTurnPlayerIdx()
    {
        return GetNextTurnPlayerIdx(CurrentPlayerIdx);
    }
    
    protected int GetNextTurnPlayerIdx(int relativeToIdx)
    {
        int nextIdx = relativeToIdx + 1;
        if (nextIdx >= PoolPlayers.Count)
        {
            nextIdx = 0;
        }

        return nextIdx;
    }
}

#if false
public class PoolTurnController : SimpleTurnController
{
    private uint TurnNumber = 0;
    private int PlayerTurnCounter = 0;

    /* Resets the game such that it starts from the first turn again */
    public void ResetTurns()
    {
        if(IsServer)
        {
            TurnNumber = 0;
            PlayerTurnCounter = 0;

            // Force the change
            SetTurnState(ETurnState.None);
            ServerUpdate();
        }

    }

    protected bool IsGameStarted()
    {
        if(!PoolGameDirector.Instance || !PoolGameDirector.Instance.HasGameStarted)
        {
            return false;
        }

        return true;
    }

    protected override void ServerUpdate()
    {
        if(!IsGameStarted())
        {
            return;
        }

        base.ServerUpdate();
    }
    /*
    protected override void DoStartTurn()
    {
        if(!IsGameStarted())
        {
            return;
        }

        // This is a counter that gets decremented every time a turn is advanced. Once it reaches 0, the player will relinquish the turn.
        PlayerTurnCounter = PoolGameDirector.Instance.GetGameRules().TurnsPerPlayer;

        Debug.Log("DoStartTurn");
        base.DoStartTurn();
    }

    protected override void DoAdvanceTurn()
    {
        if(!IsGameStarted())
        {
            return;
        }

        ++TurnNumber;

        Debug.Log("DoAdvanceTurn");

        if (--PlayerTurnCounter <= 0)
        {
            base.DoAdvanceTurn();
        }
        else
        {
            // @todo: check if this works once we get to multiplayer testing.
            Debug.Log("Continuing turn!");
            HandlePlayerContinuedTurn(GetTurnPlayer());
        }
    }

    protected override void DoEndTurn()
    {
        if(!IsGameStarted())
        {
            return;
        }

        Debug.Log("DoEndTurn");
        base.DoEndTurn();
    }
    */
}
#endif