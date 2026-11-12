using System.Collections.Generic;
using UnityEngine;

public class LightSource : MonoBehaviour
{
    public static readonly List<LightSource> All = new List<LightSource>();

    [Tooltip("Turn off to make this stop counting as a light source without removing it")]
    public bool IsLit = true;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    // keeps the list clean if "Enter Play Mode" is set to skip domain reloads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => All.Clear();
}
