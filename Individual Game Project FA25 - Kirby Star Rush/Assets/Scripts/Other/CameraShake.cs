using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("Default duration of the shake effect.")]
    public float defaultDuration = 0.2f;

    [Tooltip("Default intensity of the shake effect.")]
    public float defaultIntensity = 0.3f;

    private Coroutine shakeRoutine;

    /// <summary>
    /// Triggers a camera shake with the given duration and intensity.
    /// If not specified, it uses default values.
    /// </summary>
    public void Shake(float duration = -1f, float intensity = -1f)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        if (duration <= 0) duration = defaultDuration;
        if (intensity <= 0) intensity = defaultIntensity;

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, intensity));
    }

    private IEnumerator ShakeRoutine(float duration, float intensity)
    {
        float elapsed = 0f;
        Vector3 originalPos = transform.position; // ✅ Capture the *current* position

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * intensity;
            float offsetY = Random.Range(-1f, 1f) * intensity;

            transform.position = originalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos; // Return smoothly
        shakeRoutine = null;
    }
}
