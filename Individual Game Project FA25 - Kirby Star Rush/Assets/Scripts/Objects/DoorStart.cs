using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[ExecuteAlways]
public class DoorStart : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private Vector2 playerSpawnPosition; // Player spawn in next scene
    [SerializeField] private AudioClip doorSound;

    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private bool snapToTilemap = true;

    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange = false;
    private bool playerJustSpawned = false;
    private GameObject playerRef;
    private KirbyInputSender inputSender;

    // Remember spawn position across scenes
    private static Vector2? spawnPositionNextScene = null;

    private void Awake()
    {
        if (snapToTilemap)
            SnapToTilemap();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        if (targetTilemap == null)
            FindMainTilemap();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        if (!Application.isPlaying || !playerInRange || playerRef == null || playerJustSpawned)
            return;

        if (inputSender != null && inputSender.movementAction != null)
        {
            Vector2 moveInput = inputSender.movementAction.action.ReadValue<Vector2>();
            if (moveInput.y > 0.5f)
                StartCoroutine(TransitionSequence());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            playerRef = other.gameObject;
            inputSender = playerRef.GetComponent<KirbyInputSender>();

            if (playerJustSpawned)
                playerJustSpawned = false;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            playerRef = null;
            inputSender = null;
        }
    }

    private IEnumerator TransitionSequence()
    {
        if (playerRef == null)
            yield break;

        // Play door sound
        if (doorSound != null)
            AudioSource.PlayClipAtPoint(doorSound, transform.position);

        // Set global variable to mark that the level has started
        if (GlobalVariables.Instance != null)
        {
            GlobalVariables.Instance.levelStart = true;
            Debug.Log("[DoorStart] GlobalVariables.levelStart set to TRUE");
        }

        // Fade out
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FadeOut();

        // Save spawn position for next scene
        spawnPositionNextScene = playerSpawnPosition;

        // Destroy the player that entered the door
        Destroy(playerRef);

        // **Clear all DontDestroyOnLoad objects except GlobalVariables**
        ClearDontDestroyOnLoadExceptGlobalVariables();

        // Load the next scene
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// Destroys all DontDestroyOnLoad objects except the singleton GlobalVariables
    /// </summary>
    private void ClearDontDestroyOnLoadExceptGlobalVariables()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            if (obj != GlobalVariables.Instance?.gameObject && obj.hideFlags == HideFlags.None)
            {
                // Check if this object is marked DontDestroyOnLoad by checking its scene
                if (obj.scene.name == null || obj.scene.name == "")
                {
                    Destroy(obj);
                    Debug.Log($"Destroyed DontDestroyOnLoad object: {obj.name}");
                }
            }
        }
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Move player in new scene to the saved position
        if (spawnPositionNextScene.HasValue)
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player != null)
            {
                player.transform.position = spawnPositionNextScene.Value;

                DoorStart[] allDoors = FindObjectsOfType<DoorStart>();
                foreach (DoorStart d in allDoors)
                {
                    float distance = Vector2.Distance(player.transform.position, d.transform.position);
                    if (distance < 1f)
                        d.playerJustSpawned = true;
                }
            }
            spawnPositionNextScene = null;
        }

        // Trigger any LevelStartManagers in the new scene
        LevelStartManager[] startManagers = FindObjectsOfType<LevelStartManager>();
        foreach (LevelStartManager manager in startManagers)
        {
            manager.SceneStart();
            Debug.Log($"[{name}] Triggered SceneStart() on {manager.name}");
        }

        // Set global variable to mark that the level has started
        if (GlobalVariables.Instance != null)
        {
            GlobalVariables.Instance.levelStart = true;
            Debug.Log("[DoorStart] GlobalVariables.levelStart set to TRUE");
        }

        // Fade back in
        if (SceneFader.Instance != null)
            SceneFader.Instance.StartCoroutine(SceneFader.Instance.FadeIn());
    }

    private void SnapToTilemap()
    {
        if (targetTilemap == null)
            return;

        Vector3Int cellPos = targetTilemap.WorldToCell(transform.position);
        Vector3 snappedPos = targetTilemap.GetCellCenterWorld(cellPos);
        transform.position = snappedPos;
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
}
