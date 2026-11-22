using System.Collections.Generic;
using System.Linq;
using BetterGenshinImpact.GameTask.MapMask;
using BetterGenshinImpact.Service.I18n;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.ViewModel;

/// <summary>
/// 点位菜单中的一个方案（保存的类型组合）
/// </summary>
public partial class MapProfileVm : ObservableObject
{
    /// <summary>
    /// 方案标识
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 方案名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 方案包含的类型
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Count), nameof(ToolTip))]
    private IReadOnlyList<MapMaskSelectedLabelState> _items;

    /// <summary>
    /// 是否为当前应用的方案
    /// </summary>
    [ObservableProperty] private bool _isActive;

    /// <summary>
    /// 当前开启的类型与方案保存的不同（只对当前方案有意义）
    /// </summary>
    [ObservableProperty] private bool _isDirty;

    /// <summary>
    /// 方案包含的类型数量
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// 悬停提示：说明应用方案的效果
    /// </summary>
    public string ToolTip => string.Format(I18nService.Instance.Translate("只开启此方案的 {0} 个类型，关闭其他类型"), Count);

    /// <summary>
    /// 方案包含的类型标识
    /// </summary>
    public IEnumerable<string> ItemIds => Items.Select(x => x.Id);

    public MapProfileVm(MapMaskProfileState state)
    {
        Id = state.Id;
        Name = state.Name;
        _items = state.Items.ToList();
    }
}
