using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using MegaAgent.App.ViewModels;

namespace MegaAgent.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 滚动到底部通知
        WeakReferenceMessenger.Default.Register<ScrollToBottomMessage>(this, (_, _) => ScrollToBottom());

        // 打开设置 / 技能管理窗口（由 View 层负责窗口创建，VM 只发消息）
        WeakReferenceMessenger.Default.Register<OpenSettingsMessage>(this, (_, _) => ShowSettings());
        WeakReferenceMessenger.Default.Register<OpenSkillsMessage>(this, (_, _) => ShowSkills());
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            if (Vm?.SendMessageCommand.CanExecute(null) == true)
                Vm.SendMessageCommand.Execute(null);
        }
    }

    private void ShowSettings()
    {
        var dialog = new SettingsDialog(App.SettingsStore) { Owner = this };
        dialog.ShowDialog();
        Vm?.RefreshApiKeyStatus();
    }

    private void ShowSkills()
    {
        var dialog = new SkillsDialog(App.SkillStore) { Owner = this };
        dialog.ShowDialog();
        Vm?.RebuildSkillOptions();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnClosing(e);
    }
}
