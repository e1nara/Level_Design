using UnityEngine;
using UnityEngine.Events;

public class Oxygen : MonoBehaviour
{

/// <summary>
/// Goes on the player. Oxygen drains while the player is inside at least one spore cloud,
/// then slowly recharges (after a short delay) once the influence is over.
/// SporeCloud tells this component when the player enters or leaves a cloud.
/// </summary>

    public static Oxygen Instance { get; private set; }

    [Header("Oxygen")]
    public float MaxOxygen = 100f;

    [Tooltip("Oxygen lost per second while inside a spore cloud (overlapping clouds do not stack)")]
    public float DrainPerSecond = 15f;

    [Header("Recharge")]
    [Tooltip("Seconds after leaving the cloud before oxygen starts coming back")]
    public float RechargeDelay = 2f;

    [Tooltip("Oxygen regained per second once recharging. Keep this lower than the drain for a slow recharge")]
    public float RechargePerSecond = 6f;

    [Header("Events")]
    [Tooltip("Fires once when oxygen reaches zero. Hook up damage, a fainting effect, etc.")]
    public UnityEvent OnDepleted;
    [Tooltip("Fires once when oxygen starts coming back after being depleted")]
    public UnityEvent OnRecovered;

    public float oxygen { get; private set; }
    public float OxygenNormalized => oxygen / MaxOxygen;
    public bool IsInSpores => _cloudCount > 0;
    public bool IsDepleted { get; private set; }

    private int _cloudCount;
    private float _timeSinceExposure;

    private void Awake()
    {
        Instance = this;
        oxygen = MaxOxygen;
        _timeSinceExposure = RechargeDelay;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (_cloudCount > 0)
        {
            _timeSinceExposure = 0f;
            oxygen = Mathf.Max(0f, oxygen - DrainPerSecond * Time.deltaTime);

            if (oxygen <= 0f && !IsDepleted)
            {
                IsDepleted = true;
                OnDepleted?.Invoke();
            }
        }
        else
        {
            _timeSinceExposure += Time.deltaTime;

            if (_timeSinceExposure >= RechargeDelay && oxygen < MaxOxygen)
            {
                oxygen = Mathf.Min(MaxOxygen, oxygen + RechargePerSecond * Time.deltaTime);

                if (IsDepleted && oxygen > 0f)
                {
                    IsDepleted = false;
                    OnRecovered?.Invoke();
                }
            }
        }
    }

    // called by SporeCloud. A counter is used so overlapping clouds don't cancel each other out
    public void EnterSpores() => _cloudCount++;
    public void ExitSpores() => _cloudCount = Mathf.Max(0, _cloudCount - 1);
}