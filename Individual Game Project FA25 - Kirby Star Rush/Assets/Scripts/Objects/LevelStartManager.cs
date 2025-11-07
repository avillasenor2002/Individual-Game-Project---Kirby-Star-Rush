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
    [SerializeField] private TimerManager timerManager; // Regular timer
    [SerializeField] private TilemapTimer tilemapTimer; // Tilemap-based timer

    [Header("Timing")]
    [SerializeField] private float freezeDelay = 0.2f;
    [SerializeField] private float fadeOutDelay = 0.2f;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        // Auto-find both timer types if missing
        if (timerManager == null)
            timerManager = FindObjectOfType<TimerManager>();

        if (tilemapTimer == null)
            tilemapTimer = FindObjectOfType<TilemapTimer>();

        // --- Reset timers ---
        if (timerManager != null)
        {
            timerManager.StopAllCoroutines(); // stop previous timers
            typeof(TimerManager)
                .GetField("elapsedTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(timerManager, 0f);
            typeof(TimerManager)
                .GetField("currentTimeFormatted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(timerManager, "00:00");
        }

        if (tilemapTimer != null)
        {
            tilemapTimer.SetTime(121f);
            tilemapTimer.StopTimer();
        }

        // --- Reset and unmute all audio sources ---
        ResetAllAudioSources();

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

        ResetAllAudioSources();

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

        // Fade out UI
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

        // Start audio
        if (startPlayAudio != null)
            startPlayAudio.Play();

        ResetAllAudioSources();

        // Start whichever timer exists
        if (timerManager != null)
            timerManager.StartTimer();

        if (tilemapTimer != null)
            tilemapTimer.StartCountdown();

        // Disable levelStart flag
        if (GlobalVariables.Instance != null)
            GlobalVariables.Instance.levelStart = false;
    }

    private void ResetAllAudioSources()
    {
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allSources)
        {
            source.Stop();
            source.mute = false;
            source.volume = 1f;
        }
    }
}
