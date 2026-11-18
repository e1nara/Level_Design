using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class LightProbeProjectile : MonoBehaviour
{ 
/// <summary>
/// The light probe itself. Flies after Launch(), sticks to the first surface it hits,
/// and follows that surface if it moves (elevators, etc.). Recall() deactivates it.
/// Put a LightSource component and a Point Light child on the same prefab:
/// while the object is active it counts as light, when recalled it switches off.
/// </summary>

    [Header("Flight")]
    [Tooltip("Off = flies in a straight line to the crosshair. On = arcs downward.")]
    [SerializeField] private bool useGravity = false;
    [Tooltip("If it hasn't stuck to anything after this many seconds, it returns to the player.")]
    [SerializeField] private float maxFlightTime = 6f;

    [Header("Sticking")]
    [Tooltip("Distance of the probe's centre from the surface. Set to the collider radius to sit flush.")]
    [SerializeField] private float surfaceOffset = 0.1f;
    [Tooltip("Layers the probe can stick to. Layers not included just bounce it off.")]
    [SerializeField] private LayerMask stickMask = ~0;

    public bool IsStuck => stuck;

    /// <summary>Raised if the probe flew too long without sticking (e.g. fell into the void).</summary>
    public event Action Lost;

    private Rigidbody rb;
    private SphereCollider col;

    private bool stuck;
    private float launchTime;

    // The surface we're attached to, and our pose relative to it.
    private Transform target;
    private Vector3 localPos;
    private Quaternion localRot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<SphereCollider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Launch(Vector3 position, Vector3 velocity, Collider[] ignore)
    {
        stuck = false;
        target = null;

        gameObject.SetActive(true);

        rb.isKinematic = false;
        rb.useGravity = useGravity;
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = velocity;   // Unity 2022 and older: rb.velocity
        rb.angularVelocity = Vector3.zero;

        if (ignore != null)
        {
            foreach (Collider c in ignore)
                if (c != null) Physics.IgnoreCollision(col, c, true);
        }

        launchTime = Time.time;
    }

    public void Recall()
    {
        stuck = false;
        target = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!stuck && Time.time - launchTime > maxFlightTime)
            Lost?.Invoke();
    }

    private void LateUpdate()
    {
        if (!stuck) return;

        // The surface was destroyed or switched off (e.g. a smashed mushroom): drop the probe.
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Detach();
            return;
        }

        transform.SetPositionAndRotation(
            target.TransformPoint(localPos),
            target.rotation * localRot);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (stuck) return;
        if ((stickMask.value & (1 << collision.gameObject.layer)) == 0) return;

        ContactPoint contact = collision.GetContact(0);
        Stick(contact.point, contact.normal, collision.collider.transform);
    }

    private void Stick(Vector3 point, Vector3 normal, Transform surface)
    {
        stuck = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Vector3 pos = point + normal * surfaceOffset;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        transform.SetPositionAndRotation(pos, rot);

        // Follow the surface manually instead of parenting. Parenting would destroy the
        // probe along with its parent and can skew it on non-uniformly scaled objects.
        target = surface;
        localPos = surface.InverseTransformPoint(pos);
        localRot = Quaternion.Inverse(surface.rotation) * rot;
    }

    private void Detach()
    {
        stuck = false;
        target = null;
        rb.isKinematic = false;
        rb.useGravity = true;       // it falls until it finds something else or times out
        launchTime = Time.time;
    }
}