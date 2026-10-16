using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

    // ---------------- 输入框按键（斜杠技能浮层 + 发送） ----------------

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
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

        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            if (vm.SendMessageCommand.CanExecute(null))
                vm.SendMessageCommand.Execute(null);
        }
        else if (e.Key == Key.Escape)
        {
            vm.CloseSkillPopup();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnClosing(e);
    }
}
