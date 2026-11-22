using System;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Input.Backends.Gamepad;

/// <summary>
/// 手柄后端：游戏处于手柄操作模式时（键盘输入会被游戏忽略），通过虚拟手柄发送按键。
/// 前后台返回同一个通道；手柄输入不需要窗口句柄。
/// </summary>
public sealed class GamepadInputBackend : IInputBackend
{
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
}
