namespace BetterGenshinImpact.Core.Input.Backends.Gamepad;

/// <summary>
/// 虚拟手柄按钮（按 DualShock 4 布局命名）
/// </summary>
public enum GamepadButton
{
    /// <summary>
    /// ✕ 键
    /// </summary>
    Cross,

    /// <summary>
    /// ○ 键
    /// </summary>
    Circle,

    /// <summary>
    /// □ 键
    /// </summary>
    Square,

    /// <summary>
    /// △ 键
    /// </summary>
    Triangle,

    /// <summary>
    /// 方向键上
    /// </summary>
    DpadUp,

    /// <summary>
    /// 方向键下
    /// </summary>
    DpadDown,

    /// <summary>
    /// 方向键左
    /// </summary>
    DpadLeft,

    /// <summary>
    /// 方向键右
    /// </summary>
    DpadRight,
}

/// <summary>
/// 虚拟手柄的确认键。游戏对新连接的手柄使用默认布局（○ 确认），与玩家实体手柄的设置无关
/// </summary>
public enum GamepadConfirmButton
{
    /// <summary>
    /// ○ 确认，✕ 返回（游戏默认）
    /// </summary>
    Circle,

    /// <summary>
    /// ✕ 确认，○ 返回
    /// </summary>
    Cross,
}
