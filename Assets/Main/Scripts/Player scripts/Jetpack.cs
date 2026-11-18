using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(ThirdPersonController))]
public class Jetpack : MonoBehaviour
{
 
    [Header("Input")]
    [Tooltip("Hold to run the jetpack. Pressing it starts an automatic climb to the height limit. Add more bindings (e.g. gamepad) in the Inspector")]
    public InputAction JetpackInput = new InputAction("Jetpack", InputActionType.Button, "<Keyboard>/f");

    [Tooltip("Hold (with the engine key) to climb")]
    public InputAction AscendInput = new InputAction("JetpackAscend", InputActionType.Button, "<Keyboard>/q");

    [Tooltip("Hold (with the engine key) to descend. Cancels the automatic climb")]
    public InputAction DescendInput = new InputAction("JetpackDescend", InputActionType.Button, "<Keyboard>/e");

    [Header("Movement")]
    [Tooltip("Top climbing speed (m/s)")]
    public float MaxUpwardSpeed = 6f;

    [Tooltip("Top descending speed (m/s)")]
    public float MaxDownwardSpeed = 4f;

    [Tooltip("How quickly vertical speed changes toward its target (m/s^2). Higher = snappier, lower = smoother")]
    public float ThrustAcceleration = 25f;

    [Header("Height Limits")]
    [Tooltip("Max height above the last place the player stood on the ground")]
    public float MaxHeightAboveGround = 12f;

    [Tooltip("Climb speed eases down to zero over this many meters below the max height")]
    public float SoftCeilingZone = 3f;

    [Tooltip("Min height above the last place the player stood on the ground. While the engine runs, the player never goes lower than this")]
    public float MinHeightAboveGround = 2f;

    [Tooltip("Descent speed eases down to zero over this many meters above the min height")]
    public float SoftFloorZone = 1.5f;

    [Header("Hover Bob")]
    [Tooltip("How far (meters) the player drifts up and down while hovering. 0 = perfectly steady")]
    public float BobHeight = 0.15f;

    [Tooltip("Bobs per second while hovering")]
    public float BobFrequency = 0.8f;

    [Header("Heat")]
    public float MaxHeat = 100f;

    [Tooltip("Heat gained per second while climbing. MaxHeat / this = seconds of climbing before overheating")]
    public float HeatPerSecond = 25f;

    [Tooltip("Heat gain while hovering, compared to climbing (1 = same, 0 = hovering adds no heat)")]
    [Range(0f, 1f)] public float HoverHeatMultiplier = 0.5f;

    [Tooltip("Heat gain while descending, compared to climbing")]
    [Range(0f, 1f)] public float DescendHeatMultiplier = 0.25f;

    [Tooltip("Heat lost per second while the engine is off")]
    public float CoolPerSecond = 15f;

    [Tooltip("Extra cooling multiplier while standing on the ground (1 = same as in the air)")]
    public float GroundCoolMultiplier = 1.5f;

    [Tooltip("After overheating, the jetpack stays locked until heat drops below this fraction of MaxHeat (0-1). 0 = fully cooled")]
    [Range(0f, 1f)]
    public float RecoverThreshold = 0.4f;

    [Header("Fuel")]
    public float MaxFuel = 100f;
    public float StartingFuel = 100f;

    [Tooltip("Fuel used per second while climbing")]
    public float FuelDrainPerSecond = 10f;

    [Tooltip("Fuel use while hovering, compared to climbing (1 = same)")]
    [Range(0f, 1f)] public float HoverFuelMultiplier = 1f;

    [Tooltip("Fuel use while descending, compared to climbing")]
    [Range(0f, 1f)] public float DescendFuelMultiplier = 0.5f;

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
    private float _bobBlend;
    private bool _autoClimb;

    private void Awake()
    {
        _controller = GetComponent<ThirdPersonController>();
        _animator = GetComponentInChildren<Animator>();
        Heat = 0f;
        Fuel = Mathf.Clamp(StartingFuel, 0f, MaxFuel);
        _groundY = transform.position.y;
    }

    private void OnEnable()
    {
        JetpackInput.Enable();
        AscendInput.Enable();
        DescendInput.Enable();
    }

    private void OnDisable()
    {
        JetpackInput.Disable();
        AscendInput.Disable();
        DescendInput.Disable();
    }

    private void Update()
    {
        bool grounded = _controller.Grounded;

        if (grounded)
        {
            _groundY = transform.position.y;

            if (GroundRegenPerSecond > 0f)
            {
                AddFuel(GroundRegenPerSecond * Time.deltaTime);
            }
        }

        bool ascend = AscendInput.IsPressed();
        bool descend = DescendInput.IsPressed();
        if (ascend && descend) ascend = descend = false; // both pressed: cancel out

        // pressing F starts an automatic climb, descending cancels it
        if (JetpackInput.WasPressedThisFrame()) _autoClimb = true;
        if (descend) _autoClimb = false;

        bool canRun = !IsOverheated && Fuel > 0f;
        bool active = JetpackInput.IsPressed() && canRun;
        bool climbing = ascend || _autoClimb;

        if (active)
        {
            // ceiling: climb speed eases to 0 at the max height, negative if above it
            float heightLeft = (_groundY + MaxHeightAboveGround) - transform.position.y;
            float tUp = Mathf.Clamp(heightLeft / Mathf.Max(0.01f, SoftCeilingZone), -1f, 1f);
            float ceilingSpeed = MaxUpwardSpeed * (tUp > 0f ? Mathf.SmoothStep(0f, 1f, tUp) : tUp);

            // floor: descent speed eases to 0 at the min height, positive (pushes up) if below it
            float heightAboveFloor = transform.position.y - (_groundY + MinHeightAboveGround);
            float tDown = Mathf.Clamp(heightAboveFloor / Mathf.Max(0.01f, SoftFloorZone), -1f, 1f);
            float floorSpeed = -MaxDownwardSpeed * (tDown > 0f ? Mathf.SmoothStep(0f, 1f, tDown) : tDown);

            float targetSpeed;
            if (descend)
            {
                targetSpeed = floorSpeed;
            }
            else if (climbing)
            {
                targetSpeed = ceilingSpeed;
            }
            else
            {
                // hover: hold height, but still get pushed back inside the limits
                targetSpeed = 0f;
                if (ceilingSpeed < 0f) targetSpeed = ceilingSpeed;
                if (floorSpeed > 0f) targetSpeed = floorSpeed;
            }

            // gentle bob while hovering (including when a limit blocks movement)
            bool hovering = Mathf.Abs(targetSpeed) < 0.1f * MaxUpwardSpeed;
            _bobBlend = Mathf.MoveTowards(_bobBlend, hovering ? 1f : 0f, Time.deltaTime * 3f);
            float omega = BobFrequency * 2f * Mathf.PI;
            targetSpeed += Mathf.Cos(Time.time * omega) * BobHeight * omega * _bobBlend;

            _controller.SteerVerticalVelocity(targetSpeed, ThrustAcceleration);

            // fuel and heat both depend on what the player is doing
            float fuelRate = climbing ? 1f : (descend ? DescendFuelMultiplier : HoverFuelMultiplier);
            float heatRate = climbing ? 1f : (descend ? DescendHeatMultiplier : HoverHeatMultiplier);

            Fuel = Mathf.Max(0f, Fuel - FuelDrainPerSecond * fuelRate * Time.deltaTime);
            Heat += HeatPerSecond * heatRate * Time.deltaTime;

            if (Heat >= MaxHeat)
            {
                Heat = MaxHeat;
                SetOverheated(true);
                active = false; // stop thrusting this frame
            }
            else if (Fuel <= 0f)
            {
                active = false; // out of fuel
            }
        }
        else
        {
            float cool = CoolPerSecond * (grounded ? GroundCoolMultiplier : 1f);
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