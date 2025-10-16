using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[ExecuteAlways]
public class Door : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private Vector2 playerSpawnPosition;
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && snapToTilemap)
        {
            SnapToTilemap();
            return;
        }
#endif
        if (!Application.isPlaying || !playerInRange || playerRef == null || playerJustSpawned)
            return;

        // Get input from KirbyInputSender (new Input System)
        if (inputSender != null && inputSender.movementAction != null)
        {
            Vector2 moveInput = inputSender.movementAction.action.ReadValue<Vector2>();

            // Check if pressing up
            if (moveInput.y > 0.5f)
            {
                StartCoroutine(TransitionSequence());
                return;
            }
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
        // Maintain all "Constant" objects
        GameObject[] constants = GameObject.FindGameObjectsWithTag("Constant");
        foreach (GameObject obj in constants)
            DontDestroyOnLoad(obj);

        // Play door sound
        if (doorSound != null)
            AudioSource.PlayClipAtPoint(doorSound, transform.position);

        // Fade out
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FadeOut();

        // Set next spawn position and load scene
        spawnPositionNextScene = playerSpawnPosition;
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Remove duplicate "Constant" objects
        GameObject[] constants = GameObject.FindGameObjectsWithTag("Constant");
        var seen = new HashSet<string>();
        foreach (GameObject obj in constants)
        {
            if (seen.Contains(obj.name))
                Destroy(obj);
            else
                seen.Add(obj.name);
        }

        // Teleport player only when entering via a door
        if (spawnPositionNextScene.HasValue)
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player != null)
            {
                player.transform.position = spawnPositionNextScene.Value;

                // Lock doors near player
                Door[] allDoors = FindObjectsOfType<Door>();
                foreach (Door d in allDoors)
                {
                    float distance = Vector2.Distance(player.transform.position, d.transform.position);
                    if (distance < 1f)
                        d.playerJustSpawned = true;
                }
            }

            spawnPositionNextScene = null;
        }

        // Fade back in
        if (SceneFader.Instance != null)
            SceneFader.Instance.StartCoroutine(SceneFader.Instance.FadeIn());
    }

    private void SnapToTilemap()
    {
        if (targetTilemap == null) return;

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
