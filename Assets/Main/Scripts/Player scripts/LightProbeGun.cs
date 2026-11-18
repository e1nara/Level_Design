using UnityEngine;
using UnityEngine.InputSystem;

public class LightProbeGun : MonoBehaviour
{
/// <summary>
/// Put this on the player. Left mouse fires the probe at the crosshair,
/// right mouse retrieves it. Only one probe exists, and it is reused.
/// </summary>

    [Header("References")]
    [SerializeField] private LightProbeProjectile probePrefab;
    [Tooltip("Where the probe leaves from (e.g. an empty at hand height). Optional.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("The camera the crosshair is aimed through. Defaults to Camera.main.")]
    [SerializeField] private Camera aimCamera;

    [Header("Shooting")]
    [SerializeField] private float launchSpeed = 30f;
    [SerializeField] private float maxAimDistance = 100f;
    [Tooltip("What the aim ray can hit. Exclude the Player layer.")]
    [SerializeField] private LayerMask aimMask = ~0;
    [SerializeField] private float fireCooldown = 0.2f;

    [Header("Animation")]
    [Tooltip("Defaults to the Animator on this object.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string shootTrigger = "Shoot";
    [SerializeField] private string recallTrigger = "Recall";
    [Tooltip("Seconds between pressing fire and the probe leaving. If your clip has a " +
             "ReleaseProbe animation event, set this a bit longer than the clip as a safety net.")]
    [SerializeField] private float releaseDelay = 0.15f;
    [Tooltip("Turn the player to face the camera direction when shooting.")]
    [SerializeField] private bool faceAimOnFire = true;

    private LightProbeProjectile probe;
    private Collider[] ownColliders;
    private float nextFireTime;
    private bool firing;
    private int shootHash;
    private int recallHash;

    private void Start()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        if (animator == null) animator = GetComponent<Animator>();

        shootHash = Animator.StringToHash(shootTrigger);
        recallHash = Animator.StringToHash(recallTrigger);

        ownColliders = GetComponentsInChildren<Collider>();

        probe = Instantiate(probePrefab);
        probe.gameObject.SetActive(false);
        probe.Lost += Retrieve;
    }

    private void OnDestroy()
    {
        if (probe != null) probe.Lost -= Retrieve;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame) Fire();
        if (mouse.rightButton.wasPressedThisFrame) Retrieve();
    }

    private void Fire()
    {
        // One probe at a time, and not while a throw is already in progress.
        if (probe.gameObject.activeSelf || firing) return;
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireCooldown;

        firing = true;

        if (faceAimOnFire && aimCamera != null)
            transform.rotation = Quaternion.Euler(0f, aimCamera.transform.eulerAngles.y, 0f);

        if (animator != null) animator.SetTrigger(shootHash);

        // Fallback timer. If an animation event calls ReleaseProbe() first, this is cancelled.
        Invoke(nameof(ReleaseProbe), animator != null ? releaseDelay : 0f);
    }

    /// <summary>
    /// Actually launches the probe. Call this from an animation event on the shoot clip,
    /// at the frame where the hand lets go. The aim is read at this moment, so it follows
    /// the crosshair during the wind-up.
    /// </summary>
    public void ReleaseProbe()
    {
        if (!firing) return;
        CancelInvoke(nameof(ReleaseProbe));
        firing = false;

        Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.4f;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint = Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask,
                                           QueryTriggerInteraction.Ignore)
            ? hit.point
            : ray.GetPoint(maxAimDistance);

        Vector3 direction = (aimPoint - origin).normalized;
        probe.Launch(origin, direction * launchSpeed, ownColliders);
    }

    private void Retrieve()
    {
        // Cancel a throw that hasn't released yet.
        if (firing)
        {
            CancelInvoke(nameof(ReleaseProbe));
            firing = false;
            return;
        }

        if (!probe.gameObject.activeSelf) return;

        probe.Recall();
        if (animator != null) animator.SetTrigger(recallHash);
    }
}