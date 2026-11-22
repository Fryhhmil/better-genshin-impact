using System;
using System.Globalization;
using System.Text;

namespace BetterGenshinImpact.ViewModel;

/// <summary>
/// 标点类型搜索：不区分大小写与重音，同时匹配类型名称和所属大类名称。
/// </summary>
public static class MapLabelSearch
{
    /// <summary>
    /// 去掉重音符号并转为小写，用于比较
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>
    /// 判断类型是否匹配已规范化的搜索词
    /// </summary>
    /// <param name="itemName">类型名称</param>
    /// <param name="categoryName">所属大类名称</param>
    /// <param name="normalizedQuery">经 <see cref="Normalize"/> 处理的搜索词</param>
    public static bool Matches(string? itemName, string? categoryName, string normalizedQuery)
    {
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return true;
        }

        return Normalize(itemName).Contains(normalizedQuery, StringComparison.Ordinal)
               || Normalize(categoryName).Contains(normalizedQuery, StringComparison.Ordinal);
    }
}
