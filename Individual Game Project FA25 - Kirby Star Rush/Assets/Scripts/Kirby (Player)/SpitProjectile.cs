using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SpitProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] public Vector2 direction = Vector2.right;
    [SerializeField] private LayerMask collisionLayers;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 360f; // degrees per second

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true; // Rigidbody rotation frozen so we can rotate manually
    }

    private void Start()
    {
        rb.velocity = direction.normalized * speed;
    }

    private void Update()
    {
        // Rotate the projectile continuously
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

        // Destroy after 5 seconds to prevent leftover objects
        Destroy(gameObject, 5f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            // Try to get BasicObject component
            BasicObject destructible = collision.gameObject.GetComponent<BasicObject>();
            if (destructible != null)
            {
                destructible.TakeDamage(1);
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Also handle trigger colliders
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            BasicObject destructible = other.GetComponent<BasicObject>();
            if (destructible != null)
            {
                destructible.TakeDamage(1);
            }

            Destroy(gameObject);
        }
    }
}
