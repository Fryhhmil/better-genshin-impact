using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BetterGenshinImpact.GameTask.MapMask;
using BetterGenshinImpact.Model.MaskMap;
using BetterGenshinImpact.View.Controls.MapPointMenu;
using BetterGenshinImpact.ViewModel;

namespace BetterGenshinImpact.UnitTest.GameTaskTests.MapMaskTests;

/// <summary>
/// 点位菜单视图：在 STA 线程实际实例化 XAML 并布局，确认各模板（大类、类型卡片、方案、已开启栏、圆形按钮）可以生成。
/// 使用只含绑定属性的替身 DataContext，不构造需要游戏与网络服务的 MaskWindowViewModel。
/// </summary>
public sealed class MapPointMenuViewTests
{
    [Fact]
    public Task ViewInstantiatesAndLaysOut() => RunSta(() =>
    {
        // 视图引用的应用级转换器
        var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources["BooleanToVisibilityConverter"] = new BooleanToVisibilityConverter();
        app.Resources["BooleanToVisibilityRevertConverter"] = new BetterGenshinImpact.View.Converters.BooleanToVisibilityRevertConverter();

        var context = new FakeMenuContext();
        var view = new MapPointMenuView { DataContext = context };
        Layout(view, 1600, 900);
        Assert.True(view.ActualWidth > 0);

        var buttons = VisualChildren(view).OfType<Button>().ToList();
        // 2 个大类 + 3 张类型卡片 + 1 个方案 + 2 个已开启 + 2 个圆形按钮，以及各种操作按钮
        Assert.True(buttons.Count > 15, $"按钮数量 {buttons.Count}");
        Assert.Contains(VisualChildren(view).OfType<CheckBox>(), x => x.IsChecked == true);

        // 3 个类型（奇数）：最后一行右列为空，不能显示没有数据的空卡片
        var cardTemplate = view.Resources["MapLabelCardTemplate"];
        var cards = VisualChildren(view).OfType<ContentPresenter>().Where(x => ReferenceEquals(x.ContentTemplate, cardTemplate)).ToList();
        Assert.Equal(4, cards.Count);
        Assert.Equal(3, cards.Count(IsShown));
        Assert.DoesNotContain(cards, x => IsShown(x) && x.Content == null);

        // 切换到搜索、确认与新建方案状态后重新布局
        context.IsMapLabelSearchActive = true;
        context.IsMarkAllConfirmPending = true;
        context.IsCreatingMapProfile = true;
        context.IsMapPointPickerOpen = false;
        Layout(view, 1600, 900);
        return Task.CompletedTask;
    });

    /// <summary>
    /// 未挂到窗口时 IsVisible 恒为 false，这里沿可视树检查各级 Visibility
    /// </summary>
    private static bool IsShown(DependencyObject element)
    {
        for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { Visibility: not Visibility.Visible })
            {
                return false;
            }
        }

        return true;
    }

    private static void Layout(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }

    private static IEnumerable<DependencyObject> VisualChildren(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in VisualChildren(child)) yield return descendant;
        }
    }

    private static Task RunSta(Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(new Action(async () =>
            {
                try { await action(); completion.SetResult(); }
                catch (Exception ex) { completion.SetException(ex); }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
            }));
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    /// <summary>
    /// 只含视图绑定属性的替身
    /// </summary>
    private sealed class FakeMenuContext : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private bool _search;
        private bool _confirm;
        private bool _creating;
        private bool _open = true;

        public FakeMenuContext()
        {
            var category = new MaskMapPointLabel
            {
                LabelId = "c1",
                Name = "宝箱",
                Children =
                [
                    new MaskMapPointLabel { LabelId = "a", Name = "普通的宝箱", PointCount = 1520 },
                    new MaskMapPointLabel { LabelId = "b", Name = "一个名字非常非常长的点位类型用于测试换行显示", PointCount = 12 },
                    new MaskMapPointLabel { LabelId = "c", Name = "空类型", PointCount = 0 }
                ]
            };
            MapLabelCategories = [new MapLabelCategoryVm(category, 0), new MapLabelCategoryVm(new MaskMapPointLabel { LabelId = "c2", Name = "神瞳" }, 1)];
            MapLabelCategories[0].IsActive = true;
            MapLabelCategories[0].SelectedCount = 2;
            MapLabelItems = new ObservableCollection<MapLabelItemVm>(MapLabelCategories[0].Items);
            MapLabelItems[0].IsSelected = true;
            SelectedMapLabelItems = [MapLabelItems[0], MapLabelItems[1]];
            MapProfiles =
            [
                new MapProfileVm(new MapMaskProfileState { Id = "p1", Name = "宝箱", Items = [new MapMaskSelectedLabelState { Id = "a" }] })
                    { IsActive = true, IsDirty = true }
            ];
        }

        public double MapMenuScale => 1.0;
        public bool IsMapPointPickerOpen { get => _open; set => SetProperty(ref _open, value); }
        public bool IsMapLabelSearchActive { get => _search; set => SetProperty(ref _search, value); }
        public bool IsMarkAllConfirmPending { get => _confirm; set => SetProperty(ref _confirm, value); }
        public bool IsCreatingMapProfile { get => _creating; set => SetProperty(ref _creating, value); }
        public bool IsHoYoLabProviderSelected => true;
        public bool ShowHoYoLabAccountPanel => true;
        public bool ShowHoYoLabLoginButton => false;
        public bool HasHoYoLabPendingUploads => true;
        public bool IsHoYoLabSyncing => true;
        public bool AreMapPointsTemporarilyHidden => true;
        public bool HasSelectedMapLabelItems => true;
        public bool ShowSelectedMapLabelRail => true;
        public MaskWindowViewModel.MapPointApiProviderOption SelectedMapPointApiProviderOption => MapPointApiProviderOptions[1];
        public bool ShowMapMenuOrbBadge => true;
        public bool ShowMapProfileDirtyActions => true;
        public bool ShowMapProfileDeleteButton => false;
        public bool IsMapLabelGridLoading => false;
        public bool ShowMapLabelNoResults => false;
        public int SelectedMapLabelCount => 2;
        public string MapLabelSearchText => "宝";
        public string MapLabelGridTitle => "宝箱";
        public string MapLabelGridSubtitle => "3 个类型 · 1,532 个点位";
        public string MapPointVisibleCountText => "1,200";
        public string MapPointTotalCountText => "/ 1,532 可见";
        public string HoYoLabNicknameText => "Fryhh";
        public string HoYoLabAvatarText => "F";
        public string HoYoLabSyncStatusText => "同步中…";
        public string HoYoLabUploadButtonText => "上传 12 个本地标记";
        public string NewMapProfileName => "";
        public object Config { get; } = new { MapMaskConfig = new MapMaskConfig { HideMarkedPoints = true } };
        public ObservableCollection<MapLabelCategoryVm> MapLabelCategories { get; }
        public ObservableCollection<MapLabelItemVm> MapLabelItems { get; }
        public IReadOnlyList<MapLabelItemRow> MapLabelItemRows => MapLabelItemRow.Split(MapLabelItems);
        public ObservableCollection<MapLabelItemVm> SelectedMapLabelItems { get; }
        public ObservableCollection<MapProfileVm> MapProfiles { get; }
        public IReadOnlyList<MaskWindowViewModel.MapPointApiProviderOption> MapPointApiProviderOptions { get; } =
            [new(MapPointApiProvider.MihoyoMap, "米游社", "米游社大地图"), new(MapPointApiProvider.HoYoLab, "HoYoLAB", "HoYoLAB")];
        public IReadOnlyList<MaskWindowViewModel.MapLanguageOption> HoYoLabLanguageOptions { get; } = [new("pt-pt", "PT")];
    }
}
