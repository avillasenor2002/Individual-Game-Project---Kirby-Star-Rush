using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[ExecuteAlways]
public class Door : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private Vector2 playerSpawnPosition; // where player appears in new scene

    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private bool snapToTilemap = true;

    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange = false;
    private bool playerJustSpawned = false;
    private GameObject playerRef;

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

        // Detect "up" input from any source
        bool upInput = false;

        if (Keyboard.current != null)
            upInput |= Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame;

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            upInput |= stick.y > 0.5f || Gamepad.current.dpad.up.wasPressedThisFrame;
        }

        if (upInput)
            LoadNextScene();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            playerRef = other.gameObject;

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
        }
    }

    private void LoadNextScene()
    {
        // Maintain all objects with tag "Constant"
        GameObject[] constants = GameObject.FindGameObjectsWithTag("Constant");
        foreach (GameObject obj in constants)
        {
            DontDestroyOnLoad(obj);
        }

        // Set spawn position for next scene
        spawnPositionNextScene = playerSpawnPosition;

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Remove duplicate "Constant" objects in new scene
        GameObject[] constants = GameObject.FindGameObjectsWithTag("Constant");
        foreach (GameObject obj in constants)
        {
            if (obj.scene.name == scene.name) continue; // skip newly created objects
            foreach (GameObject other in GameObject.FindGameObjectsWithTag("Constant"))
            {
                if (other != obj && other.name == obj.name)
                    Destroy(other);
            }
        }

        // Teleport player only when entering via a door
        if (spawnPositionNextScene.HasValue)
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player != null)
            {
                player.transform.position = spawnPositionNextScene.Value;

                // Lock doors near player to prevent instant re-trigger
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
