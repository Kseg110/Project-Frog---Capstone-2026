using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ScenesManager : MonoBehaviour
{
    public static ScenesManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    // When adding a new Scene be sure to also add it to the Build Settings in Unity!
    // As well as this enum in order!
    public enum Scene
    {
        MainMenu,
        level01
    }

    // Call upon to load specific scenes
    public void LoadScene(Scene scene)
    {
        SceneManager.LoadScene(scene.ToString());
    }

    public void LoadNewGame()
    {
        SceneManager.LoadScene(Scene.level01.ToString());
    }

    // This will load the next scene in the build order automatically
    public void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(Scene.MainMenu.ToString());
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame &&
            SceneManager.GetActiveScene().name != Scene.MainMenu.ToString())
        {
            LoadMainMenu();
        }
    }
}