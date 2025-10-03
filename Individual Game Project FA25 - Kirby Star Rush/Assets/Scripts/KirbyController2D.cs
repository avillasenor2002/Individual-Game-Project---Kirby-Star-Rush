using UnityEngine;
using System.Collections;

public class KirbyController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float floatMaxSpeed = 2f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float jumpForce = 400f;
    [SerializeField] private float puffForce = 200f;
    [SerializeField] private float floatFallSpeed = 1f;
    [SerializeField] private float maxFallSpeed = -10f;

    [Header("Ground Checks")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;
    const float groundCheckRadius = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip floatStartSound;
    [SerializeField] private AudioClip flutterSound;
    [SerializeField] private AudioClip deflateSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Colliders")]
    [SerializeField] private Collider2D normalCollider;
    [SerializeField] private Collider2D floatCollider;

    private Rigidbody2D rb;
    private Animator animator;

    private bool grounded;
    private bool wasGrounded;
    private bool landingSoundPlayed;
    private bool facingRight = true;
    private bool isFloating = false;
    private bool floatStartedSoundPlayed = false;

    private float currentMaxSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentMaxSpeed = maxSpeed;

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Ensure only the normal collider starts enabled
        if (normalCollider != null) normalCollider.enabled = true;
        if (floatCollider != null) floatCollider.enabled = false;
    }

    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);

        if (jumpPressed)
            HandleJump();

        if (Input.GetMouseButtonDown(0) && isFloating)
            StopFloating();

        Move(moveInput);

        animator.SetBool("isRunning", Mathf.Abs(rb.velocity.x) > 0.1f && grounded);
    }

    private void FixedUpdate()
    {
        // Update grounded state
        wasGrounded = grounded;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundMask);
        grounded = colliders.Length > 0;

        if (!wasGrounded && grounded && !landingSoundPlayed)
            PlayLandingSound();

        if (grounded && isFloating)
            StopFloating();

        float targetMax = isFloating ? floatMaxSpeed : maxSpeed;
        currentMaxSpeed = Mathf.Lerp(currentMaxSpeed, targetMax, Time.fixedDeltaTime * 5f);

        if (isFloating && !grounded)
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -floatFallSpeed));

        if (!isFloating && rb.velocity.y < maxFallSpeed)
            rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
    }

    private void Move(float moveInput)
    {
        float targetSpeed = moveInput * currentMaxSpeed;
        float speedDif = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDif * accelRate * Time.deltaTime;

        rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();
    }

    private void HandleJump()
    {
        if (grounded)
        {
            rb.AddForce(new Vector2(0f, jumpForce));
            animator.SetBool("isJumping", true);

            if (jumpSound != null)
                audioSource.PlayOneShot(jumpSound);
        }
        else
        {
            if (!isFloating)
            {
                StartFloating();
            }
            else
            {
                // Air flutter
                rb.AddForce(new Vector2(0f, puffForce));
                PlayFlutter();
            }
        }
    }

    private void StartFloating()
    {
        isFloating = true;
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        animator.SetBool("isInflating", true);
        animator.SetBool("isFloating", false);

        // Switch colliders
        if (normalCollider != null) normalCollider.enabled = false;
        if (floatCollider != null) floatCollider.enabled = true;

        if (floatStartSound != null && !floatStartedSoundPlayed)
        {
            audioSource.PlayOneShot(floatStartSound);
            floatStartedSoundPlayed = true;
        }

        StartCoroutine(FinishInflating());
    }

    private IEnumerator FinishInflating()
    {
        yield return new WaitForSeconds(0.15f); // inflate animation duration
        animator.SetBool("isInflating", false);
        animator.SetBool("isFloating", true);
    }

    private void StopFloating()
    {
        if (isFloating)
        {
            isFloating = false;
            floatStartedSoundPlayed = false;

            animator.SetBool("isDeflating", true);
            animator.SetBool("isFloating", false);

            // Switch colliders back
            if (normalCollider != null) normalCollider.enabled = true;
            if (floatCollider != null) floatCollider.enabled = false;

            if (deflateSound != null)
                audioSource.PlayOneShot(deflateSound);

            StartCoroutine(FinishDeflating());
        }
    }

    private IEnumerator FinishDeflating()
    {
        yield return new WaitForSeconds(0.15f); // deflate animation duration
        animator.SetBool("isDeflating", false);
    }

    private void PlayFlutter()
    {
        animator.SetBool("isFluttering", true);

        if (flutterSound != null)
            audioSource.PlayOneShot(flutterSound);

        StartCoroutine(FinishFlutter());
    }

    private IEnumerator FinishFlutter()
    {
        yield return new WaitForSeconds(0.1f); // flutter animation duration
        animator.SetBool("isFluttering", false);
    }

    private void PlayLandingSound()
    {
        landingSoundPlayed = true;
        animator.SetBool("isJumping", false);
        StartCoroutine(LandAnimation());

        if (landSound != null)
            audioSource.PlayOneShot(landSound);

        StartCoroutine(ResetLandingSoundFlag());
    }

    private IEnumerator ResetLandingSoundFlag()
    {
        yield return new WaitForSeconds(0.2f);
        landingSoundPlayed = false;
    }

    private IEnumerator LandAnimation()
    {
        animator.SetBool("isLanding", true);
        yield return new WaitForSeconds(0.1f);
        animator.SetBool("isLanding", false);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
