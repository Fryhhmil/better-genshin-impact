using System.Collections.Generic;

namespace BetterGenshinImpact.ViewModel;

/// <summary>
/// 类型网格中的一行（两列）。网格按行虚拟化，只创建可见行的卡片，切换大类时不必一次生成全部卡片
/// </summary>
/// <param name="Left">左列类型</param>
/// <param name="Right">右列类型；最后一行可能为空</param>
public sealed record MapLabelItemRow(MapLabelItemVm Left, MapLabelItemVm? Right)
{
    /// <summary>
    /// 右列是否有类型。没有时必须隐藏右列：ContentPresenter 的内容为空时仍会套用卡片模板，
    /// 会显示一张继承了整行数据上下文的空卡片
    /// </summary>
    public bool HasRight => Right != null;

    /// <summary>
    /// 把类型列表按两列分行
    /// </summary>
    public static List<MapLabelItemRow> Split(IReadOnlyList<MapLabelItemVm> items)
    {
        var rows = new List<MapLabelItemRow>((items.Count + 1) / 2);
        for (var i = 0; i < items.Count; i += 2)
        {
            rows.Add(new MapLabelItemRow(items[i], i + 1 < items.Count ? items[i + 1] : null));
        }

        return rows;
    }
}
