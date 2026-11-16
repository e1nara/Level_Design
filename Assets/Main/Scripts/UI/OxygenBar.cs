using UnityEngine;
using UnityEngine.UI;

public class OxygenBar : MonoBehaviour
{
 
    [SerializeField] private Image fill;

    private void Update()
    {
        if (Oxygen.Instance == null) return;
        fill.fillAmount = Oxygen.Instance.OxygenNormalized;
    }
}