using NuevoMMO.Core;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

public sealed class TilesetPaletteDock : DockContent
{
    private readonly EditorApplication application;
    private readonly TilesetImageProvider images;
    private readonly TilesetImporter importer;
    private readonly ComboBox tilesets = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
    private readonly ComboBox autotile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly NumericUpDown zoom = new() { Minimum = 1, Maximum = 4, Value = 1, Width = 48 };
    private readonly Label selection = new() { AutoSize = true, Padding = new Padding(6, 7, 0, 0) };
    private readonly TilesetPaletteSurface surface = new();
    private readonly Panel scroller = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(24, 26, 30) };

    public TilesetPaletteDock(EditorApplication application, TilesetImageProvider images)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        this.images = images ?? throw new ArgumentNullException(nameof(images));
        importer = new TilesetImporter(application.Definitions, images);

        Text = "Tilesets";
        TabText = Text;
        HideOnClose = true;

        autotile.DataSource = Enum.GetValues<MapAutotileMode>();

        var importButton = new Button { Text = "Importar PNG", AutoSize = true };
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 34,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(3)
        };
        toolbar.Controls.Add(tilesets);
        toolbar.Controls.Add(new Label { Text = "Modo:", AutoSize = true, Padding = new Padding(5, 7, 0, 0) });
        toolbar.Controls.Add(autotile);
        toolbar.Controls.Add(new Label { Text = "Zoom:", AutoSize = true, Padding = new Padding(5, 7, 0, 0) });
        toolbar.Controls.Add(zoom);
        toolbar.Controls.Add(importButton);
        toolbar.Controls.Add(selection);

        scroller.Controls.Add(surface);
        Controls.Add(scroller);
        Controls.Add(toolbar);

        tilesets.SelectedIndexChanged += (_, _) => SelectTileset();
        autotile.SelectedIndexChanged += (_, _) =>
        {
            if (autotile.SelectedItem is MapAutotileMode mode)
                application.Maps.Palette.SetAutotileMode(mode);
        };
        zoom.ValueChanged += (_, _) => surface.Zoom = (int)zoom.Value;
        surface.TileSelected += OnTileSelected;
        importButton.Click += (_, _) => ImportTilesets();

        RefreshTilesets();
    }

    public void RefreshTilesets()
    {
        var current = tilesets.SelectedItem as TilesetDefinition;
        var values = application.Definitions.GetAll<TilesetDefinition>()
            .OrderBy(static value => value.Name)
            .ToArray();

        tilesets.BeginUpdate();
        tilesets.DataSource = null;
        tilesets.DisplayMember = nameof(TilesetDefinition.Name);
        tilesets.DataSource = values;
        if (current is not null)
        {
            var index = Array.FindIndex(values, value => value.Id == current.Id);
            if (index >= 0) tilesets.SelectedIndex = index;
        }
        tilesets.EndUpdate();
    }

    private void ImportTilesets()
    {
        var tileSize = application.Maps.Document?.TileSize ?? new Vector2IntData(32, 32);
        var imported = importer.ImportClientTilesets(tileSize);
        if (imported.Count > 0) application.Dirty.Mark();
        RefreshTilesets();
    }

    private void SelectTileset()
    {
        if (tilesets.SelectedItem is not TilesetDefinition definition)
        {
            surface.SetImage(null, default);
            return;
        }

        try
        {
            surface.SetImage(images.Get(definition), definition.TileSize);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException or ArgumentException)
        {
            surface.SetImage(null, definition.TileSize);
            selection.Text = exception.Message;
        }
    }

    private void OnTileSelected(Vector2IntData cell)
    {
        if (tilesets.SelectedItem is not TilesetDefinition definition) return;
        var mode = autotile.SelectedItem is MapAutotileMode value ? value : MapAutotileMode.None;
        application.Maps.Palette.Select(definition.Key, cell, autotileMode: mode);
        selection.Text = $"{cell.X},{cell.Y}";
    }
}

internal sealed class TilesetPaletteSurface : Control
{
    private Bitmap? image;
    private Vector2IntData tileSize;
    private Vector2IntData? selected;
    private int zoom = 1;

    public TilesetPaletteSurface()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(22, 24, 28);
        Location = Point.Empty;
        MouseDown += (_, e) =>
        {
            if (image is null || tileSize.X <= 0 || tileSize.Y <= 0 || e.Button != MouseButtons.Left) return;
            var cell = new Vector2IntData(
                e.X / Math.Max(1, tileSize.X * zoom),
                e.Y / Math.Max(1, tileSize.Y * zoom));
            if (cell.X * tileSize.X >= image.Width || cell.Y * tileSize.Y >= image.Height) return;
            selected = cell;
            TileSelected?.Invoke(cell);
            Invalidate();
        };
    }

    public int Zoom
    {
        get => zoom;
        set
        {
            zoom = Math.Clamp(value, 1, 4);
            UpdateSize();
            Invalidate();
        }
    }

    public event Action<Vector2IntData>? TileSelected;

    public void SetImage(Bitmap? bitmap, Vector2IntData size)
    {
        image = bitmap;
        tileSize = size;
        selected = null;
        UpdateSize();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (image is null) return;

        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(
            image,
            new Rectangle(0, 0, image.Width * zoom, image.Height * zoom),
            new Rectangle(0, 0, image.Width, image.Height),
            GraphicsUnit.Pixel);

        if (tileSize.X > 0 && tileSize.Y > 0)
        {
            using var gridPen = new Pen(Color.FromArgb(50, Color.White));
            var stepX = tileSize.X * zoom;
            var stepY = tileSize.Y * zoom;
            for (var x = 0; x <= Width; x += stepX) e.Graphics.DrawLine(gridPen, x, 0, x, Height);
            for (var y = 0; y <= Height; y += stepY) e.Graphics.DrawLine(gridPen, 0, y, Width, y);
        }

        if (selected is { } cell)
        {
            using var pen = new Pen(Color.Yellow, 2);
            e.Graphics.DrawRectangle(
                pen,
                cell.X * tileSize.X * zoom,
                cell.Y * tileSize.Y * zoom,
                tileSize.X * zoom - 1,
                tileSize.Y * zoom - 1);
        }
    }

    private void UpdateSize()
    {
        Size = image is null
            ? new Size(1, 1)
            : new Size(image.Width * zoom, image.Height * zoom);
    }
}
