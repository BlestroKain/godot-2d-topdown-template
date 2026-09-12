using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using NuevoMMO.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

/// <summary>
/// Cuadrícula mundial de mapas. Celdas ocupadas se abren; las vacías adyacentes crean un mapa vecino.
/// </summary>
[DesignerCategory("Form")]
public sealed class MapGridDock : DockContent
{
    private readonly EditorApplication application;
    private readonly GridSurface surface = new();
    private readonly Panel scroller = new() { AutoScroll = true, Dock = DockStyle.Fill };
    private readonly Label hint = new()
    {
        Dock = DockStyle.Top,
        Height = 28,
        Padding = new Padding(8, 6, 8, 0),
        Text = "Clic en un mapa para abrirlo. Clic en + para crear un mapa vecino."
    };

    public MapGridDock(EditorApplication application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        Text = "Mundo";
        TabText = "Mundo";
        HideOnClose = true;
        ShowHint = DockState.DockLeft;
        ClientSize = new Size(280, 420);

        scroller.BackColor = Color.FromArgb(24, 26, 30);
        scroller.Controls.Add(surface);
        Controls.Add(scroller);
        Controls.Add(hint);
        EditorTheme.ApplyWindow(this);

        surface.CellActivated += cell =>
        {
            var map = MapWorldGrid.At(this.application.Definitions, cell.X, cell.Y);
            if (map is not null)
            {
                MapActivated?.Invoke(map);
                return;
            }

            if (!MapWorldGrid.CanCreate(this.application.Definitions, cell.X, cell.Y)) return;
            CreateRequested?.Invoke(cell.X, cell.Y);
        };

        RefreshGrid();
    }

    public event Action<MapDefinition>? MapActivated;
    public event Action<int, int>? CreateRequested;

    public void RefreshGrid()
    {
        var bounds = MapWorldGrid.VisibleBounds(application.Definitions);
        var occupied = application.Definitions.GetAll<MapDefinition>()
            .ToDictionary(map => (map.GridX, map.GridY));
        surface.Bind(bounds, occupied, application.Maps.Document?.Id);
        var width = (bounds.MaxX - bounds.MinX + 1) * GridSurface.CellSize + 16;
        var height = (bounds.MaxY - bounds.MinY + 1) * GridSurface.CellSize + 16;
        surface.Size = new Size(Math.Max(width, scroller.ClientSize.Width), Math.Max(height, scroller.ClientSize.Height));
        surface.Invalidate();
    }

    [DesignerCategory("Code")]
    private sealed class GridSurface : Control
    {
        public const int CellSize = 88;
        private (int MinX, int MinY, int MaxX, int MaxY) bounds;
        private Dictionary<(int X, int Y), MapDefinition> occupied = [];
        private DefinitionId? activeId;

        public GridSurface()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(24, 26, 30);
            MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                var x = bounds.MinX + e.X / CellSize;
                var y = bounds.MinY + e.Y / CellSize;
                if (x < bounds.MinX || y < bounds.MinY || x > bounds.MaxX || y > bounds.MaxY) return;
                CellActivated?.Invoke(new Vector2IntData(x, y));
            };
        }

        public event Action<Vector2IntData>? CellActivated;

        public void Bind(
            (int MinX, int MinY, int MaxX, int MaxY) visible,
            Dictionary<(int X, int Y), MapDefinition> maps,
            DefinitionId? current)
        {
            bounds = visible;
            occupied = maps;
            activeId = current;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var emptyPen = new Pen(Color.FromArgb(90, 90, 90)) { DashStyle = DashStyle.Dash };
            using var fill = new SolidBrush(Color.FromArgb(45, 58, 72));
            using var activeFill = new SolidBrush(Color.FromArgb(0, 90, 150));
            var font = Font;

            for (var y = bounds.MinY; y <= bounds.MaxY; y++)
            for (var x = bounds.MinX; x <= bounds.MaxX; x++)
            {
                var rect = new Rectangle((x - bounds.MinX) * CellSize + 4, (y - bounds.MinY) * CellSize + 4, CellSize - 8, CellSize - 8);
                if (occupied.TryGetValue((x, y), out var map))
                {
                    e.Graphics.FillRectangle(map.Id == activeId ? activeFill : fill, rect);
                    e.Graphics.DrawRectangle(Pens.SteelBlue, rect);
                    var name = map.Name;
                    if (name.Length > 14) name = name[..13] + "…";
                    TextRenderer.DrawText(
                        e.Graphics,
                        name,
                        font,
                        rect,
                        EditorTheme.Text,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                }
                else
                {
                    e.Graphics.DrawRectangle(emptyPen, rect);
                    var canCreate = occupied.Count == 0
                        ? x == 0 && y == 0
                        : occupied.Keys.Any(cell => Math.Abs(cell.X - x) + Math.Abs(cell.Y - y) == 1);
                    if (canCreate)
                        TextRenderer.DrawText(e.Graphics, "+", font, rect, Color.FromArgb(160, 200, 160),
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }
    }
}
