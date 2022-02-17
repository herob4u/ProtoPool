using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Which type of balls can be hit 
 *  CueOnly: White cue ball only
 *  AssignedOnly: The set of balls assigned to us (i.e all solid balls, all stripe balls, etc)
 *  Any: Can hit any balls in play.
 */
public enum EPoolBallRule
{
    CueOnly,
    AssignedOnly,
    Any
};

public enum EPoolConditionEvent
{
    BallScored = 0x01,
    TotalScore = 0x02,
    Time = 0x04,
    BallsRemaining = 0x08
};

public enum EPoolConditionOperator
{
    GreaterThanEqual,
    GreaterThan,
    Equal,
    LessThan,
    LessThanEqual
};

[System.Serializable]
public class PoolCondition
{
    public EPoolConditionEvent ConditionEvent;
    public bool InvertCondition;

    [EditCondition("ConditionEvent", ((int)EPoolConditionEvent.Time) | ((int)EPoolConditionEvent.TotalScore), true)]
    public float Value;

    [EditCondition("ConditionEvent", ((int)EPoolConditionEvent.Time) | ((int)EPoolConditionEvent.TotalScore), true)]
    public EPoolConditionOperator Operator;

    [EditCondition("ConditionEvent", ((int)EPoolConditionEvent.BallScored) | ((int)EPoolConditionEvent.BallsRemaining), true)]
    public EPoolBallType[] BallTypes;
}

[CreateAssetMenu(fileName = "DefaultPoolGameRules", menuName = "Pool/PoolGameRules", order = 1), System.Serializable]
/* Data assets that define defaults and rules for the game */
public class PoolGameRules : ScriptableObject
{
    public PoolGameAssets GameAssets;
    public PoolBallAssetDb BallAssets;

    [Range(1, 4)]
    public int MaxPlayers = 2;

    [Range(1, 4)]
    public int MinPlayers = 1;

    [Range(1, 16)]
    public int TurnsPerPlayer = 1;

    public EPoolBallType[] BallsInPlay;

    public PoolCondition[] WinCondition;
    public PoolCondition[] LoseCondition;
    public PoolCondition[] FoulCondition;

    // Start is called before the first frame update
    void Start()
    {
        if(!GameAssets)
        {
            Debug.LogWarning("No PoolGameAssets set for game rule, reverting to defaults.");
            GameAssets = new PoolGameAssets();
        }

        if(!BallAssets)
        {
            Debug.LogWarning("No PoolBallAssetsDb set for game rule, reverting to defaults.");
            BallAssets = new PoolBallAssetDb();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
