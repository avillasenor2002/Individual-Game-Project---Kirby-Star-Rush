using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DeadPlayer : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 360f; // degrees per second
    [SerializeField] private bool isSpintime = false;

    [Header("Death Physics Settings")]
    [SerializeField] private float freezeDuration = 1f; // seconds to freeze position
    [SerializeField] private float upwardForce = 2f; // small initial upward force

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (rb != null)
        {
            // Freeze X and Y immediately
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

            // Start coroutine to unfreeze after delay
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        // Wait while frozen
        yield return new WaitForSeconds(freezeDuration);

        // Unfreeze X and Y but keep rotation frozen for now
        rb.constraints = RigidbodyConstraints2D.None;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Add a small upward force
        rb.velocity = new Vector2(rb.velocity.x, 0f); // reset vertical velocity
        rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Impulse);

        // Enable spinning
        isSpintime = true;
    }

    private void Update()
    {
        if (isSpintime)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }
}
