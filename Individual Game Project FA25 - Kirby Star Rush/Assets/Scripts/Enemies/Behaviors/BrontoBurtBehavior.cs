using UnityEngine;

[CreateAssetMenu(fileName = "BrontoBurtBehavior", menuName = "Enemy/Behaviors/BrontoBurt")]
public class BrontoBurtBehavior : EnemyBehavior
{
    [Header("Trigger Settings")]
    public float triggerRadius = 3f;         // Detection radius
    public float riseSpeed = 3f;             // Vertical rise speed

    [Header("Flight Behavior")]
    public EnemyBehavior flightLeft;         // BasicFlightBehavior going left
    public EnemyBehavior flightRight;        // BasicFlightBehavior going right

    public LayerMask groundLayer;            // Layers considered ground to block movement

    private bool playerInRange = false;
    private bool isRising = false;

    public override void Execute(Enemy enemy)
    {
        if (enemy == null || enemy.isBeingInhaled || enemy.isDead) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector3 enemyPos = enemy.transform.position;
        Vector3 playerPos = player.transform.position;

        // --- Check trigger radius ---
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
            return;
        }

        // --- Rising ---
        if (isRising)
        {
            float step = riseSpeed * Time.fixedDeltaTime;
            float newY = Mathf.MoveTowards(enemyPos.y, playerPos.y, step);
            enemy.transform.position = new Vector3(enemyPos.x, newY, enemyPos.z);

            if (Mathf.Approximately(newY, playerPos.y))
            {
                isRising = false;

                // --- Flip sprite to face player ---
                Vector3 scale = enemy.transform.localScale;
                scale.x = Mathf.Sign(playerPos.x - enemyPos.x) * Mathf.Abs(scale.x);
                enemy.transform.localScale = scale;

                // --- Assign flight behavior based on player's position ---
                if (playerPos.x < enemyPos.x && flightLeft != null)
                    enemy.behavior = flightLeft;
                else if (playerPos.x >= enemyPos.x && flightRight != null)
                    enemy.behavior = flightRight;

                // Set Animator float in parent
                Animator anim = enemy.GetComponentInParent<Animator>();
                if (anim != null)
                    anim.SetFloat("isFlight", 1f);
            }

            return; // Only rise until Y matched
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Enemy enemy = null;
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
