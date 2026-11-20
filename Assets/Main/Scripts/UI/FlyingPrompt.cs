using UnityEngine;
using UnityEngine.InputSystem;

public class FlyingPrompt : MonoBehaviour
{

/// <summary>
/// Shows a UI prompt only while a key is held down, and hides it when the key is released.
/// Put this on an object that is always active (e.g. the Canvas), NOT on the prompt itself,
/// because the prompt gets switched off and a disabled object can't run its Update.
/// </summary>
    [Tooltip("The key that must be held for the prompt to show")]
    [SerializeField] private Key key = Key.F;

    [Tooltip("The prompt object (for example a TextMeshPro text)")]
    [SerializeField] private GameObject promptRoot;

    [Tooltip("Optional: a CanvasGroup on the prompt for a smooth fade in and out. Leave empty for instant show/hide.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float fadeSpeed = 10f;

    private float alpha;

    private void Awake()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
    }

    private void Update()
    {
        if (promptRoot == null) return;

        Keyboard keyboard = Keyboard.current;
        bool held = keyboard != null && keyboard[key].isPressed;

        if (canvasGroup == null)
        {
            promptRoot.SetActive(held);
            return;
        }

        alpha = Mathf.MoveTowards(alpha, held ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
        canvasGroup.alpha = alpha;
        promptRoot.SetActive(alpha > 0f);
    }
}