using UnityEngine;

public class WaterDroplet : MonoBehaviour
{
    [Tooltip("Sound played when the droplet hits something")]
    public AudioClip ImpactSound;

    [Range(0f, 1f)]
    public float Volume = 0.5f;

    [Tooltip("Safety net: destroy the droplet after this many seconds if it never hits anything")]
    public float MaxLifetime = 15f;

    private bool _hasHit;

    private void Start()
    {
        Destroy(gameObject, MaxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;
        _hasHit = true;

        if (ImpactSound != null)
        {
            AudioSource.PlayClipAtPoint(ImpactSound, collision.GetContact(0).point, Volume);
        }

        Destroy(gameObject);
    }

}
