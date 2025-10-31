using UnityEngine;

public class EndStar : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, triggers level complete; if false, triggers game over.")]
    public bool isLevelComplete = true;

    [Tooltip("Optional tag to identify the player object.")]
    public string playerTag = "Player";

    private bool triggered = false; // Prevent double triggering

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered) return;
        if (!collision.CompareTag(playerTag)) return;

        triggered = true;

        LevelEndManager endManager = FindObjectOfType<LevelEndManager>();
        if (endManager != null)
        {
            if (isLevelComplete)
                endManager.TriggerLevelEnd();
            /*else
                endManager.TriggerGameOver();*/
        }

        // Optional: add a visual or sound effect
        // Example:
        // GetComponent<Animator>()?.SetTrigger("Activate");
        // AudioSource.PlayClipAtPoint(starSound, transform.position);
    }
}
