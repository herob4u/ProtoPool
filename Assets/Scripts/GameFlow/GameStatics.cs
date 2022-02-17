using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameStatics
{
    public static bool IsGamePaused { get { return Time.timeScale == 0.0f; } }
    public static float GameSpeed { get { return Time.timeScale; } }

    public static System.Action OnGamePaused { get; set; }
    public static System.Action OnGameResumed { get; set; }
    public static System.Action OnGameSpeedChanged { get; set; }

    public static void PauseGame()
    {
        if(!IsGamePaused)
        {
            Time.timeScale = 0.0f;
            
            if(OnGamePaused != null)
            {
                OnGamePaused.Invoke();
            }
        }
    }

    public static void ResumeGame()
    {
        if(IsGamePaused)
        {
            Time.timeScale = 1.0f;
            
            if(OnGameResumed != null)
            {
                OnGameResumed.Invoke();
            }
        }
    }

    public static void SetGameSpeed(float multiplier)
    {
        multiplier = Mathf.Clamp(multiplier, 0.01f, 10.0f);
        bool wasGamePaused = IsGamePaused;

        Time.timeScale = multiplier;

        if(wasGamePaused)
        {
            if(OnGameResumed != null)
            {
                OnGameResumed.Invoke();
            }
        }
    }
}
