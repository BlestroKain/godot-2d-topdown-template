using DialogueManagerRuntime;
using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient.UI;

/// <summary>
/// Transitional Godot-first presentation adapter for the current interaction protocol.
/// EventRuntime remains authoritative on the server. This adapter only turns server-originated
/// human-facing interaction lines into a Dialogue Manager resource and always skips mutations.
///
/// Once the network protocol exposes structured DialoguePacket/DialogueChoice packets, this class
/// should consume those packets directly instead of the temporary System-chat compatibility path.
/// </summary>
public partial class DialogueManagerPresenter : Node
{
    private const ulong FeedbackTimeoutMilliseconds = 2_500;

    private static readonly string[] MachineLogPrefixes =
    [
        "xp:",
        "learn:",
        "forget:",
        "give_item:",
        "give_item_failed:",
        "take_item:",
        "take_item_failed:",
        "apply_effect:",
        "remove_effect:",
        "remove_effect_missing:",
        "quest_started:",
        "quest_advanced:",
        "quest_completed:",
        "quest_failed:",
        "trigger_event_",
        "deferred:"
    ];

    private readonly InteractionInput interactions = new();

    private MmoGame game = null!;
    private MmoWindow dialogueWindow = null!;
    private Label npcName = null!;
    private Label npcTitle = null!;
    private Label dialogueText = null!;
    private VBoxContainer choices = null!;
    private Button continueButton = null!;
    private Button closeButton = null!;

    private bool pendingInteraction;
    private int pendingChatIndex;
    private ulong feedbackDeadline;
    private string pendingSpeaker = string.Empty;

    private Resource? dialogueResource;
    private DialogueLine? currentLine;
    private bool dialogueActive;
    private bool advancing;

    public override void _Ready()
    {
        // MmoGame and GameHud present at the default priority. Running after them lets this adapter
        // own the dialogue labels while a conversation is active without coupling UiSystemBinder to
        // the third-party addon.
        ProcessPriority = 100;

        game = (MmoGame)GetParent();
        var hud = game.GetNode<GameHud>("GameHud");
        dialogueWindow = hud.GetNode<MmoWindow>("Root/DialogueWindow");
        npcName = dialogueWindow.GetNode<Label>("Margin/HBox/Content/HeaderDrag/Names/NpcName");
        npcTitle = dialogueWindow.GetNode<Label>("Margin/HBox/Content/HeaderDrag/Names/NpcTitle");
        dialogueText = dialogueWindow.GetNode<Label>("Margin/HBox/Content/DialogueText");
        choices = dialogueWindow.GetNode<VBoxContainer>("Margin/HBox/Content/Choices");

        continueButton = new Button { Text = "Continuar", FocusMode = Control.FocusModeEnum.All, Visible = false };
        closeButton = new Button { Text = "Cerrar", FocusMode = Control.FocusModeEnum.All, Visible = false };
        continueButton.Pressed += Advance;
        closeButton.Pressed += CloseDialogue;
        choices.AddChild(continueButton);
        choices.AddChild(closeButton);
    }

    public override void _Process(double delta)
    {
        if (!game.Network.InWorld)
        {
            pendingInteraction = false;
            if (dialogueActive) CloseDialogue();
            return;
        }

        if (!dialogueActive && Input.IsActionJustPressed(InteractionInput.InteractAction))
            ArmForNpcInteraction();

        if (pendingInteraction)
            PollInteractionFeedback();

        if (!dialogueActive)
            return;

        if (!dialogueWindow.Visible)
        {
            EndDialogueState();
            return;
        }

        RenderCurrentLine();
    }

    private void ArmForNpcInteraction()
    {
        var target = interactions.NearestInteractable(game.Network.World);
        if (target is null || target.Kind != EntityKind.Npc)
        {
            pendingInteraction = false;
            return;
        }

        pendingSpeaker = string.IsNullOrWhiteSpace(target.DisplayName) ? "NPC" : target.DisplayName.Trim();
        pendingChatIndex = game.Network.World.Chat.Messages.Count;
        feedbackDeadline = Time.GetTicksMsec() + FeedbackTimeoutMilliseconds;
        pendingInteraction = true;
    }

    private void PollInteractionFeedback()
    {
        if (Time.GetTicksMsec() > feedbackDeadline)
        {
            pendingInteraction = false;
            return;
        }

        var messages = game.Network.World.Chat.Messages;
        if (messages.Count <= pendingChatIndex)
            return;

        for (var i = pendingChatIndex; i < messages.Count; i++)
        {
            var message = messages[i];
            if (message.Channel != ChatChannel.System)
                continue;

            var lines = ExtractPresentationLines(message.Text);
            if (lines.Count == 0)
                continue;

            pendingInteraction = false;
            _ = BeginDialogueAsync(pendingSpeaker, lines);
            return;
        }

        // Ignore unrelated/machine-only System traffic already examined while leaving the short
        // response window armed for the actual interaction reply.
        pendingChatIndex = messages.Count;
    }

    private static List<string> ExtractPresentationLines(string raw)
    {
        var result = new List<string>();
        foreach (var fragment in raw.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!IsPresentationLine(fragment))
                continue;
            result.Add(fragment);
        }
        return result;
    }

    private static bool IsPresentationLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        foreach (var prefix in MachineLogPrefixes)
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

        // These are protocol/interaction diagnostics, not authored NPC dialogue.
        return !line.Equals("Fuera de alcance.", StringComparison.OrdinalIgnoreCase)
            && !line.Equals("Nada que interactuar aquí.", StringComparison.OrdinalIgnoreCase)
            && !line.Equals("El objetivo ya no existe.", StringComparison.OrdinalIgnoreCase)
            && !line.EndsWith(" no tiene diálogo.", StringComparison.OrdinalIgnoreCase);
    }

    private async Task BeginDialogueAsync(string speaker, IReadOnlyList<string> lines)
    {
        if (advancing || lines.Count == 0)
            return;

        advancing = true;
        try
        {
            var source = string.Join("\n", lines.Select(line => $"{SanitizeSpeaker(speaker)}: {SanitizeLine(line)}"));
            dialogueResource?.Dispose();
            dialogueResource = DialogueManager.CreateResourceFromText(source);
            currentLine = await DialogueManager.GetNextDialogueLine(
                dialogueResource,
                string.Empty,
                null,
                MutationBehaviour.Skip);

            if (currentLine is null)
            {
                EndDialogueState();
                return;
            }

            pendingSpeaker = speaker;
            dialogueActive = true;
            PrepareChoiceButtons();
            dialogueWindow.Open();
            continueButton.GrabFocus();
            RenderCurrentLine();
        }
        catch (Exception exception)
        {
            GD.PushWarning($"[DialogueManagerPresenter] Dialogue Manager rejected interaction text: {exception.Message}");
            EndDialogueState();
        }
        finally
        {
            advancing = false;
        }
    }

    private async void Advance()
    {
        if (advancing || !dialogueActive || dialogueResource is null || currentLine is null)
            return;

        if (string.IsNullOrWhiteSpace(currentLine.NextId))
        {
            CloseDialogue();
            return;
        }

        advancing = true;
        continueButton.Disabled = true;
        try
        {
            currentLine = await DialogueManager.GetNextDialogueLine(
                dialogueResource,
                currentLine.NextId,
                null,
                MutationBehaviour.Skip);

            if (currentLine is null)
            {
                CloseDialogue();
                return;
            }

            RenderCurrentLine();
        }
        catch (Exception exception)
        {
            GD.PushWarning($"[DialogueManagerPresenter] Could not advance dialogue: {exception.Message}");
            CloseDialogue();
        }
        finally
        {
            advancing = false;
            continueButton.Disabled = false;
        }
    }

    private void PrepareChoiceButtons()
    {
        foreach (var child in choices.GetChildren())
        {
            if (child == continueButton || child == closeButton) continue;
            if (child is Control control) control.Visible = false;
        }
        continueButton.Visible = true;
        closeButton.Visible = true;
    }

    private void RestoreChoiceButtons()
    {
        continueButton.Visible = false;
        closeButton.Visible = false;
        foreach (var child in choices.GetChildren())
        {
            if (child == continueButton || child == closeButton) continue;
            if (child is Control control) control.Visible = true;
        }
    }

    private void RenderCurrentLine()
    {
        if (currentLine is null) return;
        npcName.Text = string.IsNullOrWhiteSpace(currentLine.Character) ? pendingSpeaker : currentLine.Character;
        npcTitle.Text = "Diálogo autoritativo · Dialogue Manager";
        dialogueText.Text = currentLine.Text;
        continueButton.Text = string.IsNullOrWhiteSpace(currentLine.NextId) ? "Terminar" : "Continuar";
    }

    private void CloseDialogue()
    {
        if (dialogueWindow.Visible)
            dialogueWindow.Close();
        EndDialogueState();
    }

    private void EndDialogueState()
    {
        pendingInteraction = false;
        dialogueActive = false;
        advancing = false;
        currentLine = null;
        dialogueResource?.Dispose();
        dialogueResource = null;
        RestoreChoiceButtons();
    }

    private static string SanitizeSpeaker(string value)
        => value.Replace("\r", " ").Replace("\n", " ").Replace(":", " -").Trim();

    private static string SanitizeLine(string value)
        => value.Replace("\r", " ").Replace("\n", " ").Trim();
}
