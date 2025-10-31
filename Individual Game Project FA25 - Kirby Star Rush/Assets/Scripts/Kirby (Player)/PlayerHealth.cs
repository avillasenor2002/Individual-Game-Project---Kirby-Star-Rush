using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Death Sequence Settings")]
    public AudioClip deathSound;
    public AudioClip postDeathSound;
    public float deathShakeIntensity = 0.5f;
    public float deathShakeDuration = 0.5f;
    public float postDeathDelay = 1.2f;
    public GameObject deadPlayerPrefab;

    [Header("Scene Reload Settings")]
    [Tooltip("If left empty, reloads the current scene on death.")]
    public string sceneToLoadOnDeath = "";

    private HPDisplay hpDisplay;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private CameraFollow2D cameraFollow;
    private Collider2D mainCollider;

    private bool isInvincible = false;
    private bool isDying = false;
    private bool positionLocked = false;

    public static PlayerHealth Instance;

    private void Awake()
    {
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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        FindSceneReferences();

        if (currentHP <= 0)
            currentHP = maxHP;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindSceneReferences();
        UpdateHPDisplay();

        // Restore control only if previously locked
        if (positionLocked)
        {
            positionLocked = false;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Keep Z rotation locked
            }
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            if (mainCollider != null)
                mainCollider.enabled = true;
        }
    }

    private void FindSceneReferences()
    {
        hpDisplay = FindObjectOfType<HPDisplay>();
        if (hpDisplay == null)
            Debug.LogWarning("[PlayerHealth] Could not find HPDisplay in scene!");

        cameraFollow = FindObjectOfType<CameraFollow2D>();
        if (cameraFollow == null)
            Debug.LogWarning("[PlayerHealth] Could not find CameraFollow2D in scene!");
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
        if (isInvincible || currentHP <= 0 || isDying)
            return;

        currentHP -= damage;
        if (currentHP < 0)
            currentHP = 0;

        UpdateHPDisplay();

        rb.velocity = Vector2.zero;
        rb.AddForce(hitNormal * knockbackForce, ForceMode2D.Impulse);

        if (damageSound != null)
            audioSource.PlayOneShot(damageSound, damageVolume);

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
            // Stop flickering immediately if dying
            if (isDying)
            {
                if (spriteRenderer != null)
                    spriteRenderer.enabled = false;
                yield break;
            }

            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(flickerSpeed);
            elapsed += flickerSpeed;
        }

        if (!isDying && spriteRenderer != null)
            spriteRenderer.enabled = true;

        isInvincible = false;
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;

        Debug.Log("[PlayerHealth] Player has died!");
        StopAllCoroutines(); // stop invincibility flicker or other ongoing routines
        if (spriteRenderer != null)
            spriteRenderer.enabled = false; // keep hidden
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Fully lock position
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        positionLocked = true;

        if (mainCollider != null)
            mainCollider.enabled = false;

        // Mute all other sounds
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allSources)
        {
            if (source != null && source != audioSource)
                source.mute = true;
        }

        // Play first death sound
        if (deathSound != null)
            audioSource.PlayOneShot(deathSound, 1f);

        // Shake camera
        if (cameraFollow != null)
            StartCoroutine(ShakeCamera(cameraFollow.transform, deathShakeIntensity, deathShakeDuration));

        // Ensure sprite stays off during sequence
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        // Spawn dead prefab
        if (deadPlayerPrefab != null)
            Instantiate(deadPlayerPrefab, transform.position, Quaternion.identity);

        // Show DEAD UI
        Image deadImage = FindDeadUIImage();
        if (deadImage != null)
        {
            Color c = deadImage.color;
            c.a = 200f / 255f;
            deadImage.color = c;
        }

        // Wait before post-death sound
        yield return new WaitForSeconds(postDeathDelay);

        // Play post-death sound and wait until finished
        if (postDeathSound != null)
        {
            audioSource.PlayOneShot(postDeathSound, 1f);
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        // Reload scene
        StartCoroutine(RestartLevelAfterDeath());
    }

    private Image FindDeadUIImage()
    {
        Image[] allImages = FindObjectsOfType<Image>(true);
        foreach (Image img in allImages)
        {
            if (img.gameObject.name == "DEAD")
                return img;
        }
        return null;
    }

    private IEnumerator ShakeCamera(Transform camTransform, float intensity, float duration)
    {
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * intensity;
            float offsetY = Random.Range(-1f, 1f) * intensity;
            camTransform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        camTransform.localPosition = originalPos;
    }

    private IEnumerator RestartLevelAfterDeath()
    {
        if (GlobalVariables.Instance != null)
            GlobalVariables.Instance.levelStart = true;

        string targetScene = string.IsNullOrEmpty(sceneToLoadOnDeath)
            ? SceneManager.GetActiveScene().name
            : sceneToLoadOnDeath;

        yield return null;
        SceneManager.LoadScene(targetScene);
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        UpdateHPDisplay();
    }
}
