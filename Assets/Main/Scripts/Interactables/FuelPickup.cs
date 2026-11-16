using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]

public class FuelPickup : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Key interactKey = Key.E;

    [Tooltip("Seconds before the station can be used again. 0 = reusable immediately")]
    [SerializeField] private float cooldown = 0f;

    [Tooltip("Don't refill (and hide the prompt) if the tank is already full")]
    [SerializeField] private bool ignoreWhenFull = true;

    [Header("Effects (optional)")]
    [SerializeField] private AudioClip refillSound;
    [SerializeField] private GameObject refillEffectPrefab;
    [Tooltip("Where the effect spawns. Leave empty to use this object's position")]
    [SerializeField] private Transform effectPoint;

    private Jetpack _jetpackInRange;
    private float _readyTime;
    private bool _promptShown;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (_jetpackInRange == null) return;

        bool canUse = CanUse(_jetpackInRange);
        SetPrompt(canUse);

        if (canUse && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            Refuel(_jetpackInRange);
        }
    }

    private bool CanUse(Jetpack jetpack)
    {
        if (Time.time < _readyTime) return false;
        if (ignoreWhenFull && jetpack.Fuel >= jetpack.MaxFuel) return false;
        return true;
    }

    private void Refuel(Jetpack jetpack)
    {
        jetpack.AddFuel(jetpack.MaxFuel); // clamped, so the tank ends up exactly full
        _readyTime = Time.time + cooldown;

        Vector3 pos = effectPoint != null ? effectPoint.position : transform.position;
        if (refillSound != null) AudioSource.PlayClipAtPoint(refillSound, pos);
        if (refillEffectPrefab != null) Instantiate(refillEffectPrefab, pos, Quaternion.identity);

        // hide the prompt right away if it can't be used again (full tank / cooldown)
        SetPrompt(CanUse(jetpack));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // this is what Update needs: without it the key press is never handled
        _jetpackInRange = other.GetComponentInParent<Jetpack>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _jetpackInRange = null;
        SetPrompt(false);
    }

    private void OnDisable()
    {
        _jetpackInRange = null;
        SetPrompt(false);
    }

    // only talks to the UI when the state actually changes
    private void SetPrompt(bool visible)
    {
        if (visible == _promptShown) return;
        _promptShown = visible;

        if (LetterUI.Instance != null) LetterUI.Instance.ShowUsePrompt(visible);
    }
}