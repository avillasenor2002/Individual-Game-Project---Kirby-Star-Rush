using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenSceneButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneName; // The scene to open

    /// <summary>
    /// Call this method to load the scene.
    /// Can be assigned to a Button OnClick event.
    /// </summary>
    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("OpenSceneButton: Scene name is empty!");
        }
    }
}
