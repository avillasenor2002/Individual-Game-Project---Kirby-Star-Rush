using System.Collections;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float elapsedTime = 0f; // Visible in Inspector
    [SerializeField] private bool isRunning = false; // Visible in Inspector

    [Header("Formatted Time")]
    [SerializeField] public string currentTimeFormatted = "00:00"; // Visible in Inspector

    public string CurrentTimeFormatted => currentTimeFormatted; // Read-only access for scripts

    // Start the timer
    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        StartCoroutine(TimerCoroutine());
    }

    // End the timer and share with GlobalVariables
    public void EndTimer()
    {
        isRunning = false;

        // Format the final time
        currentTimeFormatted = FormatTime(elapsedTime);

        // Share it with GlobalVariables
        if (GlobalVariables.Instance != null)
        {
            GlobalVariables.Instance.lastRecordedTime = currentTimeFormatted;
        }

        Debug.Log($"Timer ended: {currentTimeFormatted}");
    }

    // Coroutine to update the timer every frame
    private IEnumerator TimerCoroutine()
    {
        while (isRunning)
        {
            elapsedTime += Time.deltaTime;
            currentTimeFormatted = FormatTime(elapsedTime);
            yield return null;
        }
    }

    // Helper function to format time as "MM:SS"
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
