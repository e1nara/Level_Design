
using UnityEngine;
using UnityEngine.UI;

public class JetpackUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-find the Jetpack in the scene")]
    public Jetpack Jetpack;

    [Header("Fuel Bar")]
    [Tooltip("Image with Image Type = Filled")]
    public Image FuelFill;
    public Color FuelColor = new Color(0.2f, 0.8f, 1f);
    public Color FuelLowColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Below this fraction the fuel bar turns red and flashes")]
    [Range(0f, 1f)] public float LowFuelThreshold = 0.2f;
    public float LowFuelFlashSpeed = 6f;
    [Tooltip("Optional text shown when fuel is empty, e.g. 'OUT OF FUEL'")]
    public GameObject OutOfFuelLabel;

    [Header("Heat Bar")]
    [Tooltip("Image with Image Type = Filled")]
    public Image HeatFill;
    [Tooltip("Optional: a sprite icon (e.g. the jetpack) that is tinted by the heat instead of filled. Can be used with or without Heat Fill")]
    public Image HeatIcon;
    [Tooltip("Color from cool (left) to overheated (right)")]
    public Gradient HeatGradient;
    public Color OverheatedColor = new Color(1f, 0.1f, 0.1f);
    public float OverheatFlashSpeed = 8f;
    [Tooltip("Optional text shown while overheated, e.g. 'OVERHEATED'")]
    public GameObject OverheatedLabel;

    [Header("Smoothing")]
    [Tooltip("How fast the bars catch up to the real values")]
    public float FillSmoothing = 12f;

    private float _fuelShown;
    private float _heatShown;

    private void Reset()
    {
        // sensible default gradient: yellow -> orange -> red
        HeatGradient = new Gradient();
        HeatGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.6f),
                new GradientColorKey(new Color(1f, 0.1f, 0.1f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
    }

    private void Start()
    {
        if (Jetpack == null) Jetpack = FindFirstObjectByType<Jetpack>();
        if (Jetpack == null)
        {
            Debug.LogWarning("JetpackUI: no Jetpack found in the scene.", this);
            enabled = false;
            return;
        }

        _fuelShown = Jetpack.FuelNormalized;
        _heatShown = Jetpack.HeatNormalized;
    }

    private void Update()
    {
        float lerp = 1f - Mathf.Exp(-FillSmoothing * Time.deltaTime);
        _fuelShown = Mathf.Lerp(_fuelShown, Jetpack.FuelNormalized, lerp);
        _heatShown = Mathf.Lerp(_heatShown, Jetpack.HeatNormalized, lerp);

        UpdateFuel();
        UpdateHeat();
    }

    private void UpdateFuel()
    {
        bool empty = Jetpack.Fuel <= 0f;
        bool low = Jetpack.FuelNormalized <= LowFuelThreshold;

        if (FuelFill != null)
        {
            FuelFill.fillAmount = _fuelShown;

            if (low)
            {
                // pulse between normal and low color
                float pulse = (Mathf.Sin(Time.time * LowFuelFlashSpeed) + 1f) * 0.5f;
                FuelFill.color = Color.Lerp(FuelLowColor, FuelColor, pulse * (empty ? 0f : 1f));
            }
            else
            {
                FuelFill.color = FuelColor;
            }
        }

        if (OutOfFuelLabel != null) OutOfFuelLabel.SetActive(empty);
    }

    private void UpdateHeat()
    {
        Color heatColor;
        if (Jetpack.IsOverheated)
        {
            // flash while locked out
            float pulse = (Mathf.Sin(Time.time * OverheatFlashSpeed) + 1f) * 0.5f;
            heatColor = Color.Lerp(OverheatedColor, Color.white, pulse * 0.6f);
        }
        else
        {
            heatColor = HeatGradient.Evaluate(_heatShown);
        }

        if (HeatFill != null)
        {
            HeatFill.fillAmount = _heatShown;
            HeatFill.color = heatColor;
        }

        if (HeatIcon != null)
        {
            HeatIcon.color = heatColor;
        }

        if (OverheatedLabel != null) OverheatedLabel.SetActive(Jetpack.IsOverheated);
    }
}