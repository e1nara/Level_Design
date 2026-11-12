using StarterAssets;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider))]

public class ForceFieldWall : MonoBehaviour
{
    [Header("Power")]
    [Tooltip("Electric mushrooms that power this wall. Must be objects in the scene, not prefab assets")]
    public ElectricMushroom[] PoweredBy;

    [Tooltip("On: every mushroom must be powered. Off: any one mushroom is enough")]
    public bool RequireAll = true;

    [Header("Fade")]
    [Tooltip("Seconds for the wall to appear once the mushroom is fully glowing")]
    public float FadeInTime = 0.8f;

    [Tooltip("Seconds for the wall to disappear after the mushroom stops being powered")]
    public float FadeOutTime = 3f;

    [Tooltip("Flicker while the wall is fading (0 = none, 1 = strong)")]
    [Range(0f, 1f)] public float FlickerAmount = 0.4f;
    public float FlickerSpeed = 18f;

    [Header("Collision")]
    [Tooltip("While fading out, the wall stops blocking the player once its strength drops below this")]
    [Range(0f, 1f)] public float PassThroughThreshold = 0.35f;

    [Tooltip("While fading in, the wall starts blocking again above this (and only if the player isn't standing inside it)")]
    [Range(0f, 1f)] public float SolidThreshold = 0.6f;

    [Header("Visuals")]
    [Tooltip("Renderer of the wall. Defaults to the first renderer on this object or its children")]
    public Renderer WallRenderer;

    [Header("Debug (read only, watch these in Play mode)")]
    [SerializeField] private float _strength;      // 0 = gone, 1 = fully solid
    [SerializeField] private bool _poweredNow;     // what the mushrooms are telling the wall right now
    [SerializeField] private bool _solid;          // are the wall's colliders currently enabled

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorID = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly Collider[] OverlapBuffer = new Collider[16];

    private Collider[] _colliders;   // every non-trigger collider on this object and its children
    private Collider _collider;      // the first one, used to size the "player inside?" check
    private Bounds _fallbackBounds;
    private Material _material;
    private int _colorID;
    private Color _baseColor;
    private bool _hasEmission;
    private Color _baseEmission;
    private bool _initialized;

    private void Awake()
    {
        // collect ALL solid colliders: if the wall has more than one, disabling just one would still block the player
        var solid = new List<Collider>();
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (!c.isTrigger) solid.Add(c);
        }

        if (solid.Count == 0)
        {
            Debug.LogWarning("ForceFieldWall: no solid collider found on this object or its children.", this);
            enabled = false;
            return;
        }

        _colliders = solid.ToArray();
        _collider = _colliders[0];

        // a disabled collider reports empty bounds, so remember them now
        _fallbackBounds = _collider.bounds;
        for (int i = 1; i < _colliders.Length; i++) _fallbackBounds.Encapsulate(_colliders[i].bounds);

        if (WallRenderer == null) WallRenderer = GetComponentInChildren<Renderer>();
        if (WallRenderer == null)
        {
            Debug.LogWarning("ForceFieldWall: no renderer found.", this);
            enabled = false;
            return;
        }

        // own material copy so other walls using the same material aren't affected
        _material = WallRenderer.material;

        _colorID = _material.HasProperty(BaseColorID) ? BaseColorID : LegacyColorID;
        _baseColor = _material.GetColor(_colorID);

        _hasEmission = _material.HasProperty(EmissionColorID);
        if (_hasEmission)
        {
            _material.EnableKeyword("_EMISSION");
            _baseEmission = _material.GetColor(EmissionColorID);
        }
    }

    private void Update()
    {
        _poweredNow = IsPowered();
        float target = _poweredNow ? 1f : 0f;

        // first frame: all other scripts have run Start() by now, so mushroom states are valid
        if (!_initialized)
        {
            _initialized = true;
            _strength = target;
            SetSolid(_poweredNow);
            ApplyVisuals();
            return;
        }

        float fadeTime = target > _strength ? FadeInTime : FadeOutTime;
        _strength = Mathf.MoveTowards(_strength, target, Time.deltaTime / Mathf.Max(0.01f, fadeTime));

        if (_solid)
        {
            // faded enough for the player to walk through
            if (_strength < PassThroughThreshold) SetSolid(false);
        }
        else if (_poweredNow && _strength >= SolidThreshold && !PlayerInside())
        {
            // don't become solid around a player who is standing in the wall's space
            SetSolid(true);
        }

        ApplyVisuals();
    }

    private void SetSolid(bool solid)
    {
        _solid = solid;
        foreach (Collider c in _colliders)
        {
            if (c != null) c.enabled = solid;
        }
    }

    private bool IsPowered()
    {
        if (PoweredBy == null || PoweredBy.Length == 0) return false;

        bool any = false;
        bool all = true;

        foreach (ElectricMushroom mushroom in PoweredBy)
        {
            bool on = mushroom != null && mushroom.IsFullyPowered;
            any |= on;
            all &= on;
        }

        return RequireAll ? all : any;
    }

    // The wall's colliders are switched off while the player might be inside, so their own
    // shape can't be queried. This returns a box that approximates the wall instead.
    private void GetOverlapBox(out Vector3 center, out Vector3 halfExtents, out Quaternion rotation)
    {
        Vector3 s = _collider.transform.lossyScale;
        Vector3 absScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

        if (_colliders.Length == 1 && _collider is BoxCollider box)
        {
            center = _collider.transform.TransformPoint(box.center);
            halfExtents = Vector3.Scale(box.size * 0.5f, absScale);
            rotation = _collider.transform.rotation;
        }
        else if (_colliders.Length == 1 && _collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
        {
            // the mesh's own bounding box, rotated and scaled with the object
            Bounds b = meshCollider.sharedMesh.bounds;
            center = _collider.transform.TransformPoint(b.center);
            halfExtents = Vector3.Scale(b.extents, absScale);
            rotation = _collider.transform.rotation;
        }
        else
        {
            center = _fallbackBounds.center;
            halfExtents = _fallbackBounds.extents;
            rotation = Quaternion.identity;
        }
    }

    private bool PlayerInside()
    {
        GetOverlapBox(out Vector3 center, out Vector3 halfExtents, out Quaternion rotation);
        halfExtents += Vector3.one * 0.1f; // small margin so the wall doesn't pop in right against the player

        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, OverlapBuffer, rotation,
            ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (OverlapBuffer[i].TryGetComponent(out ThirdPersonController _)) return true;
        }

        return false;
    }

    private void ApplyVisuals()
    {
        float visible = _strength;

        // flicker only while the wall is transitioning, strongest when it's nearly gone
        if (_strength > 0f && _strength < 1f && FlickerAmount > 0f)
        {
            float noise = Mathf.PerlinNoise(Time.time * FlickerSpeed, 0.37f);
            visible *= 1f - FlickerAmount * (1f - _strength) * noise;
        }

        Color color = _baseColor;
        color.a = _baseColor.a * visible;
        _material.SetColor(_colorID, color);

        if (_hasEmission) _material.SetColor(EmissionColorID, _baseEmission * visible);

        WallRenderer.enabled = _strength > 0.001f;
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}