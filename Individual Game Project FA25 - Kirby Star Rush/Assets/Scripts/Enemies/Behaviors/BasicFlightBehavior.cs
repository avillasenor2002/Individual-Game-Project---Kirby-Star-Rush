using UnityEngine;

[CreateAssetMenu(fileName = "BasicFlightBehavior", menuName = "Enemy/Behaviors/Basic Flight")]
public class BasicFlightBehavior : EnemyBehavior
{
    [Header("Movement Settings")]
    public float flySpeed = 3f;        // Horizontal speed
    public float bobAmplitude = 0.5f;  // How high/low the enemy bobs
    public float bobFrequency = 2f;    // Speed of the bobbing (cycles per second)

    private float bobOffset;           // Random offset for variety

    public override void Execute(Enemy enemy)
    {
        if (enemy == null || enemy.isBeingInhaled || enemy.isDead) return;

        // ✅ Only run this behavior if the enemy is visible on the player's screen
        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if (renderer == null || !renderer.isVisible) return;

        // Initialize random bob offset so enemies don't bob perfectly in sync
        if (bobOffset == 0f)
            bobOffset = Random.Range(0f, Mathf.PI * 2f);

        // Horizontal movement (always to the right in local scale)
        Vector3 move = Vector3.right * flySpeed * Time.deltaTime;

        // Vertical bobbing using sine wave
        float bob = Mathf.Sin((Time.time + bobOffset) * bobFrequency * Mathf.PI * 2f) * bobAmplitude * Time.deltaTime;
        move.y = bob;

        // Apply movement
        enemy.transform.position += move;
    }
}
