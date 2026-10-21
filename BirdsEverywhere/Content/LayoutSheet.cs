using System.Globalization;
using System.Security;
using System.Text;
using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Content;

/// <summary>Reference sheet for artists: which clips use which cell, as text and as an SVG template.</summary>
public static class LayoutSheet
{
    private const int LabelHeight = 12;
    private const int FontSize = 4;

    public static SortedDictionary<int, List<string>> CellPoses(SpriteLayout layout)
    {
        SortedDictionary<int, List<string>> poses = new();
        foreach ((string name, SpriteClip clip) in layout.Clips)
        {
            Add(poses, clip.Frames, name);
            Add(poses, clip.Repeat?.Frames, $"{name}.repeat");
            Add(poses, clip.End, $"{name}.end");
        }
        return poses;
    }

    public static int CellCount(SpriteLayout layout) => CellPoses(layout).Keys.DefaultIfEmpty(-1).Max() + 1;

    public static IReadOnlyList<string> Text(SpriteLayout layout, int cols)
    {
        SortedDictionary<int, List<string>> poses = CellPoses(layout);
        int cells = CellCount(layout);
        List<string> lines = new() { $"{layout.Id}  {layout.FrameWidth}x{layout.FrameHeight}  {cells} cells  sheet width {cols * layout.FrameWidth}px ({cols} per row)" };
        for (int cell = 0; cell < cells; cell++)
        {
            lines.Add($"{cell,3}  {(poses.TryGetValue(cell, out List<string>? labels) ? string.Join(", ", labels) : "unused")}");
        }
        return lines;
    }

    /// <summary>One source pixel per SVG unit, so the template matches the final sheet's cell geometry.</summary>
    public static string Svg(SpriteLayout layout, int cols)
    {
        SortedDictionary<int, List<string>> poses = CellPoses(layout);
        int cells = CellCount(layout);
        int rows = (cells + cols - 1) / cols;
        int rowHeight = layout.FrameHeight + LabelHeight;
        StringBuilder svg = new();
        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{cols * layout.FrameWidth}\" height=\"{rows * rowHeight}\" font-family=\"monospace\" font-size=\"{FontSize}\">");
        for (int cell = 0; cell < cells; cell++)
        {
            int x = cell % cols * layout.FrameWidth;
            int y = cell / cols * rowHeight;
            string label = poses.TryGetValue(cell, out List<string>? labels) ? string.Join(", ", labels) : "unused";
            svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{x}\" y=\"{y}\" width=\"{layout.FrameWidth}\" height=\"{layout.FrameHeight}\" fill=\"none\" stroke=\"#f0f\" stroke-width=\"0.25\"/>")
                .Append(CultureInfo.InvariantCulture, $"<text x=\"{x + 1}\" y=\"{y + FontSize + 1}\">{cell}</text>")
                .Append(CultureInfo.InvariantCulture, $"<text x=\"{x + 1}\" y=\"{y + layout.FrameHeight + FontSize + 2}\">{SecurityElement.Escape(label)}</text>");
        }
        return svg.Append("</svg>").ToString();
    }

    private static void Add(SortedDictionary<int, List<string>> poses, IReadOnlyList<ClipFrame>? frames, string label)
    {
        if (frames is null)
        {
            return;
        }
        foreach (int cell in frames.Select(frame => frame.Cell).Distinct())
        {
            if (!poses.TryGetValue(cell, out List<string>? labels))
            {
                poses[cell] = labels = new List<string>();
            }
            if (!labels.Contains(label))
            {
                labels.Add(label);
            }
        }
    }
}
