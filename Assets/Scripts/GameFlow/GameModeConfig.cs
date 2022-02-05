using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MyGameModeConfig", menuName = "GameModes/GameModeConfig", order = 1), System.Serializable]
public class GameModeConfig : ScriptableObject
{
    public string GameModeName;
    public string SceneName;

    public bool IsOfflineMode;

    public List<GameObject> ServerOnlyObjects;
    public List<GameObject> ClientOnlyObjects;
}
