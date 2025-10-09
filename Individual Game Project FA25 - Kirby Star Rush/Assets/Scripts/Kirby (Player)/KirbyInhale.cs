using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KirbyInhale : MonoBehaviour
{
    [Header("Inhale Settings")]
    [SerializeField] private CapsuleCollider2D inhaleCollider;   // the Inhale Range (should be a child collider)
    [SerializeField] private float maxInhaleSpeed = 10f;          // maximum speed toward Kirby
    [SerializeField] private float destroyDistance = 0.05f;       // when object is considered "in" Kirby
    [SerializeField] private LayerMask inhalableLayer;           // layers of inhalable objects
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 1); // speed curve

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem inhaleParticles;
    [SerializeField] private AudioClip inhaleSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Player penalties (optional)")]
    [SerializeField] private float inhaleSpeedReduction = 0.5f;
    [SerializeField] private float reducedJumpForce = 200f;

    private KirbyController2D_InputSystem controller;
    private bool isInhaling = false;
    private bool inhaleLocked = false;
    private BasicObject currentInhaleTarget = null;
    private List<Collider2D> disabledColliders = new List<Collider2D>();
    private float originalMaxSpeed;
    private float originalJumpForce;
    private Transform kirbyCenter;

    // Track time since an object started being inhaled
    private float inhaleStartTime = 0f;

    private void Awake()
    {
        controller = GetComponentInParent<KirbyController2D_InputSystem>();
        if (controller == null)
            Debug.LogWarning("KirbyInhale: KirbyController2D_InputSystem not found on parent.");

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (inhaleCollider == null)
            inhaleCollider = GetComponent<CapsuleCollider2D>();

        kirbyCenter = transform.parent != null ? transform.parent : transform;

        if (animator == null)
            animator = GetComponentInParent<Animator>();
    }

    private void Update()
    {
        if (isInhaling)
            HandleInhalePull();
    }

    // ---------------- Public input methods ----------------

    public void StartInhaleIfNotFloating()
    {
        if (inhaleLocked) return;

        if (controller != null)
        {
            var isFloatingField = typeof(KirbyController2D_InputSystem)
                .GetField("isFloating", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isFloatingField != null)
            {
                bool isFloating = (bool)isFloatingField.GetValue(controller);
                if (isFloating) return;
            }
        }

        StartInhale();
    }

    public void HoldInhale()
    {
        if (isInhaling)
            HandleInhalePull();
    }

    public void StopInhalePublic()
    {
        if (!inhaleLocked)
            StopInhaleInternal();
    }

    // ---------------- Internal inhale ----------------

    private void StartInhale()
    {
        if (isInhaling) return;
        isInhaling = true;

        if (controller != null)
        {
            var maxSpeedField = typeof(KirbyController2D_InputSystem)
                .GetField("maxSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (maxSpeedField != null)
            {
                originalMaxSpeed = (float)maxSpeedField.GetValue(controller);
                maxSpeedField.SetValue(controller, originalMaxSpeed * inhaleSpeedReduction);
            }

            var jumpForceField = typeof(KirbyController2D_InputSystem)
                .GetField("jumpForce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (jumpForceField != null)
            {
                originalJumpForce = (float)jumpForceField.GetValue(controller);
                jumpForceField.SetValue(controller, reducedJumpForce);
            }
        }

        if (animator != null) animator.SetBool("isInhaling", true);
        if (inhaleParticles != null) inhaleParticles.Play();
        if (inhaleSound != null && audioSource != null) audioSource.PlayOneShot(inhaleSound);
    }

    private void StopInhaleInternal()
    {
        isInhaling = false;

        if (animator != null)
        {
            animator.SetBool("isInhaling", false);
            animator.SetTrigger("InhaleFail");
        }

        if (inhaleParticles != null && inhaleParticles.isPlaying)
            inhaleParticles.Stop();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (controller != null)
        {
            var maxSpeedField = typeof(KirbyController2D_InputSystem)
                .GetField("maxSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (maxSpeedField != null)
                maxSpeedField.SetValue(controller, originalMaxSpeed);

            var jumpForceField = typeof(KirbyController2D_InputSystem)
                .GetField("jumpForce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (jumpForceField != null)
                jumpForceField.SetValue(controller, originalJumpForce);
        }

        if (currentInhaleTarget != null)
        {
            RestoreDisabledColliders();
            currentInhaleTarget.StopBeingInhaled();
        }

        currentInhaleTarget = null;
        inhaleLocked = false;
        disabledColliders.Clear();
        inhaleStartTime = 0f;
    }

    // ---------------- Selection and movement ----------------

    private void HandleInhalePull()
    {
        if (currentInhaleTarget == null)
        {
            List<Collider2D> hits = new List<Collider2D>();
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(inhalableLayer);
            filter.useLayerMask = true;

            int found = inhaleCollider.OverlapCollider(filter, hits);

            if (found > 0)
            {
                BasicObject closest = null;
                float minDist = float.MaxValue;
                Vector3 centerPos = kirbyCenter != null ? kirbyCenter.position : transform.position;

                foreach (var hit in hits)
                {
                    if (hit == null) continue;
                    BasicObject obj = hit.GetComponent<BasicObject>();
                    if (obj == null || obj.isBeingInhaled) continue;

                    float d = Vector3.Distance(centerPos, obj.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = obj;
                    }
                }

                if (closest != null)
                {
                    currentInhaleTarget = closest;
                    currentInhaleTarget.StartBeingInhaled();

                    // disable colliders
                    disabledColliders.Clear();
                    Collider2D[] cols = currentInhaleTarget.GetComponentsInChildren<Collider2D>(true);
                    foreach (var c in cols)
                    {
                        if (c != null && c.enabled)
                        {
                            disabledColliders.Add(c);
                            c.enabled = false;
                        }
                    }

                    inhaleLocked = true;
                    inhaleStartTime = 0f; // reset curve timer
                }
            }
        }

        if (currentInhaleTarget != null)
        {
            inhaleStartTime += Time.deltaTime;

            Vector3 targetPos = kirbyCenter != null ? kirbyCenter.position : transform.position;

            // Evaluate curve (normalized time) to get speed multiplier
            float t = Mathf.Clamp01(inhaleStartTime); // assuming curve from 0 to 1 second
            float curveSpeed = speedCurve.Evaluate(t) * maxInhaleSpeed;

            currentInhaleTarget.transform.position = Vector3.MoveTowards(
                currentInhaleTarget.transform.position,
                targetPos,
                curveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(currentInhaleTarget.transform.position, targetPos) <= destroyDistance)
            {
                Destroy(currentInhaleTarget.gameObject);
                currentInhaleTarget = null;
                inhaleLocked = false;
                StopInhaleInternal();
            }
        }
    }

    private void RestoreDisabledColliders()
    {
        foreach (var c in disabledColliders)
        {
            if (c != null)
                c.enabled = true;
        }
        disabledColliders.Clear();
    }
}
