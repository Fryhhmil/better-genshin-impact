using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;

namespace BetterGenshinImpact.Core.Input.Backends.Gamepad;

/// <summary>
/// 通过 ViGEmBus 驱动创建的虚拟 DualShock 4 手柄。
/// 保持 PlayStation 按键图标，与玩家使用的 DualSense 一致；游戏允许它与实体手柄同时使用。
/// </summary>
public sealed class ViGEmDualShock4Gamepad : IVirtualGamepad
{
    /// <summary>
    /// 驱动客户端
    /// </summary>
    private readonly ViGEmClient _client;

    /// <summary>
    /// 虚拟手柄
    /// </summary>
    private readonly IDualShock4Controller _controller;

    /// <summary>
    /// 串行化按键，避免多个线程同时修改手柄状态
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _disposed;

    private ViGEmDualShock4Gamepad(ViGEmClient client, IDualShock4Controller controller)
    {
        _client = client;
        _controller = controller;
    }

    /// <summary>
    /// 创建并立即连接虚拟手柄。连接后游戏需要一点时间识别新手柄，提前连接可避免第一次按键丢失
    /// </summary>
    /// <param name="pad">创建成功的手柄</param>
    /// <param name="error">失败原因（通常是未安装 ViGEmBus 驱动）</param>
    /// <returns>是否创建成功</returns>
    public static bool TryCreate([NotNullWhen(true)] out ViGEmDualShock4Gamepad? pad, out string? error)
    {
        ViGEmClient? client = null;
        try
        {
            client = new ViGEmClient();
            var controller = client.CreateDualShock4Controller();
            controller.Connect();
            pad = new ViGEmDualShock4Gamepad(client, controller);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            client?.Dispose();
            pad = null;
            error = $"{e.GetType().Name}: {e.Message}";
            return false;
        }
    }

    public void Press(GamepadButton button, int holdMs)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            SetState(button, true);
            Thread.Sleep(holdMs);
            SetState(button, false);
        }
    }

    public void ReleaseAll()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _controller.ResetReport();
            _controller.SubmitReport();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _controller.Disconnect();
            }
            catch
            {
                // 驱动已卸载或设备已移除时断开会失败，忽略即可
            }

            _client.Dispose();
        }
    }

    /// <summary>
    /// 设置单个按钮的按下状态（方向键松开时回到中位）
    /// </summary>
    private void SetState(GamepadButton button, bool down)
    {
        switch (button)
        {
            case GamepadButton.Cross:
                _controller.SetButtonState(DualShock4Button.Cross, down);
                break;
            case GamepadButton.Circle:
                _controller.SetButtonState(DualShock4Button.Circle, down);
                break;
            case GamepadButton.Square:
                _controller.SetButtonState(DualShock4Button.Square, down);
                break;
            case GamepadButton.Triangle:
                _controller.SetButtonState(DualShock4Button.Triangle, down);
                break;
            case GamepadButton.DpadUp:
                _controller.SetDPadDirection(down ? DualShock4DPadDirection.North : DualShock4DPadDirection.None);
                break;
            case GamepadButton.DpadDown:
                _controller.SetDPadDirection(down ? DualShock4DPadDirection.South : DualShock4DPadDirection.None);
                break;
            case GamepadButton.DpadLeft:
                _controller.SetDPadDirection(down ? DualShock4DPadDirection.West : DualShock4DPadDirection.None);
                break;
            case GamepadButton.DpadRight:
                _controller.SetDPadDirection(down ? DualShock4DPadDirection.East : DualShock4DPadDirection.None);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(button), button, null);
        }
    }
}
