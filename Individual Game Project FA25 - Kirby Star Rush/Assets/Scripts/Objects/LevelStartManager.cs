using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelStartManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject startUIRoot;
    [SerializeField] private Image fadeImage; // The image to fade out (like a panel background)
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip countdownBeep;
    [SerializeField] private AudioClip goBeep;

    [Header("Timing")]
    [SerializeField] private float freezeDelay = 0.2f;
    [SerializeField] private float fadeOutDelay = 0.2f;
    [SerializeField] private float fadeDuration = 0.5f;

    public void SceneStart()
    {
        if (startUIRoot != null)
            startUIRoot.SetActive(true);

        StartCoroutine(LevelStartSequence());
    }

    private IEnumerator LevelStartSequence()
    {
        // Short delay before freezing gameplay
        yield return new WaitForSeconds(freezeDelay);
        Time.timeScale = 0f; // Freeze gameplay

        // Countdown: 3, 2, 1, Go!
        string[] countdown = { "3", "2", "1", "GO!" };
        for (int i = 0; i < countdown.Length; i++)
        {
            if (countdownText != null)
                countdownText.text = countdown[i];

            if (sfxSource != null)
            {
                if (i < countdown.Length - 1 && countdownBeep != null)
                    sfxSource.PlayOneShot(countdownBeep);
                else if (i == countdown.Length - 1 && goBeep != null)
                    sfxSource.PlayOneShot(goBeep);
            }

            yield return new WaitForSecondsRealtime(1f);
        }

        // Short pause after "Go!"
        yield return new WaitForSecondsRealtime(fadeOutDelay);

        // Fade out the UI Image
        if (fadeImage != null)
        {
            Color startColor = fadeImage.color;
            float startAlpha = startColor.a;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }

        // Hide the UI and resume gameplay
        if (startUIRoot != null)
            startUIRoot.SetActive(false);

        Time.timeScale = 1f;
    }
}
