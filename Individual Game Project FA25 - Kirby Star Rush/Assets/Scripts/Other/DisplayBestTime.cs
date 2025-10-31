using UnityEngine;
using TMPro;

public class DisplayBestTime : MonoBehaviour
{
    [Header("UI Element")]
    [SerializeField] private TextMeshProUGUI bestTimeText;

    [Header("Level Settings")]
    [SerializeField] private string levelName; // Assign the level name in the inspector

    private void Start()
    {
        if (bestTimeText == null)
        {
            Debug.LogWarning("BestTimeText TMP is not assigned!");
            return;
        }

        if (GlobalVariables.Instance == null)
        {
            Debug.LogWarning("GlobalVariables instance not found!");
            return;
        }

        // Get the best time for the specified level
        string bestTime = GlobalVariables.Instance.GetBestTime(levelName);

        // Display it on the TMP
        bestTimeText.text = bestTime;
    }
}
