using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using BetterGenshinImpact.Model.MaskMap;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.ViewModel;

/// <summary>
/// 地图标点类型（点位菜单网格中的一张卡片）
/// </summary>
public partial class MapLabelItemVm : ObservableObject
{
    /// <summary>
    /// 没有所属大类信息时使用的强调色（从存储恢复且不在当前大类树中）
    /// </summary>
    private static readonly (Brush Tint, Brush Background) DefaultTint = MapLabelCategoryPalette.Get(0);

    public string Id { get; }
    public IReadOnlyList<string> LabelIds { get; }
    public string Name { get; }
    public string IconUrl { get; }
    public int PointCount { get; }
    public string ParentId { get; }

    /// <summary>
    /// 所属大类名称（搜索结果中显示在名称下方）
    /// </summary>
    public string CategoryName { get; }

    /// <summary>
    /// 所属大类的强调色
    /// </summary>
    public Brush TintBrush { get; }

    /// <summary>
    /// 所属大类的图标底色
    /// </summary>
    public Brush TintBackgroundBrush { get; }

    /// <summary>
    /// 没有点位的类型不可开启
    /// </summary>
    public bool IsEmpty => PointCount == 0;

    [ObservableProperty] private ImageSource? _iconImage;

    /// <summary>
    /// 是否已选中（正在地图上显示），网格中高亮显示
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// 该分类下已标记的点位数量（分类点位加载后统计）
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PointCountText))]
    private int _markedCount;

    /// <summary>
    /// 网格中显示的数量：没有标记时显示总数，有标记时显示“已标记/总数”
    /// </summary>
    public string PointCountText => MarkedCount > 0
        ? $"{MarkedCount.ToString("N0", CultureInfo.CurrentCulture)}/{PointCount.ToString("N0", CultureInfo.CurrentCulture)}"
        : PointCount.ToString("N0", CultureInfo.CurrentCulture);

    /// <param name="item">类型数据</param>
    /// <param name="categoryName">所属大类名称</param>
    /// <param name="tint">所属大类强调色；为空时使用默认色</param>
    /// <param name="tintBackground">所属大类图标底色；为空时使用默认色</param>
    public MapLabelItemVm(MaskMapPointLabel item, string? categoryName = null, Brush? tint = null, Brush? tintBackground = null)
    {
        Id = item.LabelId;
        LabelIds = item.LabelIds is { Count: > 0 } ? item.LabelIds : new[] { item.LabelId };
        Name = item.Name;
        IconUrl = item.IconUrl;
        PointCount = item.PointCount;
        ParentId = item.ParentId;
        CategoryName = categoryName ?? string.Empty;
        TintBrush = tint ?? DefaultTint.Tint;
        TintBackgroundBrush = tintBackground ?? DefaultTint.Background;
    }

    public MaskMapPointLabel ToModel()
    {
        return new MaskMapPointLabel
        {
            LabelId = Id,
            LabelIds = LabelIds,
            ParentId = ParentId,
            Name = Name,
            IconUrl = IconUrl,
            PointCount = PointCount
        };
    }

    public async Task LoadIconAsync(CancellationToken ct)
    {
        if (IconImage != null || string.IsNullOrEmpty(IconUrl))
        {
            return;
        }

        var img = await MapIconImageCache.GetAsync(IconUrl, ct);
        if (img == null)
        {
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() => { IconImage = img; }, DispatcherPriority.Background);
    }
}
