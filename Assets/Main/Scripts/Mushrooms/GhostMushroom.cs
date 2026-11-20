using UnityEngine;

public class GhostMushroom : MonoBehaviour
{
/// <summary>
/// A mushroom that is solid and more opaque while its ElectricMushroom is powered,
/// and a faint, non-solid ghost while it is not.
/// Put this on the ghost mushroom root. Colliders and renderers are found in children.
/// </summary>
  [Header("Power source (must be a SCENE object, not a prefab asset)")]
    [SerializeField] private ElectricMushroom poweredBy;
    [Tooltip("If true, the ghost only solidifies when the mushroom is FULLY powered.")]
    [SerializeField] private bool requireFullPower = false;

    [Header("Look")]
    [Range(0f, 1f)][SerializeField] private float inactiveAlpha = 0.15f;
    [Range(0f, 1f)][SerializeField] private float activeAlpha = 0.7f;
    [SerializeField] private float inactiveEmission = 0.3f;
    [SerializeField] private float activeEmission = 2.0f;
    [Tooltip("Emission colour. Leave as the material's colour if unsure.")]
    [SerializeField] private Color emissionColor = new Color(0.4f, 0.9f, 1f);
    [Tooltip("How fast the look fades IN when powered (blend units per second).")]
    [SerializeField] private float fadeSpeed = 2f;
    [Tooltip("How fast the look fades OUT when power is lost. Lower = the ghost lingers longer.")]
    [SerializeField] private float fadeOutSpeed = 1f;

    [Header("Collision (blend: 0 = fully faded, 1 = fully active)")]
    [Tooltip("Colliders switch ON once the blend reaches this value (ghost looks nearly solid).")]
    [Range(0f, 1f)][SerializeField] private float colliderOnThreshold = 0.9f;
    [Tooltip("Colliders switch OFF only once the blend falls to this value (ghost looks nearly faded out). " +
             "Keep this below the ON threshold.")]
    [Range(0f, 1f)][SerializeField] private float colliderOffThreshold = 0.1f;
    [Tooltip("Optional. Leave empty to auto-find all colliders in children.")]
    [SerializeField] private Collider[] colliders;
    [Tooltip("Optional. Leave empty to auto-find all renderers in children.")]
    [SerializeField] private Renderer[] renderers;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private MaterialPropertyBlock block;
    private Color[] baseColors;
    private float blend;           // 0 = inactive, 1 = active
    private bool collidersOn;
    private bool initialized;

    private void Reset()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Awake()
    {
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>();
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();

        block = new MaterialPropertyBlock();

        // Remember each renderer's original base colour (RGB). Alpha is overridden.
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].sharedMaterial;
            if (mat != null && mat.HasProperty(BaseColorId)) baseColors[i] = mat.GetColor(BaseColorId);
            else if (mat != null && mat.HasProperty(ColorId)) baseColors[i] = mat.GetColor(ColorId);
            else baseColors[i] = Color.white;
        }

        // Start inactive. The real state is snapped in on the first Update, because
        // ElectricMushroom only works out IsPowered in its own Start(), and the order
        // of Start() calls between different scripts is not guaranteed.
        blend = 0f;
        collidersOn = true; // forces the first Apply() to switch the colliders off
        Apply();
    }

    private void Update()
    {
        float target = IsSourcePowered() ? 1f : 0f;
        float previous = blend;

        if (!initialized)
        {
            // First frame: all Start() methods have run, so IsPowered is now correct.
            initialized = true;
            blend = target;
        }
        else
        {
            float speed = target > blend ? fadeSpeed : fadeOutSpeed;
            blend = Mathf.MoveTowards(blend, target, speed * Time.deltaTime);
        }

        if (!Mathf.Approximately(previous, blend) || debugLog)
            Apply();
    }

    private bool IsSourcePowered()
    {
        if (poweredBy == null) return false;
        return requireFullPower ? poweredBy.IsFullyPowered : poweredBy.IsPowered;
    }

    private void Apply()
    {
        float alpha = Mathf.Lerp(inactiveAlpha, activeAlpha, blend);
        float emission = Mathf.Lerp(inactiveEmission, activeEmission, blend);
        Color emissive = emissionColor * emission;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            Color c = baseColors[i];
            c.a = alpha;

            renderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            block.SetColor(EmissionId, emissive);
            renderers[i].SetPropertyBlock(block);
        }

        // Hysteresis: turn on only when nearly fully active, turn off only when nearly
        // fully faded. In between, keep whatever state the colliders already have.
        if (!collidersOn && blend >= colliderOnThreshold) SetColliders(true);
        else if (collidersOn && blend <= colliderOffThreshold) SetColliders(false);
    }

    private void SetColliders(bool on)
    {
        collidersOn = on;
        foreach (var col in colliders)
            if (col != null) col.enabled = on;

        if (debugLog) Debug.Log($"{name}: colliders {(on ? "ON" : "OFF")} (blend {blend:0.00})", this);
    }
}