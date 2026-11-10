using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(LightSource))]

public class GlowingMushroom : MonoBehaviour
{
    public static readonly List<GlowingMushroom> All = new List<GlowingMushroom>();

    [Header("Break Effect")]
    [Tooltip("Small particle effect prefab played when the mushroom is destroyed")]
    public GameObject BreakEffectPrefab;

    [Tooltip("Where the effect appears. Defaults to this object's position")]
    public Transform EffectPoint;

    [Tooltip("Seconds before the spawned effect is cleaned up")]
    public float EffectLifetime = 3f;

    public AudioClip BreakSound;
    [Range(0f, 1f)] public float Volume = 0.6f;

    private bool _broken;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => All.Clear();

    public void Break()
    {
        if (_broken) return;
        _broken = true;

        Transform point = EffectPoint != null ? EffectPoint : transform;

        if (BreakEffectPrefab != null)
        {
            GameObject fx = Instantiate(BreakEffectPrefab, point.position, point.rotation);
            Destroy(fx, EffectLifetime);
        }

        if (BreakSound != null)
        {
            AudioSource.PlayClipAtPoint(BreakSound, point.position, Volume);
        }

        // destroying the object also removes its LightSource from the registry (OnDisable),
        // so electric mushrooms notice within their next check
        Destroy(gameObject);
    }
}