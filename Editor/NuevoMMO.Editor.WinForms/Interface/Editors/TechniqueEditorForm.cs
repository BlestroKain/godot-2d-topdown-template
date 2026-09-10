using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class TechniqueEditorForm : DefinitionEditorForm
{
    public TechniqueEditorForm() => InitializeComponent();

    public TechniqueEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Techniques)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not TechniqueDefinition technique) return;
        visualKeyTextBox.Text = technique.VisualKey.Value;
        SelectEnum(elementCombo, technique.Element);
        SelectEnum(targetModeCombo, technique.Targeting.Mode);
        SetNumeric(rangeNumeric, (decimal)technique.Targeting.Range);
        SetNumeric(radiusNumeric, (decimal)technique.Targeting.Radius);
        SetNumeric(maxTargetsNumeric, technique.Targeting.MaxTargets);
        lineOfSightCheck.Checked = technique.Targeting.RequiresLineOfSight;
        SetNumeric(castNumeric, technique.Timing.CastMilliseconds);
        SetNumeric(cooldownNumeric, technique.Timing.CooldownMilliseconds);
        cooldownGroupTextBox.Text = technique.Timing.CooldownGroup;
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var technique = current as TechniqueDefinition ?? throw new InvalidOperationException("La selección no es una Técnica.");
        return new TechniqueDefinition(
            id, key, name, description, enabled, version, tags,
            ReadContentKey(visualKeyTextBox),
            ReadEnum(elementCombo, technique.Element),
            new TechniqueTargetingDefinition(
                ReadEnum(targetModeCombo, technique.Targeting.Mode),
                technique.Targeting.Relations,
                (float)rangeNumeric.Value,
                (float)radiusNumeric.Value,
                technique.Targeting.AngleDegrees,
                (int)maxTargetsNumeric.Value,
                lineOfSightCheck.Checked,
                technique.Targeting.AllowEmptyPoint,
                technique.Targeting.Parameters),
            new TechniqueTimingDefinition(
                (int)castNumeric.Value,
                (int)cooldownNumeric.Value,
                cooldownGroupTextBox.Text,
                technique.Timing.IgnoreGlobalCooldown,
                technique.Timing.IgnoreCooldownReduction,
                technique.Timing.ChannelMilliseconds,
                technique.Timing.TickIntervalMilliseconds,
                technique.Timing.Parameters),
            technique.VitalCosts,
            technique.ResourceCosts,
            technique.Actions,
            technique.CastRequirements,
            technique.CannotCastMessage,
            technique.EventHooks,
            technique.VisualOverrides,
            technique.Parameters);
    }
}
