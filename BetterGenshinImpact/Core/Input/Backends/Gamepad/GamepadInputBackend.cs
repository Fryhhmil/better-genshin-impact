using System;
using System.Collections.Generic;
using System.Threading;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Input.Backends.Win32;
using BetterGenshinImpact.Helpers;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace BetterGenshinImpact.Core.Input.Backends.Gamepad;

/// <summary>
/// 手柄后端：游戏处于手柄操作模式时（键盘输入会被游戏忽略），通过虚拟手柄发送按键。
/// 前后台返回同一个通道；手柄输入不需要窗口句柄。
/// </summary>
public sealed class GamepadInputBackend : IInputBackend
{
    /// <summary>
    /// 驱动缺失的提示是否已经输出过（每个进程只提示一次）
    /// </summary>
    private static int _driverMissingWarned;

    /// <summary>
    /// 虚拟手柄设备
    /// </summary>
    private readonly IVirtualGamepad _pad;

    /// <summary>
    /// 唯一的输入通道
    /// </summary>
    private readonly GamepadChannel _channel;

    /// <param name="pad">虚拟手柄设备，由本后端负责释放</param>
    /// <param name="keyMap">键盘按键到手柄按钮的映射</param>
    /// <param name="logger">通道日志；为 null 时使用应用容器中的日志</param>
    public GamepadInputBackend(IVirtualGamepad pad, GamepadKeyMap keyMap, ILogger? logger = null)
    {
        _pad = pad ?? throw new ArgumentNullException(nameof(pad));
        _channel = new GamepadChannel(pad, keyMap ?? throw new ArgumentNullException(nameof(keyMap)), logger);
    }

    public InputBackendKind Kind => InputBackendKind.Gamepad;

    public IInputChannel Foreground => _channel;

    public IInputChannel Background => _channel;

    public void ReleaseAll() => _channel.ReleaseAll();

    public void Dispose() => _pad.Dispose();

    /// <summary>
    /// 按配置创建输入后端：手柄模式开启且 ViGEmBus 驱动可用时使用虚拟手柄，否则使用 Windows 键鼠后端。
    /// 驱动缺失时只提示一次，不抛异常（识别类功能如地图遮罩仍可使用）
    /// </summary>
    /// <param name="config">全局配置</param>
    /// <param name="hWnd">游戏窗口句柄（键鼠后端使用）</param>
    public static IInputBackend CreateOrFallback(AllConfig config, IntPtr hWnd)
    {
        if (!config.GamepadConfig.Enabled)
        {
            return new Win32InputBackend(hWnd);
        }

        if (!ViGEmDualShock4Gamepad.TryCreate(out var pad, out var error))
        {
            if (Interlocked.Exchange(ref _driverMissingWarned, 1) == 0)
            {
                App.GetLogger<GamepadInputBackend>().LogWarning(
                    "未检测到 ViGEmBus 驱动，手柄模式无法发送按键（{Error}）。请安装：https://github.com/nefarius/ViGEmBus/releases", error);
            }

            return new Win32InputBackend(hWnd);
        }

        var keyMap = new GamepadKeyMap(config.GamepadConfig.ConfirmButton, GetConfirmKeys(config));
        return new GamepadInputBackend(pad, keyMap);
    }

    /// <summary>
    /// 映射为确认键的键盘按键：空格、回车、F 与用户配置的拾取键
    /// </summary>
    private static IEnumerable<User32.VK> GetConfirmKeys(AllConfig config)
    {
        yield return User32.VK.VK_SPACE;
        yield return User32.VK.VK_RETURN;
        yield return User32.VK.VK_F;

        var pickKey = config.AutoPickConfig.PickKey;
        if (string.IsNullOrWhiteSpace(pickKey))
        {
            yield break;
        }

        User32.VK? vk = null;
        try
        {
            vk = User32Helper.ToVk(pickKey.Trim());
        }
        catch (Exception)
        {
            // 无法识别的自定义拾取键忽略，F 已经覆盖默认情况
        }

        if (vk is { } value)
        {
            yield return value;
        }
    }
}
