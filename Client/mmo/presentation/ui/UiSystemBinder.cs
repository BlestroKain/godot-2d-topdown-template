using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient.UI;

/// <summary>
/// Connects editable Godot windows to the real client projections that currently exist.
/// Server-authoritative features that do not yet have protocol/runtime state are rendered as unavailable;
/// this class never fabricates accepted gameplay results on the client.
/// </summary>
public sealed class UiSystemBinder
{
    private readonly GameHud hud;
    private NetworkBridge? network;

    private readonly MmoWindow questWindow;
    private readonly MmoWindow techniquesWindow;
    private readonly MmoWindow shopWindow;
    private readonly MmoWindow bankWindow;
    private readonly MmoWindow mailWindow;
    private readonly MmoWindow communityWindow;
    private readonly MmoWindow dialogueWindow;
    private readonly MmoWindow professionWindow;
    private readonly MmoWindow escapeWindow;

    private readonly Label questTitle;
    private readonly Label questMeta;
    private readonly Label questDescription;
    private readonly VBoxContainer questObjectives;
    private readonly Button questTrack;
    private readonly Button questShare;
    private readonly Button questAbandon;

    private readonly UiSlotGrid techniqueGrid;
    private readonly Label techniqueName;
    private readonly Label techniqueDescription;
    private readonly Label techniqueCost;
    private readonly Label techniqueCooldown;
    private readonly Label techniqueRange;
    private readonly Label techniqueArea;
    private readonly Button techniqueAssign;
    private readonly Button techniqueReset;
    private readonly Button techniqueDetails;

    private readonly Label shopItemName;
    private readonly Label shopDescription;
    private readonly Button shopBuy;
    private readonly Button shopSell;

    private readonly LineEdit bankDepositAmount;
    private readonly LineEdit bankWithdrawAmount;
    private readonly Button bankDeposit;
    private readonly Button bankWithdraw;

    private readonly Label mailBody;
    private readonly GridContainer mailHeader;
    private readonly Button mailSend;
    private readonly Button mailCollect;
    private readonly Button mailReply;
    private readonly Button mailDelete;

    private readonly VBoxContainer communityRows;
    private readonly Label communityCount;
    private readonly Label communityName;
    private readonly Label communityLevel;
    private readonly Label communityGuild;
    private readonly Label communityRank;
    private readonly Label communityStatus;
    private readonly Label partyCount;
    private readonly HBoxContainer partySlots;
    private readonly Button communityInvite;
    private readonly Button communityMessage;
    private readonly Button communityPromote;
    private readonly Button communityKick;

    private readonly Label dialogueNpcName;
    private readonly Label dialogueNpcTitle;
    private readonly Label dialogueText;
    private readonly VBoxContainer dialogueChoices;

    private readonly Label professionTitle;
    private readonly Label professionDescription;
    private readonly Label professionRequirements;
    private readonly Button professionCreate;

    private readonly Button escapeResume;
    private readonly Button escapeSettings;
    private readonly Button escapeControls;
    private readonly Button escapeCharacter;
    private readonly Button escapeLogout;

    private readonly Label locationZone;
    private readonly Label locationSubZone;
    private readonly Label minimapLocation;
    private readonly LineEdit chatInput;

    private DefinitionId selectedTechniqueId;

    public UiSystemBinder(GameHud hud)
    {
        this.hud = hud ?? throw new ArgumentNullException(nameof(hud));

        questWindow = hud.GetNode<MmoWindow>("Root/QuestJournal");
        techniquesWindow = hud.GetNode<MmoWindow>("Root/TechniquesWindow");
        shopWindow = hud.GetNode<MmoWindow>("Root/ShopWindow");
        bankWindow = hud.GetNode<MmoWindow>("Root/BankWindow");
        mailWindow = hud.GetNode<MmoWindow>("Root/MailWindow");
        communityWindow = hud.GetNode<MmoWindow>("Root/CommunityWindow");
        dialogueWindow = hud.GetNode<MmoWindow>("Root/DialogueWindow");
        professionWindow = hud.GetNode<MmoWindow>("Root/ProfessionWindow");
        escapeWindow = hud.GetNode<MmoWindow>("Root/EscapeWindow");

        questTitle = questWindow.GetNode<Label>("Margin/VBox/Body/DetailsPanel/DetailsVBox/QuestTitle");
        questMeta = questWindow.GetNode<Label>("Margin/VBox/Body/DetailsPanel/DetailsVBox/QuestMeta");
        questDescription = questWindow.GetNode<Label>("Margin/VBox/Body/DetailsPanel/DetailsVBox/Description");
        questObjectives = questWindow.GetNode<VBoxContainer>("Margin/VBox/Body/DetailsPanel/DetailsVBox/Objectives");
        questTrack = questWindow.GetNode<Button>("Margin/VBox/Body/DetailsPanel/DetailsVBox/Actions/Track");
        questShare = questWindow.GetNode<Button>("Margin/VBox/Body/DetailsPanel/DetailsVBox/Actions/Share");
        questAbandon = questWindow.GetNode<Button>("Margin/VBox/Body/DetailsPanel/DetailsVBox/Actions/Abandon");

        techniqueGrid = techniquesWindow.GetNode<UiSlotGrid>("Margin/VBox/Body/SkillGridPanel/SkillGrid");
        techniqueName = techniquesWindow.GetNode<Label>("Margin/VBox/Body/Info/VBox/SkillName");
        techniqueDescription = techniquesWindow.GetNode<Label>("Margin/VBox/Body/Info/VBox/Description");
        techniqueCost = techniquesWindow.GetNode<Label>("Margin/VBox/Body/Info/VBox/Cost");
        techniqueCooldown = techniquesWindow.GetNode<Label>("Margin/VBox/Body/Info/VBox/Cooldown");
        techniqueRange = techniquesWindow.GetNode<Label>("Margin/VBox/Body/Info/VBox/Range");
        techniqueArea = techniquesWindow.GetNode<Label>("Margin/VBox/Body/Info/VBox/Area");
        techniqueReset = techniquesWindow.GetNode<Button>("Margin/VBox/Footer/Reset");
        techniqueAssign = techniquesWindow.GetNode<Button>("Margin/VBox/Footer/Assign");
        techniqueDetails = techniquesWindow.GetNode<Button>("Margin/VBox/Footer/Details");

        shopItemName = shopWindow.GetNode<Label>("Margin/VBox/Body/DetailPanel/VBox/ItemName");
        shopDescription = shopWindow.GetNode<Label>("Margin/VBox/Body/DetailPanel/VBox/Description");
        shopBuy = shopWindow.GetNode<Button>("Margin/VBox/Body/DetailPanel/VBox/BuyButton");
        shopSell = shopWindow.GetNode<Button>("Margin/VBox/Footer/SellButton");

        bankDepositAmount = bankWindow.GetNode<LineEdit>("Margin/VBox/Body/Right/CoinsPanel/VBox/DepositRow/Amount");
        bankWithdrawAmount = bankWindow.GetNode<LineEdit>("Margin/VBox/Body/Right/CoinsPanel/VBox/WithdrawRow/Amount");
        bankDeposit = bankWindow.GetNode<Button>("Margin/VBox/Body/Right/CoinsPanel/VBox/DepositRow/Button");
        bankWithdraw = bankWindow.GetNode<Button>("Margin/VBox/Body/Right/CoinsPanel/VBox/WithdrawRow/Button");

        mailBody = mailWindow.GetNode<Label>("Margin/VBox/Body/ReadPanel/VBox/BodyText");
        mailHeader = mailWindow.GetNode<GridContainer>("Margin/VBox/Body/ReadPanel/VBox/Header");
        mailSend = mailWindow.GetNode<Button>("Margin/VBox/Body/ReadPanel/VBox/Actions/Send");
        mailCollect = mailWindow.GetNode<Button>("Margin/VBox/Body/ReadPanel/VBox/Actions/Collect");
        mailReply = mailWindow.GetNode<Button>("Margin/VBox/Body/ReadPanel/VBox/Actions/Reply");
        mailDelete = mailWindow.GetNode<Button>("Margin/VBox/Body/ReadPanel/VBox/Actions/Delete");

        communityRows = communityWindow.GetNode<VBoxContainer>("Margin/VBox/Body/Roster/VBox/Rows");
        communityCount = communityWindow.GetNode<Label>("Margin/VBox/Body/Roster/VBox/Header/Count");
        var communityFields = communityWindow.GetNode<VBoxContainer>("Margin/VBox/Body/Info/VBox/Top/Fields");
        communityName = communityFields.GetNode<Label>("Name");
        communityLevel = communityFields.GetNode<Label>("Level");
        communityGuild = communityFields.GetNode<Label>("Guild");
        communityRank = communityFields.GetNode<Label>("Rank");
        communityStatus = communityFields.GetNode<Label>("Status");
        partyCount = communityWindow.GetNode<Label>("Margin/VBox/Body/Info/VBox/PartyTitle/Count");
        partySlots = communityWindow.GetNode<HBoxContainer>("Margin/VBox/Body/Info/VBox/PartySlots");
        communityInvite = communityWindow.GetNode<Button>("Margin/VBox/Body/Info/VBox/Actions/Invite");
        communityMessage = communityWindow.GetNode<Button>("Margin/VBox/Body/Info/VBox/Actions/Message");
        communityPromote = communityWindow.GetNode<Button>("Margin/VBox/Body/Info/VBox/Actions/Promote");
        communityKick = communityWindow.GetNode<Button>("Margin/VBox/Body/Info/VBox/Actions/Kick");

        dialogueNpcName = dialogueWindow.GetNode<Label>("Margin/HBox/Content/HeaderDrag/Names/NpcName");
        dialogueNpcTitle = dialogueWindow.GetNode<Label>("Margin/HBox/Content/HeaderDrag/Names/NpcTitle");
        dialogueText = dialogueWindow.GetNode<Label>("Margin/HBox/Content/DialogueText");
        dialogueChoices = dialogueWindow.GetNode<VBoxContainer>("Margin/HBox/Content/Choices");

        professionTitle = professionWindow.GetNode<Label>("Margin/VBox/Body/Detail/VBox/Top/Text/Name");
        professionDescription = professionWindow.GetNode<Label>("Margin/VBox/Body/Detail/VBox/Top/Text/Description");
        professionRequirements = professionWindow.GetNode<Label>("Margin/VBox/Body/Detail/VBox/Requirements");
        professionCreate = professionWindow.GetNode<Button>("Margin/VBox/Body/Detail/VBox/Footer/Create");

        escapeResume = escapeWindow.GetNode<Button>("Margin/VBox/Resume");
        escapeSettings = escapeWindow.GetNode<Button>("Margin/VBox/Settings");
        escapeControls = escapeWindow.GetNode<Button>("Margin/VBox/Controls");
        escapeCharacter = escapeWindow.GetNode<Button>("Margin/VBox/Character");
        escapeLogout = escapeWindow.GetNode<Button>("Margin/VBox/Logout");

        locationZone = hud.GetNode<Label>("Root/LocationPanel/HBox/Text/Zone");
        locationSubZone = hud.GetNode<Label>("Root/LocationPanel/HBox/Text/SubZone");
        minimapLocation = hud.GetNode<Label>("Root/Minimap/VBox/Controls/Location");
        chatInput = hud.GetNode<LineEdit>("Root/ChatPanel/VBox/Input");

        WireStaticActions();
        ApplyUnavailableCapabilities();
    }

    public void Present(NetworkBridge network)
    {
        this.network = network ?? throw new ArgumentNullException(nameof(network));
        var world = network.World;

        PresentLocation(world);
        PresentTechniques(world);
        PresentParty(world.Party);
        PresentDialogueTarget(world);
    }

    private void WireStaticActions()
    {
        escapeResume.Pressed += escapeWindow.Close;
        escapeCharacter.Pressed += () =>
        {
            escapeWindow.Close();
            hud.OpenWindow("character");
        };
        escapeLogout.Pressed += () => network?.DisconnectFromServer();

        foreach (var child in techniqueGrid.GetChildren())
            if (child is UiSlot slot)
                slot.SlotActivated += SelectTechniqueSlot;

        communityRows.ChildEnteredTree += OnCommunityRowAdded;
    }

    private void ApplyUnavailableCapabilities()
    {
        // Quest progress protocol is not implemented yet. Definitions exist, runtime progress does not.
        questTitle.Text = "Registro de misiones";
        questMeta.Text = "Progreso runtime pendiente de replicación";
        questDescription.Text = "La QuestDefinition existe en Core, pero el servidor todavía no replica el progreso del personaje. La interfaz permanece de solo lectura hasta que exista ese contrato.";
        foreach (var child in questObjectives.GetChildren())
            if (child is BaseButton button) button.Disabled = true;
        questTrack.Disabled = true;
        questShare.Disabled = true;
        questAbandon.Disabled = true;

        techniqueAssign.Disabled = true; // no protocol for rebinding hotbar yet
        techniqueReset.Disabled = true;
        techniqueDetails.Disabled = false;

        shopItemName.Text = "Sin sesión de tienda";
        shopDescription.Text = "La economía autoritativa aún no expone una sesión de comerciante al cliente.";
        shopBuy.Disabled = true;
        shopSell.Disabled = true;

        bankDepositAmount.Editable = false;
        bankWithdrawAmount.Editable = false;
        bankDeposit.Disabled = true;
        bankWithdraw.Disabled = true;

        foreach (var child in mailHeader.GetChildren())
            if (child is LineEdit edit) edit.Editable = false;
        mailBody.Text = "El protocolo de correo todavía no está implementado en NuevoMMO.";
        mailSend.Disabled = true;
        mailCollect.Disabled = true;
        mailReply.Disabled = true;
        mailDelete.Disabled = true;

        communityInvite.Disabled = true;
        communityMessage.Disabled = true;
        communityPromote.Disabled = true;
        communityKick.Disabled = true;

        foreach (var child in dialogueChoices.GetChildren())
            if (child is Button button) button.Disabled = true;

        professionTitle.Text = "Profesiones";
        professionDescription.Text = "ProfessionDefinition y RecipeDefinition existen en Core; la progresión y ejecución runtime todavía esperan protocolo autoritativo.";
        professionRequirements.Text = "Estado de profesión: esperando proyección del servidor.";
        professionCreate.Disabled = true;

        escapeSettings.Disabled = true;
        escapeControls.Disabled = true;

        chatInput.Editable = false;
        chatInput.PlaceholderText = "Chat de salida pendiente de protocolo; recepción activa.";
    }

    private void PresentLocation(ClientWorldState world)
    {
        var connected = world.Session.Map is not null;
        locationZone.Text = connected ? "Mapa conectado" : "Sin mapa";
        locationSubZone.Text = connected ? "Proyección autoritativa activa" : "Esperando servidor";
        minimapLocation.Text = connected ? "⌖ Zona actual" : "⌖ Sin zona";
    }

    private void PresentTechniques(ClientWorldState world)
    {
        // Until a technique-book projection exists, the real hotbar is the authoritative client view of assigned techniques.
        for (var i = 0; i < techniqueGrid.GetChildCount(); i++)
        {
            if (techniqueGrid.GetChild(i) is not UiSlot slot) continue;
            slot.Clear();
            slot.Text = string.Empty;
            slot.Disabled = i >= world.Local.Hotbar.Slots.Count;
        }

        foreach (var binding in world.Local.Hotbar.Slots)
        {
            var slot = techniqueGrid.GetSlot(binding.Index);
            if (slot is null) continue;
            slot.Disabled = binding.IsEmpty;
            slot.Text = binding.IsEmpty ? $"{binding.Index + 1}\n—" : $"{binding.Index + 1}\n{binding.Kind}";
            slot.TooltipText = binding.IsEmpty
                ? "Slot sin técnica asignada"
                : $"Binding real de hotbar: {binding.Kind}";
        }

        if (selectedTechniqueId.IsEmpty)
        {
            techniqueName.Text = "Técnica asignada";
            techniqueDescription.Text = "Selecciona un slot ocupado. UseTechnique ya está conectado al protocolo de combate; el libro completo de técnicas requiere una proyección de técnicas conocidas.";
            techniqueCost.Text = "♦ Costo de recurso                 —";
            techniqueCooldown.Text = "⌛ Tiempo de reutilización     —";
            techniqueRange.Text = "◎ Alcance                                 —";
            techniqueArea.Text = "◉ Área de efecto                       —";
        }
    }

    private void SelectTechniqueSlot(int slotIndex)
    {
        if (network is null) return;
        var bindings = network.World.Local.Hotbar.Slots;
        if (slotIndex < 0 || slotIndex >= bindings.Count) return;
        var binding = bindings[slotIndex];
        if (binding.IsEmpty || binding.BoundId is not { } id) return;

        selectedTechniqueId = id;
        techniqueName.Text = binding.Kind.ToString();
        techniqueDescription.Text = $"Technique DefinitionId: {id}. El lanzamiento usa NetworkBridge.UseTechnique; detalles visuales completos aparecerán cuando el cliente cargue el catálogo de contenido.";
    }

    private void PresentParty(PartyState party)
    {
        var rows = communityRows.GetChildren().OfType<Button>().ToArray();
        var members = party.Members;
        communityCount.Text = $"{members.Count} / {Math.Max(1, members.Count)}";
        partyCount.Text = $"{members.Count} / {partySlots.GetChildCount()}";

        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (i >= members.Count)
            {
                row.Visible = false;
                continue;
            }

            var member = members[i];
            row.Visible = true;
            row.Text = $"{(member.Online ? "●" : "○")}   {member.Name}   · Lv {member.Level}";
            row.SetMeta("entity_id", member.EntityId.Value);
        }

        var partyButtons = partySlots.GetChildren().OfType<Button>().ToArray();
        for (var i = 0; i < partyButtons.Length; i++)
        {
            var button = partyButtons[i];
            if (i < members.Count)
            {
                var member = members[i];
                button.Text = $"{member.Name}\nLv {member.Level}";
                button.TooltipText = member.Online ? "En línea" : "Desconectado";
            }
            else
            {
                button.Text = "+";
                button.TooltipText = "Slot de grupo vacío";
            }
        }

        if (!party.HasParty)
        {
            communityName.Text = "Nombre        —";
            communityLevel.Text = "Nivel             —";
            communityGuild.Text = "Gremio          —";
            communityRank.Text = "Rango            —";
            communityStatus.Text = "Estado          Sin grupo";
        }
    }

    private void OnCommunityRowAdded(Node node)
    {
        if (node is Button button)
            button.Pressed += () => SelectCommunityMember(button);
    }

    private void SelectCommunityMember(Button button)
    {
        if (!button.HasMeta("entity_id") || network is null) return;
        var entityValue = button.GetMeta("entity_id").AsInt64();
        var member = network.World.Party.Members.FirstOrDefault(candidate => candidate.EntityId.Value == entityValue);
        if (member is null) return;

        communityName.Text = $"Nombre        {member.Name}";
        communityLevel.Text = $"Nivel             {member.Level}";
        communityGuild.Text = "Gremio          —";
        communityRank.Text = member.EntityId == network.World.Party.Leader ? "Rango            Líder" : "Rango            Miembro";
        communityStatus.Text = $"Estado          {(member.Online ? "En línea" : "Desconectado")}";
    }

    private void PresentDialogueTarget(ClientWorldState world)
    {
        if (!dialogueWindow.Visible) return;

        if (!world.Local.Target.HasTarget)
        {
            dialogueNpcName.Text = "Sin interlocutor";
            dialogueNpcTitle.Text = "Interacción";
            dialogueText.Text = "Selecciona un NPC u objeto interactuable antes de abrir diálogo.";
            return;
        }

        dialogueNpcName.Text = world.Local.Target.DisplayName;
        dialogueNpcTitle.Text = world.Local.Target.Kind.ToString();
        dialogueText.Text = "La selección usa el TargetState real. InteractRequest ya existe; el payload de diálogo/respuestas todavía no forma parte del protocolo NuevoMMO.";
    }
}
