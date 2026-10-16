using System.Windows;
using MegaAgent.App.ViewModels;
using MegaAgent.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MegaAgent.App;

/// <summary>
/// <see cref="IDialogService"/> 的 WPF 实现：所有窗口都由 DI 容器解析/构造，
/// 因此 ViewModel 侧完全看不到具体的 Window 类型。
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly IServiceProvider _services;

    public DialogService(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public bool ShowSkillEditor(Skill skill)
    {
        // Skill 是运行期参数（无法预先注册），用 ActivatorUtilities 组合「容器依赖 + 运行期参数」
        var viewModel = ActivatorUtilities.CreateInstance<SkillEditorDialogViewModel>(_services, skill);
        var dialog = ActivatorUtilities.CreateInstance<SkillEditorDialog>(_services, viewModel);
        return ShowDialog(dialog);
    }

    /// <inheritdoc />
    public bool Confirm(string message, string title, string confirmText = "确定") =>
        ShowDialog(new ConfirmDialog(title, message, confirmText));

    /// <inheritdoc />
    public void ShowInformation(string message, string title) =>
        HandyControl.Controls.MessageBox.Show(message, title,
            MessageBoxButton.OK, MessageBoxImage.Information);

    /// <inheritdoc />
    public bool ShowSettings() => ShowDialog(_services.GetRequiredService<SettingsDialog>());

    /// <inheritdoc />
    public bool ShowSkills() => ShowDialog(_services.GetRequiredService<SkillsDialog>());

    private static bool ShowDialog(Window dialog)
    {
        var owner = ResolveOwner();
        if (owner is not null && !ReferenceEquals(owner, dialog))
            dialog.Owner = owner;
        return dialog.ShowDialog() == true;
    }

    /// <summary>以当前活动窗口为宿主（无活动窗口时退回主窗口）。</summary>
    private static Window? ResolveOwner()
    {
        var windows = Application.Current?.Windows.OfType<Window>().ToList();
        return windows?.FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;
    }
}
