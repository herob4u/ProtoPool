using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurnStates
{
    // An empty placeholder state to keep the state machine valid at all times.
    public class EmptyState : TurnState
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
    }
}
/* A TurnController class that governs turn flow using a state machine of connected TurnStates */
public abstract class StatefulTurnController : NetworkBehaviourExt, ITurnController
{
    public TurnStateConfig StateConfig;
    protected class TurnStateEntry
    {
        public TurnState CurrentState;
        public TurnState NextState;
    }

    protected List<ITurnGameHandler> GameHandlers = new List<ITurnGameHandler>();
    protected TurnStateMachine StateMachine;

    // Should return whoever is the current player in the game
    public abstract GamePlayer GetTurnPlayer();
    public abstract GamePlayer GetNextTurnPlayer();
    public abstract void SetTurnPlayer(GamePlayer turnPlayer);

    public abstract int NumTurnPlayers();

    private void Awake()
    {
        StateMachine = new TurnStateMachine(this);
        StateMachine.DoInit(StateConfig);

        if(StateMachine.TurnStates.Count == 0)
        {
            StateMachine.AddTurnState<TurnStates.EmptyState>();
        }

        Debug.Assert(TurnStateMachine.ValidateTurnStates(StateMachine.TurnStates, true), "TurnStateMachine has invalid configuration");
        Debug.Log(StateMachine);
    }

    protected override void ServerUpdate()
    {
        base.ServerUpdate();
        StateMachine.DoUpdate(Time.deltaTime);
    }
}