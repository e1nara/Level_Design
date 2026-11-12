using UnityEngine;
using UnityEngine.InputSystem;

public class Letter : MonoBehaviour
{
    [TextArea(5, 20)]
    [SerializeField] private string letterContent = "Dear traveler...";

    [SerializeField] private Key interactKey = Key.E;

    private bool playerInRange;

    private void Update()
    {
        if (!playerInRange || LetterUI.Instance.IsOpen || Keyboard.current == null) return;

        if (Keyboard.current[interactKey].wasPressedThisFrame)
        {
            LetterUI.Instance.Open(letterContent);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        LetterUI.Instance.ShowReadPrompt(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        LetterUI.Instance.ShowReadPrompt(false);
    }
}