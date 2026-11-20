using TMPro;
using UnityEngine;
  using System.Collections;

public class DialogueUI : MonoBehaviour
{

/// <summary>
/// Shows dialogue lines with a typewriter effect. One instance lives on a Canvas in the scene.
/// NPCs talk to it through Begin / Advance / End and SetPrompt.
/// </summary>

    public static DialogueUI Instance { get; private set; }

    [Header("UI references")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("Optional little arrow/icon shown when a line has finished typing")]
    [SerializeField] private GameObject continueIndicator;

    [Header("Interaction prompt (\"Press T to talk\")")]
    [SerializeField] private GameObject promptRoot;
    [Tooltip("Optional. If set, the NPC fills it with the key to press")]
    [SerializeField] private TMP_Text promptText;

    [Header("Typing")]
    [SerializeField] private float charsPerSecond = 40f;
    [Tooltip("Extra pause (seconds) after . , ! ? ; :")]
    [SerializeField] private float punctuationPause = 0.12f;

    [Header("Typing sound (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeBlip;
    [Tooltip("Play the blip every N visible characters")]
    [SerializeField] private int blipEveryNChars = 2;
    [Range(0f, 0.3f)][SerializeField] private float blipPitchVariation = 0.08f;

    /// <summary>True while a conversation is on screen.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The object that started the current conversation.</summary>
    public object Owner { get; private set; }

    private string[] _lines;
    private int _lineIndex;
    private bool _typing;
    private Coroutine _typingRoutine;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        if (promptRoot != null) promptRoot.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetPrompt(bool show, string label = null)
    {
        if (promptRoot != null) promptRoot.SetActive(show);
        if (show && promptText != null && label != null) promptText.text = label;
    }

    public void Begin(object owner, string speaker, string[] lines)
    {
        if (IsOpen || lines == null || lines.Length == 0) return;

        Owner = owner;
        IsOpen = true;
        _lines = lines;
        _lineIndex = 0;

        if (speakerText != null) speakerText.text = speaker;
        panel.SetActive(true);
        ShowLine();
    }

    /// <summary>First call skips the typing, next call goes to the next line (or closes).</summary>
    public void Advance()
    {
        if (!IsOpen) return;

        if (_typing)
        {
            FinishTyping();
            return;
        }

        _lineIndex++;
        if (_lineIndex >= _lines.Length) End();
        else ShowLine();
    }

    public void End()
    {
        if (_typingRoutine != null) StopCoroutine(_typingRoutine);
        _typingRoutine = null;
        _typing = false;
        IsOpen = false;
        Owner = null;
        if (panel != null) panel.SetActive(false);
    }

    private void ShowLine()
    {
        if (_typingRoutine != null) StopCoroutine(_typingRoutine);
        _typingRoutine = StartCoroutine(TypeLine(_lines[_lineIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        _typing = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);

        bodyText.text = line;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();   // so characterInfo / characterCount are correct for this line

        int total = bodyText.textInfo.characterCount; // rich-text tags are not counted
        int shown = 0;
        float timer = 0f;
        float step = 1f / Mathf.Max(1f, charsPerSecond);

        while (shown < total)
        {
            timer += Time.unscaledDeltaTime;

            // a while loop so high speeds (or a slow frame) can reveal several characters at once
            while (timer >= step && shown < total)
            {
                timer -= step;
                shown++;

                char c = bodyText.textInfo.characterInfo[shown - 1].character;

                if (typeBlip != null && audioSource != null && !char.IsWhiteSpace(c)
                    && shown % Mathf.Max(1, blipEveryNChars) == 0)
                {
                    audioSource.pitch = 1f + Random.Range(-blipPitchVariation, blipPitchVariation);
                    audioSource.PlayOneShot(typeBlip);
                }

                if (".,!?;:".IndexOf(c) >= 0) timer -= punctuationPause;
            }

            bodyText.maxVisibleCharacters = shown;
            yield return null;
        }

        FinishTyping();
    }

    private void FinishTyping()
    {
        if (_typingRoutine != null) StopCoroutine(_typingRoutine);
        _typingRoutine = null;
        _typing = false;
        bodyText.maxVisibleCharacters = int.MaxValue;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }
}