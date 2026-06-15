using UnityEngine;

public class FogStrengthDecay : MonoBehaviour
{
    public float minSecondSpeed = 0.15f;
    public float decayRate = 0.5f;

    public float startSimulationSpeed = 1f;
    public float minSimulationSpeed = 0.1f;
    public float simulationDecayRate = 0.1f;

    private ParticleSystem ps;
    private ParticleSystem.MainModule main;

    private float secondSpeed = 4f;
    private float currentSimSpeed;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        main = ps.main;

        // Start values
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, secondSpeed);

        currentSimSpeed = startSimulationSpeed;
        main.simulationSpeed = currentSimSpeed;
    }

    void Update()
    {
        if (ps == null) return;

        // ---- Start speed decay (second value only) ----
        if (secondSpeed > minSecondSpeed)
        {
            secondSpeed -= decayRate * Time.deltaTime;

            if (secondSpeed < minSecondSpeed)
                secondSpeed = minSecondSpeed;

            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, secondSpeed);
        }

        // ---- Simulation speed decay (whole system slow-down) ----
        if (currentSimSpeed > minSimulationSpeed)
        {
            currentSimSpeed -= simulationDecayRate * Time.deltaTime;

            if (currentSimSpeed < minSimulationSpeed)
                currentSimSpeed = minSimulationSpeed;

            main.simulationSpeed = currentSimSpeed;
        }
    }
}