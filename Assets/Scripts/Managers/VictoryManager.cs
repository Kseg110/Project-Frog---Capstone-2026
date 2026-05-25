using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryManager : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // Appelé par le bouton "Quit"
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}