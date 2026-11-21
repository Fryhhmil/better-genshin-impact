using System;
using System.Globalization;
using System.Windows.Data;

namespace BetterGenshinImpact.View.Converters;

/// <summary>
/// 判断两个绑定值是否相等（用于标记列表中的当前选中项，例如分段按钮的高亮）
/// </summary>
public sealed class ObjectEqualsMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Length >= 2 && Equals(values[0], values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
