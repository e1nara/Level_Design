using UnityEngine;
[RequireComponent(typeof(Collider))]

public class DeathZOne : MonoBehaviour
{
/// <summary>
/// Put on an object with a trigger collider (e.g. a big box under the level or around hazards).
/// When the player touches it, they respawn at the nearest checkpoint they have already passed.
/// </summary>
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (CheckpointManager.Instance == null) return;

        CheckpointManager.Instance.Respawn();
    }
}