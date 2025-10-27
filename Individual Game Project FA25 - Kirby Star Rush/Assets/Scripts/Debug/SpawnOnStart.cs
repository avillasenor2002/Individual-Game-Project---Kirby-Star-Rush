using UnityEngine;

public class SpawnOnStartIfNoPlayer : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";

    [Header("Spawn Settings")]
    [SerializeField] private bool movePlayerToThisTransform = true; // Move player here at start

    [Header("Runtime Control")]
    public bool movetospawnpoint = false; // Can be set externally

    // Static flag ensures this happens only once per play session
    private static bool hasMovedPlayerThisSession = false;

    private void Start()
    {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                if (movePlayerToThisTransform)
                {
                    player.transform.position = transform.position;
                    hasMovedPlayerThisSession = true;
                    Debug.Log($"[{name}] Player moved to {transform.position}");
                }
            }
            else
            {
                Debug.Log($"[{name}] No player found in the scene.");
            }
    }
}
