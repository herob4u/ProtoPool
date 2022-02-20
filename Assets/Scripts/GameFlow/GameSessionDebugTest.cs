using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameSessionDebugTest : MonoBehaviour
{
    public GameObject NetObjectToSpawn;
    private GameObject SpawnedObject;

    public void SpawnNetObject()
    {
        NetworkManager netMgr = NetworkManager.Singleton;
        if(!netMgr)
        {
            Debug.LogWarning("Failed to spawn net object, no network manager!");
            return;
        }

        DespawnNetObject();

        SpawnedObject = Instantiate(NetObjectToSpawn);
        SpawnedObject.GetComponent<NetworkObject>().Spawn();
    }

    public void DespawnNetObject()
    {
        NetworkManager netMgr = NetworkManager.Singleton;
        if (!netMgr)
        {
            Debug.LogWarning("Failed to despawn net object, no network manager!");
            return;
        }

        if (SpawnedObject)
        {
            SpawnedObject.GetComponent<NetworkObject>().Despawn(true);
            SpawnedObject = null;
        }
    }
}
