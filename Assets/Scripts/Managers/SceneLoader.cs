using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{

    void Start()
    {
        SceneManager.LoadScene("Traps", LoadSceneMode.Additive);
        SceneManager.LoadScene("NewWalls", LoadSceneMode.Additive);
        SceneManager.LoadScene("Lighting", LoadSceneMode.Additive);
    }

}
