using System;

namespace BetterGenshinImpact.Core.Input.Backends.Gamepad;

/// <summary>
/// 虚拟手柄设备。抽象出来便于在没有驱动的环境下测试映射逻辑
/// </summary>
public interface IVirtualGamepad : IDisposable
{
    /// <summary>
    /// 按下按钮、保持指定时间后抬起（阻塞调用）
    /// </summary>
    /// <param name="button">按钮</param>
    /// <param name="holdMs">按住时长（毫秒）</param>
    void Press(GamepadButton button, int holdMs);

    /// <summary>
    /// 抬起所有按钮
    /// </summary>
    void ReleaseAll();
}
