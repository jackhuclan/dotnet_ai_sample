using System.Windows;
using MegaAgent.App.ViewModels;

namespace MegaAgent.App;

/// <summary>
/// 技能编辑对话框。字段、校验与保存逻辑都在 <see cref="SkillEditorDialogViewModel"/>
/// （由容器解析，运行期的 Skill 参数由 <see cref="DialogService"/> 传入）。
/// </summary>
public partial class SkillEditorDialog : Window
{
    public SkillEditorDialog(SkillEditorDialogViewModel viewModel)
    {
        InitializeComponent();

        viewModel.CloseRequested += (_, result) =>
        {
            if (result is not null)
            {
                // 非模态打开时（离屏探针）赋值 DialogResult 会抛异常，忽略即可
                try { DialogResult = result; }
                catch (InvalidOperationException) { }
            }
            Close();
        };

        DataContext = viewModel;
    }
}
