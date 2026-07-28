using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public enum InputDevice
    {
        MouseKeyboard,
        Gamepad
    }

    public static InputManager Instance {  get; private set; }
    public InputDevice CurrentInputDevice { get; private set; } = InputDevice.MouseKeyboard;
    public event System.Action<InputDevice> OnInputDeviceChanged;

    private InputDevice lastInputDevice;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        DetectInputDevice();
    }

    private void DetectInputDevice()
    {
        InputDevice detected = CurrentInputDevice;

        // Checks for gamepad inputs
        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.01f ||
                Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.01f ||
                Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.buttonEast.wasPressedThisFrame ||
                Gamepad.current.buttonWest.wasPressedThisFrame ||
                Gamepad.current.buttonNorth.wasPressedThisFrame ||
                Gamepad.current.leftTrigger.wasPressedThisFrame ||
                Gamepad.current.rightTrigger.wasPressedThisFrame ||
                Gamepad.current.leftShoulder.wasPressedThisFrame ||
                Gamepad.current.rightShoulder.wasPressedThisFrame)
            {
                detected = InputDevice.Gamepad;
            }
        }

        // Checks for Mouse/ Keyboard inputs
        if (Mouse.current != null)
        {
            if (Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f ||
                Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame)
            {
                detected = InputDevice.MouseKeyboard;
            }
        }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                detected = InputDevice.MouseKeyboard;
            }
        }

        // Only update if device changed
        if (detected != CurrentInputDevice)
        {
            lastInputDevice = CurrentInputDevice;
            CurrentInputDevice = detected;
            OnInputDeviceChanged?.Invoke(CurrentInputDevice);
            Debug.Log($"[InputManager] Switched to {CurrentInputDevice}");
        }
    }

    public bool IsUsingGamepad()
    {
        return CurrentInputDevice == InputDevice.Gamepad;
    }

    public bool IsUsingMouseKeyboard()
    {
        return CurrentInputDevice == InputDevice.MouseKeyboard;
    }

}
