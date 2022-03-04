using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuildResourceIndex : MonoBehaviour
{
    [MenuItem("Resources/Build")]
    public static void BuildResourceIndexMenu()
    {
        // Delete old version of index
        string indexFilePath = ResourceIndex.GetIndexPath(false);
        AssetDatabase.DeleteAsset(indexFilePath);

        // Create a new empty index
        ResourceIndex index = ScriptableObject.CreateInstance<ResourceIndex>();
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

        // Traverse all assets while keeping in mind which sub-directory they exist in. We want to separate out
        // Texture, Mesh, Material assets at the very least. Everything else goes to the root Resource/ path by default.
        foreach(string path in allAssetPaths)
        {
            if(!System.IO.File.Exists(path))
            {
                continue;
            }

            if(path.StartsWith(ResourceIndex.GetTextureDir(false)))
            {
                AddToIndex(ref index.TexturePaths, path);
            }
            else if(path.StartsWith(ResourceIndex.GetMaterialDir(false)))
            {
                AddToIndex(ref index.MaterialPaths, path);
            }
            else if(path.StartsWith(ResourceIndex.GetMeshDir(false)))
            {
                AddToIndex(ref index.MeshPaths, path);
            }
            else if(path.StartsWith("Assets/Resources"))
            {
                AddToIndex(ref index.BasePaths, path);     
            }
        }

        index.RebuildDebugEntries();

        Debug.Log($"Writing resource index to: {indexFilePath}");
        AssetDatabase.CreateAsset(index, indexFilePath);
    }

    static void AddToIndex(ref Dictionary<Hash128, string> toPathList, string assetFullPath)
    {
        string assetName = System.IO.Path.GetFileNameWithoutExtension(assetFullPath);
        string assetRelativePath = System.IO.Path.ChangeExtension(assetFullPath.TrimStart("Assets/Resources".ToCharArray()), null);
        Hash128 assetId = Hash128.Compute(assetName);

        if(toPathList.ContainsKey(assetId))
        {
            Debug.LogError($"An asset with id {assetId} already exists in the path list. \nCurrent Asset: {assetFullPath}\nExisting Asset: {toPathList[assetId]}\n");
            return;
        }

        Debug.Log($"Resource: Found {assetRelativePath}, id{assetId}");
        toPathList.Add(assetId, assetRelativePath);
    }
}
