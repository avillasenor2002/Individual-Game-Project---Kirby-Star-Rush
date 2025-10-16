using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage; // Fullscreen UI Image
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float holdDuration = 0.5f; // How long to stay white
    [SerializeField] private Color fadeColor = Color.white;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);
            fadeImage.gameObject.SetActive(false);
        }
    }

    // Fade to white, hold, then fade back out
    public IEnumerator FadeSequence()
    {
        yield return FadeOut(); // Fade in to white
        yield return new WaitForSeconds(holdDuration); // Hold on white
        yield return FadeIn(); // Fade back out
    }

    public IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        fadeImage.gameObject.SetActive(false);
    }
}
