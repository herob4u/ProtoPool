using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public interface ITurnGameHandler
{
    //public void OnTurnChanged(ETurnTransitionType transitionType, GamePlayer turnPlayer);
    //public bool CanTurnTransition(ETurnTransitionType transitionType);
}

public interface ITurnController
{
    public GamePlayer GetTurnPlayer();
    public GamePlayer GetNextTurnPlayer();
    public int NumTurnPlayers();
    public void SetTurnPlayer(GamePlayer turnPlayer);
}

public class SimpleTurnController : NetworkBehaviourExt, ITurnController
{
    public enum ETurnState
    {
        None = 0, Starting, Ending, Advancing
    }

    // Whether the advancement of the first turn should occur automatically when the turn controller is instantiated.
    // If false, the user needs to advance the turn at the beginning of the game to set the first turn player.
    [InspectorName("Advance Turn on Start")]
    public bool bAdvanceTurnOnStart;

    public delegate void TurnPlayerDelegate(GamePlayer player);
    public TurnPlayerDelegate OnPlayerRelinquishedTurn { get; set; }
    public TurnPlayerDelegate OnPlayerAcquiredTurn { get; set; }
    public TurnPlayerDelegate OnPlayerContinuedTurn { get; set; }

    // List of players to arbitrate turns
    protected List<GamePlayer> GamePlayers = new List<GamePlayer>();
    int CurrentPlayerIdx = -1;

    // We would like clients to at least query what the current turn state is, that way they can manage local displays and effects.
    protected NetworkVariable<ETurnState> TurnState = new NetworkVariable<ETurnState>(ETurnState.None);

    void Start()
    {
        if(bAdvanceTurnOnStart)
        {
            SetTurnState(ETurnState.Advancing);
        }
    }

    public bool AddPlayer(GamePlayer player)
    {
        if(player == null)
        {
            Debug.LogWarning("Attempting to add invalid player to TurnController.");
            return false;
        }

        if(GamePlayers.Contains(player))
        {
            Debug.LogWarningFormat("Attempting to add the same player '{0}' to TurnController - ensure caller is managing players correctly", player.Player.NetId);
            return false;
        }

        GamePlayers.Add(player);
        return true;
    }

    public bool RemovePlayer(GamePlayer player)
    {
        if(player == null)
        {
            Debug.LogWarning("Attempting to remove invalid player to TurnController.");
            return false;
        }

        if(!GamePlayers.Contains(player))
        {
            Debug.LogWarningFormat("Attempting to remove non-existent player '{0}' from TurnController - ensure caller is managing players correctly", player.Player.NetId);
            return false;
        }

        GamePlayers.Remove(player);

        // If we have remaining players, refresh the turns so a valid player is handling the turn.
        if(GamePlayers.Count > 0)
        {
            RefreshTurnPlayer();
        }

        return true;
    }

    public void SetTurnState(ETurnState newState)
    {
        if (!IsServer)
        {
            return;
        }

        if (TurnState.Value != newState)
        {
            if (!(((int)newState) > ((int)TurnState.Value)
                || (newState == ETurnState.Starting && TurnState.Value == ETurnState.Advancing)
                || (newState == ETurnState.None && TurnState.Value == ETurnState.Starting)))
            {
                Debug.LogWarningFormat("Unexpected turn state transition, from {0} to {1}", TurnState.Value.ToString(), newState.ToString());
            }

            TurnState.Value = newState;
        }
    }

    protected override void ServerUpdate()
    {
        /*
        if (TurnState.Value != ETurnState.None)
        {
            // Handle turn states
            bool canTransition = true;

            foreach (ITurnGameHandler handler in GameHandlers)
            {
                switch (TurnState.Value)
                {
                    case ETurnState.Starting: canTransition &= handler.CanStartTurn(); break;
                    case ETurnState.Advancing: canTransition &= handler.CanAdvanceTurn(); break;
                    case ETurnState.Ending: canTransition &= handler.CanEndTurn(); break;
                }

                if (!canTransition)
                {
                    break;
                }
            }

            if (canTransition)
            {
                switch (TurnState.Value)
                {
                    case ETurnState.Starting:
                    {
                        StartTurn();
                    }
                    break;

                    case ETurnState.Advancing:
                    {
                        AdvanceTurn();
                    }
                    break;

                    case ETurnState.Ending:
                    {
                        EndTurn();
                    }
                    break;
                }
            }
        }
        */
    }

    // Called whenever the list of players is altered. Ensures a valid turn player is selected.
    public void RefreshTurnPlayer()
    {
        GamePlayer currPlayer = GetTurnPlayer();
        if(currPlayer == null)
        {
            GamePlayer nextPlayer = GetNextTurnPlayer();
            if(nextPlayer != null)
            {
                SetTurnPlayer(nextPlayer);
            }

            // Failed to set turn player, so just force ending the turn
            if(GetTurnPlayer() != nextPlayer)
            {
                Debug.LogWarning("Ending turn because no valid player is selected for TurnController");
                //EndTurn();
            }
        }

        Debug.LogWarning("No players left for TurnController");
    }

    public void SetTurnPlayer(GamePlayer turnPlayer)
    {
        if (TurnState.Value != ETurnState.None)
        {
            Debug.LogWarning("Cannot set turn player, turn is underway");
            return;
        }

        int playerIdx = GamePlayers.IndexOf(turnPlayer);
        if(playerIdx < 0)
        {
            Debug.LogWarning("Cannot set turn player, player does not exist");
            return;
        }

        CurrentPlayerIdx = playerIdx;
    }

    public GamePlayer GetTurnPlayer()
    {
        if (CurrentPlayerIdx >= 0)
        {
            return GamePlayers[CurrentPlayerIdx];
        }

        Debug.LogWarning("Turn controller did not set first turn player - no active game players available");

        return null;
    }

    public GamePlayer GetNextTurnPlayer()
    {
        int nextIdx = GetNextTurnPlayerIdx();
        return GamePlayers[nextIdx];
    }

    public int NumTurnPlayers()
    {
        return GamePlayers.Count;
    }

    protected int GetNextTurnPlayerIdx()
    {
        int nextIdx = CurrentPlayerIdx + 1;
        if (nextIdx >= GamePlayers.Count)
        {
            nextIdx = 0;
        }

        return nextIdx;
    }
    /*
    protected override void DoStartTurn()
    {
        if (!IsServer)
        {
            return;
        }

        // Stop state changes, lets gameplay continue.
        SetTurnState(ETurnState.None);
    }

    protected override void DoAdvanceTurn()
    {
        if (!IsServer)
        {
            return;
        }

        // If the turn player changed, make sure to update the active states of the respective cues.
        GamePlayer prevPlayer = GetTurnPlayer();
        GamePlayer nextPlayer = GetNextTurnPlayer();

        if(prevPlayer != nextPlayer)
        {
            HandlePlayerRelinquishedTurn(prevPlayer);
            HandlePlayerAcquiredTurn(nextPlayer);

        }
        else
        {
            HandlePlayerContinuedTurn(nextPlayer);
        }

        CurrentPlayerIdx = GetNextTurnPlayerIdx();

        SetTurnState(ETurnState.Starting);
    }

    protected override void DoEndTurn()
    {
        if (!IsServer)
        {
            return;
        }

        if (GamePlayers.Count == 0)
        {
            return;
        }

        Debug.Log("Turn End!");

        SetTurnState(ETurnState.Advancing);
    }
    */
    protected virtual void HandlePlayerRelinquishedTurn(GamePlayer prevPlayer)
    {
        if (OnPlayerRelinquishedTurn != null)
        {
            OnPlayerRelinquishedTurn.Invoke(prevPlayer);
        }
    }
    protected virtual void HandlePlayerAcquiredTurn(GamePlayer newPlayer)
    {
        if (OnPlayerAcquiredTurn != null)
        {
            OnPlayerAcquiredTurn.Invoke(newPlayer);
        }
    }
    protected virtual void HandlePlayerContinuedTurn(GamePlayer currPlayer)
    {
        if (OnPlayerContinuedTurn != null)
        {
            OnPlayerContinuedTurn.Invoke(currPlayer);
        }
    }
}