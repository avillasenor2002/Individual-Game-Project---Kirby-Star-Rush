using UnityEngine;
using System.Collections;

public class KirbyController2D_InputSystem : MonoBehaviour
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

    [Header("Particles")]
    [SerializeField] private ParticleSystem landParticles;
    [SerializeField] private ParticleSystem runParticles;
    [SerializeField] private ParticleSystem deflateParticles; // deflate particle prefab

    private bool jumpDisabled = false;
    private float jumpDisableTimer = 0f;

    private Rigidbody2D rb;
    private Animator animator;

    private bool grounded;
    private bool wasGrounded;
    private bool landingSoundPlayed;
    private bool facingRight = true;
    private bool isFloating = false;
    private bool floatStartedSoundPlayed = false;

    private float currentMaxSpeed;
    private Vector2 moveInputValue = Vector2.zero;

    private ParticleSystem activeRunParticles;

    public bool canFloat = true;
    public bool isFacingRight { get; private set; } = true;


    // NEW: Speed multiplier for external scripts (like inhale)
    private float speedMultiplier = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentMaxSpeed = maxSpeed;

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (normalCollider != null) normalCollider.enabled = true;
        if (floatCollider != null) floatCollider.enabled = false;

        if (runParticles != null)
        {
            activeRunParticles = Instantiate(runParticles, transform);
            activeRunParticles.Stop();
        }
    }

    private void Update()
    {
        Move(moveInputValue);
        animator.SetBool("isRunning", Mathf.Abs(rb.velocity.x) > 0.1f && grounded);
        HandleRunParticles();

        // If jump is disabled, count down
        if (jumpDisabled)
        {
            jumpDisableTimer -= Time.deltaTime;
            if (jumpDisableTimer <= 0f)
            {
                jumpDisabled = false;
            }
        }

    }

    public void DisableJumpTemporarily(float duration = 0.2f)
    {
        jumpDisabled = true;
        jumpDisableTimer = duration;
    }

    private void FixedUpdate()
    {
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

    private void Move(Vector2 input)
    {
        float moveInput = input.x;
        float targetSpeed = moveInput * currentMaxSpeed * speedMultiplier; // APPLY SPEED MULTIPLIER
        float speedDif = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDif * accelRate * Time.deltaTime;

        rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        if (moveInput > 0) isFacingRight = true;
        else if (moveInput < 0) isFacingRight = false;
    }

    private void HandleRunParticles()
    {
        if (activeRunParticles == null) return;

        if (grounded && Mathf.Abs(rb.velocity.x) > 0.1f && !isFloating)
        {
            if (!activeRunParticles.isPlaying)
                activeRunParticles.Play();

            activeRunParticles.transform.position = new Vector3(groundCheck.position.x, groundCheck.position.y, activeRunParticles.transform.position.z);
        }
        else
        {
            if (activeRunParticles.isPlaying)
                activeRunParticles.Stop();
        }
    }

    // INPUT SYSTEM MESSAGE METHODS
    public void onMovement(Vector2 input) => moveInputValue = input;

    public void onJump()
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
                StartFloating();
            else
            {
                rb.AddForce(new Vector2(0f, puffForce));
                PlayFlutter();
            }
        }
    }

    public void onSpecialAction()
    {
        if (isFloating)
            StopFloating();
    }

    private void StartFloating()
    {
        if (!canFloat) return;
        isFloating = true;
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        animator.SetBool("isInflating", true);
        animator.SetBool("isFloating", false);

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
        yield return new WaitForSeconds(0.15f);
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

            if (normalCollider != null) normalCollider.enabled = true;
            if (floatCollider != null) floatCollider.enabled = false;

            if (deflateSound != null)
                audioSource.PlayOneShot(deflateSound);

            if (deflateParticles != null)
            {
                Vector3 particleScale = deflateParticles.transform.localScale;
                particleScale.x = facingRight ? Mathf.Abs(particleScale.x) : -Mathf.Abs(particleScale.x);
                deflateParticles.transform.localScale = particleScale;

                deflateParticles.Play();
            }

            StartCoroutine(FinishDeflating());
        }
    }

    private IEnumerator FinishDeflating()
    {
        yield return new WaitForSeconds(0.15f);
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
        yield return new WaitForSeconds(0.1f);
        animator.SetBool("isFluttering", false);
    }

    private void PlayLandingSound()
    {
        landingSoundPlayed = true;
        animator.SetBool("isJumping", false);
        StartCoroutine(LandAnimation());

        if (landSound != null)
            audioSource.PlayOneShot(landSound);

        if (landParticles != null)
        {
            ParticleSystem particles = Instantiate(landParticles, groundCheck.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }

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

    // Update it whenever the player changes direction
    private void UpdateFacingDirection(float moveInput)
    {
        if (moveInput > 0) isFacingRight = true;
        else if (moveInput < 0) isFacingRight = false;
    }


    // ------------------- NEW: speed multiplier -------------------
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    public float CurrentJumpForce
    {
        get { return jumpForce; }
        set { jumpForce = value; }
    }

    public float BaseMaxSpeed
    {
        get { return maxSpeed; }
    }
}
