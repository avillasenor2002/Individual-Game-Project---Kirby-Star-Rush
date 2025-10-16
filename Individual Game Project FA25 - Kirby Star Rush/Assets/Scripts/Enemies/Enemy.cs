using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int health = 3;
    public int contactDamage = 1;
    public AudioClip deathSound;
    public GameObject deathEffect;

    [Header("Tilemap Settings")]
    public Tilemap targetTilemap;
    public bool snapToTilemap = true;

    [Header("Behavior")]
    public EnemyBehavior behavior;

    [HideInInspector] public bool isBeingInhaled = false;

    public bool isDead = false;
    private Collider2D col;
    private AudioSource audioSource;

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (targetTilemap == null)
            FindMainTilemap();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (snapToTilemap && targetTilemap && !isBeingInhaled)
            SnapToTilemap();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && snapToTilemap && targetTilemap && !isBeingInhaled)
            SnapToTilemap();
    }
#endif

    private void FixedUpdate()
    {
        if (Application.isPlaying && behavior != null && !isDead && !isBeingInhaled)
        {
            behavior.Execute(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"{name} damaged player for {contactDamage} HP!");
        }

        if (other.CompareTag("PlayerAttack") || other.CompareTag("InhaledProjectile"))
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Spawn death effect detached from enemy so it persists
        if (deathEffect)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            effect.transform.parent = null;

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 2f); // fallback
            }
        }

        // Play death sound
        if (deathSound)
        {
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.PlayOneShot(deathSound);
        }

        // Destroy enemy immediately
        Destroy(gameObject);
    }


    // ---------------- Tilemap Helpers ----------------

    private void SnapToTilemap()
    {
        if (targetTilemap == null) return;
        Vector3Int cell = targetTilemap.WorldToCell(transform.position);
        transform.position = targetTilemap.GetCellCenterWorld(cell);
    }

    private void FindMainTilemap()
    {
        Tilemap[] maps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap map in maps)
        {
            if (map.gameObject.name == "Main Level" || map.CompareTag("MainTilemap"))
            {
                targetTilemap = map;
                return;
            }
        }
    }

    // ---------------- Inhale Helpers ----------------

    public void StartBeingInhaled()
    {
        isBeingInhaled = true;
        if (col != null)
            col.enabled = false;
    }

    public void StopBeingInhaled()
    {
        isBeingInhaled = false;
        if (col != null)
            col.enabled = true;

        if (snapToTilemap && targetTilemap)
            SnapToTilemap();
    }

    /// <summary>
    /// Call this when Kirby pulls the enemy all the way in
    /// </summary>
    public void OnPulledIntoKirby()
    {
        // Trigger death/destroy
        Die();
    }
}
