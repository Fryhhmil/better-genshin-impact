using System.Windows.Media;

namespace BetterGenshinImpact.ViewModel;

/// <summary>
/// 地图标点大类的强调色。各数据源的大类名称不同，因此按大类顺序循环取色。
/// </summary>
public static class MapLabelCategoryPalette
{
    /// <summary>
    /// 强调色列表（RGB），与设计稿中的大类色一致
    /// </summary>
    private static readonly Color[] Colors =
    [
        Color.FromRgb(0x7F, 0xD3, 0xFF),
        Color.FromRgb(0xE8, 0xC4, 0x6A),
        Color.FromRgb(0xF0, 0xB8, 0x6E),
        Color.FromRgb(0x9B, 0xE0, 0xC8),
        Color.FromRgb(0xF2, 0xA7, 0xC3),
        Color.FromRgb(0xC7, 0xB3, 0xFF),
        Color.FromRgb(0xFF, 0x9A, 0x85),
        Color.FromRgb(0x86, 0xBE, 0xFF),
        Color.FromRgb(0xB9, 0xD9, 0x8A),
        Color.FromRgb(0xF0, 0xC4, 0x9A),
        Color.FromRgb(0xE6, 0xE0, 0xA0)
    ];

    /// <summary>
    /// 图标底色的透明度（约 14%）
    /// </summary>
    private const byte BackgroundAlpha = 0x24;

    /// <summary>
    /// 取指定序号大类的强调色画刷与图标底色画刷
    /// </summary>
    /// <param name="index">大类序号</param>
    public static (Brush Tint, Brush Background) Get(int index)
    {
        var color = Colors[((index % Colors.Length) + Colors.Length) % Colors.Length];
        var tint = new SolidColorBrush(color);
        tint.Freeze();
        var background = new SolidColorBrush(Color.FromArgb(BackgroundAlpha, color.R, color.G, color.B));
        background.Freeze();
        return (tint, background);
    }
}
