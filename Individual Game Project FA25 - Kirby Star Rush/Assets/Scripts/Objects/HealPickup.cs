using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class HealPickup : MonoBehaviour
{
    [Header("Healing Settings")]
    public int healAmount = 1;               // Amount of HP restored
    public bool destroyOnUse = true;

    [Header("Tilemap Settings")]
    public Tilemap targetTilemap;
    public bool snapToTilemap = true;

    [Header("Audio & Effects")]
    public AudioClip pickupSound;
    public GameObject pickupEffect;          // Optional particle effect

    private Collider2D objectCollider;
    private AudioSource levelAudioSource;

    private void Awake()
    {
        objectCollider = GetComponent<Collider2D>();
        objectCollider.isTrigger = true;

        // Find a scene-wide AudioSource to play the pickup sound
        levelAudioSource = FindObjectOfType<AudioSource>();
        if (levelAudioSource == null)
        {
            Debug.LogWarning("[HealPickup] No AudioSource found in scene for playing pickup sound!");
        }

        if (targetTilemap == null)
            FindMainLevelTilemap();
    }

    private void Start()
    {
        if (snapToTilemap && targetTilemap)
            SnapToTilemap();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && snapToTilemap && targetTilemap)
            SnapToTilemap();
    }
#endif

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player != null)
        {
            // Heal the player one HP at a time to trigger HP display animation & sound
            for (int i = 0; i < healAmount; i++)
            {
                player.Heal(1);  // PlayerHealth will update the HP display

                // Play pickup sound for each HP restored
                if (pickupSound != null && levelAudioSource != null)
                {
                    levelAudioSource.PlayOneShot(pickupSound, 0.8f);
                }
            }

            // Spawn particle effect
            if (pickupEffect != null)
            {
                GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                    Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
                else
                    Destroy(effect, 2f);
            }

            if (destroyOnUse)
                Destroy(gameObject);
        }
    }

    private void SnapToTilemap()
    {
        if (targetTilemap == null) return;

        Vector3Int cellPos = targetTilemap.WorldToCell(transform.position);
        transform.position = targetTilemap.GetCellCenterWorld(cellPos);
    }

    private void FindMainLevelTilemap()
    {
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap map in tilemaps)
        {
            if (map.gameObject.name == "Main Level")
            {
                targetTilemap = map;
                return;
            }
        }

        Debug.LogWarning("[HealPickup] Could not find a Tilemap named 'Main Level' in the scene.");
    }
}
