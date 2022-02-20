using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMgr : MonoBehaviour
{
    public delegate void OnPlayerJoinedDelegate(Player player);
    public delegate void OnPlayerLeftDelegate(Player player);

    public static ulong INVALID_PLAYER_NET_ID = ulong.MaxValue;

    public OnPlayerJoinedDelegate OnPlayerJoined;
    public OnPlayerLeftDelegate OnPlayerLeft;

    public List<Player> Players = new List<Player>();
    public bool DebugGUIEnabled = true;
    
    public static PlayerMgr Instance { get; private set; }

    public virtual void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Only one instance of PlayerMgr can exist!");
            return;
        }

        Debug.Log("Created PlayerMgr");
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public List<Player> GetPlayers()
    {
        return Players;
    }

    public Player GetPlayer(ulong playerNetId)
    {
        foreach(Player player in Players)
        {
            if(player.NetId == playerNetId)
            {
                return player;
            }
        }

        return null;
    }
    
    public void NotifyPlayerJoined(Player player)
    {
        if(Players.Contains(player))
        {
            Debug.LogWarningFormat("Player {0} already exists! Ensure clean up code is correct", player.NetId);
            return;
        }

        // Convention, keep local player as index 0
        if(player.IsLocal())
        {
            Players.Insert(0, player);
        }
        else
        {
            Players.Add(player);
        }

        if (OnPlayerJoined != null)
        {
            OnPlayerJoined.Invoke(player);
        }
    }

    public void NotifyPlayerLeft(Player player)
    {
        bool removed = Players.Remove(player);
        if(!removed)
        {
            Debug.LogWarningFormat("Failed to remove player {0}. Player was never added, ensure joining code is correct", player.NetId);
        }

        if(OnPlayerLeft != null)
        {
            OnPlayerLeft.Invoke(player);
        }
    }

    public Player GetLocalPlayer()
    {
        if (Players.Count == 0)
        {
            Debug.LogWarning("No local player exists!");
            return null;
        }

        return Players[0];
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnGUI()
    {
        if(!DebugGUIEnabled)
        {
            return;
        }

        int numPlayers = Players.Count;
        if(numPlayers < 1)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 300, 100 * numPlayers));
        {
            GUILayout.BeginVertical("Players", GUIStyle.none);
            {
                int localIdx = 0;
                foreach(Player player in Players)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"[{localIdx++}]");
                        GUILayout.Label($"[NetId: {player.NetId}]");
                        GUILayout.Label($"{(player.IsLocal() ? "Local" : "Remote")}");
                        
                        if(NetworkManager.Singleton.ServerClientId == player.NetId)
                        {
                            GUILayout.Label("Server");
                        }
                        else
                        {
                            GUILayout.Label("Client");
                        }
                    }
                    GUILayout.EndHorizontal();
                }

            }
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();
    }
}
