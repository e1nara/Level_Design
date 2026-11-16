using UnityEngine;
using UnityEngine.Events;

public class ElectricMushroom : MonoBehaviour
{
    [Header("Power")]
    [Tooltip("A lit LightSource within this distance (meters) powers the mushroom")]
    public float DetectionRadius = 5f;

    [Tooltip("How often (seconds) to check for light sources")]
    public float CheckInterval = 0.1f;

    [Header("Emission")]
    [Tooltip("Renderer with the glowing material. Defaults to the first renderer found on this object or its children")]
    public Renderer GlowRenderer;

    [Tooltip("Which material slot on the renderer is the glowing one")]
    public int MaterialIndex = 0;

    [Tooltip("Emission multiplier when powered (1 = the material's own emission color)")]
    public float ActiveIntensity = 1f;

    [Tooltip("Emission multiplier when not powered. Above 0 so it looks dull rather than switched off")]
    public float DullIntensity = 0.08f;

    [Tooltip("Seconds to fade between dull and glowing")]
    public float FadeDuration = 0.7f;

    [Header("Pulse (while powered)")]
    [Tooltip("How deep the pulse is. 0 = steady glow, 0.5 = brightness swings between 50% and 100%")]
    [Range(0f, 1f)] public float PulseDepth = 0.35f;

    [Tooltip("Pulses per second")]
    public float PulseSpeed = 0.8f;

    [Tooltip("Give each mushroom a random starting point in the pulse so several mushrooms don't pulse in perfect sync")]
    public bool RandomizePhase = true;

    [Tooltip("Optional: a Light (e.g. a Point Light on this mushroom) that pulses together with the glow and is off while unpowered")]
    public Light PulseLight;

    [Header("Events (for machines, puzzles, sounds...)")]
    public UnityEvent OnPowered;
    public UnityEvent OnUnpowered;

    public bool IsPowered { get; private set; }

    // true only once the mushroom has finished fading up to its brightest glow
    public bool IsFullyPowered => IsPowered && _level >= 0.999f;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private Material _material;
    private Color _baseEmission;
    private float _level;          // 0 = dull, 1 = fully glowing
    private float _checkTimer;
    private float _pulsePhase;
    private float _baseLightIntensity;

    private void Awake()
    {
        if (RandomizePhase) _pulsePhase = Random.value * Mathf.PI * 2f;
        if (PulseLight != null) _baseLightIntensity = PulseLight.intensity;

        if (GlowRenderer == null) GlowRenderer = GetComponentInChildren<Renderer>();

        if (GlowRenderer == null)
        {
            Debug.LogWarning("ElectricMushroom: no renderer found.", this);
            enabled = false;
            return;
        }

        // .materials gives this mushroom its own material copies, so other mushrooms aren't affected
        Material[] materials = GlowRenderer.materials;
        _material = materials[Mathf.Clamp(MaterialIndex, 0, materials.Length - 1)];
        _material.EnableKeyword("_EMISSION");
        _baseEmission = _material.GetColor(EmissionColorID);

        if (_baseEmission.maxColorComponent <= 0f)
        {
            Debug.LogWarning("ElectricMushroom: the material's Emission color is black, so there is nothing to dim. " +
                             "Enable Emission on the material and give it a bright color.", this);
        }
    }

    private void Start()
    {
        // start in the correct state without fading
        IsPowered = CheckForLight();
        _level = IsPowered ? 1f : 0f;
        ApplyEmission();

        if (IsPowered) OnPowered.Invoke();
    }

    private void Update()
    {
        _checkTimer -= Time.deltaTime;
        if (_checkTimer <= 0f)
        {
            _checkTimer = CheckInterval;

            bool powered = CheckForLight();
            if (powered != IsPowered)
            {
                IsPowered = powered;
                if (IsPowered) OnPowered.Invoke();
                else OnUnpowered.Invoke();
            }
        }

        float target = IsPowered ? 1f : 0f;
        bool fading = !Mathf.Approximately(_level, target);
        if (fading)
        {
            _level = Mathf.MoveTowards(_level, target, Time.deltaTime / Mathf.Max(0.01f, FadeDuration));
        }

        // while any glow is showing, update every frame so the pulse keeps moving
        if (fading || _level > 0f) ApplyEmission();
    }

    private bool CheckForLight()
    {
        float radiusSqr = DetectionRadius * DetectionRadius;

        foreach (LightSource source in LightSource.All)
        {
            if (!source.IsLit) continue;

            if ((source.transform.position - transform.position).sqrMagnitude <= radiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyEmission()
    {
        // smooth wave between (1 - PulseDepth) and 1
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * PulseSpeed * Mathf.PI * 2f + _pulsePhase);
        float pulse = Mathf.Lerp(1f - PulseDepth, 1f, wave);

        // the pulse fades in with the power level, so a dull mushroom doesn't pulse
        float pulseFactor = Mathf.Lerp(1f, pulse, _level);

        float intensity = Mathf.Lerp(DullIntensity, ActiveIntensity, _level) * pulseFactor;
        _material.SetColor(EmissionColorID, _baseEmission * intensity);

        if (PulseLight != null)
        {
            PulseLight.enabled = _level > 0.001f;
            PulseLight.intensity = _baseLightIntensity * _level * pulseFactor;
        }
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, DetectionRadius);
    }
}