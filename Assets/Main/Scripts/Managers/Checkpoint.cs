using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Collider))]

public class Checkpoint : MonoBehaviour
{ 
/// <summary>
/// Put on an object with a trigger collider. Once the player has walked through it,
/// it becomes a valid respawn point.
/// </summary>

    [Tooltip("Where the player reappears. Leave empty to use this object's own position and rotation")]
    [SerializeField] private Transform spawnPoint;

    [Header("Events")]
    [Tooltip("Fires the first time the player passes this checkpoint (change a light color, play a sound, etc.)")]
    public UnityEvent OnReached;

    public bool IsReached { get; private set; }

    public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
    public Quaternion SpawnRotation => spawnPoint != null ? spawnPoint.rotation : transform.rotation;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsReached || !other.CompareTag("Player")) return;
        if (CheckpointManager.Instance == null) return;

        IsReached = true;
        CheckpointManager.Instance.Reach(this);
        OnReached?.Invoke();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsReached ? new Color(0.2f, 1f, 0.3f, 0.9f) : new Color(1f, 0.8f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(SpawnPosition, 0.4f);
        Gizmos.DrawLine(SpawnPosition, SpawnPosition + SpawnRotation * Vector3.forward);
    }
}