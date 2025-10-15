using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(PlatformEffector2D), typeof(Collider2D))]
[ExecuteAlways]
public class SemiSolidPlatform : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private bool snapToTilemap = true;

    [Header("Platform Settings")]
    [Tooltip("Angle in degrees below which collisions are ignored (default 180 = one-way up).")]
    [SerializeField] private float surfaceArc = 180f;

    [Tooltip("Vertical offset from the tile center. Adjust to raise or lower platform.")]
    [SerializeField] private float yOffset = 0f;

    [Tooltip("Layer used for the player. Ensure the player’s Rigidbody2D can collide properly.")]
    [SerializeField] private string playerLayerName = "Player";

    private PlatformEffector2D effector;

    private void Awake()
    {
        effector = GetComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = false;
        effector.surfaceArc = surfaceArc;
        effector.rotationalOffset = 0f;

        // Make sure collider is used by effector
        Collider2D col = GetComponent<Collider2D>();
        col.usedByEffector = true;

        if (snapToTilemap)
            SnapToTilemap();

        if (targetTilemap == null)
            FindMainTilemap();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && snapToTilemap)
            SnapToTilemap();
    }
#endif

    private void SnapToTilemap()
    {
        if (targetTilemap == null) return;

        Vector3Int cellPos = targetTilemap.WorldToCell(transform.position);
        Vector3 snappedPos = targetTilemap.GetCellCenterWorld(cellPos);
        transform.position = snappedPos + new Vector3(0f, yOffset, 0f);
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
