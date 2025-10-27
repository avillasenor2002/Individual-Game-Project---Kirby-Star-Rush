using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelEndManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject endUIRoot;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float maxOpacity = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip endMusic;
    [SerializeField] private AudioClip loopingMusic;

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowTimeScale = 0.3f;
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private bool useTimeEffects = true;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverUIRoot;
    [SerializeField] private AudioClip gameOverMusic;

    [Header("UI Navigation")]
    [SerializeField] private GameObject gameOverFirstSelected;
    [SerializeField] private GameObject levelEndFirstSelected;

    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName;

    private Image rootImage;
    private Image[] childImages;
    private Text[] childTexts;

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

    public void TriggerLevelEnd()
    {
        StartCoroutine(HandleLevelEndSequence());
    }

    public void TriggerGameOver()
    {
        StartCoroutine(HandleGameOverSequence());
    }

    private IEnumerator HandleGameOverSequence()
    {
        MuteAllOtherAudioSources();

        if (musicSource != null && gameOverMusic != null)
        {
            musicSource.loop = false;
            musicSource.Stop();
            musicSource.clip = gameOverMusic;
            musicSource.Play();
        }

        if (useTimeEffects)
            Time.timeScale = slowTimeScale;

        yield return new WaitForSecondsRealtime(slowDuration);

        if (gameOverUIRoot != null)
        {
            gameOverUIRoot.SetActive(true);
            yield return StartCoroutine(FadeUI(gameOverUIRoot));
        }

        // Reset time before scene load
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator HandleLevelEndSequence()
    {
        MuteAllOtherAudioSources();

        if (musicSource != null && endMusic != null)
        {
            musicSource.loop = false;
            musicSource.Stop();
            musicSource.clip = endMusic;
            musicSource.Play();
        }

        if (useTimeEffects)
            Time.timeScale = slowTimeScale;

        yield return new WaitForSecondsRealtime(slowDuration);

        if (endUIRoot != null)
        {
            endUIRoot.SetActive(true);
            yield return StartCoroutine(FadeUI(endUIRoot));
        }

        // Reset time before scene load
        Time.timeScale = 1f;

        // Load the next scene
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

        // Ensure final alpha
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
