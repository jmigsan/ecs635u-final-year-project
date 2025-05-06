using UnityEngine;
using System;

public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    public int hour = 7;
    public int minute = 0;

    public float secondsInMinute = 5.0f;
    float irlSeconds = 0.0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        irlSeconds += Time.deltaTime;

        if (irlSeconds >= secondsInMinute)
        {
            minute++;
            irlSeconds = 0;

            if (minute >= 60)
            {
                hour++;
                minute = 0;

                if (hour >= 24)
                {
                    hour = 0;
                }
            }
        }
    }

    public string GetTime()
    {
        return $"{hour:D2}:{minute:D2}";
    }
}