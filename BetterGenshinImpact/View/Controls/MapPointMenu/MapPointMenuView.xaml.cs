using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BetterGenshinImpact.Service.I18n;
using BetterGenshinImpact.View.Windows;
using BetterGenshinImpact.ViewModel;

namespace BetterGenshinImpact.View.Controls.MapPointMenu;

/// <summary>
/// 大地图点位菜单视图。DataContext 为遮罩窗口的 <see cref="MaskWindowViewModel"/>。
/// 遮罩窗口无法获得键盘焦点，代码后置只负责在绘制的输入框上弹出真实输入窗口。
/// </summary>
public partial class MapPointMenuView : UserControl
{
    /// <summary>
    /// 覆盖在输入框上的文本输入窗口，搜索与方案名称共用
    /// </summary>
    private OverlayTextInputWindow? _inputWindow;

    /// <summary>
    /// 当前订阅的 ViewModel
    /// </summary>
    private MaskWindowViewModel? _viewModel;

    public MapPointMenuView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// 重新加载（卸载后再次加入可视树）时恢复订阅
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }
    }

    /// <summary>
    /// 订阅 ViewModel：点位面板关闭或开始/结束新建方案时隐藏输入窗口
    /// </summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        }

        _viewModel = e.NewValue as MaskWindowViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel == null || _inputWindow == null)
        {
            return;
        }

        var shouldHide = e.PropertyName switch
        {
            nameof(MaskWindowViewModel.IsMapPointPickerOpen) => !_viewModel.IsMapPointPickerOpen,
            nameof(MaskWindowViewModel.IsCreatingMapProfile) => true,
            _ => false
        };
        if (shouldHide)
        {
            _ = Dispatcher.InvokeAsync(() => _inputWindow?.Hide());
        }
    }

    /// <summary>
    /// 视图卸载时关闭输入窗口并取消订阅
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        }

        _inputWindow?.CloseWindow();
        _inputWindow = null;
    }

    /// <summary>
    /// 点击搜索框：弹出输入窗口编辑搜索词
    /// </summary>
    private void SearchBox_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        GetInputWindow().ShowOver((FrameworkElement)sender, _viewModel, nameof(MaskWindowViewModel.MapLabelSearchText),
            I18nService.Instance.Translate("搜索点位类型（例如：水晶）"));
        e.Handled = true;
    }

    /// <summary>
    /// 点击方案名称框：弹出输入窗口编辑名称，按 Enter 直接保存
    /// </summary>
    private void ProfileNameBox_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        var viewModel = _viewModel;
        GetInputWindow().ShowOver((FrameworkElement)sender, viewModel, nameof(MaskWindowViewModel.NewMapProfileName),
            I18nService.Instance.Translate("名称（例如：稻妻宝箱）"),
            () =>
            {
                if (viewModel.CreateMapProfileCommand.CanExecute(null))
                {
                    viewModel.CreateMapProfileCommand.Execute(null);
                }
            });
        e.Handled = true;
    }

    /// <summary>
    /// 首次使用时创建输入窗口
    /// </summary>
    private OverlayTextInputWindow GetInputWindow()
    {
        return _inputWindow ??= new OverlayTextInputWindow();
    }
}
