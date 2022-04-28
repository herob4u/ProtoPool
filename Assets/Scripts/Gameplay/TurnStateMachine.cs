using System.Collections.Generic;
using UnityEngine;

public abstract class TurnState
{
    public class Event
    {
    }

    public ITurnController OwningTurnController { get; protected set; }

    public TurnState()
    {

    }

    public void SetTurnController(ITurnController owner)
    {
        OwningTurnController = owner;
    }

    public void Update(float scaledTime)
    {
        DoUpdate(scaledTime);
    }

    public virtual void OnEvent(Event turnEvent) { }

    public void Enter() { DoEnter(); }
    public void Exit() { DoExit(); }
    public virtual bool CanExit() { return true; }

    protected abstract void DoEnter();
    protected abstract void DoUpdate(float scaledDt);
    protected abstract void DoExit();
}

[System.Serializable]
public class TurnStateConfig
{
    [System.Serializable]
    public struct Entry
    {
        [SerializeField, TypeReferences.ClassExtends(typeof(TurnState))] public TypeReferences.ClassTypeReference FromState;
        [SerializeField, TypeReferences.ClassExtends(typeof(TurnState))] public TypeReferences.ClassTypeReference ToState;
    }

    public List<Entry> TurnStateConfigEntries = new List<Entry>();
}

public class TurnStateMachine
{
    public ITurnController OwningTurnController { get; }

    public class TurnStateEntry
    {
        public TurnState CurrentState;
        public TurnState NextState;

        public TurnStateEntry()
        {

        }

        public TurnStateEntry(TurnState fromState, TurnState toState = null)
        {
            CurrentState = fromState;
            NextState = toState;
        }
    }

    public List<TurnStateEntry> TurnStates { get => TurnStateEntries; }
    protected List<TurnStateEntry> TurnStateEntries = new List<TurnStateEntry>();
    protected int CurrentStateId = -1;

    public TurnStateMachine(ITurnController turnController)
    {
        OwningTurnController = turnController;
    }

    public void DoInit(TurnStateConfig config)
    {
        foreach (TurnStateConfig.Entry entry in config.TurnStateConfigEntries)
        {
            if (entry.ToState.Type != null)
            {
                AddTurnState(entry.FromState.Type, entry.ToState.Type);
            }
        }
    }

    public void DoUpdate(float scaledDt)
    {
        if (CurrentStateId == -1)
        {
            if (TurnStateEntries.Count == 0)
            {
                Debug.LogWarning("TurnStates not set up for TurnController, no state transitions will occur.");
                return;
            }

            // Go to the first state
            TransitionTurnState(0);
            return;
        }

        // Check if we can transition
        TurnStateEntry currStateEntry = GetCurrentStateEntry();
        if (currStateEntry != null)
        {
            TurnState currState = currStateEntry.CurrentState;
            currState.Update(scaledDt);

            if (currState.CanExit())
            {
                // Can perform the next transition...
                if (currStateEntry.NextState != null)
                {
                    TransitionTurnState(TurnStateEntries.FindIndex(entry => entry.CurrentState == currStateEntry.NextState));
                }
            }
        }
    }

    void TransitionTurnState(int nextStateId)
    {
        Debug.Log($"Transition from stateId {CurrentStateId} to {nextStateId}");

        if (nextStateId >= 0 && nextStateId < TurnStateEntries.Count)
        {
            TurnState currState = GetCurrentState();
            if (currState != null)
            {
                currState.Exit();
            }

            CurrentStateId = nextStateId;

            TurnState nextState = GetCurrentState();
            Debug.Assert(nextState != null, "A turn state entry is not expected to have an invalid current state");

            nextState.Enter();
            Debug.Log($"Turn state transitioned: {currState} -> {nextState}");
        }
        else
        {
            Debug.LogWarning($"Failed to perform turn state transition - nextStateId {nextStateId} has no valid entry");
        }
    }

    public TurnState GetCurrentState()
    {
        if (CurrentStateId >= 0 && CurrentStateId < TurnStateEntries.Count)
        {
            return TurnStateEntries[CurrentStateId].CurrentState;
        }

        return null;
    }

    protected TurnStateEntry GetCurrentStateEntry()
    {
        if (CurrentStateId >= 0 && CurrentStateId < TurnStateEntries.Count)
        {
            return TurnStateEntries[CurrentStateId];
        }

        return null;
    }

    public T AddTurnState<T>() where T : TurnState, new()
    {
        System.Tuple<TurnState, TurnState> result = AddTurnState(typeof(T), typeof(T));

        if (result != null)
        {
            return result.Item1 as T;
        }
        else
        {
            return null;
        }
    }

    public System.Tuple<T1, T2> AddTurnState<T1, T2>() where T1 : TurnState, new() where T2 : TurnState, new()
    {
        System.Type fromType = typeof(T1);
        System.Type toType = typeof(T2);

        System.Tuple<TurnState, TurnState> result = AddTurnState(fromType, toType);

        if (result != null)
        {
            return new System.Tuple<T1, T2>(result.Item1 as T1, result.Item2 as T2);
        }
        else
        {
            return null;
        }
    }

    protected System.Tuple<TurnState, TurnState> AddTurnState(System.Type fromStateType, System.Type toStateType)
    {
        if (fromStateType == null || toStateType == null)
        {
            return null;
        }

        TurnStateEntry currEntry = TurnStateEntries.Find(entry => entry.CurrentState.GetType() == fromStateType);
        TurnStateEntry nextEntry = TurnStateEntries.Find(entry => entry.CurrentState.GetType() == toStateType);

        TurnState fromState = (currEntry != null) ? currEntry.CurrentState : System.Activator.CreateInstance(fromStateType) as TurnState;
        TurnState toState = (nextEntry != null) ? nextEntry.CurrentState : System.Activator.CreateInstance(toStateType) as TurnState;

        // Before proceeding, ensure the "next" entry exists.
        if (nextEntry == null)
        {
            // A state that has no "next"
            nextEntry = new TurnStateEntry(toState);
            TurnStateEntries.Add(nextEntry);
        }

        // Current entry does not exist, create it and link it to the next entry
        // If the state has no Next, it will point to itself, and thus will already be handled by previous condition
        if (fromStateType != toStateType)
        {
            // Entry does not exist, create it using the from-to states
            if (currEntry == null)
            {
                currEntry = new TurnStateEntry(fromState, toState);
                TurnStateEntries.Insert(Mathf.Max(0, TurnStateEntries.Count - 1), currEntry); // Insert it such that the next entry comes after.
            }
            else
            {
                // Entry exists, only need to update NextState
                currEntry.NextState = toState;
            }
        }


        fromState.SetTurnController(OwningTurnController);
        if (toState != fromState)
        {
            toState.SetTurnController(OwningTurnController);
        }

        return new System.Tuple<TurnState, TurnState>(fromState, toState);
    }

    public static bool ValidateTurnStates(List<TurnStateEntry> entries, bool cyclicCheck = true)
    {
        Dictionary<TurnState, int> visitSet = new Dictionary<TurnState, int>();
        bool isCycled = false;

        foreach (TurnStateEntry entry in entries)
        {
            if (entry.CurrentState == null)
            {
                Debug.LogWarning("ValidateTurnStates: Entry does not have a valid current state");
                return false;
            }

            if (!visitSet.ContainsKey(entry.CurrentState))
            {
                visitSet.Add(entry.CurrentState, 0);
            }

            if (entry.NextState != null)
            {
                TurnStateEntry nextEntry = entries.Find(entry => entry.CurrentState != null && entry.CurrentState.GetType() == entry.NextState.GetType());
                if (nextEntry == null)
                {
                    Debug.LogWarning("ValidateTurnStates: Entry's NextState does not exist in the list of turn states");
                    return false;
                }
                else
                {
                    // This next state is being visited by us - increment its visit count
                    if (!visitSet.ContainsKey(nextEntry.CurrentState))
                    {
                        visitSet.Add(nextEntry.CurrentState, 1);
                    }
                    else
                    {
                        visitSet[nextEntry.CurrentState]++;
                    }

                    isCycled = (visitSet[nextEntry.CurrentState] > 1);
                }
            }
        }

        if (visitSet.Count != entries.Count)
        {
            Debug.LogWarning("ValidateTurnStates: Mismatch found. Did not explore all turn state entries - i.e state machine is not fully connected");
            return false;
        }

        return (cyclicCheck && isCycled);
    }

    public override string ToString()
    {
        string str = "TurnStates\n";
        foreach (var entry in TurnStates)
        {
            var fromStateStr = (entry.CurrentState != null) ? entry.CurrentState.GetType().Name : "null";
            var toStateStr = (entry.NextState != null) ? entry.NextState.GetType().Name : "null";

            str += $"{fromStateStr} -> {toStateStr}\n";
        }

        return str;
    }
}