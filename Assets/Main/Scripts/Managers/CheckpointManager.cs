using StarterAssets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckpointManager : MonoBehaviour
{
/// <summary>
/// One per scene. Remembers which checkpoints the player has passed and respawns the player
/// at the nearest passed checkpoint (nearest to where they died). If no checkpoint has been
/// passed yet, the player goes back to where they started.
/// </summary>

    public static CheckpointManager Instance { get; private set; }

    [Tooltip("Leave empty to auto-find the player (the object with ThirdPersonController)")]
    public Transform Player;

    [Tooltip("Ignore further deaths for this many seconds after a respawn, so the player can't be killed twice in a row")]
    public float RespawnProtection = 0.5f;

    [Header("Events")]
    [Tooltip("Fires right after the player has been moved. Use it to reset fuel, heat, oxygen, play a sound, fade the screen, etc.")]
    public UnityEvent OnRespawned;

    private readonly List<Checkpoint> _reached = new List<Checkpoint>();
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private CharacterController _characterController;
    private float _lastRespawnTime = -999f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Player == null)
        {
            ThirdPersonController controller = FindFirstObjectByType<ThirdPersonController>();
            if (controller != null) Player = controller.transform;
        }

        if (Player == null)
        {
            Debug.LogWarning("CheckpointManager: no player found.", this);
            enabled = false;
            return;
        }

        _characterController = Player.GetComponent<CharacterController>();
        _startPosition = Player.position;
        _startRotation = Player.rotation;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Called by a Checkpoint when the player enters it.</summary>
    public void Reach(Checkpoint checkpoint)
    {
        if (!_reached.Contains(checkpoint)) _reached.Add(checkpoint);
    }

    /// <summary>Called by a DeathZone. Moves the player to the nearest passed checkpoint.</summary>
    public void Respawn()
    {
        if (Player == null) return;
        if (Time.time - _lastRespawnTime < RespawnProtection) return;
        _lastRespawnTime = Time.time;

        Vector3 position = _startPosition;
        Quaternion rotation = _startRotation;

        Checkpoint nearest = GetNearestReached(Player.position);
        if (nearest != null)
        {
            position = nearest.SpawnPosition;
            rotation = nearest.SpawnRotation;
        }

        Teleport(position, rotation);
        OnRespawned?.Invoke();
    }

    private Checkpoint GetNearestReached(Vector3 from)
    {
        Checkpoint best = null;
        float bestDistance = float.MaxValue;

        foreach (Checkpoint checkpoint in _reached)
        {
            if (checkpoint == null) continue;
            float distance = (checkpoint.SpawnPosition - from).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = checkpoint;
            }
        }
        return best;
    }

    private void Teleport(Vector3 position, Quaternion rotation)
    {
        // a CharacterController overrides transform changes unless it is disabled while moving
        if (_characterController != null) _characterController.enabled = false;

        Player.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();

        if (_characterController != null) _characterController.enabled = true;
    }
}