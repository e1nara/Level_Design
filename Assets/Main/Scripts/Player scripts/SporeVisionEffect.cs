using UnityEngine;

public class SporeVisionEffect : MonoBehaviour
{
    public static SporeVisionEffect Instance { get; private set; }

    [Header("Material")]
    [Tooltip("The material using the Custom/SporeDistortion shader (same one assigned to the Full Screen Pass)")]
    public Material EffectMaterial;

    [Header("Timing")]
    [Tooltip("Seconds to reach full effect after entering the spores")]
    public float FadeInTime = 0.8f;

    [Tooltip("Seconds the effect stays at full strength after leaving the spore area")]
    public float LingerTime = 3f;

    [Tooltip("Seconds it takes to fade back to normal after the linger time")]
    public float FadeOutTime = 2f;

    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");

    private int _insideCount;
    private float _lingerTimer;
    private float _intensity;

    private void Awake()
    {
        Instance = this;
        SetIntensity(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SetIntensity(0f); // otherwise the shared material keeps its value in the editor
    }

    // called by mushrooms when the player enters / leaves their radius
    public void Enter() => _insideCount++;
    public void Exit() => _insideCount = Mathf.Max(0, _insideCount - 1);

    private void Update()
    {
        float target;

        if (_insideCount > 0)
        {
            _lingerTimer = LingerTime;
            target = 1f;
        }
        else if (_lingerTimer > 0f)
        {
            _lingerTimer -= Time.deltaTime;
            target = 1f;
        }
        else
        {
            target = 0f;
        }

        float fadeTime = target > _intensity ? FadeInTime : FadeOutTime;
        float step = Time.deltaTime / Mathf.Max(0.01f, fadeTime);
        float newIntensity = Mathf.MoveTowards(_intensity, target, step);

        if (!Mathf.Approximately(newIntensity, _intensity))
        {
            SetIntensity(newIntensity);
        }
    }

    private void SetIntensity(float value)
    {
        _intensity = value;
        if (EffectMaterial != null) EffectMaterial.SetFloat(IntensityID, value);
    }
}
