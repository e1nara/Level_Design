using UnityEngine;
using UnityEngine.InputSystem;

public class NpcDialogue : MonoBehaviour
{

/// <summary>
/// Put this on the NPC. When the player (tag "Player") is close, a prompt appears;
/// pressing the talk key starts the dialogue, and pressing it again skips the typing / goes to the next line.
/// Walking out of range closes the dialogue. The NPC's idle animation keeps running.
/// </summary>

    [Header("Dialogue")]
    public string SpeakerName = "Stranger";

    [TextArea(2, 5)]
    public string[] Lines =
    {
        "Oh! A visitor. We don't get many of those down here.",
        "Careful around the glowing mushrooms. They don't like the dark.",
    };

    [Header("Interaction")]
    [Tooltip("Distance (meters) within which the player can talk to this NPC")]
    public float TalkRadius = 3f;

    [Tooltip("Key to talk / continue. E is used by lamps and F by the jetpack in your project")]
    public Key TalkKey = Key.T;

    [Header("Facing")]
    public bool FacePlayerWhileTalking = true;
    public float TurnSpeed = 6f;

    private Transform _player;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) _player = playerObject.transform;
        else Debug.LogWarning("NpcDialogue: no object tagged 'Player' found.", this);
    }

    private void Update()
    {
        DialogueUI ui = DialogueUI.Instance;
        if (ui == null || _player == null) return;

        // horizontal distance only, so jumping next to the NPC still counts
        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        bool inRange = toPlayer.magnitude <= TalkRadius;

        bool talkingToMe = ui.IsOpen && ReferenceEquals(ui.Owner, this);

        // prompt: ask for it every frame while in range and nobody is talking.
        // DialogueUI hides it automatically on any frame nobody asks.
        if (inRange && !ui.IsOpen) ui.RequestPrompt($"(Missing scientist) Talk [{TalkKey}]");

        // walked away mid-conversation
        if (!inRange && talkingToMe)
        {
            ui.End();
            return;
        }

        if (inRange && Keyboard.current != null && Keyboard.current[TalkKey].wasPressedThisFrame)
        {
            if (talkingToMe) ui.Advance();
            else if (!ui.IsOpen) ui.Begin(this, SpeakerName, Lines);
        }

        if (talkingToMe && FacePlayerWhileTalking && toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, TurnSpeed * Time.deltaTime);
        }
    }

    private void OnDisable()
    {
        DialogueUI ui = DialogueUI.Instance;
        if (ui == null) return;

        if (ui.IsOpen && ReferenceEquals(ui.Owner, this)) ui.End();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, TalkRadius);
    }
}