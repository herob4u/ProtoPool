using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Generic data that can be added/removed on a player by game features.
// Upper layers subclass this, and define any custom data and implementation needed for a player.
public class PlayerGameInfo : MonoBehaviour
{
    protected Player OwningPlayer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public virtual void Init()
    { 
    }

    public virtual void OnAdded(Player owningPlayer)
    {
        OwningPlayer = owningPlayer;
    }

    public virtual void OnRemoved(Player owningPlayer)
    {
        OwningPlayer = null;
    }
}
