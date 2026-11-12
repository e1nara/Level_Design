using StarterAssets;
using System.Collections.Generic;
using UnityEngine;
public enum ElevatorMode
{
    Working, // rides up and down while powered
    Broken   // rises to JamHeight and stays jammed there while powered
}

// Runs before the player's scripts, so the player can include the elevator's motion in its own Move call
[DefaultExecutionOrder(-200)]
public class PoweredElevator : MonoBehaviour
{
    [Header("Power")]
    [Tooltip("Electric mushrooms that power this elevator. Must be objects in the scene, not prefab assets")]
    public ElectricMushroom[] PoweredBy;

    [Tooltip("On: every mushroom must be powered. Off: any one mushroom is enough")]
    public bool RequireAll = true;

    [Header("Mode")]
    public ElevatorMode Mode = ElevatorMode.Working;

    [Header("Movement")]
    [Tooltip("Direction of travel in world space. Normally straight up")]
    public Vector3 MoveDirection = Vector3.up;

    [Tooltip("Working mode: how far (meters) it rises before turning around")]
    public float TravelHeight = 6f;

    [Tooltip("Broken mode: how far (meters) it rises before it jams")]
    public float JamHeight = 3f;

    [Tooltip("Travel speed while powered (m/s)")]
    public float Speed = 2f;

    [Tooltip("Travel speed while descending back after losing power (m/s)")]
    public float DescendSpeed = 2f;

    [Tooltip("How quickly it speeds up and slows down (m/s^2)")]
    public float Acceleration = 2f;

    [Tooltip("Working mode: seconds it waits at the top and bottom")]
    public float PauseAtEnds = 1.5f;

    [Header("Jam (Broken mode)")]
    [Tooltip("On: stops abruptly at the jam height. Off: slows down smoothly like a working elevator")]
    public bool JamHardStop = true;

    [Tooltip("How far (meters) it shudders while jammed. 0 = perfectly still")]
    public float JamShakeAmount = 0.015f;
    public float JamShakeSpeed = 40f;

    [Header("Riders")]
    [Tooltip("Collider of the floor that characters stand on. Leave empty to use all colliders on this object")]
    public Collider RideSurface;

    [Header("Debug (read only, watch these in Play mode)")]
    [SerializeField] private float _height;
    [SerializeField] private bool _poweredNow;

    private static readonly Collider[] OverlapBuffer = new Collider[16];

    private Vector3 _origin;
    private Vector3 _direction;
    private float _velocity;
    private bool _goingUp = true;
    private float _waitTimer;

    // shape of the floor, remembered at startup so it doesn't depend on physics state
    private Vector3 _surfaceCenterOffset;
    private Vector3 _surfaceExtents;
    private float _surfaceTopOffset;

    private void Awake()
    {
        _origin = transform.position;
        _direction = MoveDirection.sqrMagnitude > 0.0001f ? MoveDirection.normalized : Vector3.up;

        CacheSurfaceBounds();
    }

    private void CacheSurfaceBounds()
    {
        var colliders = new List<Collider>();

        if (RideSurface != null)
        {
            colliders.Add(RideSurface);
        }
        else
        {
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                if (!c.isTrigger) colliders.Add(c);
            }
        }

        if (colliders.Count == 0)
        {
            Debug.LogWarning("PoweredElevator: no solid collider found, so riders can't be carried.", this);
            _surfaceExtents = Vector3.zero;
            return;
        }

        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Count; i++) bounds.Encapsulate(colliders[i].bounds);

        _surfaceCenterOffset = bounds.center - transform.position;
        _surfaceExtents = bounds.extents;
        _surfaceTopOffset = _surfaceCenterOffset.y + _surfaceExtents.y;
    }

    private void Update()
    {
        _poweredNow = IsPowered();

        float target;
        float speed = Speed;
        bool eased = true;
        bool waiting = false;

        if (!_poweredNow)
        {
            // no power: go back down to the starting position
            target = 0f;
            speed = DescendSpeed;
            _goingUp = true;
            _waitTimer = 0f;
        }
        else if (Mode == ElevatorMode.Broken)
        {
            target = Mathf.Max(0f, JamHeight);
            eased = !JamHardStop;
        }
        else
        {
            if (_waitTimer > 0f)
            {
                waiting = true;
                _waitTimer -= Time.deltaTime;
                target = _height;
            }
            else
            {
                target = _goingUp ? TravelHeight : 0f;
            }
        }

        StepHeight(target, speed, eased);

        // working elevator: turn around after reaching the top or bottom
        if (_poweredNow && Mode == ElevatorMode.Working && !waiting && Mathf.Approximately(_height, target))
        {
            _goingUp = !_goingUp;
            _waitTimer = PauseAtEnds;
        }

        // broken elevator: shudder while jammed
        float shake = 0f;
        bool jammed = _poweredNow && Mode == ElevatorMode.Broken && Mathf.Approximately(_height, target);
        if (jammed && JamShakeAmount > 0f)
        {
            shake = (Mathf.PerlinNoise(Time.time * JamShakeSpeed, 0.5f) - 0.5f) * 2f * JamShakeAmount;
        }

        MoveTo(_origin + _direction * (_height + shake));
    }

    // moves the height value toward the target with acceleration, optionally without slowing down at the end
    private void StepHeight(float target, float maxSpeed, bool eased)
    {
        float distance = target - _height;

        if (Mathf.Abs(distance) < 0.0001f)
        {
            _height = target;
            _velocity = 0f;
            return;
        }

        float desired = maxSpeed;
        if (eased)
        {
            // slow down so the elevator comes to rest exactly at the target
            desired = Mathf.Min(maxSpeed, Mathf.Sqrt(2f * Acceleration * Mathf.Abs(distance)));
        }
        desired *= Mathf.Sign(distance);

        _velocity = Mathf.MoveTowards(_velocity, desired, Acceleration * Time.deltaTime);
        float next = _height + _velocity * Time.deltaTime;

        bool passed = (distance > 0f && next >= target) || (distance < 0f && next <= target);
        if (passed)
        {
            next = target;
            _velocity = 0f;
        }

        _height = next;
    }

    private void MoveTo(Vector3 newPosition)
    {
        Vector3 delta = newPosition - transform.position;
        if (delta.sqrMagnitude < 1e-10f) return;

        // find anyone standing on the elevator BEFORE it moves
        CharacterController rider = FindRider();

        if (rider != null && rider.TryGetComponent(out ThirdPersonController player))
        {
            // The player applies this motion inside its own single Move call. Calling Move on it from
            // here as well would overwrite CharacterController.velocity, which the player's controller
            // reads to work out its walking speed.
            player.AddExternalMotion(delta);

            if (Vector3.Dot(delta, Vector3.up) > 0f)
            {
                // rising: the platform follows in LateUpdate, after the player has been lifted,
                // so it never moves up into the player's legs
                _pendingPosition = newPosition;
                _hasPendingMove = true;
            }
            else
            {
                // descending or sideways: move the platform away now; the player follows it this frame
                transform.position = newPosition;
                Physics.SyncTransforms();
            }

            return;
        }

        // any other kind of CharacterController (no ThirdPersonController): move it directly
        if (rider != null && Vector3.Dot(delta, Vector3.up) > 0f)
        {
            // rising: lift the rider first, so the platform never moves up into their legs
            // (a character embedded in the platform can't walk until it jumps free)
            CarryRider(rider, delta);
            transform.position = newPosition;
            Physics.SyncTransforms();
        }
        else
        {
            // descending or sideways: move the platform away first, then let the rider follow it down
            transform.position = newPosition;

            // make physics aware of the new position now, so the rider isn't blocked by the old one
            Physics.SyncTransforms();

            if (rider != null) CarryRider(rider, delta);
        }
    }

    // CharacterControllers don't ride moving objects by themselves, so move the rider along
    private static void CarryRider(CharacterController rider, Vector3 delta)
    {
        // CharacterController ignores moves smaller than its Min Move Distance. Elevator steps are tiny
        // when it starts or stops, so without this the rider would fall behind and sink into the platform.
        float previousMinMove = rider.minMoveDistance;
        rider.minMoveDistance = 0f;
        rider.Move(delta);
        rider.minMoveDistance = previousMinMove;
    }

    private Vector3 _pendingPosition;
    private bool _hasPendingMove;

    private void LateUpdate()
    {
        if (!_hasPendingMove) return;
        _hasPendingMove = false;

        transform.position = _pendingPosition;
        Physics.SyncTransforms();
    }

    private CharacterController FindRider()
    {
        if (_surfaceExtents == Vector3.zero) return null;

        Vector3 position = transform.position;
        Vector3 center = new Vector3(
            position.x + _surfaceCenterOffset.x,
            position.y + _surfaceTopOffset + 0.1f,
            position.z + _surfaceCenterOffset.z);
        Vector3 halfExtents = new Vector3(_surfaceExtents.x, 0.25f, _surfaceExtents.z);

        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, OverlapBuffer, Quaternion.identity,
            ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (OverlapBuffer[i] is CharacterController controller) return controller;
        }

        return null;
    }

    private bool IsPowered()
    {
        if (PoweredBy == null || PoweredBy.Length == 0) return false;

        bool any = false;
        bool all = true;

        foreach (ElectricMushroom mushroom in PoweredBy)
        {
            bool on = mushroom != null && mushroom.IsFullyPowered;
            any |= on;
            all &= on;
        }

        return RequireAll ? all : any;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? _origin : transform.position;
        Vector3 direction = MoveDirection.sqrMagnitude > 0.0001f ? MoveDirection.normalized : Vector3.up;

        Collider c = GetComponentInChildren<Collider>();
        Vector3 size = c != null ? c.bounds.size : Vector3.one;
        Vector3 offset = c != null && !Application.isPlaying ? c.bounds.center - transform.position : Vector3.zero;

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        Gizmos.DrawLine(origin, origin + direction * TravelHeight);
        Gizmos.DrawWireCube(origin + offset + direction * TravelHeight, size);

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(origin + offset + direction * JamHeight, size);
    }
}