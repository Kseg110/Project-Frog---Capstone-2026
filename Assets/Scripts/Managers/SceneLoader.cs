using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{

    void Start()
    {
        SceneManager.LoadScene("Traps", LoadSceneMode.Additive);
    }

}
