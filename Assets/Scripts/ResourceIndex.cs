using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DebugResourceIndexEntry
{
    public string Hash;
    public string Path;

    public DebugResourceIndexEntry(Hash128 hash, string path)
    {
        Hash = hash.ToString();
        Path = path;
    }
}

public class ResourceIndex : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField]
    public Dictionary<Hash128, string> BasePaths = new Dictionary<Hash128, string>();

    // Hash is generated from the name of the asset (not full path). The string path is the relative path in the respective directory.
    // So a path "DevTextures/texture" in TexturePaths translates to "Textures/DevTextures/texture".
    // This implies a limitation - there can be NO DUPLICATE names under a directory.
    [SerializeField] public Dictionary<Hash128, string> TexturePaths = new Dictionary<Hash128, string>();
    [SerializeField] public Dictionary<Hash128, string> MaterialPaths = new Dictionary<Hash128, string>();
    [SerializeField] public Dictionary<Hash128, string> MeshPaths = new Dictionary<Hash128, string>();

    [SerializeField] private List<string> BaseKeys = new List<string>(); // String because Hash128 doesn't serialize properly and zeroes out.
    [SerializeField] private List<string> TextureKeys = new List<string>();
    [SerializeField] private List<string> MaterialKeys = new List<string>();
    [SerializeField] private List<string> MeshKeys = new List<string>();

    [SerializeField] private List<string> BaseValues = new List<string>();
    [SerializeField] private List<string> TextureValues = new List<string>();
    [SerializeField] private List<string> MaterialValues = new List<string>();
    [SerializeField] private List<string> MeshValues = new List<string>();

    [ReadOnly] public DebugResourceIndexEntry[] DebugBaseEntries;
    [ReadOnly] public DebugResourceIndexEntry[] DebugTextureEntries;
    [ReadOnly] public DebugResourceIndexEntry[] DebugMeshEntries;
    [ReadOnly] public DebugResourceIndexEntry[] DebugMaterialEntries;

    public void OnBeforeSerialize()
    {
        SerializeDictionary(ref BaseKeys, ref BaseValues, BasePaths);
        SerializeDictionary(ref TextureKeys, ref TextureValues, TexturePaths);
        SerializeDictionary(ref MaterialKeys, ref MaterialValues, MaterialPaths);
        SerializeDictionary(ref MeshKeys, ref MeshValues, MeshPaths);
    }

    void SerializeDictionary(ref List<string> outKeys, ref List<string> outValues, Dictionary<Hash128, string> dictionary)
    {
        outKeys.Clear();
        outValues.Clear();

        foreach (KeyValuePair<Hash128, string> pair in dictionary)
        {
            outKeys.Add(pair.Key.ToString());
            outValues.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        DeserializeDictionary(BaseKeys, BaseValues, ref BasePaths);
        DeserializeDictionary(TextureKeys, TextureValues, ref TexturePaths);
        DeserializeDictionary(MaterialKeys, MaterialValues, ref MaterialPaths);
        DeserializeDictionary(MeshKeys, MeshValues, ref MeshPaths);
    }

    void DeserializeDictionary(List<string> inKeys, List<string> inValues, ref Dictionary<Hash128, string> dictionary)
    {
        dictionary.Clear();

        Debug.Log($"DeserializeDictionary: keyCount={inKeys.Count}, valueCount={inValues.Count}");
        if (inKeys.Count != inValues.Count)
        {
            throw new System.Exception("Deserialized Key and Value list size do not match. Ensure both lists are serialized.");
        }

        for (int i = 0; i < inKeys.Count; ++i)
        {
            dictionary.Add(Hash128.Parse(inKeys[i]), inValues[i]);
        }
    }

    /* Returns the full path for a resource from its ID, to be used for loading via Resource.Load(relativeResPath)*/
    public bool FindResourcePath<T>(Hash128 resourceId, out string path)
    {
        Dictionary<Hash128, string> resourcePaths = BasePaths;

        System.Type type = typeof(T);
        if(type.IsSubclassOf(typeof(Texture)))
        {
            resourcePaths = TexturePaths;
        }
        else if(type == typeof(Mesh))
        {
            resourcePaths = MeshPaths;
        }
        else if (type == typeof(Material))
        {
            resourcePaths = MaterialPaths;
        }

        return FindResourcePath(resourcePaths, resourceId, out path);
    }

    bool FindResourcePath(Dictionary<Hash128, string> inPaths, Hash128 resourceId, out string path)
    {
        return inPaths.TryGetValue(resourceId, out path);
    }

    public void RebuildDebugEntries()
    {
        RebuildDebugEntries(out DebugBaseEntries, BasePaths);
        RebuildDebugEntries(out DebugTextureEntries, TexturePaths);
        RebuildDebugEntries(out DebugMeshEntries, MeshPaths);
        RebuildDebugEntries(out DebugMaterialEntries, MaterialPaths);
    }

    private void RebuildDebugEntries(out DebugResourceIndexEntry[] outDebugEntries, Dictionary<Hash128, string> pathList)
    {
        outDebugEntries = new DebugResourceIndexEntry[pathList.Count];

        int idx = 0;
        foreach (KeyValuePair<Hash128, string> pair in pathList)
        {
            outDebugEntries[idx++] = new DebugResourceIndexEntry(pair.Key, pair.Value);
        }
    }

    public static string GetIndexPath(bool bRelative = true)
    {
        return bRelative ? "index" : "Assets/Resources/index.asset";
    }

    public static string GetResourceDir<T>()
    {
        System.Type type = typeof(T);

        if(type == typeof(Texture))
        {
            return GetTextureDir();
        }
        else if(type == typeof(Mesh))
        {
            return GetMeshDir();
        }
        else if(type == typeof(Material))
        {
            return GetMaterialDir();
        }

        return "";
    }

    public static string GetTextureDir(bool bRelative = true)
    {
        return bRelative ? "Textures/" : "Assets/Resources/Textures/";
    }

    public static string GetMeshDir(bool bRelative = true)
    {
        return bRelative ? "Models/" : "Assets/Resources/Models/";
    }

    public static string GetMaterialDir(bool bRelative = true)
    {
        return bRelative ? "Materials/" : "Assets/Resources/Materials/";
    }
}
