using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(ThirdPersonController))]

public class Jetpack : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Hold to use the jetpack. Add more bindings (e.g. gamepad) in the Inspector")]
    public InputAction JetpackInput = new InputAction("Jetpack", InputActionType.Button, "<Keyboard>/f");

    [Header("Movement")]
    [Tooltip("Top climbing speed (m/s)")]
    public float MaxUpwardSpeed = 6f;

    [Tooltip("How quickly vertical speed changes toward its target (m/s^2). Higher = snappier, lower = smoother")]
    public float ThrustAcceleration = 25f;

    [Header("Height Limit")]
    [Tooltip("Max height above the last place the player stood on the ground")]
    public float MaxHeightAboveGround = 12f;

    [Tooltip("Climb speed eases down to zero over this many meters below the limit")]
    public float SoftCeilingZone = 3f;

    [Header("Hover Bob")]
    [Tooltip("How far (meters) the player drifts up and down while hovering near the height limit. 0 = perfectly steady")]
    public float BobHeight = 0.15f;

    [Tooltip("Bobs per second while hovering")]
    public float BobFrequency = 0.8f;

    [Header("Heat")]
    public float MaxHeat = 100f;

    [Tooltip("Heat gained per second while the jetpack is active (including hovering). MaxHeat / this = seconds of use before overheating")]
    public float HeatPerSecond = 25f;

    [Tooltip("Heat lost per second while the jetpack is not active")]
    public float CoolPerSecond = 15f;

    [Tooltip("Extra cooling multiplier while standing on the ground (1 = same as in the air)")]
    public float GroundCoolMultiplier = 1.5f;

    [Tooltip("After overheating, the jetpack stays locked until heat drops below this fraction of MaxHeat (0-1). 0 = fully cooled")]
    [Range(0f, 1f)]
    public float RecoverThreshold = 0.4f;

    [Header("Fuel")]
    public float MaxFuel = 100f;
    public float StartingFuel = 100f;

    [Tooltip("Fuel used per second while the jetpack is active (including hovering)")]
    public float FuelDrainPerSecond = 10f;

    [Tooltip("Fuel regained per second while standing on the ground. 0 = only fuel packs refill")]
    public float GroundRegenPerSecond = 0f;

    [Header("Effects (optional)")]
    public ParticleSystem ThrustParticles;
    public AudioSource ThrustLoopSound;
    [Tooltip("Played while the jetpack is overheated and cooling down (e.g. smoke)")]
    public ParticleSystem OverheatParticles;
    [Tooltip("One-shot sound played the moment the jetpack overheats")]
    public AudioSource OverheatSound;

    public float Fuel { get; private set; }
    public float FuelNormalized => Fuel / MaxFuel;
    public float Heat { get; private set; }
    public float HeatNormalized => Heat / MaxHeat;
    public bool IsOverheated { get; private set; }
    public bool IsThrusting { get; private set; }

    private ThirdPersonController _controller;
    private Animator _animator;
    private readonly int _animIDJetpack = Animator.StringToHash("Jetpack");
    private float _groundY;

    private void Awake()
    {
        _controller = GetComponent<ThirdPersonController>();
        _animator = GetComponentInChildren<Animator>();
        Heat = 0f;
        Fuel = Mathf.Clamp(StartingFuel, 0f, MaxFuel);
        _groundY = transform.position.y;
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

        // the jetpack is "active" while the button is held, it is not overheated and there is fuel
        bool active = JetpackInput.IsPressed() && !IsOverheated && Fuel > 0f;

        if (active)
        {
            // target climb speed: full speed far below the limit, easing to 0 at the limit
            // (and a gentle push downward if the player is somehow above it)
            float heightLeft = (_groundY + MaxHeightAboveGround) - transform.position.y;
            float t = Mathf.Clamp(heightLeft / Mathf.Max(0.01f, SoftCeilingZone), -1f, 1f);
            float targetSpeed = MaxUpwardSpeed * (t > 0f ? Mathf.SmoothStep(0f, 1f, t) : t);

            // gentle bob: full strength when hovering, fading out while climbing at speed
            float hoverWeight = 1f - Mathf.Clamp01(Mathf.Abs(targetSpeed) / Mathf.Max(0.01f, MaxUpwardSpeed));
            float omega = BobFrequency * 2f * Mathf.PI;
            targetSpeed += Mathf.Cos(Time.time * omega) * BobHeight * omega * hoverWeight;

            _controller.SteerVerticalVelocity(targetSpeed, ThrustAcceleration);

            Fuel = Mathf.Max(0f, Fuel - FuelDrainPerSecond * Time.deltaTime);

            Heat += HeatPerSecond * Time.deltaTime;
            if (Heat >= MaxHeat)
            {
                Heat = MaxHeat;
                SetOverheated(true);
                active = false; // stop thrusting this frame
            }
        }
        else
        {
            float cool = CoolPerSecond * (_controller.Grounded ? GroundCoolMultiplier : 1f);
            Heat = Mathf.Max(0f, Heat - cool * Time.deltaTime);

            if (IsOverheated && Heat <= MaxHeat * RecoverThreshold)
            {
                SetOverheated(false);
            }
        }

        SetThrusting(active);
    }

    public void AddFuel(float amount)
    {
        Fuel = Mathf.Clamp(Fuel + amount, 0f, MaxFuel);
    }

    private void SetOverheated(bool overheated)
    {
        if (overheated == IsOverheated) return;
        IsOverheated = overheated;

        if (OverheatParticles != null)
        {
            if (overheated) OverheatParticles.Play();
            else OverheatParticles.Stop();
        }

        if (overheated && OverheatSound != null) OverheatSound.Play();
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