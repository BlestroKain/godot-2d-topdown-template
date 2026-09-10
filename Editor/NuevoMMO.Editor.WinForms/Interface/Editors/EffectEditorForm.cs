using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class EffectEditorForm : DefinitionEditorForm
{
    public EffectEditorForm() => InitializeComponent();

    public EffectEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Effects)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not EffectDefinition effect) return;
        visualKeyTextBox.Text = effect.VisualKey.Value;
        SelectEnum(dispositionCombo, effect.Disposition);
        SetNumeric(durationNumeric, effect.Lifecycle.DurationMilliseconds);
        SetNumeric(tickNumeric, effect.Lifecycle.TickIntervalMilliseconds);
        SelectEnum(stackPolicyCombo, effect.Lifecycle.StackPolicy);
        SetNumeric(maxStacksNumeric, effect.Lifecycle.MaxStacks);
        dispellableCheck.Checked = effect.Lifecycle.Dispellable;
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var effect = current as EffectDefinition ?? throw new InvalidOperationException("La selección no es un Efecto.");
        return new EffectDefinition(
            id, key, name, description, enabled, version, tags,
            ReadContentKey(visualKeyTextBox),
            ReadEnum(dispositionCombo, effect.Disposition),
            new EffectLifecycleDefinition(
                (int)durationNumeric.Value,
                (int)tickNumeric.Value,
                ReadEnum(stackPolicyCombo, effect.Lifecycle.StackPolicy),
                (int)maxStacksNumeric.Value,
                dispellableCheck.Checked,
                effect.Lifecycle.Parameters),
            effect.FlatStats,
            effect.PercentStats,
            effect.Modifiers,
            effect.OnApply,
            effect.OnTick,
            effect.OnExpire,
            effect.Categories,
            effect.VisualOverrides,
            effect.Parameters);
    }
}
