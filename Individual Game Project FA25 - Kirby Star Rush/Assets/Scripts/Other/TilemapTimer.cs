using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float startTime = 9999f; // max 4 digits
    [SerializeField] private bool countingDown = true;

    [Header("Tilemap & Tiles")]
    [SerializeField] private Tilemap timerTilemap;
    [SerializeField] private List<Tile> numberTiles; // 0-9, index 0 = 0, index 1 = 1, etc.
    [SerializeField] private Vector3Int startingCell = Vector3Int.zero; // top-left cell for first digit

    private float currentTime;

    private void Awake()
    {
        // Auto-find the "Timer" tilemap if missing
        if (timerTilemap == null)
        {
            FindTimerTilemap();
        }

        currentTime = Mathf.Clamp(startTime, 0, 9999);
        UpdateTilemapDisplay();
    }

    private void Update()
    {
        if (timerTilemap == null)
        {
            FindTimerTilemap(); // keep trying to find it if it was deleted/reloaded
            if (timerTilemap == null)
                return; // stop if still not found
        }

        if (countingDown)
        {
            currentTime -= Time.deltaTime;
            if (currentTime < 0f) currentTime = 0f;
        }
        else
        {
            currentTime += Time.deltaTime;
            if (currentTime > 9999f) currentTime = 9999f;
        }

        UpdateTilemapDisplay();
    }

    private void UpdateTilemapDisplay()
    {
        if (timerTilemap == null || numberTiles == null || numberTiles.Count < 10)
            return;

        int timeInt = Mathf.FloorToInt(currentTime);
        string timeString = timeInt.ToString("0000"); // ensure 4 digits

        // Clear previous tiles
        for (int i = 0; i < 4; i++)
        {
            Vector3Int cellPos = startingCell + new Vector3Int(i, 0, 0);
            timerTilemap.SetTile(cellPos, null);
        }

        // Set new digit tiles
        for (int i = 0; i < 4; i++)
        {
            int digit = int.Parse(timeString[i].ToString());
            Vector3Int cellPos = startingCell + new Vector3Int(i, 0, 0);
            if (digit >= 0 && digit <= 9)
            {
                timerTilemap.SetTile(cellPos, numberTiles[digit]);
            }
        }
    }

    /// <summary>
    /// Finds a Tilemap named "Timer" in the scene.
    /// </summary>
    private void FindTimerTilemap()
    {
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap map in allTilemaps)
        {
            if (map.gameObject.name == "Timer")
            {
                timerTilemap = map;
                Debug.Log("[TilemapTimer] Found and assigned Tilemap: Timer");
                return;
            }
        }

        Debug.LogWarning("[TilemapTimer] Could not find a Tilemap named 'Timer' in the scene.");
    }

    /// <summary>Set the timer to a specific value.</summary>
    public void SetTime(float newTime)
    {
        currentTime = Mathf.Clamp(newTime, 0, 9999);
        UpdateTilemapDisplay();
    }

    /// <summary>Start counting down.</summary>
    public void StartCountdown() => countingDown = true;

    /// <summary>Stop the timer.</summary>
    public void StopTimer() => countingDown = false;

    public void AddTime(float amount)
    {
        currentTime = Mathf.Clamp(currentTime + amount, 0, 9999);
        UpdateTilemapDisplay();
    }

}
