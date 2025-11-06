using UnityEngine;

[CreateAssetMenu(fileName = "BasicFlightBehavior", menuName = "Enemy/Behaviors/Basic Flight")]
public class BasicFlightBehavior : EnemyBehavior
{
    [Header("Movement Settings")]
    public float flySpeed = 3f;           // Horizontal speed
    public float bobAmplitude = 0.5f;     // How high/low the enemy bobs
    public float bobFrequency = 2f;       // Speed of the bobbing (cycles per second)
    public float wallCheckDistance = 0.1f;// Distance in front to check for obstacles
    public LayerMask obstacleMask;        // Layers considered obstacles

    [Header("Turn Settings")]
    public bool enableTurning = true;     // Toggle turning on collisions
    public bool enableSpriteFlip = true;  // Toggle sprite flipping

    [Header("Manual Direction (override automatic turning)")]
    public float manualDirection = 1f;    // Set to 1 = right, -1 = left, 0 = auto

    private float bobOffset;              // Random offset for variety

    public override void Execute(Enemy enemy)
    {
        if (enemy == null || enemy.isBeingInhaled || enemy.isDead) return;

        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if (renderer == null || !renderer.isVisible) return;

        if (bobOffset == 0f)
            bobOffset = Random.Range(0f, Mathf.PI * 2f);

        BasicFlightData data = enemy.GetComponent<BasicFlightData>();
        if (data == null)
            data = enemy.gameObject.AddComponent<BasicFlightData>();

        // Determine actual direction
        float moveDir = manualDirection != 0f ? Mathf.Sign(manualDirection) : data.direction;

        // --- Wall detection and turning ---
        if (enableTurning && manualDirection == 0f)
        {
            Collider2D col = enemy.GetComponent<Collider2D>();
            bool isTouching = false;
            if (col != null)
            {
                Vector2 origin = new Vector2(enemy.transform.position.x, enemy.transform.position.y);
                Vector2 dirVec = Vector2.right * data.direction;
                RaycastHit2D hit = Physics2D.Raycast(origin, dirVec, wallCheckDistance, obstacleMask);

                if (hit.collider != null)
                {
                    isTouching = true;
                    if (!data.wasTouching)
                    {
                        data.direction *= -1f;
                        data.wasTouching = true;
                    }
                }
            }

            if (!isTouching)
                data.wasTouching = false;
        }

        // Horizontal movement
        Vector3 move = Vector3.right * flySpeed * moveDir * Time.deltaTime;

        // Vertical bobbing
        float bob = Mathf.Sin((Time.time + bobOffset) * bobFrequency * Mathf.PI * 2f) * bobAmplitude * Time.deltaTime;
        move.y = bob;

        enemy.transform.position += move;

        // Flip sprite only if enabled
        if (enableSpriteFlip)
        {
            Vector3 scale = enemy.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveDir);
            enemy.transform.localScale = scale;
        }
    }
}

public class BasicFlightData : MonoBehaviour
{
    public float direction = 1f;       // 1 = right, -1 = left
    public bool wasTouching = false;   // True if enemy is currently touching a wall
}
