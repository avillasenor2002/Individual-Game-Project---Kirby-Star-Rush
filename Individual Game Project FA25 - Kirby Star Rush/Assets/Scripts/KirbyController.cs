using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KirbyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float floatJumpForce = 4f; // smaller jump for float hops
    public float floatFallSpeed = 1f;

    [Header("Momentum Settings")]
    public float acceleration = 10f;
    public float deceleration = 15f;

    private Rigidbody2D rb;
    private float targetMove;
    private float currentMove;
    private bool isFloating;
    private bool canJump = false; // true if touching Ground
    private bool hasJumpedOnce = false; // to track if we’re airborne after initial jump

    [SerializeField] private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // --- Input (GetKey) ---
        targetMove = 0f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            targetMove = -1f;
            animator.SetBool("isRunning", true);
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) 
        {
            targetMove = 1f; 
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }


        // --- Momentum ---
        if (targetMove != 0f)
            currentMove = Mathf.MoveTowards(currentMove, targetMove, acceleration * Time.deltaTime);
        else
            currentMove = Mathf.MoveTowards(currentMove, 0f, deceleration * Time.deltaTime);

        rb.velocity = new Vector2(currentMove * moveSpeed, rb.velocity.y);

        // flip sprite horizontally based on movement
        if (Mathf.Abs(currentMove) > 0.01f)
            transform.localScale = new Vector3(Mathf.Sign(currentMove), 1f, 1f);

        // --- Jump (GetKeyDown) ---
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (canJump)
            {
                // full jump
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                canJump = false;
                hasJumpedOnce = true;
            }
            else if (hasJumpedOnce)
            {
                // smaller float jump if already in air
                rb.velocity = new Vector2(rb.velocity.x, floatJumpForce);
                isFloating = true;
            }
        }

        // --- Floating while holding jump ---
        if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) && isFloating)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -floatFallSpeed));
        }

        // stop floating on jump release
        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
            isFloating = false;
    }

    // When player touches any collider tagged "Ground", allow jump again
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            canJump = true;
            isFloating = false;
            hasJumpedOnce = false; // reset airborne state when grounded
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            canJump = false;
        }
    }
}
