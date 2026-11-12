using StarterAssets;
using UnityEngine;

public class SporeCloud : MonoBehaviour
{
// Goes on the spore cloud PREFAB (the same object as its Particle System).
// While the cloud exists and the player is within Radius of it, the screen effect is applied.
    [Tooltip("How close (meters, world space) the player must be to the cloud for the screen effect to apply")]
    public float Radius = 3f;

    [Tooltip("0 = the effect lasts as long as the cloud exists. Otherwise it stops after this many seconds")]
    public float EffectDuration = 0f;

    private const float ExitHysteresis = 0.3f; // extra meters before the player counts as "left", prevents flicker at the edge

    private static Transform _player;

    private float _age;
    private bool _playerInside;
    private bool _affecting;

    private static Transform GetPlayer()
    {
        if (_player == null)
        {
            ThirdPersonController controller = FindFirstObjectByType<ThirdPersonController>();
            if (controller != null) _player = controller.transform;
        }
        return _player;
    }

    private void Update()
    {
        _age += Time.deltaTime;

        // A plain distance check instead of a trigger collider: it works even if the player is
        // already standing inside the cloud when it appears, and doesn't depend on physics events.
        Transform player = GetPlayer();
        if (player == null)
        {
            _playerInside = false;
        }
        else
        {
            float distance = Vector3.Distance(player.position, transform.position);
            float limit = _playerInside ? Radius + ExitHysteresis : Radius;
            _playerInside = distance <= limit;
        }

        bool active = EffectDuration <= 0f || _age < EffectDuration;
        bool shouldAffect = _playerInside && active;
        if (shouldAffect == _affecting) return;

        SporeVisionEffect effect = SporeVisionEffect.Instance;
        if (effect == null) return;

        _affecting = shouldAffect;
        if (_affecting) effect.Enter();
        else effect.Exit();
    }

    // runs when the cloud is destroyed too, so the effect can't get stuck on
    private void OnDisable()
    {
        if (_affecting && SporeVisionEffect.Instance != null)
        {
            SporeVisionEffect.Instance.Exit();
        }
        _affecting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 1f, 0.3f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}