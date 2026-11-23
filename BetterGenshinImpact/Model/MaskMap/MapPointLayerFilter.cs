namespace BetterGenshinImpact.Model.MaskMap;

/// <summary>
/// 标点按层级过滤：地面层与其他层（地下、洞穴、上下层）
/// </summary>
public enum MapPointLayerFilter
{
    /// <summary>
    /// 全部标点
    /// </summary>
    All,

    /// <summary>
    /// 只显示地面层标点
    /// </summary>
    SurfaceOnly,

    /// <summary>
    /// 只显示其他层标点（地下、洞穴、上下层）
    /// </summary>
    OtherLevelsOnly
}
