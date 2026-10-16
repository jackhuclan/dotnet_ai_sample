using System.Windows;
using System.Windows.Controls;

namespace MegaAgent.App;

/// <summary>
/// 让 <see cref="HandyControl.Controls.PasswordBox"/> 的密码可以参与 MVVM 双向绑定。
/// <para>
/// 背景：WPF 的密码框（原生与 HandyControl 皆然）<c>Password</c> 都是普通 CLR 属性、不是依赖属性，
/// 无法直接 <c>Password="{Binding ...}"</c>（会抛 XamlParseException）。
/// 这里提供一个可绑定的附加属性，并与模板内部的原生输入控件双向同步。
/// </para>
/// 用法：<c>&lt;hc:PasswordBox local:PasswordBinding.Password="{Binding ApiKey, Mode=TwoWay}" /&gt;</c>
/// </summary>
public static class PasswordBinding
{
    /// <summary>可绑定的密码文本（ViewModel 侧的值）。默认值为 null，确保首次绑定时回调必定触发。</summary>
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBinding),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPasswordChanged));

    /// <summary>内部同步标记，避免「控件 ↔ 附加属性」互相回写造成死循环。</summary>
    private static readonly DependencyProperty IsSyncingProperty =
        DependencyProperty.RegisterAttached("IsSyncing", typeof(bool), typeof(PasswordBinding),
            new PropertyMetadata(false));

    /// <summary>是否已挂接模板内部控件的变更事件。</summary>
    private static readonly DependencyProperty IsHookedProperty =
        DependencyProperty.RegisterAttached("IsHooked", typeof(bool), typeof(PasswordBinding),
            new PropertyMetadata(false));

    public static string? GetPassword(DependencyObject element) =>
        (string?)element.GetValue(PasswordProperty);

    public static void SetPassword(DependencyObject element, string? value) =>
        element.SetValue(PasswordProperty, value);

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HandyControl.Controls.PasswordBox box) return;

        EnsureHooked(box);

        // 由内部控件回写触发：只需把值传给 ViewModel，不再反向设置控件
        if ((bool)box.GetValue(IsSyncingProperty)) return;

        var value = e.NewValue as string ?? string.Empty;
        box.SetValue(IsSyncingProperty, true);
        try
        {
            if (box.Password != value) box.Password = value;

            // 部分模板不会把外层 Password 回推到内部原生控件，这里补一手
            if (box.Template?.FindName("PART_PasswordBox", box) is System.Windows.Controls.PasswordBox inner
                && inner.Password != value)
                inner.Password = value;
        }
        finally
        {
            box.SetValue(IsSyncingProperty, false);
        }
    }

    /// <summary>挂接模板内部的 PART_PasswordBox（密文）/ PART_TextBox（明文）变更事件。</summary>
    private static void EnsureHooked(HandyControl.Controls.PasswordBox box)
    {
        if ((bool)box.GetValue(IsHookedProperty)) return;
        box.SetValue(IsHookedProperty, true);

        void Hook()
        {
            box.ApplyTemplate();
            HookPart(box, "PART_PasswordBox");
            HookPart(box, "PART_TextBox");
        }

        if (box.IsLoaded) Hook();
        else box.Loaded += (_, _) => Hook();
    }

    private static void HookPart(HandyControl.Controls.PasswordBox box, string partName)
    {
        switch (box.Template?.FindName(partName, box))
        {
            case System.Windows.Controls.PasswordBox inner:
                inner.PasswordChanged += (s, _) =>
                    PushToSource(box, ((System.Windows.Controls.PasswordBox)s).Password);
                break;
            case TextBox plain:
                plain.TextChanged += (s, _) => PushToSource(box, ((TextBox)s).Text);
                break;
        }
    }

    /// <summary>内部控件 → 附加属性（绑定会把新值推给 ViewModel）。</summary>
    private static void PushToSource(HandyControl.Controls.PasswordBox box, string text)
    {
        box.SetValue(IsSyncingProperty, true);
        try
        {
            SetPassword(box, text);
        }
        finally
        {
            box.SetValue(IsSyncingProperty, false);
        }
    }
}
