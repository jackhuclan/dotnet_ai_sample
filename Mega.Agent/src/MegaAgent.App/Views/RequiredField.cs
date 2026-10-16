using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MegaAgent.App;

/// <summary>
/// 必填字段校验：为空时将输入框边框变红，并在其下方显示「XXX 为必填项」提示。
/// </summary>
internal sealed class RequiredField
{
    private static readonly Brush DangerBrush =
        Application.Current?.TryFindResource("DangerBrush") as Brush
        ?? new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D));

    private readonly Control _box;
    private readonly TextBlock _error;
    private readonly Func<string?> _value;

    public RequiredField(Control box, TextBlock error, string label, Func<string?> value)
    {
        _box = box;
        _error = error;
        _value = value;

        _error.Text = $"{label}为必填项";
        _error.Visibility = Visibility.Collapsed;
    }

    /// <summary>当前是否为空。</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(_value());

    /// <summary>校验一次：为空则标红并返回 false。</summary>
    public bool Validate()
    {
        if (!IsEmpty)
        {
            Clear();
            return true;
        }

        _box.BorderBrush = DangerBrush;
        _box.BorderThickness = new Thickness(1);
        _error.Visibility = Visibility.Visible;
        return false;
    }

    /// <summary>清除错误状态（恢复样式默认边框）。</summary>
    public void Clear()
    {
        _box.ClearValue(Control.BorderBrushProperty);
        _error.Visibility = Visibility.Collapsed;
    }
}
