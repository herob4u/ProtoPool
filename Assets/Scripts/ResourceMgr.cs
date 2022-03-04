using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceMgr : MonoBehaviour
{
    // The key val registry of built resources.
    private ResourceIndex Index;

    private static ResourceMgr s_instance = null;
    public static ResourceMgr Instance { get
        {
            if(s_instance == null)
            {
                GameObject obj = new GameObject("ResourceMgr");
                s_instance = obj.AddComponent<ResourceMgr>();
            }

            return s_instance;
        } }

    void Awake()
    {
        if(s_instance != null)
        {
            Debug.LogError("ResourceMgr already exist, destroying this instance");
            Destroy(this);
            return;
        }

        s_instance = this;

        Index = Resources.Load<ResourceIndex>(ResourceIndex.GetIndexPath());
        if(!Index)
        {
            Debug.LogError("Resource Index failed to load! Further resource queries will fail.");
            return;
        }

        Logger.LogScreen("Resource Index successfully loaded", Color.green, 5.0f);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        s_instance = null;
    }

    public T GetResource<T>(string resourceName) where T : Object
    {
        Hash128 resourceId = Hash128.Compute(resourceName);
        return GetResource<T>(resourceId);
    }

    public T GetResource<T>(Hash128 resourceId) where T : Object
    {
        if(Index)
        {
            string path;
            if(Index.FindResourcePath<T>(resourceId, out path))
            {
                // Got a valid path, load the resource.
                T resource = Resources.Load<T>(path);
                return resource;
            }
        }

        return null;
    }

    public Hash128 GetResourceId<T>(T resource) where T : Object
    {
        if(!resource)
        {
            return Hash128.Compute("");
        }

        // The ID used in the ResourceIndex is simply the hash of the name of the asset. This is because we have no way of determining
        // the full path of an asset in runtime.
        return Hash128.Compute(resource.name); // for now, just the name...
    }
}
