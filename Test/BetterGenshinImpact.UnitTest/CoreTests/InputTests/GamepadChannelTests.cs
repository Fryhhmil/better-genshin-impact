using System;
using System.Collections.Generic;
using BetterGenshinImpact.Core.Input.Backends.Gamepad;
using BetterGenshinImpact.Core.Simulator.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Vanara.PInvoke;

namespace BetterGenshinImpact.UnitTest.CoreTests.InputTests;

/// <summary>
/// 手柄后端：键盘按键与游戏动作映射到虚拟手柄按钮，不支持的操作只忽略不抛异常。
/// </summary>
public class GamepadChannelTests
{
    /// <summary>
    /// 记录按下过的按钮的假手柄，不需要驱动。
    /// </summary>
    private sealed class RecordingGamepad : IVirtualGamepad
    {
        /// <summary>
        /// 按顺序记录的按钮
        /// </summary>
        public List<GamepadButton> Pressed { get; } = [];

        /// <summary>
        /// ReleaseAll 被调用的次数
        /// </summary>
        public int ReleaseAllCount { get; private set; }

        public void Press(GamepadButton button, int holdMs) => Pressed.Add(button);

        public void ReleaseAll() => ReleaseAllCount++;

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// 按指定确认键创建后端，确认键集合为空格与 F。
    /// </summary>
    private static (GamepadInputBackend Backend, RecordingGamepad Pad) Create(GamepadConfirmButton confirm)
    {
        var pad = new RecordingGamepad();
        var backend = new GamepadInputBackend(pad, new GamepadKeyMap(confirm, [User32.VK.VK_SPACE, User32.VK.VK_F]), NullLogger.Instance);
        return (backend, pad);
    }

    [Fact]
    public void SpaceAndPickKeyPressConfiguredConfirm()
    {
        var (backend, pad) = Create(GamepadConfirmButton.Circle);
        backend.Background.Keyboard.KeyPress(User32.VK.VK_SPACE);
        backend.Background.Keyboard.KeyPress(User32.VK.VK_F);
        Assert.Equal([GamepadButton.Circle, GamepadButton.Circle], pad.Pressed);
    }

    [Fact]
    public void CrossConfigSwapsConfirmAndCancel()
    {
        var (backend, pad) = Create(GamepadConfirmButton.Cross);
        backend.Background.Keyboard.KeyPress(User32.VK.VK_SPACE);
        backend.Background.Keyboard.KeyPress(User32.VK.VK_ESCAPE);
        Assert.Equal([GamepadButton.Cross, GamepadButton.Circle], pad.Pressed);
    }

    [Fact]
    public void WAndSMoveDpad()
    {
        var (backend, pad) = Create(GamepadConfirmButton.Circle);
        backend.Background.Keyboard.KeyPress(User32.VK.VK_W);
        backend.Background.Keyboard.KeyPress(User32.VK.VK_S);
        Assert.Equal([GamepadButton.DpadUp, GamepadButton.DpadDown], pad.Pressed);
    }

    [Fact]
    public void InteractActionPressesConfirm()
    {
        var (backend, pad) = Create(GamepadConfirmButton.Circle);
        backend.Background.SimulateAction(GIActions.PickUpOrInteract);
        Assert.Equal([GamepadButton.Circle], pad.Pressed);
    }

    [Fact]
    public void UnmappedInputIsIgnored()
    {
        var (backend, pad) = Create(GamepadConfirmButton.Circle);
        backend.Background.Keyboard.KeyPress(User32.VK.VK_Q);
        backend.Background.Mouse.LeftButtonClick();
        backend.Background.Mouse.MoveMouseBy(5, 5);
        Assert.Empty(pad.Pressed);
    }

    [Fact]
    public void ReleaseAllReachesPad()
    {
        var (backend, pad) = Create(GamepadConfirmButton.Circle);
        backend.ReleaseAll();
        Assert.Equal(1, pad.ReleaseAllCount);
    }
}
