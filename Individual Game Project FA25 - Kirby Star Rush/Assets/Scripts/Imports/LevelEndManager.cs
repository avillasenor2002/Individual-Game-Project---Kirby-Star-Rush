using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelEndManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject endUIRoot;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float maxOpacity = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip endMusic;

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowTimeScale = 0.3f;
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private bool useTimeEffects = true;

    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName;

    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI bestTimeText;

    [Header("Level Name for Best Times")]
    [SerializeField] private string levelName;

    private TimerManager timerManager;
    private Image rootImage;
    private Image[] childImages;
    private Text[] childTexts;

    private bool levelEnded = false; // Prevent multiple triggers

    private void Start()
    {
        if (endUIRoot != null)
        {
            endUIRoot.SetActive(false);
            rootImage = endUIRoot.GetComponent<Image>();
            childImages = endUIRoot.GetComponentsInChildren<Image>(true);
            childTexts = endUIRoot.GetComponentsInChildren<Text>(true);
            childImages = System.Array.FindAll(childImages, img => img != rootImage);
        }
    }

    /// <summary>
    /// Public method to trigger the level end manually
    /// </summary>
    public void TriggerLevelEnd()
    {
        if (!levelEnded)
        {
            levelEnded = true;
            StartCoroutine(HandleLevelEndSequence());
        }
    }

    /// <summary>
    /// Optional: call this when a player enters a trigger to end the level
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!levelEnded && other.CompareTag("Player"))
        {
            TriggerLevelEnd();
        }
    }

    private IEnumerator HandleLevelEndSequence()
    {
        MuteAllOtherAudioSources();

        // Play end music
        if (musicSource != null && endMusic != null)
        {
            musicSource.loop = false;
            musicSource.Stop();
            musicSource.clip = endMusic;
            musicSource.Play();
        }

        // Slow motion effect
        if (useTimeEffects)
            Time.timeScale = slowTimeScale;

        yield return new WaitForSecondsRealtime(slowDuration);

        // Activate UI and fade in
        if (endUIRoot != null)
        {
            endUIRoot.SetActive(true);
            yield return StartCoroutine(FadeUI(endUIRoot));
        }

        Time.timeScale = 1f;

        // Find TimerManager in the scene if not assigned
        if (timerManager == null)
        {
            timerManager = FindObjectOfType<TimerManager>();
            if (timerManager == null)
            {
                Debug.LogWarning("No TimerManager found in the scene.");
            }
        }

        bool isNewRecord = false;

        if (timerManager != null && GlobalVariables.Instance != null)
        {
            // Stop timer and get current time
            timerManager.EndTimer();
            string playerTime = timerManager.currentTimeFormatted;

            // Check best time
            string previousBest = GlobalVariables.Instance.GetBestTime(levelName);
            if (GlobalVariables.Instance.IsTimeBetter(playerTime, previousBest))
            {
                // New record
                GlobalVariables.Instance.RecordLevelTime(levelName, playerTime);
                isNewRecord = true;
            }

            string bestTimeToShow = GlobalVariables.Instance.GetBestTime(levelName);

            // Update UI
            if (currentTimeText != null)
                currentTimeText.text = playerTime + (isNewRecord ? "!" : "");
            if (bestTimeText != null)
                bestTimeText.text = bestTimeToShow + (isNewRecord ? "!" : "");
        }

        // Wait 5 seconds before loading next scene
        yield return new WaitForSeconds(5f);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FadeUI(GameObject uiRoot)
    {
        Image[] images = uiRoot.GetComponentsInChildren<Image>(true);
        Text[] texts = uiRoot.GetComponentsInChildren<Text>(true);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, maxOpacity, elapsed / fadeDuration);

            foreach (var img in images)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }

            foreach (var txt in texts)
            {
                Color c = txt.color;
                c.a = alpha;
                txt.color = c;
            }

            yield return null;
        }

        foreach (var img in images)
        {
            Color c = img.color;
            c.a = maxOpacity;
            img.color = c;
        }
        foreach (var txt in texts)
        {
            Color c = txt.color;
            c.a = maxOpacity;
            txt.color = c;
        }
    }

    private void MuteAllOtherAudioSources()
    {
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allSources)
        {
            if (source != null && source != musicSource)
                source.mute = true;
        }
    }
}
