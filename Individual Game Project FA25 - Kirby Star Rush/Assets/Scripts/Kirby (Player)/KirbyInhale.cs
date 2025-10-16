using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KirbyInhale : MonoBehaviour
{
    [Header("Inhale Settings")]
    [SerializeField] private CapsuleCollider2D inhaleCollider;
    [SerializeField] private float maxInhaleSpeed = 10f;
    [SerializeField] private float destroyDistance = 0.05f;
    [SerializeField] private LayerMask inhalableLayer;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 1);

    [Header("Hitbox Toggle")]
    [SerializeField] private Collider2D activeCollider;
    [SerializeField] private Collider2D inactiveCollider;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem inhaleParticles;
    [SerializeField] private AudioClip inhaleSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Player penalties")]
    [SerializeField] private float inhaleSpeedReduction = 0.5f;
    [SerializeField] private float mouthfullSpeedReduction = 0.5f;

    private KirbyController2D_InputSystem controller;
    private bool isInhaling = false;
    private bool inhaleLocked = false;
    private bool mouthfull = false;
    private Component currentInhaleTarget = null; // ✅ Changed to Component
    private List<Collider2D> disabledColliders = new List<Collider2D>();
    private Transform kirbyCenter;
    private float inhaleStartTime = 0f;
    private bool successTriggered = false;

    [Header("Spit Projectile")]
    [SerializeField] private GameObject spitProjectilePrefab;
    [SerializeField] private Vector3 spitOffset = new Vector3(0.5f, 0f, 0f);

    [Header("Spit Sound")]
    [SerializeField] private AudioClip spitSound;

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

        activeCollider.enabled = false;
        inactiveCollider.enabled = true;
    }

    private void Update()
    {
        if (isInhaling)
            HandleInhalePull();

        if (animator != null)
            animator.SetBool("isFULL", mouthfull);

        if (controller != null)
            controller.canFloat = !(isInhaling || mouthfull);

        if (activeCollider != null)
            activeCollider.enabled = isInhaling || mouthfull;
        if (inactiveCollider != null)
            inactiveCollider.enabled = !(isInhaling || mouthfull);
    }

    public void StartInhaleIfNotFloating()
    {
        if (mouthfull)
        {
            EmptyMouth();
            return;
        }

        if (inhaleLocked) return;

        if (controller != null)
        {
            var isFloatingField = typeof(KirbyController2D_InputSystem)
                .GetField("isFloating", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isFloatingField != null && (bool)isFloatingField.GetValue(controller))
                return;
        }

        StartInhale();
    }

    public void StopInhalePublic()
    {
        if (!inhaleLocked && !mouthfull)
            StopInhaleInternal();
    }

    public void EmptyMouth()
    {
        if (!mouthfull) return;

        mouthfull = false;
        successTriggered = false;
        ResetAnimationTriggers();

        if (animator != null)
        {
            animator.SetTrigger("Spit");
            StartCoroutine(ResetTriggerAfterAnimation("Spit"));
        }

        if (controller != null)
        {
            controller.SetSpeedMultiplier(1f);
            controller.canFloat = true;
        }

        if (spitSound != null && audioSource != null)
            audioSource.PlayOneShot(spitSound);

        if (spitProjectilePrefab != null && controller != null)
        {
            Vector3 spawnPos = transform.position + (controller.isFacingRight ? spitOffset : new Vector3(-spitOffset.x, spitOffset.y, spitOffset.z));
            GameObject proj = Instantiate(spitProjectilePrefab, spawnPos, Quaternion.identity);

            SpitProjectile projectileScript = proj.GetComponent<SpitProjectile>();
            if (projectileScript != null)
                projectileScript.direction = controller.isFacingRight ? Vector2.right : Vector2.left;
        }
    }

    private void StartInhale()
    {
        if (isInhaling) return;
        isInhaling = true;

        activeCollider.enabled = true;
        inactiveCollider.enabled = false;

        if (controller != null)
            controller.SetSpeedMultiplier(inhaleSpeedReduction);

        animator?.SetBool("isInhaling", true);
        inhaleParticles?.Play();
        if (inhaleSound != null && audioSource != null)
            audioSource.PlayOneShot(inhaleSound);
    }

    private void StopInhaleInternal()
    {
        isInhaling = false;

        activeCollider.enabled = mouthfull;
        inactiveCollider.enabled = !mouthfull;

        animator?.SetBool("isInhaling", false);
        if (!mouthfull && !successTriggered && animator != null)
            animator.SetTrigger("InhaleFail");

        inhaleParticles?.Stop();
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (controller != null)
        {
            float speed = mouthfull ? mouthfullSpeedReduction : 1f;
            controller.SetSpeedMultiplier(speed);
            controller.canFloat = !mouthfull;
        }

        if (currentInhaleTarget != null)
        {
            RestoreDisabledColliders();

            // ✅ Handle both BasicObject and Enemy
            if (currentInhaleTarget is BasicObject bo)
                bo.StopBeingInhaled();
            else if (currentInhaleTarget is Enemy en)
                en.StopBeingInhaled();
        }

        currentInhaleTarget = null;
        inhaleLocked = false;
        disabledColliders.Clear();
        inhaleStartTime = 0f;

        ResetAnimationTriggers();
    }

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
                Component closest = null;
                float minDist = float.MaxValue;
                Vector3 centerPos = kirbyCenter != null ? kirbyCenter.position : transform.position;

                foreach (var hit in hits)
                {
                    if (hit == null) continue;

                    BasicObject bo = hit.GetComponent<BasicObject>();
                    Enemy en = hit.GetComponent<Enemy>();
                    if ((bo == null && en == null) || (bo != null && bo.isBeingInhaled) || (en != null && en.isBeingInhaled)) continue;

                    float d = Vector3.Distance(centerPos, hit.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = (Component)bo ?? (Component)en;
                    }
                }

                if (closest != null)
                {
                    currentInhaleTarget = closest;

                    if (currentInhaleTarget is BasicObject bObj)
                        bObj.StartBeingInhaled();
                    else if (currentInhaleTarget is Enemy enemyObj)
                        enemyObj.StartBeingInhaled();

                    disabledColliders.Clear();
                    foreach (var c in currentInhaleTarget.GetComponentsInChildren<Collider2D>(true))
                    {
                        if (c != null && c.enabled)
                        {
                            disabledColliders.Add(c);
                            c.enabled = false;
                        }
                    }

                    inhaleLocked = true;
                    inhaleStartTime = 0f;
                }
            }
        }

        if (currentInhaleTarget != null)
        {
            inhaleStartTime += Time.deltaTime;
            Vector3 targetPos = kirbyCenter != null ? kirbyCenter.position : transform.position;

            float t = Mathf.Clamp01(inhaleStartTime);
            float curveSpeed = speedCurve.Evaluate(t) * maxInhaleSpeed;

            currentInhaleTarget.transform.position = Vector3.MoveTowards(
                currentInhaleTarget.transform.position,
                targetPos,
                curveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(currentInhaleTarget.transform.position, targetPos) <= destroyDistance)
            {
                // ✅ Handle Enemy or BasicObject properly
                if (currentInhaleTarget is BasicObject bo)
                    Destroy(bo.gameObject);
                else if (currentInhaleTarget is Enemy en)
                    en.OnPulledIntoKirby();

                currentInhaleTarget = null;
                inhaleLocked = false;
                StopInhaleInternal();

                if (!mouthfull && !successTriggered)
                {
                    mouthfull = true;
                    successTriggered = true;
                    PlayInhaleSuccessSequence();

                    if (controller != null)
                    {
                        controller.SetSpeedMultiplier(mouthfullSpeedReduction);
                        controller.canFloat = false;
                    }
                }
            }
        }
    }

    private void PlayInhaleSuccessSequence()
    {
        if (animator == null) return;
        animator.SetTrigger("InhaleSuccess");
        StartCoroutine(ResetTriggerAfterAnimation("InhaleSuccess"));
    }

    private System.Collections.IEnumerator ResetTriggerAfterAnimation(string triggerName)
    {
        if (animator == null) yield break;

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(triggerName))
            yield return null;

        while (animator.GetCurrentAnimatorStateInfo(0).IsName(triggerName) &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        animator.ResetTrigger(triggerName);
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

    private void ResetAnimationTriggers()
    {
        if (animator == null) return;
        animator.ResetTrigger("InhaleSuccess");
        animator.ResetTrigger("InhaleFail");
        animator.ResetTrigger("Spit");
    }

    public bool IsMouthfull => mouthfull;
}
