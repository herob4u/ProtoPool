using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolTurnController : SimpleTurnController
{
    private int PlayerTurnCounter = 0;

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

        base.DoStartTurn();
    }

    protected override void DoAdvanceTurn()
    {
        if(!IsGameStarted())
        {
            return;
        }

        if(--PlayerTurnCounter <= 0)
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

        base.DoEndTurn();
    }
}
