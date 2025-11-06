using UnityEngine;

[CreateAssetMenu(fileName = "EnemyPatrolBehavior", menuName = "Enemy/Behaviors/Patrol")]
public class EnemyPatrolBehavior : EnemyBehavior
{
    [Header("Patrol Settings")]
    public float moveSpeed = 2f;
    public float wallCheckDistance = 0.1f;
    public float ledgeCheckDistance = 0.2f;

    public override void Execute(Enemy enemy)
    {
        if (enemy == null || enemy.isBeingInhaled || enemy.isDead)
            return;

        // Runtime data per enemy
        EnemyPatrolData patrolData = enemy.GetComponent<EnemyPatrolData>();
        if (patrolData == null)
        {
            patrolData = enemy.gameObject.AddComponent<EnemyPatrolData>();
            patrolData.direction = 1f;
        }

        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null) return;

        Bounds bounds = col.bounds;

        // --- Wall detection ---
        Vector2 wallOrigin = new Vector2(bounds.center.x + patrolData.direction * bounds.extents.x, bounds.center.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(wallOrigin, new Vector2(wallCheckDistance, bounds.size.y * 0.9f), 0f);
        bool hitWall = false;
        foreach (var h in hits)
        {
            if (h != col && !h.isTrigger) // ignore self and triggers
            {
                hitWall = true;
                break;
            }
        }

        // --- Ledge detection ---
        Vector2 ledgeOrigin = new Vector2(bounds.center.x + patrolData.direction * bounds.extents.x, bounds.min.y);
        RaycastHit2D ledgeHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDistance);
        bool noGroundAhead = (ledgeHit.collider == null || ledgeHit.collider.isTrigger);

        // --- Flip only if wall or ledge ---
        if (hitWall || noGroundAhead)
        {
            patrolData.direction *= -1f;
        }

        // --- Move enemy ---
        enemy.transform.position += new Vector3(patrolData.direction * moveSpeed * Time.fixedDeltaTime, 0f, 0f);

        // --- Sprite faces movement ---
        Vector3 scale = enemy.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(patrolData.direction);
        enemy.transform.localScale = scale;
    }

    public class EnemyPatrolData : MonoBehaviour
    {
        [HideInInspector] public float direction = 1f;
    }
}
