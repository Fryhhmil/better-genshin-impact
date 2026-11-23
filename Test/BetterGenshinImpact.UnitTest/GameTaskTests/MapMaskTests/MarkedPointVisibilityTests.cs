using BetterGenshinImpact.Model.MaskMap;

namespace BetterGenshinImpact.UnitTest.GameTaskTests.MapMaskTests;

/// <summary>
/// “隐藏已标记的标点”开关下点位是否绘制的测试
/// </summary>
public sealed class MarkedPointVisibilityTests
{
    /// <summary>开关关闭时已标记的点位仍绘制（半透明），开启时不绘制；未标记的点位始终绘制</summary>
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    public void IsDrawn_HidesMarkedPointsOnlyWhenEnabled(bool isMarked, bool hideMarked, bool expected)
    {
        var point = new MaskMapPoint { IsHidden = isMarked };
        Assert.Equal(expected, point.IsDrawn(hideMarked, MapPointLayerFilter.All));
    }

    /// <summary>层级过滤：全部时都绘制；只显示地面时不绘制其他层；只显示其他层时不绘制地面</summary>
    [Theory]
    [InlineData(false, MapPointLayerFilter.All, true)]
    [InlineData(true, MapPointLayerFilter.All, true)]
    [InlineData(false, MapPointLayerFilter.SurfaceOnly, true)]
    [InlineData(true, MapPointLayerFilter.SurfaceOnly, false)]
    [InlineData(false, MapPointLayerFilter.OtherLevelsOnly, false)]
    [InlineData(true, MapPointLayerFilter.OtherLevelsOnly, true)]
    public void IsDrawn_FiltersByLayer(bool isOtherLevel, MapPointLayerFilter layerFilter, bool expected)
    {
        var point = new MaskMapPoint { IsOtherLevel = isOtherLevel };
        Assert.Equal(expected, point.IsDrawn(false, layerFilter));
    }
}
