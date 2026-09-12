using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

[DesignerCategory("Form")]
public sealed partial class TilesetPaletteDock : DockContent
{
    private EditorApplication? application;
    private TilesetImageProvider? images;

    public TilesetPaletteDock()
    {
        InitializeComponent();
        EditorTheme.ApplyWindow(this);
        autotile.DataSource = Enum.GetValues<MapAutotileMode>();
        tilesets.SelectedIndexChanged += (_, _) => SelectTileset();
        autotile.SelectedIndexChanged += (_, _) =>
        {
            if (application is not null && autotile.SelectedItem is MapAutotileMode mode)
                application.Maps.Palette.SetAutotileMode(mode);
        };
        zoom.ValueChanged += (_, _) => surface.Zoom = (int)zoom.Value;
        surface.TileSelected += OnTileSelected;
    }

    public TilesetPaletteDock(EditorApplication application, TilesetImageProvider images)
        : this()
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        this.images = images ?? throw new ArgumentNullException(nameof(images));
        RefreshTilesets();
    }

    /// <summary>
    /// Se dispara cuando el usuario elige explícitamente un tile de la imagen.
    /// MainForm lo usa para activar inmediatamente la herramienta Pintar, igual que Intersect.
    /// </summary>
    public event Action<TilesetDefinition, Vector2IntData>? TileBrushSelected;

    public void RefreshTilesets()
    {
        if (application is null) return;

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

        // DataSource no garantiza SelectedIndexChanged en todos los escenarios de recarga.
        // Forzamos la sincronización para que la imagen visible siempre corresponda al listado.
        SelectTileset();
    }

    private void SelectTileset()
    {
        if (application is null || images is null || tilesets.SelectedItem is not TilesetDefinition definition)
        {
            surface.SetImage(null, default);
            selection.Text = "Sin tileset";
            return;
        }

        try
        {
            var image = images.Get(definition);
            surface.SetImage(image, definition.TileSize);

            // Mantiene la celda actual al refrescar el mismo tileset. Al cambiar de tileset,
            // deja preparada la primera celda como pincel para que el flujo sea inmediato.
            var palette = application.Maps.Palette;
            var cell = palette.HasSelection && palette.SelectedTilesetKey == definition.Key
                ? palette.SelectedAtlasCell
                : new Vector2IntData(0, 0);

            if (!surface.ContainsCell(cell))
                cell = new Vector2IntData(0, 0);

            SetSelectedTile(definition, cell, activatePaint: false);
            surface.SelectCell(cell);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException or ArgumentException)
        {
            surface.SetImage(null, definition.TileSize);
            selection.Text = exception.Message;
        }
    }

    private void OnTileSelected(Vector2IntData cell)
    {
        if (application is null || tilesets.SelectedItem is not TilesetDefinition definition) return;
        SetSelectedTile(definition, cell, activatePaint: true);
    }

    private void SetSelectedTile(TilesetDefinition definition, Vector2IntData cell, bool activatePaint)
    {
        if (application is null) return;

        var mode = autotile.SelectedItem is MapAutotileMode value ? value : MapAutotileMode.None;
        application.Maps.Palette.Select(definition.Key, cell, autotileMode: mode);
        surface.SelectCell(cell);
        selection.Text = $"{definition.Name} · {cell.X},{cell.Y}";

        if (activatePaint)
            TileBrushSelected?.Invoke(definition, cell);
    }
}

[DesignerCategory("Code")]
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
            if (!ContainsCell(cell)) return;
            SelectCell(cell);
            TileSelected?.Invoke(cell);
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

    public bool ContainsCell(Vector2IntData cell)
    {
        if (image is null || tileSize.X <= 0 || tileSize.Y <= 0 || cell.X < 0 || cell.Y < 0)
            return false;

        return cell.X * tileSize.X < image.Width && cell.Y * tileSize.Y < image.Height;
    }

    public void SelectCell(Vector2IntData cell)
    {
        if (!ContainsCell(cell)) return;
        selected = cell;
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
            // Intersect-style: el pincel activo queda claramente marcado sobre el atlas.
            using var shadowPen = new Pen(Color.Black, 4);
            using var pen = new Pen(Color.Yellow, 2);
            var rectangle = new Rectangle(
                cell.X * tileSize.X * zoom,
                cell.Y * tileSize.Y * zoom,
                Math.Max(1, tileSize.X * zoom - 1),
                Math.Max(1, tileSize.Y * zoom - 1));
            e.Graphics.DrawRectangle(shadowPen, rectangle);
            e.Graphics.DrawRectangle(pen, rectangle);
        }
    }

    private void UpdateSize()
    {
        Size = image is null
            ? new Size(1, 1)
            : new Size(image.Width * zoom, image.Height * zoom);
    }
}
