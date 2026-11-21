using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BetterGenshinImpact.GameTask.MapMask;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Model.MaskMap;
using BetterGenshinImpact.Service;
using BetterGenshinImpact.Service.HoYoLab;
using BetterGenshinImpact.Service.I18n;
using BetterGenshinImpact.View.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.ViewModel
{
    /// <summary>
    /// 遮罩窗口：HoYoLAB 账号标记同步。
    /// 数据源为 HoYoLAB 且已登录时，隐藏点位以账号上的“图钉”为准，右键标记会同步到账号；本地状态文件保持不变。
    /// </summary>
    public partial class MaskWindowViewModel
    {
        /// <summary>
        /// 提瓦特地图 ID，与点位列表请求一致
        /// </summary>
        private const int HoYoLabMapId = 2;

        /// <summary>
        /// 本地标记上传时每次请求的间隔，避免请求过快
        /// </summary>
        private static readonly TimeSpan UploadInterval = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// HoYoLAB 标记接口
        /// </summary>
        private readonly HoYoLabMarkApiService _hoYoLabMarkApi;

        /// <summary>
        /// HoYoLAB 登录状态
        /// </summary>
        private readonly HoYoLabAccountService _hoYoLabAccount;

        /// <summary>
        /// 账号标记缓存；尚未加载成功时暂用本地状态
        /// </summary>
        private readonly HoYoLabMarkTracker _markTracker = new();

        /// <summary>
        /// 是否正在上传本地标记
        /// </summary>
        private bool _isUploadingLocalMarks;

        /// <summary>
        /// 当前是否以 HoYoLAB 账号为标记来源（数据源为 HoYoLAB 且已登录）
        /// </summary>
        private bool IsHoYoLabAccountMode =>
            Config.MapMaskConfig.MapPointApiProvider == MapPointApiProvider.HoYoLab && _hoYoLabAccount.IsLoggedIn;

        /// <summary>
        /// 是否已登录 HoYoLAB
        /// </summary>
        public bool IsHoYoLabLoggedIn => _hoYoLabAccount.IsLoggedIn;

        /// <summary>
        /// 是否显示“登录 HoYoLAB”按钮
        /// </summary>
        public bool ShowHoYoLabLoginButton => IsHoYoLabProviderSelected && !_hoYoLabAccount.IsLoggedIn;

        /// <summary>
        /// 是否显示账号信息与操作按钮
        /// </summary>
        public bool ShowHoYoLabAccountPanel => IsHoYoLabProviderSelected && _hoYoLabAccount.IsLoggedIn;

        /// <summary>
        /// 账号显示文本，如“已登录：昵称”
        /// </summary>
        public string HoYoLabAccountText => string.IsNullOrWhiteSpace(_hoYoLabAccount.Nickname)
            ? I18nService.Instance.Translate("已登录")
            : string.Format(I18nService.Instance.Translate("已登录：{0}"), _hoYoLabAccount.Nickname);

        /// <summary>
        /// 账号名称（没有昵称时显示“HoYoLAB”）
        /// </summary>
        public string HoYoLabNicknameText => string.IsNullOrWhiteSpace(_hoYoLabAccount.Nickname)
            ? "HoYoLAB"
            : _hoYoLabAccount.Nickname!;

        /// <summary>
        /// 头像中显示的首字母
        /// </summary>
        public string HoYoLabAvatarText => HoYoLabNicknameText.Trim() is { Length: > 0 } name
            ? char.ToUpperInvariant(name[0]).ToString()
            : "H";

        /// <summary>
        /// 是否正在从账号同步标记
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HoYoLabSyncStatusText))]
        private bool _isHoYoLabSyncing;

        /// <summary>
        /// 上次同步成功的时间；未同步过时为空
        /// </summary>
        private DateTime? _hoYoLabLastSyncedAt;

        /// <summary>
        /// 同步状态文字：同步中 / x 分钟前已同步
        /// </summary>
        public string HoYoLabSyncStatusText
        {
            get
            {
                if (IsHoYoLabSyncing)
                {
                    return I18nService.Instance.Translate("同步中…");
                }

                if (_hoYoLabLastSyncedAt is not { } at)
                {
                    return I18nService.Instance.Translate("尚未同步");
                }

                var elapsed = DateTime.Now - at;
                if (elapsed.TotalMinutes < 1)
                {
                    return I18nService.Instance.Translate("刚刚已同步");
                }

                return elapsed.TotalHours < 1
                    ? string.Format(I18nService.Instance.Translate("{0} 分钟前已同步"), (int)elapsed.TotalMinutes)
                    : string.Format(I18nService.Instance.Translate("{0} 小时前已同步"), (int)elapsed.TotalHours);
            }
        }

        /// <summary>
        /// 只保存在本机、尚未上传到账号的标记数量
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasHoYoLabPendingUploads), nameof(HoYoLabUploadButtonText))]
        private int _hoYoLabPendingUploadCount;

        /// <summary>
        /// 是否有待上传的本地标记（显示上传按钮）
        /// </summary>
        public bool HasHoYoLabPendingUploads => HoYoLabPendingUploadCount > 0;

        /// <summary>
        /// 上传按钮文字，包含数量
        /// </summary>
        public string HoYoLabUploadButtonText => string.Format(I18nService.Instance.Translate("上传 {0} 个本地标记"), HoYoLabPendingUploadCount);

        /// <summary>
        /// 计算只保存在本机、账号上还没有的标记点位 ID
        /// </summary>
        /// <param name="marked">账号上已有的标记</param>
        private static IReadOnlyList<long> GetPendingLocalMarkIds(IReadOnlySet<long> marked)
        {
            // 所有语言的本地文件都可能有标记，点位 ID 在各语言下相同
            var localIds = new List<long>();
            foreach (var lang in new[] { MapMaskConfig.HoYoLabLanguageEnUs, MapMaskConfig.HoYoLabLanguagePtPt, MapMaskConfig.HoYoLabLanguageEsEs })
            {
                var keys = MapMaskStateStorage.Read($"{MapPointApiProvider.HoYoLab}_{lang}").HiddenMapPointKeys;
                localIds.AddRange(MapMarkSync.ParseLocalPointIds(keys));
            }

            return MapMarkSync.PendingUpload(localIds, marked);
        }

        /// <summary>
        /// 账号标记加载后重新统计待上传数量
        /// </summary>
        private void RefreshHoYoLabPendingUploadCount()
        {
            var marked = _markTracker.MarkedIds;
            HoYoLabPendingUploadCount = IsHoYoLabAccountMode && marked != null
                ? GetPendingLocalMarkIds(marked).Count
                : 0;
        }


        /// <summary>
        /// 订阅登录状态变化，已登录时加载账号标记
        /// </summary>
        private void InitHoYoLabAccount()
        {
            _hoYoLabAccount.SessionChanged += (_, _) => UIDispatcherHelper.Invoke(OnHoYoLabSessionChanged);
            if (IsHoYoLabAccountMode)
            {
                _ = RefreshHoYoLabMarksAsync();
            }
        }

        /// <summary>
        /// 登录、退出或会话过期：清空缓存并按新状态重新应用隐藏点位
        /// </summary>
        private void OnHoYoLabSessionChanged()
        {
            _markTracker.Reset();
            NotifyHoYoLabAccountPropertiesChanged();
            if (IsHoYoLabAccountMode)
            {
                _ = RefreshHoYoLabMarksAsync();
            }
            else
            {
                ReapplyHiddenMapPoints();
            }
        }

        /// <summary>
        /// 数据源切换后刷新账号相关界面，切到 HoYoLAB 时加载账号标记
        /// </summary>
        private void OnHoYoLabProviderChanged()
        {
            NotifyHoYoLabAccountPropertiesChanged();
            if (IsHoYoLabAccountMode && _markTracker.NeedsLoad)
            {
                _ = RefreshHoYoLabMarksAsync();
            }
        }

        /// <summary>
        /// 通知账号相关的绑定属性与命令状态
        /// </summary>
        private void NotifyHoYoLabAccountPropertiesChanged()
        {
            OnPropertyChanged(nameof(IsHoYoLabLoggedIn));
            OnPropertyChanged(nameof(ShowHoYoLabLoginButton));
            OnPropertyChanged(nameof(ShowHoYoLabAccountPanel));
            OnPropertyChanged(nameof(HoYoLabAccountText));
            OnPropertyChanged(nameof(HoYoLabNicknameText));
            OnPropertyChanged(nameof(HoYoLabAvatarText));
            OnPropertyChanged(nameof(HoYoLabSyncStatusText));
            RefreshHoYoLabPendingUploadCount();
            // 账号模式下禁用批量标记
            NotifyMapMenuFooterChanged();
        }

        /// <summary>
        /// 账号模式且账号标记已加载时，用账号标记填充隐藏集合
        /// </summary>
        /// <returns>是否已用账号标记填充；false 时调用方继续读取本地状态</returns>
        private bool TryLoadAccountHiddenKeys()
        {
            if (!IsHoYoLabAccountMode)
            {
                return false;
            }

            var marked = _markTracker.MarkedIds;
            if (marked == null)
            {
                // 首次加载失败或尚未加载：每次重新载入点位时重试
                if (_markTracker.NeedsLoad)
                {
                    _ = RefreshHoYoLabMarksAsync();
                }

                return false;
            }

            var lang = HoYoLabMapApiService.NormalizeLanguage(Config.MapMaskConfig.HoYoLabLanguage);
            foreach (var id in marked)
            {
                _hiddenMapPointIds.Add(MapMarkSync.ToKey(lang, id));
            }

            return true;
        }

        /// <summary>
        /// 重新读取隐藏集合并刷新点位显示
        /// </summary>
        private void ReapplyHiddenMapPoints()
        {
            LoadHiddenMapPointKeysFromStorage();
            ApplyMapPointHiddenStates();
            SyncPointInfoPopupHiddenState();
        }

        /// <summary>
        /// 打开登录窗口
        /// </summary>
        [RelayCommand]
        private void LoginHoYoLab()
        {
            var lang = HoYoLabMapApiService.NormalizeLanguage(Config.MapMaskConfig.HoYoLabLanguage);
            HoYoLabLoginWindow.ShowLogin(null, lang);
        }

        /// <summary>
        /// 退出登录，恢复使用本地标记
        /// </summary>
        [RelayCommand]
        private void LogoutHoYoLab()
        {
            _hoYoLabAccount.Logout();
            _logger.LogInformation("已退出 HoYoLAB 登录，改用本地标记");
        }

        /// <summary>
        /// 从账号重新加载标记（例如在手机或网页上标记后）
        /// </summary>
        [RelayCommand]
        private async Task RefreshHoYoLabMarksAsync()
        {
            var cookie = _hoYoLabAccount.CookieHeader;
            if (!IsHoYoLabAccountMode || cookie == null)
            {
                return;
            }

            var version = _markTracker.BeginLoad();
            var lang = HoYoLabMapApiService.NormalizeLanguage(Config.MapMaskConfig.HoYoLabLanguage);
            IsHoYoLabSyncing = true;
            try
            {
                var ids = await _hoYoLabMarkApi.GetMarkedPointIdsAsync(cookie, HoYoLabMapId, lang, CancellationToken.None);
                // 加载期间发生过右键切换、登录状态或数据源变化时丢弃结果
                if (!_markTracker.CompleteLoad(version, ids) || !IsHoYoLabAccountMode)
                {
                    return;
                }

                _hoYoLabLastSyncedAt = DateTime.Now;
                ReapplyHiddenMapPoints();
                RefreshHoYoLabPendingUploadCount();
                _logger.LogInformation("已同步 HoYoLAB 账号标记：{Count} 个", ids.Count);
            }
            catch (HoYoLabSessionExpiredException)
            {
                HandleHoYoLabSessionExpired();
            }
            catch (Exception ex)
            {
                _markTracker.FailLoad(version);
                _logger.LogWarning("读取 HoYoLAB 账号标记失败：{Message}", ex.Message);
            }
            finally
            {
                IsHoYoLabSyncing = false;
            }
        }

        /// <summary>
        /// 账号模式下的右键切换：同一点位请求未完成时忽略；界面先更新，再同步到 HoYoLAB，失败时恢复
        /// </summary>
        /// <param name="point">切换的点位</param>
        private void ToggleAccountMark(MaskMapPoint point)
        {
            if (!long.TryParse(point.Id, out var pointId))
            {
                _logger.LogDebug("HoYoLAB 点位 ID 无法解析：{Id}", point.Id);
                return;
            }

            var key = GetMapPointKey(point);
            var hidden = !_hiddenMapPointIds.Contains(key);
            if (!_markTracker.TryBeginToggle(pointId, hidden))
            {
                return;
            }

            SetHiddenKey(key, hidden);
            _ = SyncToggleToAccountAsync(key, pointId, hidden);
        }

        /// <summary>
        /// 把切换结果发送到账号；失败时把点位恢复为切换前的状态
        /// </summary>
        /// <param name="key">点位键</param>
        /// <param name="pointId">点位 ID</param>
        /// <param name="hidden">切换后的状态，true 表示已标记</param>
        private async Task SyncToggleToAccountAsync(string key, long pointId, bool hidden)
        {
            var cookie = _hoYoLabAccount.CookieHeader;
            var lang = HoYoLabMapApiService.NormalizeLanguage(Config.MapMaskConfig.HoYoLabLanguage);
            var success = false;
            try
            {
                if (cookie == null)
                {
                    return;
                }

                if (hidden)
                {
                    await _hoYoLabMarkApi.AddMarkAsync(cookie, HoYoLabMapId, pointId, lang, CancellationToken.None);
                }
                else
                {
                    await _hoYoLabMarkApi.DeleteMarkAsync(cookie, HoYoLabMapId, pointId, lang, CancellationToken.None);
                }

                success = true;
            }
            catch (HoYoLabSessionExpiredException)
            {
                HandleHoYoLabSessionExpired();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("HoYoLAB 标记同步失败：{Message}", ex.Message);
            }
            finally
            {
                _markTracker.EndToggle(pointId, hidden, success);
                // 同一点位的请求是串行的，失败时直接恢复为切换前的状态
                if (!success && IsHoYoLabAccountMode)
                {
                    SetHiddenKey(key, !hidden);
                }
            }
        }

        /// <summary>
        /// 设置单个点位键的隐藏状态并刷新显示
        /// </summary>
        private void SetHiddenKey(string key, bool hidden)
        {
            if (hidden)
            {
                _hiddenMapPointIds.Add(key);
            }
            else
            {
                _hiddenMapPointIds.Remove(key);
            }

            ApplyMapPointHiddenStates();
            SyncPointInfoPopupHiddenState();
        }

        /// <summary>
        /// 会话过期：退出登录并提示，之后使用本地标记
        /// </summary>
        private void HandleHoYoLabSessionExpired()
        {
            _logger.LogWarning("HoYoLAB 登录已过期，已切换为本地标记，请重新登录");
            _hoYoLabAccount.Logout();
        }

        /// <summary>
        /// 把以前只保存在本地的 HoYoLAB 标记上传到账号
        /// </summary>
        [RelayCommand]
        private async Task UploadLocalMarksAsync()
        {
            var cookie = _hoYoLabAccount.CookieHeader;
            if (_isUploadingLocalMarks || !IsHoYoLabAccountMode || cookie == null)
            {
                return;
            }

            var marked = _markTracker.MarkedIds;
            if (marked == null)
            {
                _logger.LogWarning("HoYoLAB 账号标记尚未加载，请先点击刷新");
                return;
            }

            var pending = GetPendingLocalMarkIds(marked);
            if (pending.Count == 0)
            {
                _logger.LogInformation("没有需要上传的本地标记");
                return;
            }

            var confirm = ThemedMessageBox.Question(
                string.Format(I18nService.Instance.Translate("将上传 {0} 个本地标记到 HoYoLAB 账号，是否继续？"), pending.Count),
                I18nService.Instance.Translate("上传本地标记"), MessageBoxButton.YesNo, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _isUploadingLocalMarks = true;
            var uploadLang = HoYoLabMapApiService.NormalizeLanguage(Config.MapMaskConfig.HoYoLabLanguage);
            int uploaded = 0, failed = 0;
            try
            {
                foreach (var id in pending)
                {
                    try
                    {
                        await _hoYoLabMarkApi.AddMarkAsync(cookie, HoYoLabMapId, id, uploadLang, CancellationToken.None);
                        uploaded++;
                    }
                    catch (HoYoLabSessionExpiredException)
                    {
                        HandleHoYoLabSessionExpired();
                        return;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogDebug("上传本地标记 {Id} 失败：{Message}", id, ex.Message);
                    }

                    await Task.Delay(UploadInterval);
                }
            }
            finally
            {
                _isUploadingLocalMarks = false;
                _logger.LogInformation("本地标记上传完成：成功 {Uploaded} 个，失败 {Failed} 个", uploaded, failed);
            }

            await RefreshHoYoLabMarksAsync();
        }
    }
}
