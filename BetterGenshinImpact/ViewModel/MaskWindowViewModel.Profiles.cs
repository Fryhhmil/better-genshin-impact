using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask.MapMask;
using BetterGenshinImpact.Model.MaskMap;
using BetterGenshinImpact.Service.I18n;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterGenshinImpact.ViewModel
{
    /// <summary>
    /// 遮罩窗口：点位类型方案。方案是一组保存的类型，应用后只开启其中的类型。
    /// 方案保存在本机、按数据源分别存放（各数据源的类型不同），不同步到 HoYoLAB 账号。
    /// </summary>
    public partial class MaskWindowViewModel
    {
        /// <summary>
        /// 当前数据源的方案列表
        /// </summary>
        public ObservableCollection<MapProfileVm> MapProfiles { get; } = [];

        /// <summary>
        /// 是否正在输入新方案名称
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowMapProfileDeleteButton), nameof(NewMapProfileHeader))]
        private bool _isCreatingMapProfile;

        /// <summary>
        /// 新方案名称
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateMapProfileCommand))]
        private string _newMapProfileName = string.Empty;

        /// <summary>
        /// 当前应用的方案
        /// </summary>
        private MapProfileVm? ActiveMapProfile => MapProfiles.FirstOrDefault(x => x.IsActive);

        /// <summary>
        /// 当前方案已修改：显示“还原 / 保存到方案”
        /// </summary>
        public bool ShowMapProfileDirtyActions => ActiveMapProfile is { IsDirty: true };

        /// <summary>
        /// 当前方案未修改：显示删除按钮
        /// </summary>
        public bool ShowMapProfileDeleteButton => !IsCreatingMapProfile && ActiveMapProfile is { IsDirty: false };

        /// <summary>
        /// 删除按钮的提示文字
        /// </summary>
        public string MapProfileDeleteToolTip => string.Format(I18nService.Instance.Translate("删除方案 {0}"), ActiveMapProfile?.Name ?? string.Empty);

        /// <summary>
        /// 新建方案时的说明文字
        /// </summary>
        public string NewMapProfileHeader => string.Format(I18nService.Instance.Translate("新方案包含 {0} 个类型"), SelectedMapLabelItems.Count);

        /// <summary>
        /// 从存储读取当前数据源的方案
        /// </summary>
        private void LoadMapProfiles()
        {
            var state = MapMaskStateStorage.Read(GetMapMaskDataSourceKey());
            MapProfiles.Clear();
            foreach (var profile in state.Profiles)
            {
                MapProfiles.Add(new MapProfileVm(profile) { IsActive = profile.Id == state.ActiveProfileId });
            }

            RefreshMapProfileState();
        }

        /// <summary>
        /// 切换数据源时清空方案（加载新数据源的大类后重新读取）
        /// </summary>
        private void ClearMapProfiles()
        {
            MapProfiles.Clear();
            IsCreatingMapProfile = false;
            RefreshMapProfileState();
        }

        /// <summary>
        /// 按当前已开启的类型刷新方案的“已修改”状态与相关按钮
        /// </summary>
        private void RefreshMapProfileState()
        {
            var selectedIds = SelectedMapLabelItems.Select(x => x.Id).ToList();
            foreach (var profile in MapProfiles)
            {
                profile.IsDirty = profile.IsActive && !MapLabelSelection.SameSet(selectedIds, profile.ItemIds);
            }

            OnPropertyChanged(nameof(ShowMapProfileDirtyActions));
            OnPropertyChanged(nameof(ShowMapProfileDeleteButton));
            OnPropertyChanged(nameof(MapProfileDeleteToolTip));
            OnPropertyChanged(nameof(NewMapProfileHeader));
            OnPropertyChanged(nameof(MapMenuOrbToolTip));
        }

        /// <summary>
        /// 应用方案：只开启方案中的类型，关闭其他类型
        /// </summary>
        [RelayCommand]
        private async Task ApplyMapProfileAsync(MapProfileVm? profile)
        {
            if (profile == null)
            {
                return;
            }

            SetActiveMapProfile(profile.Id);
            PointInfoPopup.Close();
            await ReplaceSelectedMapLabelItemsAsync(profile.Items);
        }

        /// <summary>
        /// 还原：回到当前方案保存的类型
        /// </summary>
        [RelayCommand]
        private async Task RevertMapProfileAsync()
        {
            var profile = ActiveMapProfile;
            if (profile == null)
            {
                return;
            }

            PointInfoPopup.Close();
            await ReplaceSelectedMapLabelItemsAsync(profile.Items);
        }

        /// <summary>
        /// 把当前开启的类型保存到当前方案
        /// </summary>
        [RelayCommand]
        private void SaveActiveMapProfile()
        {
            var profile = ActiveMapProfile;
            if (profile == null)
            {
                return;
            }

            var items = CreateSelectedLabelStates();
            MapMaskStateStorage.Update(GetMapMaskDataSourceKey(), state =>
            {
                var saved = state.Profiles.FirstOrDefault(x => x.Id == profile.Id);
                if (saved != null)
                {
                    saved.Items = items;
                }
            });
            profile.Items = items;
            RefreshMapProfileState();
        }

        /// <summary>
        /// 删除当前方案（不改变已开启的类型）
        /// </summary>
        [RelayCommand]
        private void DeleteActiveMapProfile()
        {
            var profile = ActiveMapProfile;
            if (profile == null)
            {
                return;
            }

            MapMaskStateStorage.Update(GetMapMaskDataSourceKey(), state =>
            {
                state.Profiles.RemoveAll(x => x.Id == profile.Id);
                state.ActiveProfileId = null;
            });
            MapProfiles.Remove(profile);
            RefreshMapProfileState();
        }

        /// <summary>
        /// 开始新建方案：需要至少开启一个类型
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedMapLabelItems))]
        private void StartCreateMapProfile()
        {
            NewMapProfileName = string.Empty;
            IsCreatingMapProfile = true;
        }

        /// <summary>
        /// 取消新建方案
        /// </summary>
        [RelayCommand]
        private void CancelCreateMapProfile()
        {
            IsCreatingMapProfile = false;
        }

        /// <summary>
        /// 用当前开启的类型创建方案，并设为当前方案
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCreateMapProfile))]
        private void CreateMapProfile()
        {
            var name = NewMapProfileName.Trim();
            if (name.Length == 0)
            {
                return;
            }

            var state = new MapMaskProfileState
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Items = CreateSelectedLabelStates()
            };
            MapMaskStateStorage.Update(GetMapMaskDataSourceKey(), s =>
            {
                s.Profiles.Add(state);
                s.ActiveProfileId = state.Id;
            });

            MapProfiles.Add(new MapProfileVm(state));
            IsCreatingMapProfile = false;
            NewMapProfileName = string.Empty;
            SetActiveMapProfile(state.Id);
        }

        private bool CanCreateMapProfile() => !string.IsNullOrWhiteSpace(NewMapProfileName);

        /// <summary>
        /// 设置当前方案并保存
        /// </summary>
        private void SetActiveMapProfile(string? id)
        {
            foreach (var profile in MapProfiles)
            {
                profile.IsActive = profile.Id == id;
            }

            MapMaskStateStorage.Update(GetMapMaskDataSourceKey(), state => state.ActiveProfileId = id);
            RefreshMapProfileState();
        }

        /// <summary>
        /// 当前开启的类型转换为可保存的标签状态
        /// </summary>
        private List<MapMaskSelectedLabelState> CreateSelectedLabelStates()
        {
            return SelectedMapLabelItems
                .Select(x => new MapMaskSelectedLabelState
                {
                    Id = x.Id,
                    LabelIds = x.LabelIds.ToList(),
                    ParentId = x.ParentId,
                    Name = x.Name,
                    IconUrl = x.IconUrl,
                    PointCount = x.PointCount
                })
                .ToList();
        }

        /// <summary>
        /// 用给定的标签整体替换已开启的类型，然后保存并刷新地图点位
        /// </summary>
        private async Task ReplaceSelectedMapLabelItemsAsync(IEnumerable<MapMaskSelectedLabelState> items)
        {
            var added = new List<MapLabelItemVm>();
            // 批量替换，只在结束后刷新一次高亮、徽标与方案状态
            BatchUpdateSelectedMapLabelItems(selected =>
            {
                selected.Clear();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var saved in items)
                {
                    if (!ids.Add(saved.Id))
                    {
                        continue;
                    }

                    // 优先使用大类树中的实例，保留大类颜色与已加载的图标
                    var item = FindMapLabelItem(saved.Id)
                               ?? new MapLabelItemVm(new MaskMapPointLabel
                               {
                                   LabelId = saved.Id,
                                   LabelIds = saved.LabelIds ?? [],
                                   ParentId = saved.ParentId,
                                   Name = saved.Name,
                                   IconUrl = saved.IconUrl,
                                   PointCount = saved.PointCount
                               });
                    selected.Add(item);
                    added.Add(item);
                }
            });

            SaveSelectedMapLabelItemsToStorage();
            StartRefreshSelectedMapPoints();
            await Task.WhenAll(added.Select(x => EnsureIconLoadedAsync(x, CancellationToken.None)));
        }
    }
}
