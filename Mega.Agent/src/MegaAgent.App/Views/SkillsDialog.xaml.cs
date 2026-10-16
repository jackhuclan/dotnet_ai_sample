using System.Windows;
using MegaAgent.App.ViewModels;
using MegaAgent.Core.Models;
using MegaAgent.Core.Services;

namespace MegaAgent.App;

/// <summary>
/// 技能管理对话框。列表状态与增删改逻辑都在 <see cref="SkillsDialogViewModel"/>；
/// 本类只实现 <see cref="IDialogService"/>，把「打开编辑器」「弹确认框」这类窗口级能力提供给 ViewModel。
/// </summary>
public partial class SkillsDialog : Window, IDialogService
{
    public SkillsDialog(SkillStore store)
    {
        InitializeComponent();

        var vm = new SkillsDialogViewModel(store, this);
        vm.CloseRequested += (_, _) => Close();

        DataContext = vm;
    }

    /// <inheritdoc />
    public bool ShowSkillEditor(Skill skill)
    {
        var editor = new SkillEditorDialog(skill) { Owner = this };
        return editor.ShowDialog() == true;
    }

    /// <inheritdoc />
    public bool Confirm(string message, string title) =>
        HandyControl.Controls.MessageBox.Show(message, title,
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
