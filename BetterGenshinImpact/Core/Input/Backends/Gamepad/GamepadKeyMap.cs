using System.Collections.Generic;
using System.Linq;
using Vanara.PInvoke;

namespace BetterGenshinImpact.Core.Input.Backends.Gamepad;

/// <summary>
/// 键盘按键到虚拟手柄按钮的映射。只覆盖跳过对话需要的按键：确认、返回与方向
/// </summary>
public sealed class GamepadKeyMap
{
    /// <summary>
    /// 映射为确认键的键盘按键（空格、交互键等）
    /// </summary>
    private readonly HashSet<User32.VK> _confirmKeys;

    /// <param name="confirm">虚拟手柄的确认键</param>
    /// <param name="confirmKeys">映射为确认键的键盘按键</param>
    public GamepadKeyMap(GamepadConfirmButton confirm, IEnumerable<User32.VK> confirmKeys)
    {
        Confirm = confirm == GamepadConfirmButton.Cross ? GamepadButton.Cross : GamepadButton.Circle;
        Cancel = confirm == GamepadConfirmButton.Cross ? GamepadButton.Circle : GamepadButton.Cross;
        _confirmKeys = confirmKeys.ToHashSet();
    }

    /// <summary>
    /// 确认键
    /// </summary>
    public GamepadButton Confirm { get; }

    /// <summary>
    /// 返回键（确认键的另一个）
    /// </summary>
    public GamepadButton Cancel { get; }

    /// <summary>
    /// 把键盘按键映射为手柄按钮
    /// </summary>
    /// <param name="key">键盘按键</param>
    /// <param name="button">对应的手柄按钮</param>
    /// <returns>没有对应按钮时返回 false</returns>
    public bool TryMap(User32.VK key, out GamepadButton button)
    {
        if (_confirmKeys.Contains(key))
        {
            button = Confirm;
            return true;
        }

        switch (key)
        {
            case User32.VK.VK_ESCAPE:
                button = Cancel;
                return true;
            case User32.VK.VK_W or User32.VK.VK_UP:
                button = GamepadButton.DpadUp;
                return true;
            case User32.VK.VK_S or User32.VK.VK_DOWN:
                button = GamepadButton.DpadDown;
                return true;
            case User32.VK.VK_A or User32.VK.VK_LEFT:
                button = GamepadButton.DpadLeft;
                return true;
            case User32.VK.VK_D or User32.VK.VK_RIGHT:
                button = GamepadButton.DpadRight;
                return true;
            default:
                button = default;
                return false;
        }
    }
}
