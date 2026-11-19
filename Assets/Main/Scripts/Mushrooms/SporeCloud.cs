using StarterAssets;
using UnityEngine;

public class SporeCloud : MonoBehaviour
{
 
    // Goes on the spore cloud PREFAB (the same object as its Particle System).
    // While the cloud exists and the player is inside its area, the screen effect is applied
    // and the player's oxygen drains.

    public enum AreaShape { Box, Capsule, Sphere }

    [Header("Area")]
    [Tooltip("Shape of the area that triggers the effect. Match it to the shape of the particle cloud.")]
    public AreaShape Shape = AreaShape.Box;

    [Tooltip("Box only: full size in meters (X, Y, Z)")]
    public Vector3 Size = new Vector3(3f, 2f, 3f);

    [Tooltip("Capsule and Sphere: radius in meters")]
    public float Radius = 3f;

    [Tooltip("Capsule only: total height in meters, including the rounded ends. The capsule stands along the Y axis.")]
    public float Height = 3f;

    [Tooltip("Moves the area relative to the cloud's pivot, in meters")]
    public Vector3 Center = Vector3.zero;

    [Tooltip("On: the area turns with the cloud object. Off: the area always stays upright and axis-aligned.")]
    public bool UseTransformRotation = true;

    [Header("Effect")]
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
            ThirdPersonController controller = FindAnyObjectByType<ThirdPersonController>();
            if (controller != null) _player = controller.transform;
        }
        return _player;
    }

    private Quaternion AreaRotation => UseTransformRotation ? transform.rotation : Quaternion.identity;

    /// <summary>
    /// True if the world-space point is inside the area. 'margin' grows the area on every side,
    /// which is how the exit hysteresis is applied. The area uses meters and ignores the object's scale.
    /// </summary>
    private bool IsInside(Vector3 worldPoint, float margin)
    {
        // Convert the point into the area's own space (rotation only, so the size stays in world meters).
        Vector3 p = Quaternion.Inverse(AreaRotation) * (worldPoint - transform.position) - Center;

        switch (Shape)
        {
            case AreaShape.Box:
                {
                    Vector3 half = Size * 0.5f;
                    return Mathf.Abs(p.x) <= half.x + margin
                        && Mathf.Abs(p.y) <= half.y + margin
                        && Mathf.Abs(p.z) <= half.z + margin;
                }

            case AreaShape.Capsule:
                {
                    // A capsule is a line segment (the straight middle part) with a radius around it.
                    float segment = Mathf.Max(0f, Height * 0.5f - Radius);
                    float clampedY = Mathf.Clamp(p.y, -segment, segment);
                    float r = Radius + margin;
                    Vector3 offset = new Vector3(p.x, p.y - clampedY, p.z);
                    return offset.sqrMagnitude <= r * r;
                }

            default: // Sphere
                {
                    float r = Radius + margin;
                    return p.sqrMagnitude <= r * r;
                }
        }
    }

    private void Update()
    {
        _age += Time.deltaTime;

        // A plain geometry check instead of a trigger collider: it works even if the player is
        // already standing inside the cloud when it appears, and doesn't depend on physics events.
        Transform player = GetPlayer();
        if (player == null)
        {
            _playerInside = false;
        }
        else
        {
            _playerInside = IsInside(player.position, _playerInside ? ExitHysteresis : 0f);
        }

        bool active = EffectDuration <= 0f || _age < EffectDuration;
        bool shouldAffect = _playerInside && active;
        if (shouldAffect == _affecting) return;

        SporeVisionEffect effect = SporeVisionEffect.Instance;
        Oxygen oxygen = Oxygen.Instance;
        if (effect == null && oxygen == null) return;

        _affecting = shouldAffect;
        if (_affecting)
        {
            if (effect != null) effect.Enter();
            if (oxygen != null) oxygen.EnterSpores();
        }
        else
        {
            if (effect != null) effect.Exit();
            if (oxygen != null) oxygen.ExitSpores();
        }
    }

    // runs when the cloud is destroyed too, so the effect and oxygen drain can't get stuck on
    private void OnDisable()
    {
        if (_affecting)
        {
            if (SporeVisionEffect.Instance != null) SporeVisionEffect.Instance.Exit();
            if (Oxygen.Instance != null) Oxygen.Instance.ExitSpores();
        }
        _affecting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 1f, 0.3f, 0.35f);

        // Draw in the area's own space so the gizmo matches exactly what IsInside tests.
        Gizmos.matrix = Matrix4x4.TRS(transform.position, AreaRotation, Vector3.one);

        switch (Shape)
        {
            case AreaShape.Box:
                Gizmos.DrawWireCube(Center, Size);
                break;

            case AreaShape.Capsule:
                {
                    float segment = Mathf.Max(0f, Height * 0.5f - Radius);
                    Vector3 top = Center + Vector3.up * segment;
                    Vector3 bottom = Center - Vector3.up * segment;

                    Gizmos.DrawWireSphere(top, Radius);
                    Gizmos.DrawWireSphere(bottom, Radius);

                    Gizmos.DrawLine(top + Vector3.right * Radius, bottom + Vector3.right * Radius);
                    Gizmos.DrawLine(top - Vector3.right * Radius, bottom - Vector3.right * Radius);
                    Gizmos.DrawLine(top + Vector3.forward * Radius, bottom + Vector3.forward * Radius);
                    Gizmos.DrawLine(top - Vector3.forward * Radius, bottom - Vector3.forward * Radius);
                    break;
                }

            default:
                Gizmos.DrawWireSphere(Center, Radius);
                break;
        }
    }
}