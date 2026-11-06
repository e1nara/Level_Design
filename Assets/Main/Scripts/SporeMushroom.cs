using StarterAssets;
using UnityEngine;
 using System.Collections;

public class SporeMushroom : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("How close the player must get (meters) for the screen effect to apply. Measured from this object's pivot")]
    public float TriggerRadius = 3f;

    [Tooltip("Seconds before the mushroom can puff again")]
    public float Cooldown = 5f;

    [Header("Spores")]
    [Tooltip("Particle system PREFAB (from the Project window). A new copy is created for every puff")]
    public ParticleSystem SporePrefab;

    [Tooltip("Where the spores appear, e.g. an empty child at the cap. Defaults to this object")]
    public Transform SporeOrigin;

    [Tooltip("How many spore particles are released per puff")]
    public int SporeCount = 60;

    [Tooltip("Delay between the shake starting and the spores releasing")]
    public float SporeDelay = 0.35f;

    [Tooltip("How long (seconds) the spore cloud counts as 'in the air' after a puff. The screen effect only applies during this time. Match it to your particles' lifetime")]
    public float CloudDuration = 6f;

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

    private void Awake()
    {
        _originalRotation = transform.localRotation;
        _originalScale = transform.localScale;
    }

    [Header("Touch")]
    [Tooltip("Which layers can set off the puff by touching the mushroom. Default: everything")]
    public LayerMask TouchLayers = ~0;

    private const float ExitHysteresis = 0.3f; // extra meters before the player counts as "left", prevents flicker at the edge

    private static Transform _player;

    private bool _playerInside;   // player is inside the radius
    private bool _affecting;      // this mushroom is currently applying the screen effect
    private float _cloudEndTime = -1f;

    private static Transform GetPlayer()
    {
        if (_player == null)
        {
            ThirdPersonController controller = FindFirstObjectByType<ThirdPersonController>();
            if (controller != null) _player = controller.transform;
        }
        return _player;
    }

    // A plain distance check instead of a trigger collider: it doesn't depend on physics events,
    // so the shaking and swelling of the mushroom can't make it flicker
    private void UpdatePlayerInside()
    {
        Transform player = GetPlayer();
        if (player == null)
        {
            _playerInside = false;
            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);
        float limit = _playerInside ? TriggerRadius + ExitHysteresis : TriggerRadius;
        _playerInside = distance <= limit;
    }

    // The screen effect applies only while the player is in the radius AND spores are in the air
    private void Update()
    {
        UpdatePlayerInside();

        bool shouldAffect = _playerInside && Time.time < _cloudEndTime;
        if (shouldAffect == _affecting) return;

        SporeVisionEffect effect = SporeVisionEffect.Instance;
        if (effect == null) return;

        _affecting = shouldAffect;
        if (_affecting) effect.Enter();
        else effect.Exit();
    }

    // Called by SporeTouchSensor when something touches the mushroom
    public void NotifyTouched(Collider other)
    {
        if ((TouchLayers.value & (1 << other.gameObject.layer)) == 0) return;
        TriggerPuff();
    }

    public void TriggerPuff()
    {
        if (_routine != null) return; // already puffing
        if (Time.time - _lastPuffTime < Cooldown) return;

        _lastPuffTime = Time.time;
        _routine = StartCoroutine(PuffRoutine());
    }

    private void OnDisable()
    {
        // if the mushroom is destroyed or disabled while affecting the player, don't leave the effect stuck on
        _playerInside = false;
        if (_affecting && SporeVisionEffect.Instance != null)
        {
            SporeVisionEffect.Instance.Exit();
        }
        _affecting = false;
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
        _cloudEndTime = Time.time + CloudDuration;

        if (SporePrefab != null)
        {
            Transform origin = SporeOrigin != null ? SporeOrigin : transform;

            // keep the prefab's own rotation (e.g. cone pointing up) on top of the origin's rotation
            ParticleSystem spores = Instantiate(
                SporePrefab,
                origin.position,
                origin.rotation * SporePrefab.transform.rotation);

            spores.Play();
            spores.Emit(SporeCount);

            // remove the copy once the last particle has faded out
            var main = spores.main;
            Destroy(spores.gameObject, main.duration + main.startLifetime.constantMax);
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