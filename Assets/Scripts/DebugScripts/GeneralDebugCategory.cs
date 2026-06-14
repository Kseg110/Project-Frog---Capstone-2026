using UnityEngine;

[CreateAssetMenu(fileName = "GeneralDebugCategory", menuName = "Scriptable Objects/GeneralDebugCategory")]
public class GeneralDebugCategory : DebugCategory
{
    [Header("Game Speed Settings")]
    public float minSpeed = 0f;
    public float maxSpeed = 3f;
    public float defaultSpeed = 1f;

    float runtimeSpeed;
    float baseFixedDeltaTime;

    private void OnEnable()
    {
        runtimeSpeed = defaultSpeed;
        baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    public override void Draw()
    {
        // GAME SPEED CONTROL
        GUILayout.Label("General Debug Settings", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Game Speed", GUILayout.Width(100));

        float newSpeed = GUILayout.HorizontalSlider(runtimeSpeed, minSpeed, maxSpeed, GUILayout.Width(200));
        string speedText = GUILayout.TextField(runtimeSpeed.ToString("0.00"), GUILayout.Width(50));
        GUILayout.EndHorizontal();

        if (float.TryParse(speedText, out float parsedSpeed))
            newSpeed = Mathf.Clamp(parsedSpeed, minSpeed, maxSpeed);

        if (!Mathf.Approximately(newSpeed, runtimeSpeed))
        {
            runtimeSpeed = newSpeed;
            ApplyGameSpeed(runtimeSpeed);
        }

        GUILayout.Space(6);
        GUILayout.Label($"Current Game Speed: {runtimeSpeed:0.00}x", GUI.skin.label);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Speed", GUILayout.Width(100)))
        {
            runtimeSpeed = defaultSpeed;
            ApplyGameSpeed(runtimeSpeed);
        }
        if (GUILayout.Button("Pause (0)", GUILayout.Width(100)))
        {
            runtimeSpeed = 0f;
            ApplyGameSpeed(runtimeSpeed);
        }
        GUILayout.EndHorizontal();
    }

    void ApplyGameSpeed(float speed) //Used to change game speed
    {
        speed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        if (!Mathf.Approximately(Time.timeScale, speed))
        {
            Time.timeScale = speed;
            Time.fixedDeltaTime = baseFixedDeltaTime * Mathf.Max(0.0001f, Time.timeScale);
        }
    }
}
