#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class RecipeEditorForm
{
    private IContainer? components;
    private ComboBox professionCombo = null!;
    private ComboBox outputItemCombo = null!;
    private NumericUpDown outputQuantityNumeric = null!;
    private TextBox inputItemsTextBox = null!;
    private NumericUpDown requiredLevelNumeric = null!;
    private TextBox stationKeyTextBox = null!;
    private NumericUpDown craftTimeNumeric = null!;
    private NumericUpDown failureNumeric = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        professionCombo = new ComboBox();
        outputItemCombo = new ComboBox();
        outputQuantityNumeric = new NumericUpDown { Minimum = 1, Maximum = 9999, Value = 1 };
        inputItemsTextBox = new TextBox { AcceptsReturn = true, Multiline = true, ScrollBars = ScrollBars.Vertical };
        requiredLevelNumeric = new NumericUpDown { Maximum = 10000 };
        stationKeyTextBox = new TextBox();
        craftTimeNumeric = new NumericUpDown { Maximum = 3_600_000, Increment = 100 };
        failureNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100 };
        SuspendLayout();
        Name = "RecipeEditorForm";
        Text = "Recetas";
        specificTabPage.Text = "Receta";
        AddSpecificRow(0, "Profesión", professionCombo);
        AddSpecificRow(1, "Resultado", outputItemCombo);
        AddSpecificRow(2, "Cantidad", outputQuantityNumeric);
        AddSpecificRow(3, "Insumos (claves)", inputItemsTextBox, 90);
        AddSpecificRow(4, "Nivel requerido", requiredLevelNumeric);
        AddSpecificRow(5, "Estación", stationKeyTextBox);
        AddSpecificRow(6, "Tiempo ms", craftTimeNumeric);
        AddSpecificRow(7, "Fallo %", failureNumeric);
        ResumeLayout(false);
    }
}
