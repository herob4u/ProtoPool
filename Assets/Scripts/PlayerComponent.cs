using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// A component that exists only on the local player. Used for local behavior such as user input, camera, cosmetic, etc.
// Not replicated, and will be automatically destroyed if spawned on a remote/server owned player.
// @todo: should we restrict this component to only exist on a Player object?
public class PlayerComponent : MonoBehaviour
{
    public Player OwningPlayer { get; private set; }

    void Awake()
    {
        if(NetworkManager.Singleton && !NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("PlayerComponent being added on the server. PlayerComponents can only exist on local clients!");
            Destroy(this);
            return;
        }

        NetworkObject networkObject = GetComponentInParent<NetworkObject>();
        if(networkObject && !networkObject.IsOwner)
        {
            Debug.LogError("PlayerComponent added to an object not owned by local player. Removing...");
            Destroy(this);
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        NetworkObject networkObject = GetComponentInParent<NetworkObject>();
        if (networkObject && !networkObject.IsOwner)
        {
            Debug.LogError("PlayerComponent added to an object not owned by local player. Removing...");
            Destroy(this);
            return;
        }
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
