using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
