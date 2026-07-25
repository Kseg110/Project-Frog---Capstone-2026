using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputUISwap : MonoBehaviour
{
    [Header("Assign all keyboard images here (will be enabled when using Keyboard/Mouse)")]
    [SerializeField] private List<Image> keyboardImages = new();

    [Header("Assign all gamepad images here (will be enabled when using Gamepad)")]
    [SerializeField] private List<Image> gamepadImages = new();

    private void OnEnable()
    {
        StartCoroutine(WaitForInputManager());
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnInputDeviceChanged -= OnInputDeviceChanged;
    }

    private IEnumerator WaitForInputManager()
    {
        // Wait until the InputManager singleton exists (no short timeout)
        while (InputManager.Instance == null)
            yield return null;

        InputManager.Instance.OnInputDeviceChanged += OnInputDeviceChanged;
        Debug.Log($"[InputUIswap] Subscribed to InputManager. Current device: {InputManager.Instance.CurrentInputDevice}");
        ApplyInput(InputManager.Instance.CurrentInputDevice);
    }

    private void OnInputDeviceChanged(InputManager.InputDevice device)
    {
        ApplyInput(device);
    }

    private void ApplyInput(InputManager.InputDevice device)
    {
        bool usingGamepad = device == InputManager.InputDevice.Gamepad;
        Debug.Log($"[InputUIswap] Applying input: {(usingGamepad ? "Gamepad" : "MouseKeyboard")} (keyboard:{keyboardImages.Count}, gamepad:{gamepadImages.Count})");

        // Keyboard images: active when NOT using gamepad
        foreach (var img in keyboardImages)
        {
            if (img == null) continue;
            img.gameObject.SetActive(!usingGamepad);
            if (!img.gameObject.activeInHierarchy && !img.gameObject.activeSelf)
                Debug.LogWarning($"[InputUIswap] '{img.gameObject.name}' is still inactive (a parent may be disabled).");
        }

        // Gamepad images: active when using gamepad
        foreach (var img in gamepadImages)
        {
            if (img == null) continue;
            img.gameObject.SetActive(usingGamepad);
            if (!img.gameObject.activeInHierarchy && !img.gameObject.activeSelf)
                Debug.LogWarning($"[InputUIswap] '{img.gameObject.name}' is still inactive (a parent may be disabled).");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (InputManager.Instance != null)
            ApplyInput(InputManager.Instance.CurrentInputDevice);
    }
#endif
}
