using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Service.I18n;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.ViewModel
{
    /// <summary>
    /// 遮罩窗口：大地图点位菜单的界面状态（网格标题、批量开关、底栏统计与确认、右侧已开启栏、圆形按钮）。
    /// </summary>
    public partial class MaskWindowViewModel
    {
        /// <summary>
        /// 设计稿的参考分辨率，菜单按遮罩窗口相对它的比例缩放
        /// </summary>
        private const double MapMenuReferenceWidth = 1600;

        /// <summary>
        /// 设计稿的参考分辨率高度
        /// </summary>
        private const double MapMenuReferenceHeight = 900;

        /// <summary>
        /// 底栏是否正在确认“全部标记为已收集”
        /// </summary>
        [ObservableProperty] private bool _isMarkAllConfirmPending;

        /// <summary>
        /// 菜单整体缩放比例（相对 1600×900 的设计稿）
        /// </summary>
        public double MapMenuScale
        {
            get
            {
                if (MaskWindowWidth <= 0 || MaskWindowHeight <= 0)
                {
                    return 1.0;
                }

                return Math.Min(MaskWindowWidth / MapMenuReferenceWidth, MaskWindowHeight / MapMenuReferenceHeight);
            }
        }

        partial void OnMaskWindowWidthChanged(double value) => OnPropertyChanged(nameof(MapMenuScale));

        partial void OnMaskWindowHeightChanged(double value) => OnPropertyChanged(nameof(MapMenuScale));

        #region 网格

        /// <summary>
        /// 是否正在搜索（有搜索词）
        /// </summary>
        public bool IsMapLabelSearchActive => !string.IsNullOrWhiteSpace(MapLabelSearchText);

        /// <summary>
        /// 网格标题：当前大类名称，搜索时为“搜索结果”
        /// </summary>
        public string MapLabelGridTitle => IsMapLabelSearchActive
            ? string.Format(I18nService.Instance.Translate("“{0}”的搜索结果"), MapLabelSearchText.Trim())
            : SelectedMapLabelCategory?.Name ?? string.Empty;

        /// <summary>
        /// 网格副标题：类型数与点位数
        /// </summary>
        public string MapLabelGridSubtitle => IsMapLabelSearchActive
            ? string.Format(I18nService.Instance.Translate("全部大类中共 {0} 个类型"), MapLabelItems.Count)
            : string.Format(I18nService.Instance.Translate("{0} 个类型 · {1} 个点位"),
                MapLabelItems.Count, FormatCount(MapLabelItems.Sum(x => x.PointCount)));

        /// <summary>
        /// 网格是否显示加载占位（切换大类、数据源或语言时）
        /// </summary>
        public bool IsMapLabelGridLoading => IsMapLabelItemsLoading || IsMapLabelTreeLoading;

        /// <summary>
        /// 搜索没有结果时显示提示
        /// </summary>
        public bool ShowMapLabelNoResults => !IsMapLabelGridLoading && IsMapLabelSearchActive && MapLabelItems.Count == 0;

        /// <summary>
        /// 无结果提示文字
        /// </summary>
        public string MapLabelNoResultsText => string.Format(I18nService.Instance.Translate("没有找到与“{0}”匹配的点位类型。"), MapLabelSearchText.Trim());

        partial void OnIsMapLabelItemsLoadingChanged(bool value) => NotifyMapLabelGridHeaderChanged();

        partial void OnIsMapLabelTreeLoadingChanged(bool value) => NotifyMapLabelGridHeaderChanged();

        partial void OnSelectedMapLabelCategoryChanged(MapLabelCategoryVm? value)
        {
            RefreshMapLabelCategoryActiveState();
            NotifyMapLabelGridHeaderChanged();
        }

        partial void OnMapLabelCategoriesChanged(ObservableCollection<MapLabelCategoryVm> value)
        {
            MapLabelSelection.ApplyCategoryCounts(value, SelectedMapLabelItems);
            RefreshMapLabelCategoryActiveState();
        }

        /// <summary>
        /// 当前大类高亮；搜索时不高亮任何大类
        /// </summary>
        private void RefreshMapLabelCategoryActiveState()
        {
            foreach (var category in MapLabelCategories)
            {
                category.IsActive = !IsMapLabelSearchActive && ReferenceEquals(category, SelectedMapLabelCategory);
            }
        }

        /// <summary>
        /// 通知网格标题、副标题、加载与无结果状态
        /// </summary>
        private void NotifyMapLabelGridHeaderChanged()
        {
            OnPropertyChanged(nameof(IsMapLabelSearchActive));
            OnPropertyChanged(nameof(MapLabelGridTitle));
            OnPropertyChanged(nameof(MapLabelGridSubtitle));
            OnPropertyChanged(nameof(IsMapLabelGridLoading));
            OnPropertyChanged(nameof(ShowMapLabelNoResults));
            OnPropertyChanged(nameof(MapLabelNoResultsText));
            EnableAllMapLabelItemsCommand.NotifyCanExecuteChanged();
            DisableAllMapLabelItemsCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 选择点位数据源（分段按钮）。遮罩窗口无法获得焦点，ListBox 点击不会选中，因此用按钮命令切换
        /// </summary>
        [RelayCommand]
        private void SelectMapPointApiProviderOption(MapPointApiProviderOption? option)
        {
            if (option != null)
            {
                SelectedMapPointApiProviderOption = option;
            }
        }

        /// <summary>
        /// 清空搜索词
        /// </summary>
        [RelayCommand]
        private void ClearMapLabelSearch()
        {
            MapLabelSearchText = string.Empty;
        }

        /// <summary>
        /// 开启网格中当前显示的全部类型（跳过没有点位的类型）
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEnableAllMapLabelItems))]
        private async Task EnableAllMapLabelItemsAsync()
        {
            var selectedIds = SelectedMapLabelItems.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            var toAdd = MapLabelItems.Where(x => !x.IsEmpty && !selectedIds.Contains(x.Id)).ToList();
            if (toAdd.Count == 0)
            {
                return;
            }

            BatchUpdateSelectedMapLabelItems(selected =>
            {
                foreach (var item in toAdd)
                {
                    selected.Add(item);
                }
            });

            SaveSelectedMapLabelItemsToStorage();
            StartRefreshSelectedMapPoints();
            await Task.WhenAll(toAdd.Select(x => EnsureIconLoadedAsync(x, CancellationToken.None)));
        }

        private bool CanEnableAllMapLabelItems() => MapLabelItems.Any(x => !x.IsEmpty && !x.IsSelected);

        /// <summary>
        /// 关闭网格中当前显示的全部类型
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDisableAllMapLabelItems))]
        private void DisableAllMapLabelItems()
        {
            var gridIds = MapLabelItems.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            var toRemove = SelectedMapLabelItems.Where(x => gridIds.Contains(x.Id)).ToList();
            if (toRemove.Count == 0)
            {
                return;
            }

            BatchUpdateSelectedMapLabelItems(selected =>
            {
                foreach (var item in toRemove)
                {
                    selected.Remove(item);
                }
            });

            SaveSelectedMapLabelItemsToStorage();
            StartRefreshSelectedMapPoints();
        }

        private bool CanDisableAllMapLabelItems() => MapLabelItems.Any(x => x.IsSelected);

        /// <summary>
        /// 后台加载大类图标
        /// </summary>
        private async Task LoadCategoryIconAsync(MapLabelCategoryVm category)
        {
            try
            {
                await _iconLoadSemaphore.WaitAsync();
                try
                {
                    await category.LoadIconAsync(CancellationToken.None);
                }
                finally
                {
                    _iconLoadSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "加载地图大类图标失败");
            }
        }

        #endregion

        #region 已开启栏与圆形按钮

        /// <summary>
        /// 已开启的类型数量
        /// </summary>
        public int SelectedMapLabelCount => SelectedMapLabelItems.Count;

        /// <summary>
        /// 是否有已开启的类型（显示右侧已开启栏）
        /// </summary>
        public bool HasSelectedMapLabelItems => SelectedMapLabelItems.Count > 0;

        /// <summary>
        /// 右侧已开启栏只在点位面板打开且有已开启类型时显示
        /// </summary>
        public bool ShowSelectedMapLabelRail => IsMapPointPickerOpen && SelectedMapLabelItems.Count > 0;

        /// <summary>
        /// 已开启栏顶部的数量文字
        /// </summary>
        public string SelectedMapLabelCountText => string.Format(I18nService.Instance.Translate("{0} 个已开启"), SelectedMapLabelItems.Count);

        /// <summary>
        /// 菜单收起且有已开启类型时，地球按钮显示数量徽标
        /// </summary>
        public bool ShowMapMenuOrbBadge => !IsMapPointPickerOpen && SelectedMapLabelItems.Count > 0;

        /// <summary>
        /// 地球按钮的提示文字
        /// </summary>
        public string MapMenuOrbToolTip
        {
            get
            {
                if (IsMapPointPickerOpen)
                {
                    return I18nService.Instance.Translate("关闭点位菜单");
                }

                var profile = ActiveMapProfile;
                return profile == null
                    ? I18nService.Instance.Translate("打开点位菜单")
                    : string.Format(I18nService.Instance.Translate("打开点位菜单 · 方案 {0}"), profile.Name);
            }
        }

        partial void OnIsMapPointPickerOpenChanged(bool value)
        {
            // 关闭菜单时取消未完成的确认与新建方案
            if (!value)
            {
                IsMarkAllConfirmPending = false;
                IsCreatingMapProfile = false;
            }
            else
            {
                // 点位面板与层级菜单都在圆形按钮上方，同时只展开一个
                IsMapPointLayerMenuOpen = false;
                // 打开时刷新“x 分钟前同步”的相对时间
                OnPropertyChanged(nameof(HoYoLabSyncStatusText));
            }

            OnPropertyChanged(nameof(ShowMapMenuOrbBadge));
            OnPropertyChanged(nameof(ShowSelectedMapLabelRail));
            OnPropertyChanged(nameof(MapMenuOrbToolTip));
        }

        /// <summary>
        /// 已选列表变化后通知相关显示与命令状态
        /// </summary>
        private void NotifyMapMenuSelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedMapLabelCount));
            OnPropertyChanged(nameof(HasSelectedMapLabelItems));
            OnPropertyChanged(nameof(ShowSelectedMapLabelRail));
            OnPropertyChanged(nameof(SelectedMapLabelCountText));
            OnPropertyChanged(nameof(ShowMapMenuOrbBadge));
            OnPropertyChanged(nameof(MapMenuOrbToolTip));
            OnPropertyChanged(nameof(MapLabelGridSubtitle));
            EnableAllMapLabelItemsCommand.NotifyCanExecuteChanged();
            DisableAllMapLabelItemsCommand.NotifyCanExecuteChanged();
            StartCreateMapProfileCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region 底栏

        /// <summary>
        /// 可见点位数（格式化）
        /// </summary>
        public string MapPointVisibleCountText => FormatCount(VisibleMapPointCount);

        /// <summary>
        /// “/ 总数 可见”文字
        /// </summary>
        public string MapPointTotalCountText => string.Format(I18nService.Instance.Translate("/ {0} 可见"), FormatCount(MapPointTotalCount));

        /// <summary>
        /// 确认栏标题
        /// </summary>
        public string MarkAllConfirmTitle => string.Format(I18nService.Instance.Translate("将已加载的 {0} 个点位标记为已收集？"), FormatCount(MapPointTotalCount));

        /// <summary>
        /// 确认栏说明：账号模式下不可批量标记，因此这里只会保存到本机
        /// </summary>
        public string MarkAllConfirmSubtitle => I18nService.Instance.Translate("保存在本机。可通过“全部取消标记”撤销。");

        /// <summary>
        /// 账号模式下批量按钮禁用时的提示
        /// </summary>
        public string MarkAllToolTip => IsHoYoLabAccountMode
            ? I18nService.Instance.Translate("登录 HoYoLAB 时不支持批量标记，请逐个右键标记")
            : I18nService.Instance.Translate("把已开启类型的全部点位标记为已收集");

        /// <summary>
        /// 全部标记：需要有已加载的点位，且未登录 HoYoLAB（避免一次改动大量账号标记）
        /// </summary>
        private bool CanMarkAllMapPoints() => !IsHoYoLabAccountMode && MapPoints.Count > 0;

        /// <summary>
        /// 全部取消标记：需要已加载的点位中有已收集的，且未登录 HoYoLAB
        /// </summary>
        private bool CanUnmarkAllMapPoints() => !IsHoYoLabAccountMode && HasHiddenMapPoints;

        /// <summary>
        /// 进入“全部标记”确认状态
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanMarkAllMapPoints))]
        private void AskMarkAllMapPoints()
        {
            IsMarkAllConfirmPending = true;
        }

        /// <summary>
        /// 取消“全部标记”确认
        /// </summary>
        [RelayCommand]
        private void CancelMarkAllMapPoints()
        {
            IsMarkAllConfirmPending = false;
        }

        /// <summary>
        /// 点位或标记变化后通知底栏
        /// </summary>
        private void NotifyMapMenuFooterChanged()
        {
            OnPropertyChanged(nameof(VisibleMapPointCount));
            OnPropertyChanged(nameof(MapPointVisibleCountText));
            OnPropertyChanged(nameof(MapPointTotalCountText));
            OnPropertyChanged(nameof(MarkAllConfirmTitle));
            OnPropertyChanged(nameof(MarkAllToolTip));
            AskMarkAllMapPointsCommand.NotifyCanExecuteChanged();
            HideAllMapPointsCommand.NotifyCanExecuteChanged();
            ShowAllMapPointsCommand.NotifyCanExecuteChanged();
            if (MapPoints.Count == 0)
            {
                IsMarkAllConfirmPending = false;
            }
        }

        #endregion

        /// <summary>
        /// 按当前区域格式化数字（千位分隔）
        /// </summary>
        private static string FormatCount(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
    }
}
