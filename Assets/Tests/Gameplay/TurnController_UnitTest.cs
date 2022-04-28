using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestTurnStateIdle : TurnState
{
    public bool bIsIdling = false;

    protected override void DoEnter()
    {
        bIsIdling = true;
    }

    protected override void DoExit()
    {
        bIsIdling = false;
    }

    protected override void DoUpdate(float scaledDt)
    {
    }

    public override bool CanExit()
    {
        return !bIsIdling;
    }
}

public class TestTurnStateWait : TurnState
{
    float timer = 0.0f;
    
    protected override void DoEnter()
    {
        timer = 5.0f;   
    }

    protected override void DoExit()
    {
    }

    protected override void DoUpdate(float scaledDt)
    {
        timer -= scaledDt;
        Debug.Log($"DoUpdate: timer = {timer}");
    }

    public override bool CanExit()
    {
        Debug.Log($"CanExit = {timer <= 0.0f}");
        return timer <= 0.0f;
    }
}

public class TurnController_UnitTest
{
    TurnStateMachine StateMachine;
    ITurnController TurnController;

    [SetUp]
    public void Setup()
    {
        TurnController = NSubstitute.Substitute.For<ITurnController>();
        StateMachine = new TurnStateMachine(TurnController);
    }

    [TearDown]
    public void TearDown()
    {
        TurnController = null;
        StateMachine = null;
    }

    [Test]
    public void TurnController_CreateSingleState()
    {
        StateMachine.AddTurnState<TestTurnStateIdle>();
        Assert.That(StateMachine.TurnStates.Count == 1);

        Assert.That(null == StateMachine.GetCurrentState()); // State not transitioned to yet
        StateMachine.DoUpdate(0.0f);
        Assert.That(typeof(TestTurnStateIdle) == StateMachine.GetCurrentState().GetType()); // State entered
        StateMachine.DoUpdate(1000.0f);
        Assert.That(typeof(TestTurnStateIdle) == StateMachine.GetCurrentState().GetType()); // State did not exit
    }

    [Test]
    public void TurnController_CreateDualStates()
    {
        var stateTuple = StateMachine.AddTurnState<TestTurnStateIdle, TestTurnStateWait>();
        StateMachine.DoUpdate(1.0f);

        Assert.That(typeof(TestTurnStateIdle) == StateMachine.GetCurrentState().GetType());
        Assert.That(StateMachine.TurnStates.Count == 2);
    }

    [Test]
    public void TurnController_InitConfig()
    {
        TurnStateConfig config = new TurnStateConfig();
        TurnStateConfig.Entry entry1 = new TurnStateConfig.Entry();

        entry1.FromState = typeof(TestTurnStateIdle);
        entry1.ToState = typeof(TestTurnStateWait);

        config.TurnStateConfigEntries.Add(entry1);

        StateMachine.DoInit(config);
        StateMachine.DoUpdate(1.0f);

        Debug.Log(StateMachine);

        Assert.That(StateMachine.TurnStates.Count == 2);
        Assert.That(StateMachine.GetCurrentState().GetType() == typeof(TestTurnStateIdle));
    }

    [Test]
    public void TurnController_Transition()
    {
        var stateTuple = StateMachine.AddTurnState<TestTurnStateIdle, TestTurnStateWait>();
        StateMachine.AddTurnState<TestTurnStateWait, TestTurnStateIdle>();

        Debug.Log("Turn States:");
        foreach(var entry in StateMachine.TurnStates)
        {
            var fromStateStr = (entry.CurrentState != null) ? entry.CurrentState.GetType().Name : "null";
            var toStateStr = (entry.NextState != null) ? entry.NextState.GetType().Name : "null";

            Debug.Log($"{fromStateStr} -> {toStateStr}");
        }

        StateMachine.DoUpdate(1.0f);

        stateTuple.Item1.bIsIdling = false;
        StateMachine.DoUpdate(1.0f);

        Assert.That(typeof(TestTurnStateWait) == StateMachine.GetCurrentState().GetType());
        StateMachine.DoUpdate(6.0f); // 5s elapsed, CanExit should be true

        Assert.That(typeof(TestTurnStateIdle) == StateMachine.GetCurrentState().GetType());
    }
    private TurnStateMachine.TurnStateEntry MakeEntry<T1, T2>() where T1 : TurnState, new() where T2 : TurnState, new()
    {
        T1 fromState = new T1();
        T2 toState = new T2();

        return new TurnStateMachine.TurnStateEntry(fromState, toState);
    }

    private TurnStateMachine.TurnStateEntry MakeEntry<T1>() where T1 : TurnState, new()
    {
        T1 fromState = new T1();

        return new TurnStateMachine.TurnStateEntry(fromState);
    }

    private TurnStateMachine.TurnStateEntry MakeEntry(TurnState fromState = null, TurnState toState = null)
    {
        return new TurnStateMachine.TurnStateEntry(fromState, toState);
    }


    [Test]
    public void TurnController_ValidationSuccess()
    {
        TurnState mockState1 = NSubstitute.Substitute.For<TurnState>();
        TurnState mockState2 = NSubstitute.Substitute.For<TurnState>();
        TurnState mockState3 = NSubstitute.Substitute.For<TurnState>();

        List<TurnStateMachine.TurnStateEntry> entries = new List<TurnStateMachine.TurnStateEntry>();

        var rootEntry = MakeEntry<TestTurnStateIdle, TestTurnStateWait>();
        entries.Add(rootEntry);
        entries.Add(MakeEntry(rootEntry.NextState, mockState1));
        entries.Add(MakeEntry(mockState1, mockState2));
        entries.Add(MakeEntry(mockState2, mockState3));
        entries.Add(MakeEntry(mockState3, rootEntry.CurrentState));

        Assert.IsTrue(TurnStateMachine.ValidateTurnStates(entries));
    }

    public void TurnController_ValidationSingle()
    {
        TurnState state = new TestTurnStateIdle();
        List<TurnStateMachine.TurnStateEntry> entries = new List<TurnStateMachine.TurnStateEntry>();
        entries.Add(new TurnStateMachine.TurnStateEntry(state, state));

        Assert.IsTrue(TurnStateMachine.ValidateTurnStates(entries));
    }

    [Test]
    public void TurnController_ValidationFailCycle()
    {
        TurnState mockState1 = NSubstitute.Substitute.For<TurnState>();

        List<TurnStateMachine.TurnStateEntry> entries = new List<TurnStateMachine.TurnStateEntry>();

        var rootEntry = MakeEntry<TestTurnStateIdle, TestTurnStateWait>();
        entries.Add(rootEntry);
        entries.Add(MakeEntry(rootEntry.NextState, mockState1));

        Assert.IsFalse(TurnStateMachine.ValidateTurnStates(entries));
    }

    [Test]
    public void TurnController_ValidationFailDisconnected()
    {
        List<TurnStateMachine.TurnStateEntry> entries = new List<TurnStateMachine.TurnStateEntry>();

        entries.Add(MakeEntry<TestTurnStateIdle>());
        entries.Add(MakeEntry<TestTurnStateWait>());

        Assert.IsFalse(TurnStateMachine.ValidateTurnStates(entries));
    }

    [Test]
    public void TurnController_EmptyStateMachine()
    {
        Assert.Ignore("Not Implemented!");
    }

    // A Test behaves as an ordinary method
    [Test]
    public void TurnController_UnitTestSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    //[Test]

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    /*
    [UnityTest]
    public IEnumerator TurnController_UnitTestWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
    */
}
