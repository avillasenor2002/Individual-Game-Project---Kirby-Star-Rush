using UnityEngine;

public class RestartOnTouch : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player"; // Tag used to identify the player

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            KillPlayer(collision.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider != null && collision.collider.CompareTag(playerTag))
        {
            KillPlayer(collision.collider.gameObject);
        }
    }

    private void KillPlayer(GameObject player)
    {
        if (player == null) return;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            // Compute a hit normal pointing from this object to the player
            Vector2 hitNormal = (player.transform.position - transform.position).normalized;
            if (hitNormal == Vector2.zero) hitNormal = Vector2.up;

            // Ensure we inflict enough damage to reduce HP to zero.
            // Use the player's currentHP so normal death logic runs.
            int damage = Mathf.Max(1, playerHealth.currentHP);

            playerHealth.TakeDamage(damage, hitNormal);
        }
        else
        {
            Debug.LogWarning($"RestartOnTouch: GameObject '{player.name}' does not have a PlayerHealth component.");
        }
    }
}
