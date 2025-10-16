using UnityEngine;

[CreateAssetMenu(fileName = "BrontoBurtBehavior", menuName = "Enemy/Behaviors/BrontoBurt")]
public class BrontoBurtBehavior : EnemyBehavior
{
    [Header("Trigger Settings")]
    public float triggerRadius = 3f;         // Radius around enemy to detect player
    public float riseSpeed = 3f;             // Vertical speed while rising
    public EnemyBehavior flightBehavior;     // Behavior to switch to after rising

    private bool playerInRange = false;
    private bool isRising = false;

    public override void Execute(Enemy enemy)
    {
        if (enemy == null || enemy.isBeingInhaled || enemy.isDead) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector3 enemyPos = enemy.transform.position;
        Vector3 playerPos = player.transform.position;

        // Check if player is inside radius
        float distance = Vector2.Distance(enemyPos, playerPos);
        if (distance <= triggerRadius)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                isRising = true; // Start rising only when player enters radius
            }
        }
        else
        {
            playerInRange = false;
            return; // Player outside radius → do nothing
        }

        // Rising behavior: move vertically toward player's Y
        if (isRising)
        {
            float step = riseSpeed * Time.fixedDeltaTime;
            float newY = Mathf.MoveTowards(enemyPos.y, playerPos.y, step);
            enemy.transform.position = new Vector3(enemyPos.x, newY, enemyPos.z);

            // Switch to flight behavior when reached player's Y
            if (Mathf.Approximately(newY, playerPos.y) && flightBehavior != null)
            {
                enemy.behavior = flightBehavior;
            }
        }
    }

#if UNITY_EDITOR
    // Draw radius in editor
    private void OnDrawGizmosSelected()
    {
        Enemy enemy = null;

        // Draw gizmo where this behavior is active
        Enemy[] enemies = Object.FindObjectsOfType<Enemy>();
        foreach (Enemy e in enemies)
        {
            if (e.behavior == this)
            {
                enemy = e;
                break;
            }
        }

        if (enemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, triggerRadius);
        }
    }
#endif
}
