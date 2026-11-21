using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using BetterGenshinImpact.Model.MaskMap;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.ViewModel;

/// <summary>
/// 地图标点大类（点位菜单左侧列表的一项）
/// </summary>
public partial class MapLabelCategoryVm : ObservableObject
{
    public string Id { get; }
    public string Name { get; }
    public string IconUrl { get; }
    public ObservableCollection<MapLabelItemVm> Items { get; }

    /// <summary>
    /// 大类强调色，用于图标与类型卡片
    /// </summary>
    public Brush TintBrush { get; }

    /// <summary>
    /// 大类图标底色（强调色约 14% 透明度）
    /// </summary>
    public Brush TintBackgroundBrush { get; }

    /// <summary>
    /// 大类图标
    /// </summary>
    [ObservableProperty] private ImageSource? _iconImage;

    /// <summary>
    /// 是否为当前显示的大类（搜索时没有当前大类）
    /// </summary>
    [ObservableProperty] private bool _isActive;

    /// <summary>
    /// 该大类下已开启的类型数量
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedItems))]
    private int _selectedCount;

    /// <summary>
    /// 是否有已开启的类型（显示蓝色徽标）
    /// </summary>
    public bool HasSelectedItems => SelectedCount > 0;

    /// <param name="category">大类数据</param>
    /// <param name="index">大类序号，用于取强调色</param>
    public MapLabelCategoryVm(MaskMapPointLabel category, int index = 0)
    {
        Id = category.LabelId;
        Name = category.Name;
        IconUrl = category.IconUrl;
        (TintBrush, TintBackgroundBrush) = MapLabelCategoryPalette.Get(index);
        Items = new ObservableCollection<MapLabelItemVm>((category.Children ?? Array.Empty<MaskMapPointLabel>())
            .Select(x => new MapLabelItemVm(x, Name, TintBrush, TintBackgroundBrush)));
    }

    /// <summary>
    /// 加载大类图标；失败时保持无图标
    /// </summary>
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
