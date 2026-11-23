using System.ComponentModel;
using BetterGenshinImpact.GameTask.MapMask;
using BetterGenshinImpact.Model.MaskMap;
using BetterGenshinImpact.Service.I18n;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterGenshinImpact.ViewModel
{
    /// <summary>
    /// 遮罩窗口：临时隐藏全部标点（只影响显示，不修改任何标记），方便查看地图后再恢复
    /// </summary>
    public partial class MaskWindowViewModel
    {
        /// <summary>
        /// 是否临时隐藏全部标点；不保存，重启后恢复显示
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowBigMapPoints), nameof(ShowMiniMapPoints))]
        private bool _areMapPointsTemporarilyHidden;

        /// <summary>
        /// 大地图标点层是否显示
        /// </summary>
        public bool ShowBigMapPoints => IsInBigMapUi && !AreMapPointsTemporarilyHidden;

        /// <summary>
        /// 小地图标点层是否显示
        /// </summary>
        public bool ShowMiniMapPoints => Config.MapMaskConfig.MiniMapMaskEnabled && !AreMapPointsTemporarilyHidden;

        /// <summary>
        /// 订阅影响标点层显示的配置变化
        /// </summary>
        private void InitMapPointsVisibility()
        {
            Config.MapMaskConfig.PropertyChanged += OnMapMaskConfigPropertyChangedForVisibility;
        }

        /// <summary>
        /// 小地图遮罩开关变化时刷新小地图标点层
        /// </summary>
        private void OnMapMaskConfigPropertyChangedForVisibility(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MapMaskConfig.MiniMapMaskEnabled))
            {
                OnPropertyChanged(nameof(ShowMiniMapPoints));
            }
            else if (e.PropertyName == nameof(MapMaskConfig.HideMarkedPoints))
            {
                // “隐藏已收集”影响底栏的可见数量
                NotifyMapMenuFooterChanged();
            }
            else if (e.PropertyName == nameof(MapMaskConfig.LayerFilter))
            {
                // 层级过滤影响底栏的可见数量与层级按钮状态
                NotifyMapMenuFooterChanged();
                OnPropertyChanged(nameof(IsMapPointLayerFilterActive));
                OnPropertyChanged(nameof(MapPointLayerFilterToolTip));
                OnPropertyChanged(nameof(IsLayerFilterAll));
                OnPropertyChanged(nameof(IsLayerFilterSurfaceOnly));
                OnPropertyChanged(nameof(IsLayerFilterOtherLevelsOnly));
            }
        }

        /// <summary>
        /// 层级菜单：当前为“全部标点”
        /// </summary>
        public bool IsLayerFilterAll => Config.MapMaskConfig.LayerFilter == MapPointLayerFilter.All;

        /// <summary>
        /// 层级菜单：当前为“只显示地面”
        /// </summary>
        public bool IsLayerFilterSurfaceOnly => Config.MapMaskConfig.LayerFilter == MapPointLayerFilter.SurfaceOnly;

        /// <summary>
        /// 层级菜单：当前为“只显示其他层”
        /// </summary>
        public bool IsLayerFilterOtherLevelsOnly => Config.MapMaskConfig.LayerFilter == MapPointLayerFilter.OtherLevelsOnly;

        /// <summary>
        /// 层级菜单是否展开（不保存）
        /// </summary>
        [ObservableProperty]
        private bool _isMapPointLayerMenuOpen;

        /// <summary>
        /// 是否启用了层级过滤（非“全部”），启用时层级按钮高亮
        /// </summary>
        public bool IsMapPointLayerFilterActive => Config.MapMaskConfig.LayerFilter != MapPointLayerFilter.All;

        /// <summary>
        /// 层级按钮的提示文字
        /// </summary>
        public string MapPointLayerFilterToolTip => Config.MapMaskConfig.LayerFilter switch
        {
            MapPointLayerFilter.SurfaceOnly => I18nService.Instance.Translate("按层级筛选：只显示地面"),
            MapPointLayerFilter.OtherLevelsOnly => I18nService.Instance.Translate("按层级筛选：只显示其他层"),
            _ => I18nService.Instance.Translate("按层级筛选：全部标点")
        };

        /// <summary>
        /// 切换层级菜单；展开时收起点位面板（两者都在圆形按钮上方，同时只展开一个）
        /// </summary>
        [RelayCommand]
        private void ToggleMapPointLayerMenu()
        {
            IsMapPointLayerMenuOpen = !IsMapPointLayerMenuOpen;
            if (IsMapPointLayerMenuOpen)
            {
                IsMapPointPickerOpen = false;
            }
        }

        /// <summary>
        /// 选择层级过滤并收起菜单；选择后关闭点位详情弹窗，避免详情对应的点位已被过滤
        /// </summary>
        /// <param name="filter">层级过滤</param>
        [RelayCommand]
        private void SetMapPointLayerFilter(MapPointLayerFilter filter)
        {
            Config.MapMaskConfig.LayerFilter = filter;
            IsMapPointLayerMenuOpen = false;
            PointInfoPopup.Close();
        }

        /// <summary>
        /// 切换临时隐藏全部标点；隐藏时关闭点位详情弹窗
        /// </summary>
        [RelayCommand]
        private void ToggleMapPointsTemporarilyHidden()
        {
            AreMapPointsTemporarilyHidden = !AreMapPointsTemporarilyHidden;
            if (AreMapPointsTemporarilyHidden)
            {
                PointInfoPopup.Close();
            }
        }

        /// <summary>
        /// 眼睛按钮的提示文字
        /// </summary>
        public string MapPointsEyeToolTip => AreMapPointsTemporarilyHidden
            ? I18nService.Instance.Translate("重新显示标点")
            : I18nService.Instance.Translate("临时隐藏全部标点");

        partial void OnAreMapPointsTemporarilyHiddenChanged(bool value)
        {
            OnPropertyChanged(nameof(MapPointsEyeToolTip));
        }
    }
}
