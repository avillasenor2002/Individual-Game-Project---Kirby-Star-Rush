using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP = 6;
    public int currentHP;

    [Header("Damage Settings")]
    public float invincibilityDuration = 1.5f;
    public float flickerSpeed = 0.1f;
    public float knockbackForce = 5f;

    [Header("Audio")]
    public AudioClip damageSound;
    public float damageVolume = 0.8f;

    [Header("References")]
    private HPDisplay hpDisplay;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private CameraShake cameraShake;
    private Collider2D mainCollider;

    private bool isInvincible = false;

    // Singleton instance
    public static PlayerHealth Instance;

    private void Awake()
    {
        // Implement Singleton and persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Ensure we have an AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Subscribe to sceneLoaded to refresh HPDisplay
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Find initial HPDisplay and CameraShake in scene
        FindSceneReferences();

        // Initialize HP if first instance
        if (currentHP <= 0)
            currentHP = maxHP;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh references when new scene loads
        FindSceneReferences();
        UpdateHPDisplay();
    }

    private void FindSceneReferences()
    {
        hpDisplay = FindObjectOfType<HPDisplay>();
        if (hpDisplay == null)
            Debug.LogWarning("[PlayerHealth] Could not find HPDisplay in scene!");

        cameraShake = FindObjectOfType<CameraShake>();
        if (cameraShake == null)
            Debug.LogWarning("[PlayerHealth] Could not find CameraShake in scene!");
    }

    private void UpdateHPDisplay()
    {
        if (hpDisplay != null)
            hpDisplay.UpdateHPDisplay(currentHP, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.otherCollider != mainCollider) return;

        Enemy enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null)
        {
            Vector2 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector2.zero;
            TakeDamage(enemy.contactDamage, hitNormal);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!mainCollider || !mainCollider.enabled) return;
        if (other.GetComponentInParent<PlayerHealth>() == this) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null && mainCollider.bounds.Intersects(other.bounds))
        {
            Vector2 hitNormal = (transform.position - other.transform.position).normalized;
            TakeDamage(enemy.contactDamage, hitNormal);
        }
    }

    public void TakeDamage(int damage, Vector2 hitNormal)
    {
        if (isInvincible || currentHP <= 0)
            return;

        currentHP -= damage;
        if (currentHP < 0)
            currentHP = 0;

        UpdateHPDisplay();

        // Knockback
        rb.velocity = Vector2.zero;
        rb.AddForce(hitNormal * knockbackForce, ForceMode2D.Impulse);

        // Play sound
        if (damageSound != null)
            audioSource.PlayOneShot(damageSound, damageVolume);

        // Camera shake
        if (cameraShake != null)
            cameraShake.Shake(0.2f, 0.3f);

        StartCoroutine(InvincibilityRoutine());

        if (currentHP <= 0)
            Die();
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(flickerSpeed);
            elapsed += flickerSpeed;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Player has died!");
        // TODO: Add respawn or restart logic
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        UpdateHPDisplay();
    }
}
