using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.Core.Input.Backends.Gamepad;

/// <summary>
/// 手柄模式配置：游戏使用手柄操作时，通过虚拟手柄发送按键
/// </summary>
[Serializable]
public partial class GamepadConfig : ObservableObject
{
    /// <summary>
    /// 是否启用手柄模式（下次绑定游戏运行环境时生效）
    /// </summary>
    [ObservableProperty]
    private bool _enabled = false;

    /// <summary>
    /// 虚拟手柄的确认键
    /// </summary>
    [ObservableProperty]
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    private GamepadConfirmButton _confirmButton = GamepadConfirmButton.Circle;
}
