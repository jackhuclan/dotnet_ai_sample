using System.Windows;
using MegaAgent.App.ViewModels;

namespace MegaAgent.App;

/// <summary>
/// 技能管理对话框。列表状态与增删改逻辑都在 <see cref="SkillsDialogViewModel"/>；
/// 编辑器/确认框的打开由注入的 <see cref="IDialogService"/> 负责，本类不再实现窗口交互。
/// </summary>
public partial class SkillsDialog : Window
{
    public SkillsDialog(SkillsDialogViewModel viewModel)
    {
        InitializeComponent();

        viewModel.CloseRequested += (_, _) => Close();
        DataContext = viewModel;
    }
}
