using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Helper monobehavior to tag game objects and positoning locations for pool balls within a rack */
public class PoolRackSlot : MonoBehaviour
{
    /* The ball type to spawn in this slot */
    public EPoolBallType BallType;
    public bool IsPhysicsAsleepPostSpawn = true;
    public bool IsPhysicsAsleepPostPlace = false;
}
