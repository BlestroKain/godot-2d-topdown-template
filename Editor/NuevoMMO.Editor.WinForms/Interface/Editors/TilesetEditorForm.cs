using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class TilesetEditorForm : DefinitionEditorForm
{
    public TilesetEditorForm() => InitializeComponent();

    public TilesetEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Tilesets)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not TilesetDefinition tileset) return;
        BindVisualKey(textureKeyTextBox, AssetKind.Tileset, tileset.TextureKey);
        SetNumeric(tileWidthNumeric, tileset.TileSize.X);
        SetNumeric(tileHeightNumeric, tileset.TileSize.Y);
        SetNumeric(autotileFramesNumeric, tileset.AutotileAnimationFrames);
        SetNumeric(autotileMsNumeric, tileset.AutotileFrameMilliseconds);
        SetNumeric(waterfallFramesNumeric, tileset.WaterfallAnimationFrames);
        SetNumeric(waterfallMsNumeric, tileset.WaterfallFrameMilliseconds);
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var tileset = current as TilesetDefinition ?? throw new InvalidOperationException("La selección no es un Tileset.");
        return new TilesetDefinition(
            id, key, name, description, enabled, version, tags,
            ReadVisualKey(textureKeyTextBox, AssetKind.Tileset),
            new Vector2IntData((int)tileWidthNumeric.Value, (int)tileHeightNumeric.Value),
            (int)autotileFramesNumeric.Value,
            (int)autotileMsNumeric.Value,
            (int)waterfallFramesNumeric.Value,
            (int)waterfallMsNumeric.Value,
            tileset.Metadata);
    }
}
