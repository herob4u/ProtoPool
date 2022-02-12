using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoolBallAssetDb", menuName = "Pool/PoolBallAssetDatabase", order = 1), System.Serializable]
public class PoolBallAssetDb : ScriptableObject
{
    public PoolBallDescriptor[] Descriptors = new PoolBallDescriptor[Pool.NUM_BALLS];

    public PoolBallAssetDb()
    {
        for (int i = 0; i < Pool.NUM_BALLS; i++)
        {
            Descriptors[i].BallType = (EPoolBallType)i;
        }
    }

    public PoolBallDescriptor Get(EPoolBallType ballType)
    {
        foreach (PoolBallDescriptor descriptor in Descriptors)
        {
            if (descriptor.BallType == ballType)
            {
                return descriptor;
            }
        }

        return new PoolBallDescriptor(null, EPoolBallType.Cue);
    }
}