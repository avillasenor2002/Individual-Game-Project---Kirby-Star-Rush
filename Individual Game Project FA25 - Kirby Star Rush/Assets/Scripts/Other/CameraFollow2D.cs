using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target; // The GameObject to keep on screen
    [SerializeField] private Tilemap tilemap; // The Tilemap to determine bounds
    [SerializeField] private TileBase boundaryTile; // The tile used as the boundary marker

    [SerializeField] private float smoothTime = 0.2f; // Normal smooth follow time
    [SerializeField] private float edgeBuffer = 0.4f; // Buffer near screen edges before moving

    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private float leftBound, rightBound, topBound, bottomBound;

    private bool firstMoveDone = false; // Skip normal follow until initial rapid scroll is done
    private bool switchingTarget = false; // True when camera is moving fast to a new target
    private float fastSmoothTime = 0.02f;  // Extremely fast follow time

    void Start()
    {
        cam = Camera.main;
        CalculateTilemapBounds();
        StartCoroutine(RapidScrollToTarget());
    }

    void LateUpdate()
    {
        // --- Handle missing target ---
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
                switchingTarget = true; // trigger fast scroll to new target
            }
            else
            {
                return; // No player in scene
            }
        }

        if (!firstMoveDone) return; // Skip normal follow until initial rapid scroll completes

        FollowTarget();
    }

    private void FollowTarget()
    {
        float currentSmoothTime = smoothTime;

        if (switchingTarget)
            currentSmoothTime = fastSmoothTime; // use extremely fast scroll

        Vector3 targetPosition = target.position;
        Vector3 viewportPosition = cam.WorldToViewportPoint(targetPosition);
        Vector3 desiredPosition = transform.position;

        if (viewportPosition.x < edgeBuffer)
            desiredPosition.x = targetPosition.x - (cam.orthographicSize * cam.aspect);
        else if (viewportPosition.x > 1f - edgeBuffer)
            desiredPosition.x = targetPosition.x + (cam.orthographicSize * cam.aspect);

        if (viewportPosition.y < edgeBuffer)
            desiredPosition.y = targetPosition.y - cam.orthographicSize;
        else if (viewportPosition.y > 1f - edgeBuffer)
            desiredPosition.y = targetPosition.y + cam.orthographicSize;

        float cameraHalfWidth = cam.orthographicSize * cam.aspect;
        float cameraHalfHeight = cam.orthographicSize;

        float clampedX = Mathf.Clamp(desiredPosition.x, leftBound + cameraHalfWidth, rightBound - cameraHalfWidth);
        float clampedY = Mathf.Clamp(desiredPosition.y, bottomBound + cameraHalfHeight, topBound - cameraHalfHeight);

        Vector3 clampedPosition = new Vector3(clampedX, clampedY, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, clampedPosition, ref velocity, currentSmoothTime);

        // If we are close enough to the new target, restore normal smoothTime
        if (switchingTarget && Vector3.Distance(transform.position, clampedPosition) < 0.01f)
        {
            switchingTarget = false;
        }
    }

    private void CalculateTilemapBounds()
    {
        if (tilemap == null || boundaryTile == null)
        {
            Debug.LogError("Tilemap or boundary tile not assigned!");
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        bool boundsSet = false;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (tilemap.GetTile(pos) == boundaryTile)
            {
                Vector3 worldPos = tilemap.GetCellCenterWorld(pos);
                if (!boundsSet)
                {
                    leftBound = rightBound = worldPos.x;
                    bottomBound = topBound = worldPos.y;
                    boundsSet = true;
                }
                else
                {
                    leftBound = Mathf.Min(leftBound, worldPos.x);
                    rightBound = Mathf.Max(rightBound, worldPos.x);
                    bottomBound = Mathf.Min(bottomBound, worldPos.y);
                    topBound = Mathf.Max(topBound, worldPos.y);
                }
            }
        }

        if (!boundsSet)
        {
            Debug.LogError("No boundary tiles found in the tilemap!");
            return;
        }

        float tileSize = tilemap.cellSize.x; // Assuming square tiles
        leftBound += tileSize;
        rightBound -= tileSize;
        bottomBound += tileSize;
        topBound -= tileSize;
    }

    private IEnumerator RapidScrollToTarget()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (target == null) yield break;

        float elapsed = 0f;
        float rapidTime = 0.05f; // VERY FAST scroll
        Vector3 startPos = transform.position;

        float cameraHalfWidth = cam.orthographicSize * cam.aspect;
        float cameraHalfHeight = cam.orthographicSize;

        Vector3 targetPos = new Vector3(
            Mathf.Clamp(target.position.x, leftBound + cameraHalfWidth, rightBound - cameraHalfWidth),
            Mathf.Clamp(target.position.y, bottomBound + cameraHalfHeight, topBound - cameraHalfHeight),
            transform.position.z
        );

        while (elapsed < rapidTime)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / rapidTime);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.position = targetPos;
        firstMoveDone = true;
    }
}
