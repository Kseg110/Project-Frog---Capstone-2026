using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(ParticleSystemRenderer))]
public class PoisonTrapPollenParticles : MonoBehaviour
{
    [Header("Link")]
    public PoisonTrapPlant poisonTrap;


    [Header("Material")]
    public Material particleMaterial;


    [Header("Color")]
    public Color pollenColor = new Color(1f, 0.9f, 0.4f, 0.8f);


    [Header("Amount")]
    public float particlesPerRadius = 100f;


    [Header("Particle Look")]
    public float particleSize = 0.05f;
    public float lifetime = 8f;


    [Header("Radius Multiplier")]
    public float radiusMultiplier = 2f;



    [Header("Center Fast -> Outside Slow")]
    public float centerSpeed = 0.5f;
    public float outerSpeed = 0.02f;

    [Tooltip("Controls where speed changes")]
    public float distanceSpeedPower = 1f;



    [Header("Random Floating")]
    public float randomFloatAmount = 0.2f;


    [Header("Noise")]
    public float noiseStrength = 0.5f;
    public float noiseSpeed = 0.2f;



    [Header("Live Update")]
    public bool updateEveryChange = true;



    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;

    private ParticleSystem.Particle[] particles;



    private void Awake()
    {
        Setup();
        ApplySettings();
    }


    private void OnEnable()
    {
        Setup();
        ApplySettings();
    }



    private void Setup()
    {
        if (ps == null)
            ps = GetComponent<ParticleSystem>();

        if (psRenderer == null)
            psRenderer = GetComponent<ParticleSystemRenderer>();

        if (poisonTrap == null)
            poisonTrap = GetComponentInParent<PoisonTrapPlant>();

        if (particles == null)
            particles = new ParticleSystem.Particle[20000];
    }



    private void Update()
    {
        if (updateEveryChange)
        {
            ApplySettings();
        }


        if (Application.isPlaying)
        {
            MovePollen();
        }
    }



    private void ApplySettings()
    {
        if (ps == null)
            return;


        float radius = 3f;

        if (poisonTrap != null)
            radius = poisonTrap.radius;


        // DOUBLE SIZE HERE
        float finalRadius = radius * radiusMultiplier;



        int amount =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    finalRadius * particlesPerRadius
                ),
                50,
                20000
            );



        // MATERIAL

        if (particleMaterial != null)
            psRenderer.sharedMaterial = particleMaterial;


        psRenderer.renderMode =
            ParticleSystemRenderMode.Billboard;



        // MAIN

        var main = ps.main;

        main.loop = true;
        main.playOnAwake = true;

        main.maxParticles = amount;

        main.startLifetime = lifetime;

        // DOUBLE PARTICLE SIZE
        main.startSize = particleSize * 2f;

        main.startColor = pollenColor;


        // no shooting
        main.startSpeed = 0f;

        main.gravityModifier = 0f;


        main.simulationSpace =
            ParticleSystemSimulationSpace.Local;



        // FULL SPHERE

        var shape = ps.shape;

        shape.enabled = true;

        shape.shapeType =
            ParticleSystemShapeType.Sphere;


        shape.radius = finalRadius;


        shape.radiusThickness = 1f;



        // FLOATING MOVEMENT

        var noise = ps.noise;

        noise.enabled = true;

        noise.strength = noiseStrength;

        noise.frequency = noiseSpeed;

        noise.scrollSpeed = 0.1f;

        noise.damping = true;



        // no built-in direction

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = false;



        var force = ps.forceOverLifetime;
        force.enabled = false;



        var collision = ps.collision;
        collision.enabled = false;



        var trails = ps.trails;
        trails.enabled = false;



        // keep filled

        var emission = ps.emission;

        emission.enabled = true;

        emission.rateOverTime =
            amount / Mathf.Max(lifetime, 0.1f);



        if (Application.isPlaying)
        {
            if (ps.particleCount < amount)
            {
                ps.Emit(amount - ps.particleCount);
            }
        }
    }




    private void MovePollen()
    {
        int count =
            ps.GetParticles(particles);


        if (count == 0)
            return;



        float radius = 3f;

        if (poisonTrap != null)
            radius = poisonTrap.radius * radiusMultiplier;



        for (int i = 0; i < count; i++)
        {
            Vector3 pos =
                particles[i].position;



            float distance =
                pos.magnitude;



            float percent =
                Mathf.Clamp01(distance / radius);



            // CENTER FAST
            // OUTSIDE SLOW

            float speed =
                Mathf.Lerp(
                    centerSpeed,
                    outerSpeed,
                    Mathf.Pow(
                        percent,
                        distanceSpeedPower
                    )
                );



            Vector3 direction;


            if (distance > 0.01f)
                direction = pos.normalized;
            else
                direction = Random.onUnitSphere;



            Vector3 random =
                Random.insideUnitSphere *
                randomFloatAmount;



            particles[i].velocity =
                (direction * speed)
                + random;



            // HARD KEEP INSIDE SPHERE

            if (distance > radius)
            {
                particles[i].position =
                    Random.insideUnitSphere * radius;
            }
        }


        ps.SetParticles(
            particles,
            count
        );
    }
}