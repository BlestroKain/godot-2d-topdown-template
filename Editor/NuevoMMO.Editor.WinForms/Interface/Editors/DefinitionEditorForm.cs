using System.Text.Json;
using System.Text.Json.Nodes;
using NuevoMMO.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

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
        Configure(application, descriptor);
    }

    public event Action<GameDefinition?>? ContentChanged;

    public Type? DefinitionType => descriptor?.DefinitionType;

    protected void Configure(EditorApplication editorApplication, DefinitionEditorDescriptor editorDescriptor)
    {
        application = editorApplication ?? throw new ArgumentNullException(nameof(editorApplication));
        descriptor = editorDescriptor ?? throw new ArgumentNullException(nameof(editorDescriptor));
        Text = editorDescriptor.Title;
        TabText = editorDescriptor.Title;
        RefreshDefinitions();
    }

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
            application.Dirty.Mark();
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
            application.Dirty.Mark();
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
            var node = JsonNode.Parse(jsonTextBox.Text)?.AsObject()
                ?? throw new JsonException("El documento JSON está vacío.");
            node["id"] = JsonSerializer.SerializeToNode(selectedDefinition.Id, ContentPackage.JsonOptions);
            node["key"] = JsonSerializer.SerializeToNode(new ContentKey(keyTextBox.Text), ContentPackage.JsonOptions);
            node["name"] = nameTextBox.Text;
            node["description"] = descriptionTextBox.Text;
            node["enabled"] = enabledCheckBox.Checked;
            node["version"] = (int)versionNumeric.Value;
            node["tags"] = new JsonArray(ParseTags().Select(JsonValue.Create).ToArray<JsonNode?>());

            var replacement = DeserializeNode(node);
            if (replacement.Id != selectedDefinition.Id)
                throw new InvalidOperationException("El ID de una definición existente no puede cambiarse.");

            application.Definitions.Replace(replacement);
            application.Dirty.Mark();
            selectedDefinition = replacement;
            RefreshDefinitions(replacement.Id);
            ContentChanged?.Invoke(replacement);
            editorStatusLabel.Text = $"Guardado en el proyecto: {replacement.Name}.";
        }
        catch (Exception exception)
        {
            editorTabs.SelectedTab = jsonTabPage;
            ShowError("No se pudo guardar", exception);
        }
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
