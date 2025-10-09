using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways] // Runs in Edit mode too, so you can see snapping in editor
public class BasicObject : MonoBehaviour
{
    [Header("Block Settings")]
    public int health = 1;
    public GameObject destroyEffect;
    public AudioClip destroySound;
    public bool destroyOnCollision = false;
    public string destroyTag = "PlayerAttack";

    [Header("Tilemap Settings")]
    public Tilemap targetTilemap;
    public bool snapToTilemap = true;

    [HideInInspector]
    public bool isBeingInhaled = false;       // Tracks if object is currently inhaled

    private AudioSource audioSource;
    private bool destroyed = false;
    private Collider2D objectCollider;

    private void Awake()
    {
        if (targetTilemap == null)
            FindMainLevelTilemap();

        objectCollider = GetComponent<Collider2D>();
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (destroyOnCollision && !destroyed)
            TakeDamage(1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!destroyed && other.CompareTag(destroyTag))
            TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (destroyed) return;

        health -= amount;
        if (health <= 0)
            DestroyBlock();
    }

    private void DestroyBlock()
    {
        destroyed = true;

        if (destroyEffect)
            Instantiate(destroyEffect, transform.position, Quaternion.identity);

        if (destroySound)
        {
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.PlayOneShot(destroySound);
            Destroy(gameObject, destroySound.length);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SnapToTilemap()
    {
        if (targetTilemap == null) return;

        Vector3Int cellPos = targetTilemap.WorldToCell(transform.position);
        Vector3 snappedPos = targetTilemap.GetCellCenterWorld(cellPos);
        transform.position = snappedPos;
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

        Debug.LogWarning("DestructibleBlock could not find a Tilemap named 'Main Level' in the scene.");
    }

    // ===================== Inhale Helpers =====================

    public void StartBeingInhaled()
    {
        isBeingInhaled = true;

        // Disable collider so it doesn't interfere with Kirby
        if (objectCollider != null)
            objectCollider.enabled = false;
    }

    public void StopBeingInhaled()
    {
        isBeingInhaled = false;

        // Re-enable collider
        if (objectCollider != null)
            objectCollider.enabled = true;

        // Optionally snap back to tilemap
        if (snapToTilemap && targetTilemap)
            SnapToTilemap();
    }
}
