using UnityEngine;

[CreateAssetMenu(fileName = "EnemyPatrolTilemapBehavior", menuName = "Enemy/Behaviors/Tilemap Patrol")]
public class EnemyPatrolTilemapBehavior : EnemyBehavior
{
    [Header("Patrol Settings")]
    public float moveSpeed = 2f;              // Positive = right, Negative = left
    public LayerMask collisionMask;           // Assign walls & floor layers

    [Header("Check Settings")]
    public float groundCheckDistance = 0.2f;
    public float wallCheckDistance = 0.1f;

    public override void Execute(Enemy enemy)
    {
        if (enemy == null || enemy.isBeingInhaled) return;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        Collider2D col = enemy.GetComponent<Collider2D>();
        if (rb == null || col == null) return;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        float dir = Mathf.Sign(moveSpeed);
        Bounds bounds = col.bounds;

        // --- GROUND CHECK ---
        Vector2 groundCheckPos = new Vector2(bounds.center.x + dir * bounds.extents.x, bounds.min.y - 0.05f);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, groundCheckDistance, collisionMask);

        // --- WALL CHECK ---
        Vector2 wallCheckPos = new Vector2(bounds.center.x + dir * bounds.extents.x + 0.05f, bounds.center.y);
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheckPos, Vector2.right * dir, wallCheckDistance, collisionMask);

        // Debug rays
        Debug.DrawRay(groundCheckPos, Vector2.down * groundCheckDistance, Color.green);
        Debug.DrawRay(wallCheckPos, Vector2.right * dir * wallCheckDistance, Color.red);

        bool turnAround = false;
        if (groundHit.collider == null) turnAround = true;  // No ground ahead → turn
        if (wallHit.collider != null) turnAround = true;    // Wall ahead → turn

        if (turnAround)
        {
            // Swap movement direction
            moveSpeed *= -1f;

            // Flip sprite on X axis
            Vector3 scale = enemy.transform.localScale;
            scale.x = Mathf.Sign(moveSpeed) * Mathf.Abs(scale.x);
            enemy.transform.localScale = scale;

            return; // skip movement this frame
        }

        // Move enemy with Rigidbody2D velocity
        rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
    }
}
