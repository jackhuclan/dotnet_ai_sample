using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MegaAgent.App;

/// <summary>
/// 必填校验的视觉反馈：把 ViewModel 的「为空」状态绑定到控件边框上，
/// 为 true 时边框变红，为 false 时恢复样式默认边框。
/// <para>
/// 用法：<c>&lt;hc:TextBox local:RequiredBorder.HasError="{Binding NameHasError}" /&gt;</c>
/// </para>
/// 纯声明式，不含任何业务判断（判断逻辑全在 ViewModel）。
/// </summary>
public static class RequiredBorder
{
    private static readonly Brush DangerBrush =
        Application.Current?.TryFindResource("DangerBrush") as Brush
        ?? new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D));

    public static readonly DependencyProperty HasErrorProperty =
        DependencyProperty.RegisterAttached(
            "HasError",
            typeof(bool),
            typeof(RequiredBorder),
            new PropertyMetadata(false, OnHasErrorChanged));

    public static bool GetHasError(DependencyObject element) =>
        (bool)element.GetValue(HasErrorProperty);

    public static void SetHasError(DependencyObject element, bool value) =>
        element.SetValue(HasErrorProperty, value);

    private static void OnHasErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Control control) return;

        if (e.NewValue is true)
        {
            control.SetCurrentValue(Control.BorderBrushProperty, DangerBrush);
            control.SetCurrentValue(Control.BorderThicknessProperty, new Thickness(1));
        }
        else
        {
            // 清除本地值，让样式里的默认边框重新生效
            control.ClearValue(Control.BorderBrushProperty);
            control.ClearValue(Control.BorderThicknessProperty);
        }
    }
}
