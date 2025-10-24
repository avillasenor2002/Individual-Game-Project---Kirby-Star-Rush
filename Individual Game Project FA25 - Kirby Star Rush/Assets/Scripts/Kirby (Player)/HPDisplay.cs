using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class HPDisplay : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap hpTilemap;

    [Header("HP Tiles")]
    [SerializeField] private Tile fullTile;   // Tile when HP point is filled
    [SerializeField] private Tile emptyTile;  // Tile when HP point is empty

    [Header("Display Settings")]
    [SerializeField] private Vector3Int startingCell = Vector3Int.zero; // Leftmost HP cell
    [SerializeField] private int maxHP = 6; // Number of HP points
    [SerializeField] private float tileUpdateDelay = 0.05f; // Delay per tile
    [SerializeField] private AudioClip healTileSound;       // Sound when a tile is restored
    [SerializeField] private float healTileVolume = 0.8f;

    private AudioSource audioSource;
    private Coroutine currentUpdateRoutine;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        // Auto-find HP tilemap if missing
        if (hpTilemap == null)
            FindHPTilemap();

        // Auto-find AudioSource in scene
        audioSource = FindObjectOfType<AudioSource>();
        if (audioSource == null && healTileSound != null)
        {
            Debug.LogWarning("[HPDisplay] Could not find any AudioSource in scene for heal sounds!");
        }

        // Subscribe to sceneLoaded to update display
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh HP tilemap reference
        if (hpTilemap == null)
            FindHPTilemap();

        // Update player reference
        playerHealth = PlayerHealth.Instance;
        if (playerHealth != null)
        {
            UpdateHPDisplay(playerHealth.currentHP, false);
        }
    }

    /// <summary>
    /// Updates the HP tilemap display.
    /// </summary>
    /// <param name="currentHP">Current HP value.</param>
    /// <param name="isHealing">Set true only when restoring HP.</param>
    public void UpdateHPDisplay(int currentHP, bool isHealing = false)
    {
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (hpTilemap == null)
            return;

        if (currentUpdateRoutine != null)
            StopCoroutine(currentUpdateRoutine);

        currentUpdateRoutine = StartCoroutine(UpdateTilesRoutine(currentHP, isHealing));
    }

    private IEnumerator UpdateTilesRoutine(int currentHP, bool isHealing)
    {
        for (int i = 0; i < maxHP; i++)
        {
            Vector3Int cellPos = startingCell + new Vector3Int(i, 0, 0);
            Tile tileToSet = i < currentHP ? fullTile : emptyTile;

            hpTilemap.SetTile(cellPos, tileToSet);

            // Play heal sound only if HP is being restored
            if (isHealing && i < currentHP && healTileSound != null && audioSource != null)
                audioSource.PlayOneShot(healTileSound, healTileVolume);

            yield return new WaitForSeconds(tileUpdateDelay);
        }
    }

    /// <summary>
    /// Clears all HP tiles (optional).
    /// </summary>
    public void ClearHPDisplay()
    {
        if (hpTilemap == null)
            return;

        for (int i = 0; i < maxHP; i++)
        {
            Vector3Int cellPos = startingCell + new Vector3Int(i, 0, 0);
            hpTilemap.SetTile(cellPos, null);
        }
    }

    /// <summary>
    /// Finds a tilemap in the scene named "HP" or tagged "HP".
    /// </summary>
    private void FindHPTilemap()
    {
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap map in allTilemaps)
        {
            if (map.gameObject.name == "HP" || map.CompareTag("HP"))
            {
                hpTilemap = map;
                Debug.Log("[HPDisplay] Found and assigned HP Tilemap.");
                return;
            }
        }
    }
}
