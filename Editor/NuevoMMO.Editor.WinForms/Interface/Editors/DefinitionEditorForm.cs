using System.Text.Json;
using System.Text.Json.Nodes;
using NuevoMMO.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class DefinitionEditorForm : DockContent
{
    private EditorApplication? application;
    private DefinitionEditorDescriptor? descriptor;
    private GameDefinition? selectedDefinition;
    private bool refreshing;

    protected DefinitionEditorForm()
    {
        InitializeComponent();
        WireEvents();
    }

    protected DefinitionEditorForm(EditorApplication application, DefinitionEditorDescriptor descriptor)
        : this()
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        this.descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    protected void FinishSetup()
    {
        if (application is null || descriptor is null) return;
        Text = descriptor.Title;
        TabText = descriptor.Title;
        EditorTheme.ApplyWindow(this);
        RefreshDefinitions();
    }

    public event Action<GameDefinition?>? ContentChanged;

    public Type? DefinitionType => descriptor?.DefinitionType;

    protected EditorApplication Editor => application ?? throw new InvalidOperationException("El editor no está configurado.");

    public void SelectDefinition(DefinitionId id) => RefreshDefinitions(id);

    public void RefreshDefinitions() => RefreshDefinitions(selectedDefinition?.Id);

    private void WireEvents()
    {
        newButton.Click += (_, _) => CreateDefinition();
        duplicateButton.Click += (_, _) => DuplicateDefinition();
        deleteButton.Click += (_, _) => DeleteDefinition();
        saveButton.Click += (_, _) => SaveDefinition();
        refreshButton.Click += (_, _) => RefreshDefinitions();
        searchTextBox.TextChanged += (_, _) => RefreshDefinitions(selectedDefinition?.Id);
        definitionsListBox.SelectedIndexChanged += (_, _) => SelectListItem();
    }

    private void RefreshDefinitions(DefinitionId? selectedId)
    {
        if (application is null || descriptor is null) return;

        refreshing = true;
        try
        {
            var query = searchTextBox.Text.Trim();
            var definitions = application.Content.Snapshot().All()
                .Where(descriptor.DefinitionType.IsInstanceOfType)
                .Where(definition => query.Length == 0 ||
                    definition.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    definition.Key.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(static definition => definition.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(static definition => new DefinitionListItem(definition))
                .ToArray();

            definitionsListBox.BeginUpdate();
            definitionsListBox.Items.Clear();
            definitionsListBox.Items.AddRange(definitions);
            definitionsListBox.EndUpdate();

            var selection = selectedId is null
                ? null
                : definitions.FirstOrDefault(item => item.Definition.Id == selectedId.Value);
            definitionsListBox.SelectedItem = selection ?? definitions.FirstOrDefault();
            if (definitions.Length == 0) LoadDefinition(null);
        }
        finally
        {
            refreshing = false;
        }

        SelectListItem();
    }

    private void SelectListItem()
    {
        if (refreshing) return;
        LoadDefinition((definitionsListBox.SelectedItem as DefinitionListItem)?.Definition);
    }

    private void LoadDefinition(GameDefinition? definition)
    {
        selectedDefinition = definition;
        var enabled = definition is not null;
        keyTextBox.Enabled = enabled;
        nameTextBox.Enabled = enabled;
        descriptionTextBox.Enabled = enabled;
        enabledCheckBox.Enabled = enabled;
        versionNumeric.Enabled = enabled;
        tagsTextBox.Enabled = enabled;
        jsonTextBox.Enabled = enabled;
        duplicateButton.Enabled = enabled;
        deleteButton.Enabled = enabled;
        saveButton.Enabled = enabled;
        SetSpecificEnabled(enabled);

        if (definition is null)
        {
            idTextBox.Clear();
            keyTextBox.Clear();
            nameTextBox.Clear();
            descriptionTextBox.Clear();
            enabledCheckBox.Checked = false;
            versionNumeric.Value = 1;
            tagsTextBox.Clear();
            jsonTextBox.Clear();
            ClearSpecific();
            editorStatusLabel.Text = "No hay definiciones. Use Nuevo para crear una.";
            return;
        }

        idTextBox.Text = definition.Id.ToString();
        keyTextBox.Text = definition.Key.Value;
        nameTextBox.Text = definition.Name;
        descriptionTextBox.Text = definition.Description;
        enabledCheckBox.Checked = definition.Enabled;
        versionNumeric.Value = Math.Clamp(definition.Version, (int)versionNumeric.Minimum, (int)versionNumeric.Maximum);
        tagsTextBox.Text = string.Join(", ", definition.Tags);
        BindSpecific(definition);
        jsonTextBox.Text = JsonSerializer.Serialize(definition, definition.GetType(), ContentPackage.JsonOptions);
        editorStatusLabel.Text = $"{definition.GetType().Name} — {definition.Key}";
    }

    private void CreateDefinition()
    {
        if (application is null || descriptor is null) return;
        try
        {
            var definition = descriptor.Create(application);
            application.Definitions.Register(definition);
            application.Content.Persist(definition);
            RefreshDefinitions(definition.Id);
            ContentChanged?.Invoke(definition);
            editorStatusLabel.Text = $"Creado: {definition.Name}. Complete sus datos y guarde.";
        }
        catch (Exception exception)
        {
            ShowError("No se pudo crear", exception);
        }
    }

    private void DuplicateDefinition()
    {
        if (application is null || descriptor is null || selectedDefinition is null) return;
        try
        {
            var identity = descriptor.NextIdentity(application);
            var node = SerializeNode(selectedDefinition);
            node["id"] = JsonSerializer.SerializeToNode(identity.Id, ContentPackage.JsonOptions);
            node["key"] = JsonSerializer.SerializeToNode(identity.Key, ContentPackage.JsonOptions);
            node["name"] = $"{selectedDefinition.Name} (copia)";
            var duplicate = DeserializeNode(node);
            application.Definitions.Register(duplicate);
            application.Content.Persist(duplicate);
            RefreshDefinitions(duplicate.Id);
            ContentChanged?.Invoke(duplicate);
            editorStatusLabel.Text = $"Duplicado: {duplicate.Name}.";
        }
        catch (Exception exception)
        {
            ShowError("No se pudo duplicar", exception);
        }
    }

    private void DeleteDefinition()
    {
        if (application is null || selectedDefinition is null) return;
        var result = MessageBox.Show(
            this,
            $"¿Eliminar '{selectedDefinition.Name}'? Las referencias se comprobarán al validar el proyecto.",
            "Eliminar definición",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        var removed = selectedDefinition;
        application.Definitions.Unregister(removed.Id);
        application.Dirty.Mark();
        RefreshDefinitions(null);
        ContentChanged?.Invoke(null);
        editorStatusLabel.Text = $"Eliminado: {removed.Name}.";
    }

    private void SaveDefinition()
    {
        if (application is null || descriptor is null || selectedDefinition is null) return;
        try
        {
            var identity = (
                selectedDefinition.Id,
                new ContentKey(keyTextBox.Text),
                nameTextBox.Text,
                descriptionTextBox.Text,
                enabledCheckBox.Checked,
                (int)versionNumeric.Value,
                ParseTags());

            var replacement = TryBuildFromFields(
                    identity.Id, identity.Item2, identity.Item3, identity.Item4,
                    identity.Item5, identity.Item6, identity.Item7, selectedDefinition)
                ?? BuildFromJson(selectedDefinition);

            if (replacement.Id != selectedDefinition.Id)
                throw new InvalidOperationException("El ID de una definición existente no puede cambiarse.");

            application.Definitions.Replace(replacement);
            application.Content.Persist(replacement);
            selectedDefinition = replacement;
            RefreshDefinitions(replacement.Id);
            ContentChanged?.Invoke(replacement);
            editorStatusLabel.Text = application.Content.CurrentPath is null
                ? $"En memoria: {replacement.Name}. Use Archivo → Guardar para crear game.db."
                : $"Guardado en game.db: {replacement.Name}.";
        }
        catch (Exception exception)
        {
            ShowError("No se pudo guardar", exception);
        }
    }

    protected virtual void BindSpecific(GameDefinition definition)
    {
    }

    protected virtual void ClearSpecific()
    {
    }

    protected virtual void SetSpecificEnabled(bool enabled)
    {
        specificTable.Enabled = enabled;
    }

    protected virtual GameDefinition? TryBuildFromFields(
        DefinitionId id,
        ContentKey key,
        string name,
        string description,
        bool enabled,
        int version,
        string[] tags,
        GameDefinition current)
        => null;

    private GameDefinition BuildFromJson(GameDefinition current)
    {
        var node = JsonNode.Parse(jsonTextBox.Text)?.AsObject()
            ?? throw new JsonException("El documento JSON está vacío.");
        node["id"] = JsonSerializer.SerializeToNode(current.Id, ContentPackage.JsonOptions);
        node["key"] = JsonSerializer.SerializeToNode(new ContentKey(keyTextBox.Text), ContentPackage.JsonOptions);
        node["name"] = nameTextBox.Text;
        node["description"] = descriptionTextBox.Text;
        node["enabled"] = enabledCheckBox.Checked;
        node["version"] = (int)versionNumeric.Value;
        node["tags"] = new JsonArray(ParseTags().Select(static tag => (JsonNode?)JsonValue.Create(tag)).ToArray());
        return DeserializeNode(node);
    }

    protected void FillEnum<T>(ComboBox combo) where T : struct, Enum
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.DataSource = Enum.GetValues<T>();
    }

    protected static void SelectEnum<T>(ComboBox combo, T value) where T : struct, Enum
        => combo.SelectedItem = value;

    protected static T ReadEnum<T>(ComboBox combo, T fallback) where T : struct, Enum
        => combo.SelectedItem is T value ? value : fallback;

    protected void BindDefinitionCombo<T>(ComboBox combo, DefinitionId? selected, bool optional = true)
        where T : GameDefinition
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        var options = new List<DefinitionPick>(optional ? [new DefinitionPick("(ninguno)", null)] : []);
        options.AddRange(Editor.Definitions.GetAll<T>()
            .OrderBy(static definition => definition.Name)
            .Select(static definition => new DefinitionPick($"{definition.Name}  [{definition.Key}]", definition.Id)));
        combo.DisplayMember = nameof(DefinitionPick.Label);
        combo.ValueMember = nameof(DefinitionPick.Id);
        combo.DataSource = options;
        if (options.Count == 0) return;
        var index = options.FindIndex(option => option.Id == selected);
        combo.SelectedIndex = index >= 0 ? index : 0;
    }

    protected static DefinitionId? ReadDefinitionId(ComboBox combo)
        => combo.SelectedItem is DefinitionPick pick ? pick.Id : null;

    protected static DefinitionId RequireDefinitionId(ComboBox combo, string field)
        => ReadDefinitionId(combo) ?? throw new InvalidOperationException($"{field} es obligatorio.");

    protected static ContentKey ReadContentKey(TextBox box, bool required = true)
    {
        var value = box.Text.Trim();
        if (value.Length == 0)
        {
            if (required) throw new InvalidOperationException($"{box.Name} requiere una ContentKey.");
            return default;
        }

        return new ContentKey(value);
    }

    protected static ContentKey? ReadOptionalContentKey(TextBox box)
    {
        var value = box.Text.Trim();
        return value.Length == 0 ? null : new ContentKey(value);
    }

    protected static void SetNumeric(NumericUpDown numeric, decimal value)
        => numeric.Value = Math.Clamp(value, numeric.Minimum, numeric.Maximum);

    protected void BindAssetCombo(ComboBox combo, AssetKind kind, ContentKey selected)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDown;
        var names = Editor.Assets.ListFileNames(kind).ToList();
        var stem = AssetCatalog.FileStem(selected);
        if (stem.Length > 0 && !names.Contains(stem, StringComparer.OrdinalIgnoreCase))
            names.Insert(0, stem);
        combo.Items.Clear();
        foreach (var name in names) combo.Items.Add(name);
        combo.Text = stem;
    }

    protected ContentKey ReadAssetKey(ComboBox combo, AssetKind kind, bool required = true)
    {
        var stem = combo.Text.Trim();
        if (stem.Length == 0)
        {
            if (required) throw new InvalidOperationException($"Hace falta un archivo en {AssetCatalog.Folder(kind)}.");
            return default;
        }

        return AssetCatalog.Key(kind, stem);
    }

    protected void BindVisualKey(TextBox box, AssetKind kind, ContentKey selected)
    {
        box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        box.AutoCompleteSource = AutoCompleteSource.CustomSource;
        var source = new AutoCompleteStringCollection();
        foreach (var name in Editor.Assets.ListFileNames(kind))
        {
            source.Add(name);
            source.Add(AssetCatalog.Key(kind, name).Value);
        }

        box.AutoCompleteCustomSource = source;
        box.PlaceholderText = $"{AssetCatalog.Folder(kind)}/archivo.png";
        box.Text = selected.IsEmpty ? string.Empty : selected.Value;
    }

    protected ContentKey ReadVisualKey(TextBox box, AssetKind kind, bool required = true)
    {
        var text = box.Text.Trim();
        if (text.Length == 0)
        {
            if (required) throw new InvalidOperationException($"VisualKey vacío. Use un archivo de resources/{AssetCatalog.Folder(kind)}.");
            return default;
        }

        return text.Contains('.') ? new ContentKey(text) : AssetCatalog.Key(kind, text);
    }

    protected DefinitionId ResolveDefinition<T>(string text, string field) where T : GameDefinition
    {
        text = text.Trim();
        if (text.Length == 0) throw new InvalidOperationException($"{field} está vacío.");
        if (DefinitionId.TryParse(text, out var id) && Editor.Definitions.TryGet<T>(id, out _))
            return id;
        if (Editor.Definitions.TryGet<T>(new ContentKey(text), out var definition) && definition is not null)
            return definition.Id;
        throw new InvalidOperationException($"No existe {typeof(T).Name} '{text}' para {field}.");
    }

    protected DefinitionId[] ResolveDefinitionList<T>(TextBox box) where T : GameDefinition
        => box.Text
            .Split([',', '\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => ResolveDefinition<T>(value, box.Name))
            .Distinct()
            .ToArray();

    protected sealed record DefinitionPick(string Label, DefinitionId? Id)
    {
        public override string ToString() => Label;
    }

    private JsonObject SerializeNode(GameDefinition definition)
        => JsonSerializer.SerializeToNode(definition, definition.GetType(), ContentPackage.JsonOptions)?.AsObject()
           ?? throw new JsonException("No se pudo serializar la definición.");

    private GameDefinition DeserializeNode(JsonObject node)
    {
        if (descriptor is null) throw new InvalidOperationException("El editor no está configurado.");
        return JsonSerializer.Deserialize(node.ToJsonString(ContentPackage.JsonOptions), descriptor.DefinitionType, ContentPackage.JsonOptions)
               as GameDefinition
               ?? throw new JsonException($"No se pudo leer {descriptor.DefinitionType.Name}.");
    }

    private string[] ParseTags() => tagsTextBox.Text
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private void ShowError(string title, Exception exception)
    {
        editorStatusLabel.Text = exception.Message;
        MessageBox.Show(this, exception.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private sealed class DefinitionListItem(GameDefinition definition)
    {
        public GameDefinition Definition { get; } = definition;
        public override string ToString() => $"{Definition.Name}  [{Definition.Key}]";
    }
}
