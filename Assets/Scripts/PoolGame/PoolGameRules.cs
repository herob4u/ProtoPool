using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
