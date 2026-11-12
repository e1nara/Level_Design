using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class LetterUI : MonoBehaviour
{
    public static LetterUI Instance { get; private set; }

    [SerializeField] private GameObject readPrompt;
    [SerializeField] private GameObject usePrompt;

    [SerializeField] private GameObject letterPanel;
    [SerializeField] private TMP_Text letterText;

    public bool IsOpen => letterPanel.activeSelf;

    private void Awake()
    {
        Instance = this;
        readPrompt.SetActive(false);
        letterPanel.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void ShowReadPrompt(bool show)
    {
        // Don't show the prompt while the letter is open
        readPrompt.SetActive(show && !IsOpen);
    }

    public void ShowUsePrompt(bool show)
    {
        // Don't show the prompt while the letter is open
        usePrompt.SetActive(show && !IsOpen);
    }

    public void Open(string text)
    {
        letterText.text = text;
        letterPanel.SetActive(true);
        readPrompt.SetActive(false);
        Time.timeScale = 0f; // optional: pause the game while reading
    }

    public void Close()
    {
        letterPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}