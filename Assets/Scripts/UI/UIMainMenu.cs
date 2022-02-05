using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMainMenu : MonoBehaviour
{
    public GameObject JoinLobbyDialog;

    // Start is called before the first frame update
    void Start()
    {
        if(JoinLobbyDialog)
        {
            JoinLobbyDialog.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCreateLobby()
    {
        GameFlow.SessionCreateParams createParams;
        createParams.GameMode = "PoolGameMode";
        createParams.ListeningPort = 7777;
        createParams.MaxPlayers = 2;
        createParams.SessionPrivacy = GameFlow.ESessionPrivacy.Public;

        GameFlow.BootstrapGameMode.Instance.CreateGameSession(createParams);
    }

    public void OnJoinLobby()
    {
        JoinLobbyDialog.SetActive(true);
    }
}
