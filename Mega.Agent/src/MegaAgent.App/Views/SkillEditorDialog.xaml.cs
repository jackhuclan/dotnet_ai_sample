using System.Windows;
using MegaAgent.App.ViewModels;
using MegaAgent.Core.Models;

namespace MegaAgent.App;

/// <summary>
/// 技能编辑对话框。字段、校验与保存逻辑都在 <see cref="SkillEditorDialogViewModel"/>。
/// </summary>
public partial class SkillEditorDialog : Window
{
    public SkillEditorDialog(Skill skill)
    {
        InitializeComponent();

        var vm = new SkillEditorDialogViewModel(skill);
        vm.CloseRequested += (_, result) =>
        {
            if (result is not null)
            {
                // 非模态打开时（离屏探针）赋值 DialogResult 会抛异常，忽略即可
                try { DialogResult = result; }
                catch (InvalidOperationException) { }
            }
            Close();
        };

        DataContext = vm;
    }
}
