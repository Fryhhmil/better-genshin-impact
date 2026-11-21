using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace BetterGenshinImpact.View.Windows;

/// <summary>
/// 遮罩窗口的文本输入窗口。遮罩窗口无法获得键盘焦点，因此用一个可激活的小窗口覆盖在遮罩中绘制的输入框上，
/// 文本双向绑定到指定的 ViewModel 属性；失去焦点、按 Esc 或 Enter 时隐藏。
/// </summary>
public partial class OverlayTextInputWindow : Window
{
    /// <summary>
    /// 为 true 时允许真正关闭窗口；平时关闭请求只隐藏，便于复用
    /// </summary>
    private bool _allowClose;

    /// <summary>
    /// 按 Enter 时执行的回调（例如保存新方案）
    /// </summary>
    private Action? _submitted;

    public OverlayTextInputWindow()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
        Deactivated += OnDeactivated;
    }

    /// <summary>
    /// 覆盖在目标元素上显示，并把输入框绑定到 ViewModel 的指定属性
    /// </summary>
    /// <param name="target">遮罩中绘制的输入框</param>
    /// <param name="dataContext">绑定源</param>
    /// <param name="propertyPath">绑定的属性路径</param>
    /// <param name="placeholder">占位提示</param>
    /// <param name="submitted">按 Enter 时执行的回调</param>
    public void ShowOver(FrameworkElement target, object dataContext, string propertyPath, string placeholder, Action? submitted = null)
    {
        DataContext = dataContext;
        _submitted = submitted;
        PlaceholderText.Text = placeholder;
        InputTextBox.SetBinding(TextBox.TextProperty, new Binding(propertyPath)
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        UpdatePlaceholder();

        // 屏幕坐标为物理像素，按本窗口自身的 DPI 换算为 DIP
        var handle = new WindowInteropHelper(this).EnsureHandle();
        var fromDevice = HwndSource.FromHwnd(handle)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = fromDevice.Transform(target.PointToScreen(new Point(0, 0)));
        var bottomRight = fromDevice.Transform(target.PointToScreen(new Point(target.ActualWidth, target.ActualHeight)));
        Left = topLeft.X;
        Top = topLeft.Y;
        Width = Math.Max(80, bottomRight.X - topLeft.X);
        Height = Math.Max(24, bottomRight.Y - topLeft.Y);

        // 目标框所在的菜单可能被缩放，按实际显示高度与布局高度之比放大文字，使其与菜单中的文字一致
        var scale = target.ActualHeight > 0 ? Height / target.ActualHeight : 1.0;
        InputTextBox.FontSize = 13 * scale;
        PlaceholderText.FontSize = 13 * scale;
        ((FrameworkElement)InputTextBox.Parent).Margin = new Thickness(10 * scale, 0, 10 * scale, 0);

        if (!IsVisible)
        {
            Show();
        }

        Topmost = true;
        Activate();
        InputTextBox.Focus();
        InputTextBox.CaretIndex = InputTextBox.Text?.Length ?? 0;
    }

    /// <summary>
    /// 真正关闭窗口（所属视图卸载时调用）
    /// </summary>
    public void CloseWindow()
    {
        _allowClose = true;
        Close();
    }

    /// <summary>
    /// 隐藏窗口并解除绑定，避免隐藏期间继续写入属性
    /// </summary>
    private void HideAndDetach()
    {
        Hide();
        BindingOperations.ClearBinding(InputTextBox, TextBox.TextProperty);
        _submitted = null;
    }

    /// <summary>
    /// 点击其他位置（窗口失活）时隐藏，避免残留在屏幕上
    /// </summary>
    private void OnDeactivated(object? sender, EventArgs e)
    {
        HideAndDetach();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideAndDetach();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            var submitted = _submitted;
            HideAndDetach();
            submitted?.Invoke();
            e.Handled = true;
        }
    }

    private void InputTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder();
    }

    /// <summary>
    /// 输入为空时显示占位提示
    /// </summary>
    private void UpdatePlaceholder()
    {
        PlaceholderText.Visibility = string.IsNullOrEmpty(InputTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            HideAndDetach();
            return;
        }

        base.OnClosing(e);
    }
}
