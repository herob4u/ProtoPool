using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lobby : ScriptableObject
{
    public int MaxPlayers;
    public string Password;
    public List<Player> Players;
}

// Server-side lobby
public class LobbyMgr : MonoBehaviour
{
    public Lobby CurrentLobby { get; private set; }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void CreateLobby(int maxPlayers, string password)
    {

    }

    public void LeaveLobby()
    {

    }

    public void DestroyLobby()
    {

    }
}
