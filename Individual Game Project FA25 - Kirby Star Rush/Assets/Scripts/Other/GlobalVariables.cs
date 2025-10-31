using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelTimeEntry
{
    public string levelName;
    public string bestTime = "00:00";
}

public class GlobalVariables : MonoBehaviour
{
    public static GlobalVariables Instance { get; private set; }

    [Header("Global Flags")]
    public bool levelStart = false;

    [Header("Timer Data")]
    public string lastRecordedTime = "00:00";

    [Header("Best Times Editable in Inspector")]
    public List<LevelTimeEntry> bestTimesList = new List<LevelTimeEntry>();

    // Internal dictionary for quick lookup
    private Dictionary<string, string> bestTimes = new Dictionary<string, string>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Populate dictionary from inspector list
        foreach (var entry in bestTimesList)
        {
            if (!bestTimes.ContainsKey(entry.levelName))
                bestTimes.Add(entry.levelName, entry.bestTime);
        }
    }

    /// <summary>
    /// Record a level's time and update the best if better
    /// </summary>
    public void RecordLevelTime(string levelName, string time)
    {
        lastRecordedTime = time;

        // Check dictionary
        if (bestTimes.ContainsKey(levelName))
        {
            if (IsTimeBetter(time, bestTimes[levelName]))
            {
                bestTimes[levelName] = time;
                UpdateListEntry(levelName, time);
            }
        }
        else
        {
            bestTimes.Add(levelName, time);
            UpdateListEntry(levelName, time);
        }
    }

    /// <summary>
    /// Get best time for a level
    /// </summary>
    public string GetBestTime(string levelName)
    {
        if (bestTimes.ContainsKey(levelName))
            return bestTimes[levelName];
        return "00:00";
    }

    /// <summary>
    /// Compare two times in MM:SS format
    /// </summary>
    public bool IsTimeBetter(string newTime, string oldTime)
    {
        int newSeconds = TimeStringToSeconds(newTime);
        int oldSeconds = TimeStringToSeconds(oldTime);
        return newSeconds < oldSeconds;
    }

    /// <summary>
    /// Convert MM:SS to seconds
    /// </summary>
    private int TimeStringToSeconds(string time)
    {
        string[] split = time.Split(':');
        if (split.Length != 2) return int.MaxValue;
        int minutes = int.Parse(split[0]);
        int seconds = int.Parse(split[1]);
        return minutes * 60 + seconds;
    }

    /// <summary>
    /// Updates the inspector list when the dictionary changes
    /// </summary>
    private void UpdateListEntry(string levelName, string time)
    {
        LevelTimeEntry entry = bestTimesList.Find(e => e.levelName == levelName);
        if (entry != null)
        {
            entry.bestTime = time;
        }
        else
        {
            bestTimesList.Add(new LevelTimeEntry { levelName = levelName, bestTime = time });
        }
    }

    /// <summary>
    /// Return a copy of all best times
    /// </summary>
    public Dictionary<string, string> GetAllBestTimes()
    {
        return new Dictionary<string, string>(bestTimes);
    }
}
