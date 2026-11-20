using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Lamp))]
[RequireComponent(typeof(LightSource))]
public class LampFlicker : MonoBehaviour
{
   

/// <summary>
/// Makes a Lamp flicker. The lamp's visuals dip, and (optionally) its LightSource
/// drops out too, so nearby ElectricMushrooms discharge and recharge with the flicker.
/// Add this to the same object as the Lamp.
/// </summary>
  [Header("When")]
    [Tooltip("Flicker randomly while the lamp is on")]
    public bool FlickerWhileOn = true;

    [Tooltip("Seconds of steady light between flicker bursts (random between the two)")]
    public Vector2 CalmTime = new Vector2(2f, 6f);

    [Tooltip("Also stutter for a moment each time the lamp is switched on")]
    public bool FlickerOnTurnOn = true;

    [Header("Burst")]
    [Tooltip("Number of dips in one burst (random between the two, inclusive)")]
    public Vector2Int DipsPerBurst = new Vector2Int(2, 5);

    [Tooltip("Seconds the light is dipped for each dip. For the mushrooms to notice, keep above their Check Interval (0.1s). " +
             "To see them discharge noticeably, use 0.3 - 0.8s (their Fade Duration)")]
    public Vector2 DipTime = new Vector2(0.15f, 0.6f);

    [Tooltip("Seconds of light between dips inside a burst")]
    public Vector2 GapTime = new Vector2(0.1f, 0.4f);

    [Header("Look")]
    [Tooltip("Brightness (0-1) of the lamp during a dip. 0 = fully dark")]
    [Range(0f, 1f)] public float DipBrightness = 0.05f;

    [Tooltip("How fast the visible light moves between bright and dipped (brightness units per second). Higher = harsher")]
    public float VisualSpeed = 20f;

    [Header("Power")]
    [Tooltip("If on, the LightSource also switches off during a dip, so electric mushrooms discharge. " +
             "If off, only the lamp's look flickers")]
    public bool AffectsPower = true;

    [Header("Debug")]
    public bool DebugLog = false;

    private Lamp _lamp;
    private LightSource _lightSource;
    private float _targetFactor = 1f;
    private bool _stutterRequested;
    private Coroutine _routine;

    private void Awake()
    {
        _lamp = GetComponent<Lamp>();
        _lightSource = GetComponent<LightSource>();
    }

    private void OnEnable()
    {
        _lamp.OnTurnedOn.AddListener(HandleTurnedOn);
        _routine = StartCoroutine(Run());
    }

    private void OnDisable()
    {
        _lamp.OnTurnedOn.RemoveListener(HandleTurnedOn);
        if (_routine != null) StopCoroutine(_routine);
        Restore();
    }

    private void HandleTurnedOn()
    {
        if (FlickerOnTurnOn) _stutterRequested = true;
    }

    private void Update()
    {
        // smooth the visible brightness toward the target so dips aren't a single-frame pop
        _lamp.FlickerFactor = Mathf.MoveTowards(_lamp.FlickerFactor, _targetFactor, VisualSpeed * Time.deltaTime);
    }

    private IEnumerator Run()
    {
        while (true)
        {
            // wait for a reason to flicker
            float calm = Random.Range(CalmTime.x, CalmTime.y);
            float waited = 0f;
            while (waited < calm && !_stutterRequested)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            bool stutter = _stutterRequested;
            _stutterRequested = false;

            if (!_lamp.IsOn) continue;
            if (!stutter && !FlickerWhileOn) continue;

            int dips = Random.Range(DipsPerBurst.x, DipsPerBurst.y + 1);
            for (int i = 0; i < dips; i++)
            {
                if (!_lamp.IsOn) break;

                SetDipped(true);
                yield return new WaitForSeconds(Random.Range(DipTime.x, DipTime.y));

                SetDipped(false);
                yield return new WaitForSeconds(Random.Range(GapTime.x, GapTime.y));
            }

            Restore();
        }
    }

    private void SetDipped(bool dipped)
    {
        _targetFactor = dipped ? DipBrightness : 1f;

        // only touch the power flag while the lamp is actually on,
        // otherwise a late flicker could relight a lamp the player just switched off
        if (AffectsPower && _lamp.IsOn)
            _lightSource.IsLit = !dipped;

        if (DebugLog) Debug.Log($"{name}: {(dipped ? "dip" : "lit")}", this);
    }

    private void Restore()
    {
        _targetFactor = 1f;
        if (_lamp != null) _lamp.FlickerFactor = 1f;
        if (_lightSource != null && _lamp != null) _lightSource.IsLit = _lamp.IsOn;
    }
}