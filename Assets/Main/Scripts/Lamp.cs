using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(LightSource))]
public class Lamp : MonoBehaviour
{
    public static readonly List<Lamp> All = new List<Lamp>();

    [Header("State")]
    public bool StartOn = false;

    [Header("Spotlight")]
    [Tooltip("The lamp's spotlight. Defaults to the first Light on this object or its children")]
    public Light Spotlight;

    [Header("Emissive Material")]
    [Tooltip("Renderer with the glowing lamp material. Defaults to the first renderer found")]
    public Renderer LampRenderer;

    [Tooltip("Which material slot on the renderer is the glowing one")]
    public int MaterialIndex = 0;

    [Header("Transition")]
    [Tooltip("Seconds the light and glow take to fade when toggled. 0 = instant")]
    public float FadeTime = 0.2f;

    [Header("Feedback")]
    public AudioClip ToggleSound;
    [Range(0f, 1f)] public float Volume = 0.6f;

    [Header("Events")]
    public UnityEvent OnTurnedOn;
    public UnityEvent OnTurnedOff;

    public bool IsOn { get; private set; }

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private LightSource _lightSource;
    private Material _material;
    private Color _baseEmission;
    private float _baseIntensity;
    private float _level; // 0 = fully off, 1 = fully on

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => All.Clear();

    private void Awake()
    {
        _lightSource = GetComponent<LightSource>();

        if (Spotlight == null) Spotlight = GetComponentInChildren<Light>();
        if (Spotlight != null) _baseIntensity = Spotlight.intensity;

        if (LampRenderer == null) LampRenderer = GetComponentInChildren<Renderer>();
        if (LampRenderer != null)
        {
            // own material copy, so other objects sharing the material aren't affected
            Material[] materials = LampRenderer.materials;
            _material = materials[Mathf.Clamp(MaterialIndex, 0, materials.Length - 1)];
            _material.EnableKeyword("_EMISSION");
            _baseEmission = _material.GetColor(EmissionColorID);

            if (_baseEmission.maxColorComponent <= 0f)
            {
                Debug.LogWarning("Lamp: the material's Emission color is black, so there is nothing to turn off. " +
                                 "Enable Emission on the material and give it a bright color.", this);
            }
        }

        // set the starting state without any fade or sound
        IsOn = StartOn;
        _lightSource.IsLit = IsOn;
        _level = IsOn ? 1f : 0f;
        ApplyLevel();
    }

    private void Update()
    {
        float target = IsOn ? 1f : 0f;
        if (Mathf.Approximately(_level, target)) return;

        _level = FadeTime <= 0f
            ? target
            : Mathf.MoveTowards(_level, target, Time.deltaTime / FadeTime);

        ApplyLevel();
    }

    public void Toggle() => SetOn(!IsOn);

    public void SetOn(bool on)
    {
        if (on == IsOn) return;
        IsOn = on;

        // electric mushrooms look at this flag, so they react right away
        _lightSource.IsLit = on;

        if (ToggleSound != null)
        {
            AudioSource.PlayClipAtPoint(ToggleSound, transform.position, Volume);
        }

        if (on) OnTurnedOn.Invoke();
        else OnTurnedOff.Invoke();
    }

    private void ApplyLevel()
    {
        if (Spotlight != null)
        {
            Spotlight.enabled = _level > 0.001f;
            Spotlight.intensity = _baseIntensity * _level;
        }

        if (_material != null)
        {
            _material.SetColor(EmissionColorID, _baseEmission * _level);
        }
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}