using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using MegaAgent.App.ViewModels;

namespace MegaAgent.App;

public partial class MainWindow : Window
{
    /// <summary>抽屉宽度占聊天区宽度的比例。</summary>
    private const double DrawerRatio = 0.5;

    private static readonly TimeSpan AnimDuration = TimeSpan.FromMilliseconds(220);

    private bool _sidebarOpen;
    private double _drawerWidth;

    /// <summary>「…」菜单当前对应的会话。</summary>
    private ConversationViewModel? _menuTarget;

    /// <summary>「…」下拉菜单的内容总宽（Card 150 + Margin 8*2）。</summary>
    private const double ConversationMenuTotalWidth = 166;

    public MainWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<ScrollToBottomMessage>(this, (_, _) => ChatScrollViewer.ScrollToEnd());
        WeakReferenceMessenger.Default.Register<OpenSettingsMessage>(this, (_, _) => ShowSettings());
        WeakReferenceMessenger.Default.Register<OpenSkillsMessage>(this, (_, _) => ShowSkills());

        // 窗口固定尺寸：只需在首次布局后计算一次抽屉宽度（聊天区的 50%）
        Loaded += (_, _) =>
        {
            _drawerWidth = RootLayout.ActualWidth * DrawerRatio;
            SidebarDrawer.Width = _drawerWidth;
        };
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    // ---------------- 会话抽屉：默认隐藏，展开覆盖聊天区 50% 宽度 ----------------

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e) => SetSidebarOpen(!_sidebarOpen);

    private void SidebarScrim_Click(object sender, MouseButtonEventArgs e) => SetSidebarOpen(false);

    private void SetSidebarOpen(bool open)
    {
        if (_sidebarOpen == open) return;
        _sidebarOpen = open;

        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var width = _drawerWidth > 0 ? _drawerWidth : Width * DrawerRatio;

        if (open)
        {
            SidebarLayer.Visibility = Visibility.Visible;
            DrawerTransform.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation { From = -width, To = 0, Duration = AnimDuration, EasingFunction = ease });
            SidebarScrim.BeginAnimation(OpacityProperty,
                new DoubleAnimation { From = 0, To = 1, Duration = AnimDuration });
        }
        else
        {
            ConversationMenu.IsOpen = false;
            var slide = new DoubleAnimation { From = 0, To = -width, Duration = AnimDuration, EasingFunction = ease };
            slide.Completed += (_, _) =>
            {
                if (!_sidebarOpen) SidebarLayer.Visibility = Visibility.Collapsed;
            };
            DrawerTransform.BeginAnimation(TranslateTransform.XProperty, slide);
            SidebarScrim.BeginAnimation(OpacityProperty,
                new DoubleAnimation { From = 1, To = 0, Duration = AnimDuration });
        }
    }

    // ---------------- 消息区滚动 ----------------

    /// <summary>
    /// 消息气泡里的 Markdown 渲染器（md:MarkdownScrollViewer）本身是 ScrollViewer，
    /// 会把鼠标滚轮事件标记为已处理，导致外层消息列表收不到滚轮、内容不滚动。
    /// 在 Preview（隧道）阶段就由外层接管：把滚轮增量换算成外层的垂直偏移。
    /// </summary>
    private void ChatScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer || e.Handled) return;

        // 0.5 倍增量 ≈ 每格滚动 60px，与 WPF 默认「3 行」手感接近
        viewer.ScrollToVerticalOffset(viewer.VerticalOffset - e.Delta * 0.5);
        e.Handled = true;
    }

    // ---------------- 头像浮层菜单（聊天区右上角） ----------------

    private void AvatarButton_Click(object sender, RoutedEventArgs e)
    {
        if (AvatarMenu.IsOpen)
        {
            AvatarMenu.IsOpen = false;
            return;
        }

        // Popup 内容宽度 = 222(Card) + 10*2(Margin)
        const double popupTotalWidth = 242;

        AvatarMenu.PlacementTarget = TopAvatarButton;
        AvatarMenu.Placement = PlacementMode.Bottom;
        // 右边缘与头像对齐，向下弹出
        AvatarMenu.HorizontalOffset = TopAvatarButton.ActualWidth - popupTotalWidth;
        AvatarMenu.VerticalOffset = 4;
        AvatarMenu.IsOpen = true;
    }

    private void OpenSettingsFromMenu_Click(object sender, RoutedEventArgs e)
    {
        AvatarMenu.IsOpen = false;
        ShowSettings();
    }

    private void OpenSkillsFromMenu_Click(object sender, RoutedEventArgs e)
    {
        AvatarMenu.IsOpen = false;
        ShowSkills();
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

    // ---------------- 会话列表：… 菜单 / 重命名 / 删除 ----------------

    /// <summary>拦住冒泡，避免点「…」时把该会话切成当前会话。</summary>
    private void ConversationMore_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => e.Handled = true;

    private void ConversationMore_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement anchor)
            ToggleConversationMenu(anchor);
    }

    /// <summary>键盘（空格/回车）激活路径。</summary>
    private void ConversationMore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement anchor)
            ToggleConversationMenu(anchor);
    }

    private void ToggleConversationMenu(FrameworkElement anchor)
    {
        if (anchor.DataContext is not ConversationViewModel conv) return;

        // 同一个「…」再点一次 = 收起
        if (ConversationMenu.IsOpen && ReferenceEquals(_menuTarget, conv))
        {
            ConversationMenu.IsOpen = false;
            return;
        }

        _menuTarget = conv;
        ConversationMenu.PlacementTarget = anchor;
        ConversationMenu.Placement = PlacementMode.Bottom;
        // 右边缘与「…」对齐
        ConversationMenu.HorizontalOffset = anchor.ActualWidth - ConversationMenuTotalWidth;
        ConversationMenu.VerticalOffset = 2;
        ConversationMenu.IsOpen = true;
    }

    private void MenuRename_Click(object sender, RoutedEventArgs e)
    {
        ConversationMenu.IsOpen = false;
        Vm?.BeginRename(_menuTarget);
    }

    private void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        ConversationMenu.IsOpen = false;
        ConfirmDelete(_menuTarget);
    }

    private void ContextRename_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi)
            Vm?.BeginRename(mi.DataContext as ConversationViewModel);
    }

    private void ContextDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi)
            ConfirmDelete(mi.DataContext as ConversationViewModel);
    }

    /// <summary>删除前二次确认：确认才删，取消则什么都不做。</summary>
    private void ConfirmDelete(ConversationViewModel? conv)
    {
        if (conv is null) return;
        var dialog = new ConfirmDialog("删除会话", $"确认要删除「{conv.Title}」会话吗？", "删除")
        {
            Owner = this,
        };
        if (dialog.ShowDialog() == true)
            Vm?.DeleteConversation(conv);
    }

    // ---------------- 会话重命名：行内文本框 ----------------

    /// <summary>文本框出现时自动聚焦并全选标题。</summary>
    private void RenameBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox box || e.NewValue is not true) return;
        box.Dispatcher.BeginInvoke(new Action(() =>
        {
            box.Focus();
            box.SelectAll();
        }), DispatcherPriority.Input);
    }

    /// <summary>Enter 确认重命名，Esc 放弃。</summary>
    private void RenameBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box) return;
        var conv = box.DataContext as ConversationViewModel;
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                Vm?.CommitRename(conv);
                break;
            case Key.Escape:
                e.Handled = true;
                Vm?.CancelRename(conv);
                break;
        }
    }

    /// <summary>点到别处也算编辑完成。</summary>
    private void RenameBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox box) return;
        Vm?.CommitRename(box.DataContext as ConversationViewModel);
    }

    // ---------------- 输入框按键（斜杠技能浮层 + 发送） ----------------

    /// <summary>
    /// 用 PreviewKeyDown 而非 KeyDown：AcceptsReturn=True 的 TextBox 内部
    /// TextEditor 会先消费 Enter（插入换行并标记 Handled），KeyDown 收不到。
    /// </summary>
    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var vm = Vm;
        if (vm is null) return;

        if (vm.IsSkillPopupOpen)
        {
            switch (e.Key)
            {
                case Key.Up:
                    vm.MoveSkillSelection(-1);
                    e.Handled = true;
                    return;
                case Key.Down:
                    vm.MoveSkillSelection(1);
                    e.Handled = true;
                    return;
                case Key.Enter:
                case Key.Tab:
                    vm.CommitSkillSelection();
                    e.Handled = true;
                    return;
                case Key.Escape:
                    vm.CloseSkillPopup();
                    e.Handled = true;
                    return;
            }
        }

        if (e.Key != Key.Enter)
        {
            if (e.Key == Key.Escape)
                vm.CloseSkillPopup();
            return;
        }

        // Enter 发送；Shift+Enter 交给 TextBox 自己插入换行
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) return;

        e.Handled = true;
        if (vm.SendMessageCommand.CanExecute(null))
            vm.SendMessageCommand.Execute(null);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnClosing(e);
    }
}
