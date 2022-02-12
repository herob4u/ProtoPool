using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultPoolGameAssets", menuName = "Pool/PoolGameAssets", order = 1), System.Serializable]
public class PoolGameAssets : ScriptableObject
{
    public Material CueMaterial;
    public GameObject PoolTablePrefab;
    public GameObject PoolCuePrefab;

    // Start is called before the first frame update
    void Start()
    {
        if(!CueMaterial)
        {
            CueMaterial = Resources.Load<Material>("Default-Material");
        }

        if(!PoolTablePrefab)
        {
        }

        if(!PoolCuePrefab)
        {
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
