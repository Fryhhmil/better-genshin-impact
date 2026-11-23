using System.Collections.Generic;

namespace BetterGenshinImpact.Model.MaskMap;

public class MaskMapPoint
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    
    /// <summary>
    /// 游戏中的坐标 X
    /// </summary>
    public double GameX { get; set; }
    
    /// <summary>
    /// 游戏中的坐标 Y
    /// </summary>
    public double GameY { get; set; }
    
    /// <summary>
    /// 2048级别游戏图像地图的坐标 X
    /// </summary>
    public double ImageX { get; set; }
    
    /// <summary>
    /// 2048级别游戏图像地图的坐标 Y
    /// </summary>
    public double ImageY { get; set; }
    
    public string LabelId { get; set; } = string.Empty;

    public List<MaskMapLink> VideoUrls { get; set; } = new();

    public bool IsHidden { get; set; }

    /// <summary>
    /// 是否位于其他层（地下、洞穴、上下层）：米游社/HoYoLAB 的 icon_sign 非 0，或空荧酒馆的 extra.underground.is_underground；
    /// 绘制时在图标角落显示层级角标，并参与层级过滤
    /// </summary>
    public bool IsOtherLevel { get; set; }

    /// <summary>
    /// 是否绘制该点位：已标记（IsHidden）的点位默认半透明绘制，开启“隐藏已标记的标点”时不绘制；不符合层级过滤的点位不绘制
    /// </summary>
    /// <param name="hideMarked">是否隐藏已标记的标点</param>
    /// <param name="layerFilter">层级过滤</param>
    public bool IsDrawn(bool hideMarked, MapPointLayerFilter layerFilter) =>
        !(hideMarked && IsHidden) && MatchesLayer(layerFilter);

    /// <summary>
    /// 是否符合层级过滤
    /// </summary>
    /// <param name="layerFilter">层级过滤</param>
    public bool MatchesLayer(MapPointLayerFilter layerFilter) => layerFilter switch
    {
        MapPointLayerFilter.SurfaceOnly => !IsOtherLevel,
        MapPointLayerFilter.OtherLevelsOnly => IsOtherLevel,
        _ => true
    };

    public bool Contains(double px, double py)
    {
        return px >= X - MaskMapPointStatic.Width / 2 && px <= X + MaskMapPointStatic.Width / 2 &&
               py >= Y - MaskMapPointStatic.Height / 2 && py <= Y + MaskMapPointStatic.Height / 2;
    }
}

public class MaskMapPointStatic
{
    public static readonly int Width = 32;

    public static readonly int Height = 32;
}
