using UnityEngine;

public class SporeTrigger : MonoBehaviour
{
    [Tooltip("Leave empty to use the SporeMushroom on a parent object")]
    public SporeMushroom Mushroom;

    private void Awake()
    {
        if (Mushroom == null) Mushroom = GetComponentInParent<SporeMushroom>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // ignore other trigger zones (fuel packs, other mushrooms' areas, etc.)
        if (other.isTrigger) return;

        if (Mushroom != null) Mushroom.NotifyTouched(other);
    }

}
