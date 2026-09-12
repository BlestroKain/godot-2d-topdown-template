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
        BindVisualKey(visualKeyTextBox, AssetKind.Spell, technique.VisualKey);
        SelectEnum(elementCombo, technique.Element);
        SelectEnum(targetModeCombo, technique.Targeting.Mode);
        SetNumeric(rangeNumeric, (decimal)technique.Targeting.Range);
        SetNumeric(radiusNumeric, (decimal)technique.Targeting.Radius);
        SetNumeric(maxTargetsNumeric, technique.Targeting.MaxTargets);
        lineOfSightCheck.Checked = technique.Targeting.RequiresLineOfSight;
        SetNumeric(castNumeric, technique.Timing.CastMilliseconds);
        SetNumeric(cooldownNumeric, technique.Timing.CooldownMilliseconds);
        cooldownGroupTextBox.Text = technique.Timing.CooldownGroup;
        SetNumeric(manaCostNumeric, (decimal)technique.VitalCosts.GetValueOrDefault(VitalId.Mana));
        actionsTextBox.Text = string.Join(Environment.NewLine, technique.Actions.Select(action =>
            $"{action.Kind}, {action.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {action.Element}, {action.Moment}"));
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var technique = current as TechniqueDefinition ?? throw new InvalidOperationException("La selección no es una Técnica.");
        var costs = new Dictionary<VitalId, float>(technique.VitalCosts);
        if (manaCostNumeric.Value > 0) costs[VitalId.Mana] = (float)manaCostNumeric.Value;
        else costs.Remove(VitalId.Mana);
        var actions = new List<TechniqueActionDefinition>();
        foreach (var line in actionsTextBox.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 0) continue;
            var kind = Enum.Parse<TechniqueActionKind>(parts[0], ignoreCase: true);
            var amount = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
            var element = parts.Length > 2 ? Enum.Parse<Element>(parts[2], ignoreCase: true) : technique.Element;
            var moment = parts.Length > 3 ? Enum.Parse<TechniqueActionMoment>(parts[3], ignoreCase: true) : TechniqueActionMoment.Impact;
            actions.Add(new TechniqueActionDefinition(kind, moment, amount, element));
        }

        return new TechniqueDefinition(
            id, key, name, description, enabled, version, tags,
            ReadVisualKey(visualKeyTextBox, AssetKind.Spell),
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
            costs,
            technique.ResourceCosts,
            actions.Count == 0 ? technique.Actions : actions.ToArray(),
            technique.CastRequirements,
            technique.CannotCastMessage,
            technique.EventHooks,
            technique.VisualOverrides,
            technique.Parameters);
    }
}
