using System;
using BetterGenshinImpact.GameTask.AutoSkip;

namespace BetterGenshinImpact.UnitTest.GameTaskTests.AutoSkipTests;

/// <summary>
/// 手柄模式选择对话选项：第一个选项默认选中，按方向键下移动到目标选项。
/// </summary>
public class GamepadOptionPlannerTests
{
    [Fact]
    public void FirstOptionNeedsNoMove()
    {
        Assert.Equal(0, GamepadOptionPlanner.DownPresses(3, last: false, random: false, new Random(1)));
    }

    [Fact]
    public void LastOptionMovesToBottom()
    {
        Assert.Equal(3, GamepadOptionPlanner.DownPresses(4, last: true, random: false, new Random(1)));
    }

    [Fact]
    public void ZeroCountedTreatedAsOne()
    {
        Assert.Equal(0, GamepadOptionPlanner.DownPresses(0, last: true, random: false, new Random(1)));
        Assert.Equal(0, GamepadOptionPlanner.DownPresses(0, last: false, random: true, new Random(1)));
    }

    [Fact]
    public void RandomStaysInRange()
    {
        var rng = new Random(1);
        var seen = new bool[3];
        for (var i = 0; i < 200; i++)
        {
            var r = GamepadOptionPlanner.DownPresses(3, last: false, random: true, rng);
            Assert.InRange(r, 0, 2);
            seen[r] = true;
        }

        // 200 次内每个选项都应该出现过
        Assert.All(seen, Assert.True);
    }
}
