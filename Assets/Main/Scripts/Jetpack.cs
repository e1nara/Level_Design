using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(ThirdPersonController))]

public class Jetpack : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Hold to use the jetpack. Add more bindings (e.g. gamepad) in the Inspector")]
    public InputAction JetpackInput = new InputAction("Jetpack", InputActionType.Button, "<Keyboard>/f");

    [Header("Thrust")]
    [Tooltip("Upward acceleration in m/s^2. Must be higher than your controller's gravity (15)")]
    public float ThrustAcceleration = 30f;

    [Tooltip("The jetpack won't push you upward faster than this (m/s)")]
    public float MaxUpwardSpeed = 6f;

    [Header("Height Limit")]
    [Tooltip("Max height above the last place the player stood on the ground")]
    public float MaxHeightAboveGround = 12f;

    [Tooltip("Thrust fades out over this many meters below the limit, so it feels like a soft ceiling")]
    public float SoftCeilingZone = 2f;

    [Header("Fuel")]
    public float MaxFuel = 100f;
    public float StartingFuel = 100f;

    [Tooltip("Fuel used per second while thrusting")]
    public float FuelDrainPerSecond = 20f;

    [Tooltip("Fuel regained per second while standing on the ground. 0 = only fuel packs refill")]
    public float GroundRegenPerSecond = 0f;

    [Header("Effects (optional)")]
    public ParticleSystem ThrustParticles;
    public AudioSource ThrustLoopSound;

    public float Fuel { get; private set; }
    public float FuelNormalized => Fuel / MaxFuel;
    public bool IsThrusting { get; private set; }

    private ThirdPersonController _controller;
    private float _groundY;
    private Animator _animator;
    private readonly int _animIDJetpack = Animator.StringToHash("Jetpack");
    
    private void Awake()
    {
        _controller = GetComponent<ThirdPersonController>();
        Fuel = Mathf.Clamp(StartingFuel, 0f, MaxFuel);
        _groundY = transform.position.y;
        TryGetComponent(out _animator);
    }

    private void OnEnable() => JetpackInput.Enable();
    private void OnDisable() => JetpackInput.Disable();

    private void Update()
    {
        if (_controller.Grounded)
        {
            _groundY = transform.position.y;

            if (GroundRegenPerSecond > 0f)
            {
                AddFuel(GroundRegenPerSecond * Time.deltaTime);
            }
        }

        // 1 well below the limit, fading to 0 at the limit
        float heightLeft = (_groundY + MaxHeightAboveGround) - transform.position.y;
        float ceilingFactor = Mathf.Clamp01(heightLeft / Mathf.Max(0.01f, SoftCeilingZone));

        bool wantsThrust = JetpackInput.IsPressed() && Fuel > 0f && ceilingFactor > 0f;

        if (wantsThrust)
        {
            _controller.ApplyThrust(ThrustAcceleration * ceilingFactor, MaxUpwardSpeed);
            Fuel = Mathf.Max(0f, Fuel - FuelDrainPerSecond * Time.deltaTime);
        }

        SetThrusting(wantsThrust);
    }

    public void AddFuel(float amount)
    {
        Fuel = Mathf.Clamp(Fuel + amount, 0f, MaxFuel);
    }

    private void SetThrusting(bool thrusting)
    {
        if (thrusting == IsThrusting) return;
        IsThrusting = thrusting;
        if (_animator != null) _animator.SetBool(_animIDJetpack, thrusting);

        if (ThrustParticles != null)
        {
            if (thrusting) ThrustParticles.Play();
            else ThrustParticles.Stop();
        }

        if (ThrustLoopSound != null)
        {
            if (thrusting) ThrustLoopSound.Play();
            else ThrustLoopSound.Stop();
        }
    }
}

