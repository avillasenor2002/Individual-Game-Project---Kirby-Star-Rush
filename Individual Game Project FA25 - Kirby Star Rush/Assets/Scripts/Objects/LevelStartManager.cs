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
    [SerializeField] private AudioSource sfxSource;         // Countdown SFX source
    [SerializeField] private AudioClip countdownBeep;
    [SerializeField] private AudioClip goBeep;
    [SerializeField] private AudioSource startPlayAudio;    // Main background music to mute/unmute

    [Header("Timer")]
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private TilemapTimer tilemapTimer;

    [Header("Timing")]
    [SerializeField] private float freezeDelay = 0.2f;
    [SerializeField] private float fadeOutDelay = 0.2f;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        // Auto-locate timers
        if (timerManager == null)
            timerManager = FindObjectOfType<TimerManager>();
        if (tilemapTimer == null)
            tilemapTimer = FindObjectOfType<TilemapTimer>();

        ResetTimers();

        // --- Only mute music, NOT SFX ---
        if (startPlayAudio != null)
            startPlayAudio.mute = true;

        if (GlobalVariables.Instance != null && GlobalVariables.Instance.levelStart)
        {
            SceneStart();
        }
        else
        {
            SkipIntro();
        }
    }

    private void ResetTimers()
    {
        if (timerManager != null)
        {
            timerManager.StopAllCoroutines();
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
    }

    private void SkipIntro()
    {
        if (startUIRoot != null)
            startUIRoot.SetActive(false);

        if (fadeImage != null)
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0f);

        // Immediately unmute and play the main music
        if (startPlayAudio != null)
        {
            startPlayAudio.mute = false;
            if (!startPlayAudio.isPlaying)
                startPlayAudio.Play();
        }
    }

    public void SceneStart()
    {
        if (startUIRoot != null)
            startUIRoot.SetActive(true);

        StartCoroutine(LevelStartSequence());
    }

    private IEnumerator LevelStartSequence()
    {
        // Freeze briefly
        yield return new WaitForSeconds(freezeDelay);
        Time.timeScale = 0f;

        // Countdown 3..2..1..GO!
        string[] countdown = { "3", "2", "1", "GO!" };
        for (int i = 0; i < countdown.Length; i++)
        {
            if (countdownText != null)
                countdownText.text = countdown[i];

            // --- Play countdown sound effects (not muted) ---
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

        // Resume time
        Time.timeScale = 1f;

        // ✅ Unmute and play background music now
        if (startPlayAudio != null)
        {
            startPlayAudio.mute = false;
            if (!startPlayAudio.isPlaying)
                startPlayAudio.Play();
        }

        // Start whichever timer exists
        if (timerManager != null)
            timerManager.StartTimer();
        if (tilemapTimer != null)
            tilemapTimer.StartCountdown();

        // Disable levelStart flag
        if (GlobalVariables.Instance != null)
            GlobalVariables.Instance.levelStart = false;
    }
}
