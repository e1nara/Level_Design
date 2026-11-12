using StarterAssets;
using UnityEngine;
using System.Collections;
public class BouncyMushroom : MonoBehaviour
{
    [Header("Bounce")]
    [Tooltip("How high (in meters) the player is launched")]
    public float BounceHeight = 6f;

    [Tooltip("Minimum time between bounces, prevents double triggers")]
    public float Cooldown = 0.2f;

    [Header("Squash & Stretch")]
    [Tooltip("How strongly the trampoline squashes at the start (0.3 = 30%)")]
    public float Strength = 0.3f;

    [Tooltip("How long the wobble lasts in seconds")]
    public float Duration = 0.6f;

    [Tooltip("How many wobbles during the duration")]
    public float Wobbles = 3f;

    private Vector3 _originalScale;
    private float _lastBounceTime = -10f;
    private Coroutine _routine;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - _lastBounceTime < Cooldown) return;

        if (other.TryGetComponent(out ThirdPersonController player))
        {
            _lastBounceTime = Time.time;
            player.Bounce(BounceHeight);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(BounceAnimation());
        }
    }

    private IEnumerator BounceAnimation()
    {
        float t = 0f;
        while (t < Duration)
        {
            t += Time.deltaTime;
            float normalized = t / Duration;

            // damped sine wave: starts at -1 (squashed), wobbles, settles at 0
            float wave = -Mathf.Cos(normalized * Wobbles * Mathf.PI * 2f) * (1f - normalized);
            float offset = wave * Strength;

            // squash Y, stretch X/Z (and the reverse) to keep volume feel
            transform.localScale = new Vector3(
                _originalScale.x * (1f - offset * 0.5f),
                _originalScale.y * (1f + offset),
                _originalScale.z * (1f - offset * 0.5f));

            yield return null;
        }

        transform.localScale = _originalScale;
        _routine = null;
    }

}
