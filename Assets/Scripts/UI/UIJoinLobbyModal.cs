using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIJoinLobbyModal : MonoBehaviour
{
    private string ipAddress = "127.0.0.1";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnJoin()
    {
        GameFlow.SessionJoinParams joinParams;
        joinParams.ConnectPort = 7777;
        joinParams.IPAddress = ipAddress;
        joinParams.GameMode = "PoolGameMode";

        GameFlow.BootstrapGameMode.Instance.JoinGameSession(joinParams);
    }

    public void OnBack()
    {
        gameObject.SetActive(false);
    }

    public void OnIPAddressChanged(string newIPAddress)
    {
        ipAddress = newIPAddress;
    }
}
