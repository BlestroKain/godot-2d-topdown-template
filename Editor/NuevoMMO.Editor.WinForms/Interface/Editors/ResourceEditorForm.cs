using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class ResourceEditorForm : DefinitionEditorForm
{
    public ResourceEditorForm() => InitializeComponent();

    public ResourceEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Resources)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not ResourceDefinition resource) return;
        BindVisualKey(visualKeyTextBox, AssetKind.Resource, resource.VisualKey);
        exhaustedVisualTextBox.Text = resource.ExhaustedVisualKey?.Value ?? string.Empty;
        BindDefinitionCombo<LootTableDefinition>(lootTableCombo, resource.Harvest.LootTableId);
        BindDefinitionCombo<ProfessionDefinition>(professionCombo, resource.Harvest.RequiredProfessionId);
        SetNumeric(professionLevelNumeric, resource.Harvest.RequiredProfessionLevel);
        toolKeyTextBox.Text = resource.Harvest.RequiredToolKey?.Value ?? string.Empty;
        SetNumeric(respawnNumeric, resource.Harvest.RespawnMilliseconds);
        blockAvailableCheck.Checked = resource.Harvest.BlocksMovementWhileAvailable;
        blockExhaustedCheck.Checked = resource.Harvest.BlocksMovementWhileExhausted;
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var resource = current as ResourceDefinition ?? throw new InvalidOperationException("La selección no es un Recurso.");
        var professionId = ReadDefinitionId(professionCombo);
        var professionLevel = (int)professionLevelNumeric.Value;
        if (professionLevel > 0 && professionId is null)
            throw new InvalidOperationException("Nivel de profesión requiere una profesión.");
        return new ResourceDefinition(
            id, key, name, description, enabled, version, tags,
            ReadVisualKey(visualKeyTextBox, AssetKind.Resource),
            resource.PropertyIds,
            ReadOptionalContentKey(exhaustedVisualTextBox),
            new ResourceHarvestDefinition(
                ReadDefinitionId(lootTableCombo),
                professionId,
                professionLevel,
                ReadOptionalContentKey(toolKeyTextBox),
                resource.Harvest.HealthRange,
                (int)respawnNumeric.Value,
                blockAvailableCheck.Checked,
                blockExhaustedCheck.Checked,
                resource.Harvest.Parameters),
            resource.PropertyRanges,
            resource.EventHooks,
            resource.Metadata);
    }
}
