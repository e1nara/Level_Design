using StarterAssets;
using UnityEngine;
 using System.Collections;
[RequireComponent(typeof(SphereCollider))]

public class SporeMushroom : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("How close the player must get (meters). Applied to the trigger collider")]
    public float TriggerRadius = 3f;

    [Tooltip("Seconds before the mushroom can puff again")]
    public float Cooldown = 5f;

    [Header("Spores")]
    [Tooltip("Particle system placed at the cap of the mushroom")]
    public ParticleSystem SporeParticles;

    [Tooltip("How many spore particles are released per puff")]
    public int SporeCount = 60;

    [Tooltip("Delay between the shake starting and the spores releasing")]
    public float SporeDelay = 0.35f;

    public AudioClip PuffSound;
    [Range(0f, 1f)] public float Volume = 0.5f;

    [Header("Shake")]
    public float ShakeDuration = 0.7f;

    [Tooltip("Max tilt in degrees")]
    public float ShakeAngle = 8f;

    [Tooltip("How fast the mushroom wobbles")]
    public float ShakeFrequency = 35f;

    [Tooltip("How much the mushroom swells when it releases spores (0.15 = 15%)")]
    public float PuffScale = 0.15f;

    private Quaternion _originalRotation;
    private Vector3 _originalScale;
    private float _lastPuffTime = -100f;
    private Coroutine _routine;

    private void Reset()
    {
        SphereCollider trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = TriggerRadius;
    }

    private void Awake()
    {
        _originalRotation = transform.localRotation;
        _originalScale = transform.localScale;

        SphereCollider trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        // radius is in local space, so compensate for the object's scale
        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        trigger.radius = TriggerRadius / Mathf.Max(0.0001f, maxScale);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_routine != null) return; // already puffing
        if (Time.time - _lastPuffTime < Cooldown) return;
        if (!other.TryGetComponent(out ThirdPersonController _)) return;

        _lastPuffTime = Time.time;
        _routine = StartCoroutine(PuffRoutine());
    }

    private IEnumerator PuffRoutine()
    {
        float t = 0f;
        bool released = false;
        float releaseTime = 0f;

        while (t < ShakeDuration)
        {
            t += Time.deltaTime;
            float normalized = t / ShakeDuration;
            float damper = 1f - normalized; // wobble fades out

            // wobble around two axes at slightly different speeds so it isn't a perfect circle
            float tiltX = Mathf.Sin(t * ShakeFrequency) * ShakeAngle * damper;
            float tiltZ = Mathf.Cos(t * ShakeFrequency * 0.8f) * ShakeAngle * damper;
            transform.localRotation = _originalRotation * Quaternion.Euler(tiltX, 0f, tiltZ);

            if (!released && t >= SporeDelay)
            {
                released = true;
                releaseTime = t;
                ReleaseSpores();
            }

            // quick swell that decays right after the release
            float swell = released ? Mathf.Exp(-10f * (t - releaseTime)) * PuffScale : 0f;
            transform.localScale = _originalScale * (1f + swell);

            yield return null;
        }

        // in case the delay is longer than the shake
        if (!released) ReleaseSpores();

        transform.localRotation = _originalRotation;
        transform.localScale = _originalScale;
        _routine = null;
    }

    private void ReleaseSpores()
    {
        if (SporeParticles != null)
        {
            SporeParticles.Play();
            SporeParticles.Emit(SporeCount);
        }

        if (PuffSound != null)
        {
            AudioSource.PlayClipAtPoint(PuffSound, transform.position, Volume);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 1f, 0.3f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, TriggerRadius);
    }
}
