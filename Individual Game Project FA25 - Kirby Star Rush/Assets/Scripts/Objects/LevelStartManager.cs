using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelStartManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject startUIRoot;
    [SerializeField] private Image fadeImage;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip countdownBeep;
    [SerializeField] private AudioClip goBeep;
    [SerializeField] private AudioSource startPlayAudio; // AudioSource to start after countdown

    [Header("Timer")]
    [SerializeField] private TimerManager timerManager; // Reference to TimerManager

    [Header("Timing")]
    [SerializeField] private float freezeDelay = 0.2f;
    [SerializeField] private float fadeOutDelay = 0.2f;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        // Find TimerManager automatically if not assigned
        if (timerManager == null)
        {
            timerManager = FindObjectOfType<TimerManager>();
            if (timerManager == null)
                Debug.LogWarning("No TimerManager found in the scene.");
        }

        if (GlobalVariables.Instance != null && GlobalVariables.Instance.levelStart)
        {
            SceneStart();
        }
        else
        {
            SkipIntro();
        }
    }

    private void SkipIntro()
    {
        if (startUIRoot != null)
            startUIRoot.SetActive(false);

        if (fadeImage != null)
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0f);

        UnmuteAllAudio();

        if (startPlayAudio != null && !startPlayAudio.isPlaying)
            startPlayAudio.Play();
    }

    public void SceneStart()
    {
        if (startUIRoot != null)
            startUIRoot.SetActive(true);

        StartCoroutine(LevelStartSequence());
    }

    private IEnumerator LevelStartSequence()
    {
        // Freeze gameplay briefly
        yield return new WaitForSeconds(freezeDelay);
        Time.timeScale = 0f;

        // Countdown: 3, 2, 1, GO!
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

        yield return new WaitForSecondsRealtime(fadeOutDelay);

        // Fade out the UI image
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

        if (startUIRoot != null)
            startUIRoot.SetActive(false);

        Time.timeScale = 1f;

        // Start level audio if assigned
        if (startPlayAudio != null)
            startPlayAudio.Play();

        // Unmute all audio sources in the scene
        UnmuteAllAudio();

        // Start the timer
        if (timerManager != null)
        {
            timerManager.StartTimer();
        }

        // Set levelStart to false after countdown
        if (GlobalVariables.Instance != null)
            GlobalVariables.Instance.levelStart = false;
    }

    private void UnmuteAllAudio()
    {
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allSources)
        {
            source.mute = false;
            source.volume = 1f;
        }
    }
}
