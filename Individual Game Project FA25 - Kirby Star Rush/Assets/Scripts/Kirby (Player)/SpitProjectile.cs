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

    [Header("Destroy Effects")]
    [SerializeField] private GameObject destroyEffect; // Optional particle prefab
    [SerializeField] private AudioClip destroySound;   // Optional sound effect

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private bool isDestroying = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        rb.velocity = direction.normalized * speed;
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    private void HandleCollision(GameObject target)
    {
        if (isDestroying) return;

        // Damage BasicObject
        BasicObject destructible = target.GetComponent<BasicObject>();
        if (destructible != null)
            destructible.TakeDamage(1);

        // Damage Enemy
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
            enemy.TakeDamage(1);

        DestroyProjectile();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
            HandleCollision(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
            HandleCollision(other.gameObject);
    }

    private void DestroyProjectile()
    {
        if (isDestroying) return;
        isDestroying = true;

        // Spawn particle effect
        if (destroyEffect != null)
        {
            GameObject effect = Instantiate(destroyEffect, transform.position, Quaternion.identity);
            // Detach from projectile so it won't be destroyed with it
            effect.transform.parent = null;

            // If it's a particle system, ensure it auto-destroys after finishing
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // Fallback: destroy after 2 seconds if no ParticleSystem component
                Destroy(effect, 2f);
            }
        }

        // Play sound
        if (destroySound != null && audioSource != null)
            audioSource.PlayOneShot(destroySound);

        // Destroy projectile immediately
        Destroy(gameObject);
    }

}
