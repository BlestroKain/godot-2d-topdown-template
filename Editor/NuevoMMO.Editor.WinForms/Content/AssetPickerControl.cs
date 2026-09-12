using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Selector visual de archivos de <c>Client/resources/{tipo}</c>. Lista + vista previa.
/// </summary>
public sealed class AssetPickerControl : UserControl
{
    private readonly TextBox filter = new();
    private readonly ListView list = new();
    private readonly PictureBox preview = new();
    private readonly Label caption = new();
    private readonly ImageList thumbs = new() { ImageSize = new Size(48, 48), ColorDepth = ColorDepth.Depth32Bit };
    private readonly Dictionary<string, int> thumbIndex = new(StringComparer.OrdinalIgnoreCase);
    private AssetLibrary? library;
    private AssetKind kind;
    private ContentKey selected;
    private bool suppressing;

    public AssetPickerControl()
    {
        filter.PlaceholderText = "Filtrar imagen…";
        filter.Dock = DockStyle.Top;
        filter.TextChanged += (_, _) => Rebuild();

        list.Dock = DockStyle.Fill;
        list.HideSelection = false;
        list.View = View.List;
        list.SmallImageList = thumbs;
        list.FullRowSelect = true;
        list.SelectedIndexChanged += (_, _) => OnListSelected();

        preview.Dock = DockStyle.Fill;
        preview.SizeMode = PictureBoxSizeMode.Zoom;
        preview.BackColor = Color.FromArgb(22, 24, 28);
        preview.BorderStyle = BorderStyle.FixedSingle;

        caption.Dock = DockStyle.Bottom;
        caption.Height = 22;
        caption.TextAlign = ContentAlignment.MiddleLeft;
        caption.ForeColor = EditorTheme.Muted;
        caption.Padding = new Padding(6, 0, 0, 0);
        caption.Text = "Sin imagen";

        var previewHost = new Panel { Dock = DockStyle.Right, Width = 176, Padding = new Padding(8) };
        previewHost.Controls.Add(preview);
        previewHost.Controls.Add(caption);

        Controls.Add(list);
        Controls.Add(previewHost);
        Controls.Add(filter);
        BackColor = EditorTheme.Surface;
    }

    public event Action<ContentKey>? SelectionChanged;

    public AssetKind Kind
    {
        get => kind;
        set => kind = value;
    }

    public ContentKey SelectedKey => selected;

    public void Bind(AssetLibrary assets, AssetKind assetKind, ContentKey current)
    {
        library = assets ?? throw new ArgumentNullException(nameof(assets));
        kind = assetKind;
        selected = current;
        Rebuild();
        SelectKey(current);
    }

    public void ClearSelection()
    {
        selected = default;
        list.SelectedItems.Clear();
        preview.Image = null;
        caption.Text = "Sin imagen";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            preview.Image?.Dispose();
            thumbs.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Rebuild()
    {
        if (library is null) return;
        suppressing = true;
        try
        {
            var query = filter.Text.Trim();
            var names = library.ListFileNames(kind)
                .Where(name => query.Length == 0 || name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            list.BeginUpdate();
            list.Items.Clear();
            foreach (var name in names)
            {
                var key = AssetCatalog.Key(kind, name);
                var item = new ListViewItem(name) { Tag = key };
                if (thumbIndex.TryGetValue(name, out var index))
                    item.ImageIndex = index;
                list.Items.Add(item);
            }

            list.EndUpdate();
        }
        finally
        {
            suppressing = false;
        }

        SelectKey(selected);
    }

    private void SelectKey(ContentKey key)
    {
        suppressing = true;
        try
        {
            list.SelectedItems.Clear();
            foreach (ListViewItem item in list.Items)
            {
                if (item.Tag is ContentKey candidate && candidate == key)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }
        finally
        {
            suppressing = false;
        }

        ShowPreview(key);
    }

    private void OnListSelected()
    {
        if (suppressing) return;
        if (list.SelectedItems.Count == 0) return;
        if (list.SelectedItems[0].Tag is not ContentKey key) return;
        selected = key;
        EnsureThumb(list.SelectedItems[0]);
        ShowPreview(key);
        SelectionChanged?.Invoke(key);
    }

    private void EnsureThumb(ListViewItem item)
    {
        if (library is null || item.Tag is not ContentKey key) return;
        var stem = AssetCatalog.FileStem(key);
        if (thumbIndex.ContainsKey(stem)) return;
        if (!library.TryResolve(key, out var path)) return;
        try
        {
            using var source = Image.FromFile(path);
            var bitmap = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(22, 24, 28));
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(source, new Rectangle(0, 0, 48, 48));
            }

            thumbs.Images.Add(bitmap);
            thumbIndex[stem] = thumbs.Images.Count - 1;
            item.ImageIndex = thumbIndex[stem];
        }
        catch (Exception exception) when (exception is IOException or OutOfMemoryException or ArgumentException)
        {
        }
    }

    private void ShowPreview(ContentKey key)
    {
        preview.Image?.Dispose();
        preview.Image = null;
        if (key.IsEmpty || library is null || !library.TryResolve(key, out var path))
        {
            caption.Text = key.IsEmpty ? "Sin imagen" : key.Value;
            return;
        }

        try
        {
            using var source = Image.FromFile(path);
            preview.Image = new Bitmap(source);
            caption.Text = Path.GetFileName(path);
        }
        catch (Exception exception) when (exception is IOException or OutOfMemoryException or ArgumentException)
        {
            caption.Text = key.Value;
        }
    }
}
