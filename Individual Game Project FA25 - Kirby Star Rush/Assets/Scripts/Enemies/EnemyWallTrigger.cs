using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyWallTrigger : MonoBehaviour
{
    public EnemyPatrolBehavior patrolBehavior;
    private Enemy enemy;

    private void Awake()
    {
        // Find the parent Enemy script
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
            Debug.LogError("EnemyWallTrigger: No parent Enemy found!");
    }
}
