using System.IO;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.GameTask.Model.Area.Converter;
using OpenCvSharp;

namespace BetterGenshinImpact.UnitTest.GameTaskTests;

/// <summary>
/// 手柄界面下的对话与大地图识别。
/// 截图含 UID，不纳入版本库，放在仓库根目录的 .local/gamepad-screens 下；缺少截图时测试直接返回。
/// talk-controller-confirm.png 上盖着“手柄连接错误”弹窗（界面变暗），不是正常对话状态，不参与断言。
/// </summary>
public class GamepadUiRecognitionTests
{
    /// <summary>
    /// 截图目录（相对测试输出目录）
    /// </summary>
    private const string ScreensDir = @"..\..\..\..\..\.local\gamepad-screens\";

    [Theory]
    [InlineData("talk-controller.png")]
    [InlineData("talk-controller-option.png")]
    public void TalkScreensAreTalkNotBigMap(string file)
    {
        using var region = Load(file);
        if (region is null)
        {
            return;
        }

        Assert.True(Bv.IsInTalkUi(region));
        Assert.False(Bv.IsInBigMapUi(region));
    }

    [Fact]
    public void MapScreenIsBigMapNotTalk()
    {
        using var region = Load("map-controller.png");
        if (region is null)
        {
            return;
        }

        Assert.True(Bv.IsInBigMapUi(region));
        Assert.False(Bv.IsInTalkUi(region));
    }

    /// <summary>
    /// 读取 1080p 截图为识别区域，文件不存在时返回 null
    /// </summary>
    private static ImageRegion? Load(string file)
    {
        var path = Path.Combine(ScreensDir, file);
        if (!File.Exists(path))
        {
            return null;
        }

        // 截图工具裁掉了 1 像素（1919×1079），补边还原为游戏实际的 1920×1080，避免模板被缩放
        using var raw = new Mat(path);
        var mat = new Mat();
        Cv2.CopyMakeBorder(raw, mat, 0, 1080 - raw.Height, 0, 1920 - raw.Width, BorderTypes.Replicate);
        return new ImageRegion(mat, 0, 0, new DesktopRegion(), converter: new ScaleConverter(1d));
    }
}
