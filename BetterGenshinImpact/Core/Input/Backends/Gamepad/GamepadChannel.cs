using BetterGenshinImpact.Core.Simulator.Extensions;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace BetterGenshinImpact.Core.Input.Backends.Gamepad;

/// <summary>
/// 手柄通道：把键盘按键与游戏动作映射成虚拟手柄按钮。
/// <list type="bullet">
/// <item>能映射的按键按下即完成一次“按下 + 抬起”（手柄按钮按住一小段时间），抬起不再重复发送；</item>
/// <item>不能映射的按键、鼠标操作只限频 warn 后忽略。坐标点击由“移动 + 左键”组成，两步都会被忽略。</item>
/// </list>
/// </summary>
internal sealed class GamepadChannel : InputChannelBase
{
    /// <summary>
    /// 每次按钮按住的时长。游戏 30 fps 时一帧约 33 ms，留足余量避免丢输入
    /// </summary>
    private const int PressHoldMs = 120;

    /// <summary>
    /// 虚拟手柄设备
    /// </summary>
    private readonly IVirtualGamepad _pad;

    /// <summary>
    /// 键盘按键映射
    /// </summary>
    private readonly GamepadKeyMap _keyMap;

    /// <param name="pad">虚拟手柄设备</param>
    /// <param name="keyMap">键盘按键映射</param>
    /// <param name="logger">日志；为 null 时使用应用容器中的日志</param>
    public GamepadChannel(IVirtualGamepad pad, GamepadKeyMap keyMap, ILogger? logger = null) : base(logger)
    {
        _pad = pad;
        _keyMap = keyMap;
    }

    /// <summary>
    /// 交互动作直接映射为确认键，其余动作暂不支持
    /// </summary>
    public override IInputChannel SimulateAction(GIActions action, KeyType type = KeyType.KeyPress)
    {
        if (action == GIActions.PickUpOrInteract)
        {
            // 按下/抬起分开调用时只在按下时发送一次完整点击
            if (type != KeyType.KeyUp)
            {
                _pad.Press(_keyMap.Confirm, PressHoldMs);
            }

            return this;
        }

        WarnUnsupported($"手柄后端暂不支持动作 {action}");
        return this;
    }

    protected override void OnKeyDown(User32.VK key) => PressMapped(key);

    /// <summary>
    /// 按下时已经完成了整次按键，抬起无需操作
    /// </summary>
    protected override void OnKeyUp(User32.VK key)
    {
    }

    protected override void OnKeyPress(User32.VK key) => PressMapped(key);

    protected override void OnMouseButton(InputMouseButton button, bool down) => WarnUnsupported("手柄后端不支持鼠标按键");

    protected override void OnMoveBy(int dx, int dy) => WarnUnsupported("手柄后端不支持鼠标移动");

    protected override void OnMoveTo(double absX, double absY) => WarnUnsupported("手柄后端不支持鼠标定位");

    protected override void OnScroll(int clicks) => WarnUnsupported("手柄后端不支持鼠标滚轮");

    /// <summary>
    /// 手柄按钮按下即抬起，没有持续按下的键
    /// </summary>
    public override bool IsKeyDown(User32.VK key) => false;

    public override void ReleaseAll()
    {
        TakePressedKeys();
        _pad.ReleaseAll();
    }

    /// <summary>
    /// 能映射的按键发送一次手柄按钮，否则忽略
    /// </summary>
    private void PressMapped(User32.VK key)
    {
        if (_keyMap.TryMap(key, out var button))
        {
            _pad.Press(button, PressHoldMs);
        }
        else
        {
            WarnUnsupported($"手柄后端不支持按键 {key}");
        }
    }
}
