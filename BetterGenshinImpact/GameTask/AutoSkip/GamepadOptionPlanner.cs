using System;

namespace BetterGenshinImpact.GameTask.AutoSkip;

/// <summary>
/// 手柄模式下选择对话选项的移动次数。
/// 打开选项时第一个选项默认选中；数出屏幕上的选项后用方向键下移动，不依赖方向键上能否从第一个循环到最后一个
/// </summary>
public static class GamepadOptionPlanner
{
    /// <summary>
    /// 计算需要按方向键下的次数
    /// </summary>
    /// <param name="optionCount">屏幕上识别到的选项数量；为 0 时（只识别到拾取提示）按 1 个处理</param>
    /// <param name="last">是否选择最后一个选项</param>
    /// <param name="random">是否随机选择</param>
    /// <param name="rng">随机数生成器</param>
    /// <returns>方向键下的次数，0 表示直接确认第一个选项</returns>
    public static int DownPresses(int optionCount, bool last, bool random, Random rng)
    {
        var count = Math.Max(optionCount, 1);
        if (random)
        {
            return rng.Next(0, count);
        }

        return last ? count - 1 : 0;
    }
}
