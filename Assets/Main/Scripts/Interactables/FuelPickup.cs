using UnityEngine;

[RequireComponent(typeof(Collider))]

public class FuelPickup : MonoBehaviour
{
    [Tooltip("Fuel given to the jetpack on pickup")]
    public float FuelAmount = 30f;

    [Tooltip("Seconds until the pickup reappears. 0 or less = single use (destroyed)")]
    public float RespawnTime = 0f;

    [Tooltip("Don't collect if the tank is already full")]
    public bool IgnoreWhenFull = true;

    [Header("Visuals (optional)")]
    public float SpinSpeed = 90f;
    public float BobHeight = 0.2f;
    public float BobSpeed = 2f;

    [Header("Effects (optional)")]
    public AudioClip PickupSound;
    public GameObject PickupEffectPrefab;

    private Vector3 _startPos;
    private Collider _collider;
    private Renderer[] _renderers;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        _renderers = GetComponentsInChildren<Renderer>();
        _startPos = transform.position;
    }

    private void Update()
    {
        //// idle spin + bob so the pickup is easy to spot
        //transform.Rotate(Vector3.up, SpinSpeed * Time.deltaTime, Space.World);
        //transform.position = _startPos + Vector3.up * (Mathf.Sin(Time.time * BobSpeed) * BobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        // the jetpack sits on the player root, the collider may be on a child
        Jetpack jetpack = other.GetComponentInParent<Jetpack>();
        if (jetpack == null) return;
        if (IgnoreWhenFull && jetpack.Fuel >= jetpack.MaxFuel) return;

        jetpack.AddFuel(FuelAmount);

        if (PickupSound != null) AudioSource.PlayClipAtPoint(PickupSound, transform.position);
        if (PickupEffectPrefab != null) Instantiate(PickupEffectPrefab, transform.position, Quaternion.identity);

        if (RespawnTime > 0f) StartCoroutine(RespawnRoutine());
        else Destroy(gameObject);
    }

    private System.Collections.IEnumerator RespawnRoutine()
    {
        SetCollected(true);
        yield return new WaitForSeconds(RespawnTime);
        SetCollected(false);
    }

    private void SetCollected(bool collected)
    {
        _collider.enabled = !collected;
        foreach (var r in _renderers) r.enabled = !collected;
    }
}