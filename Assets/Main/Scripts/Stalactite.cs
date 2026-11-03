using UnityEngine;
using System.Collections;
public class Stalactite : MonoBehaviour
{
    [Header("Droplet")]
    [Tooltip("Prefab with Rigidbody, Collider and WaterDroplet script")]
    public GameObject DropletPrefab;

    [Tooltip("Empty child object placed at the tip of the stalactite")]
    public Transform Tip;

    [Header("Timing")]
    [Tooltip("Average seconds between droplets")]
    public float Interval = 2f;

    [Tooltip("Random +/- seconds added to the interval so it doesn't feel mechanical")]
    public float IntervalJitter = 0.8f;

    private Collider[] _ownColliders;

    private void Start()
    {
        _ownColliders = GetComponentsInChildren<Collider>();
        StartCoroutine(DripRoutine());
    }

    private IEnumerator DripRoutine()
    {
        // random start delay so multiple stalactites don't drip in sync
        yield return new WaitForSeconds(Random.Range(0f, Interval));

        while (true)
        {
            SpawnDroplet();
            float wait = Interval + Random.Range(-IntervalJitter, IntervalJitter);
            yield return new WaitForSeconds(Mathf.Max(0.1f, wait));
        }
    }

    private void SpawnDroplet()
    {
        if (DropletPrefab == null || Tip == null) return;

        GameObject droplet = Instantiate(DropletPrefab, Tip.position, Quaternion.identity);

        // don't let the droplet collide with the stalactite it came from
        if (droplet.TryGetComponent(out Collider dropletCollider))
        {
            foreach (Collider c in _ownColliders)
            {
                Physics.IgnoreCollision(dropletCollider, c);
            }
        }
    }
}
