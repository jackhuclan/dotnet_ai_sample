using System.Windows;
using MegaAgent.App.ViewModels;

namespace MegaAgent.App;

/// <summary>
/// 模型设置对话框。业务逻辑全部在 <see cref="SettingsDialogViewModel"/>，
/// 这里只负责绑定 DataContext，以及响应「关闭对话框」请求（ViewModel 由容器注入）。
/// </summary>
public partial class SettingsDialog : Window
{
    public SettingsDialog(SettingsDialogViewModel viewModel)
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
