using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class Logger
{
    private const float DefaultDisplayDuration = 3.0f;

    public static void LogScreen(string msg, float duration = DefaultDisplayDuration)
    {
        LogScreen(msg, Color.yellow, duration);
    }

    public static void LogScreenFormat(string format, params object[] args)
    {
        LogScreen(string.Format(format, args));
    }

    public static void LogScreen(string msg, Color color, float duration = DefaultDisplayDuration)
    {
        ScreenLogger.Instance.Log(msg, color, duration);
        Debug.Log(msg);
    }
}


public class ScreenLogger : MonoBehaviour
{
    public static ScreenLogger Instance { get => GetInstance(); }
    private static ScreenLogger m_instance = null;

    protected class MessageInfo
    {
        public string Message;
        public float RemainingTime;
        public Color DisplayColor;

        public MessageInfo(string msg, Color color, float duration)
        {
            Message = msg;
            DisplayColor = color;
            RemainingTime = duration;
        }
    }

    protected Queue<MessageInfo> MessageQueue = new Queue<MessageInfo>();
    GUIStyle Style = new GUIStyle();

    // Start is called before the first frame update
    void Start()
    {
    }

    private static ScreenLogger GetInstance()
    {
        if(!m_instance)
        {
            GameObject loggerObj = new GameObject("ScreenLogger");
            loggerObj.AddComponent<ScreenLogger>(); // Awake method will handle setting instance

            return m_instance;
        }

        return m_instance;
    }

    private void Awake()
    {
        if(m_instance)
        {
            Destroy(this);
            return;
        }

        m_instance = this;
    }

    public void Log(string msg, Color color, float duration)
    {
        MessageQueue.Enqueue(new MessageInfo(msg, color, duration));
    }

    // Update is called once per frame
    void Update()
    {
        foreach(MessageInfo msgInfo in MessageQueue)
        {
            msgInfo.RemainingTime -= Time.unscaledDeltaTime;
        }

        ClearExpiredMessages();
        
    }

    void ClearExpiredMessages()
    {
        while(MessageQueue.Count > 0 && MessageQueue.Peek().RemainingTime <= 0)
        {
            MessageQueue.Dequeue();
        }
    }

    private void OnGUI()
    {
        // Draw the actual queue elements
        GUILayout.BeginArea(new Rect(600, 10, 300, 1000 /* height of screen */));
            int idx = 0;
            foreach (MessageInfo msgInfo in MessageQueue)
            {
                DrawMessage(idx++, msgInfo.Message, msgInfo.DisplayColor);
            }
        GUILayout.EndArea();
    }

    void DrawMessage(int idx, string msg, Color color)
    {
        Style.normal.textColor = color;
        GUILayout.Label(msg, Style);
    }
}
