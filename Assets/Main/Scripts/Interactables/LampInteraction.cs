using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.Controls.AxisControl;

public class LampInteraction : MonoBehaviour
{
    [Tooltip("Add more bindings (e.g. gamepad) in the Inspector")]
    public InputAction ToggleInput = new InputAction("ToggleLamp", InputActionType.Button, "<Keyboard>/e");

    [Tooltip("How close (meters) a lamp must be to be switched")]
    public float Range = 2.5f;

    [Tooltip("Height above the player's feet that distance is measured from")]
    public float ReachHeight = 0.9f;

    private void OnEnable() => ToggleInput.Enable();
    private void OnDisable() => ToggleInput.Disable();

    private void Update()
    {
        if (!ToggleInput.WasPressedThisFrame()) return;

        Lamp target = FindNearest();
        if (target != null) target.Toggle();
    }

    private Lamp FindNearest()
    {
        Vector3 origin = transform.position + Vector3.up * ReachHeight;
        Lamp best = null;
        float bestSqr = Range * Range;

        foreach (Lamp lamp in Lamp.All)
        {
            float sqr = (lamp.transform.position - origin).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = lamp;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * ReachHeight, Range);
    }
}