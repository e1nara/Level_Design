using UnityEngine;
using UnityEngine.InputSystem;

public class DestroyObject : MonoBehaviour
{
    [Tooltip("Add more bindings (e.g. gamepad) in the Inspector")]
    public InputAction SmashInput = new InputAction("Smash", InputActionType.Button, "<Keyboard>/e");

    [Tooltip("How close (meters) a glowing mushroom must be to be destroyed")]
    public float Range = 2.5f;

    [Tooltip("Height above the player's feet that distance is measured from")]
    public float ReachHeight = 0.9f;

    private void OnEnable() => SmashInput.Enable();
    private void OnDisable() => SmashInput.Disable();

    private void Update()
    {
        if (!SmashInput.WasPressedThisFrame()) return;

        GlowingMushroom target = FindNearest();
        if (target != null) target.Break();
    }

    private GlowingMushroom FindNearest()
    {
        Vector3 origin = transform.position + Vector3.up * ReachHeight;
        GlowingMushroom best = null;
        float bestSqr = Range * Range;

        foreach (GlowingMushroom mushroom in GlowingMushroom.All)
        {
            float sqr = (mushroom.transform.position - origin).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = mushroom;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * ReachHeight, Range);
    }
}