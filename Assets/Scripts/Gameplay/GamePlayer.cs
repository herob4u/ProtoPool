using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayer
{
    public Player Player;
    public bool IsReady { get; private set; }

    public GamePlayer(Player player)
    {
        Player = player;
        IsReady = false;
    }

    public void SetAsReady()
    {
        IsReady = true;
    }
}

public interface IGamePlayerObserver
{
    void OnGamePlayerAdded(GamePlayer player);
    void OnGamePlayerRemoved(GamePlayer player);
}
