using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MegaAgent.App;
using MegaAgent.App.ViewModels;
using MegaAgent.Core.Models;
using MegaAgent.Core.Services;

namespace MegaAgent.UiProbe;

/// <summary>
/// 离屏 UI 探针：不依赖鼠标/键盘模拟，直接实例化对话框、触发校验、把结果渲染成 PNG。
/// 用于验证「必填校验（红框 + 必填提示）」等纯样式类改造。
/// </summary>
internal static class ProbeProgram
{
    private const string ShotDir = @"E:\dotnet_ai_sample\Mega.Agent\docs\screenshots";

    [STAThread]
    private static void Main()
    {
        var app = new MegaAgent.App.App();
        app.InitializeComponent();
        Directory.CreateDirectory(ShotDir);

        // 安装 Dispatcher 同步上下文，使 await 续体回到 UI 线程（与真实 Application.Run 一致）
        SynchronizationContext.SetSynchronizationContext(
            new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

        ProbeSettingsDialog();
        ProbeSkillEditorDialog();
        ProbeAvatarMenu();
        ProbeEnterSend();

        Console.WriteLine("probe done");
    }

    // ---------------- 输入区：占位符/提示文案 + Enter 发送链路 ----------------

    private static void ProbeEnterSend()
    {
        var settings = new SettingsStore();
        settings.Load();
        var skills = new SkillStore();
        skills.Load();
        var conversations = new ConversationStore();
        conversations.Load();
        var agent = new AgentService(settings);

        // 探针会真实写入会话文件，结束后还原
        var convPath = StoragePaths.ConversationsPath;
        var backup = Path.Combine(Path.GetTempPath(), "mega-conversations.backup.json");
        if (File.Exists(convPath)) File.Copy(convPath, backup, true);

        try
        {
            var vm = new MainViewModel(settings, skills, conversations, agent);
            var win = new MainWindow { DataContext = vm, Left = -4000, Top = -4000, ShowActivated = false };
            win.Show();
            Pump();

            Save(win, "82-input-area");

            var box = (HandyControl.Controls.TextBox)win.FindName("InputBox")!;
            Console.WriteLine($"placeholder = {HandyControl.Controls.InfoElement.GetPlaceholder(box)}");

            var conv = vm.CurrentConversation;
            if (conv is null)
            {
                Console.WriteLine("没有可用会话，跳过 Enter 验证");
                win.Close();
                return;
            }

            var before = conv.Messages.Count;
            vm.DraftInput = "只回复两个字：收到";
            box.CaretIndex = box.Text.Length;
            Pump();

            // 模拟 Enter（走 PreviewKeyDown，与真实按键同一条事件路径）
            RaiseKey(box, Key.Enter);
            Pump();

            var draftCleared = string.IsNullOrEmpty(vm.DraftInput);
            var appended = conv.Messages.Count - before;
            Console.WriteLine($"Enter 后：DraftInput=\"{vm.DraftInput}\" 新增消息={appended} IsBusy={vm.IsBusy}");

            // 等流式回复
            var sw = Stopwatch.StartNew();
            var reply = string.Empty;
            while (sw.ElapsedMilliseconds < 15000)
            {
                PumpFor(200);
                var last = conv.Messages[^1];
                if (!string.IsNullOrEmpty(last.Content)) reply = last.Content;
                if (!vm.IsBusy && reply.Length > 0) break;
            }
            Console.WriteLine($"流式回复：{reply}");

            var pass = draftCleared && appended >= 2 && reply.Length > 0;
            Console.WriteLine(pass ? "ENTER-SEND PASS" : "ENTER-SEND FAIL");

            vm.StopCommand.Execute(null);
            PumpFor(500);
            win.Close();
        }
        finally
        {
            if (File.Exists(backup))
            {
                File.Copy(backup, convPath, true);
                Console.WriteLine("conversations.json 已还原");
            }
        }
    }

    private static void RaiseKey(UIElement target, Key key)
    {
        var args = new KeyEventArgs(Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(target), 0, key)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent,
        };
        target.RaiseEvent(args);
    }

    private static void PumpFor(int milliseconds)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < milliseconds)
        {
            Pump();
            Thread.Sleep(25);
        }
    }

    // ---------------- 头像菜单：验证「技能管理」文案与头像 ----------------

    private static void ProbeAvatarMenu()
    {
        // 不设置 DataContext：仅验证静态布局（绑定为空不影响文案渲染）
        var win = new MainWindow { Left = -4000, Top = -4000, ShowActivated = false };
        win.Show();
        Pump();

        var popup = (Popup)win.FindName("AvatarMenu")!;
        popup.PlacementTarget = win;
        popup.Placement = PlacementMode.Relative;
        popup.HorizontalOffset = 0;
        popup.VerticalOffset = 0;
        popup.IsOpen = true;
        Pump();

        if (popup.Child is FrameworkElement card)
        {
            card.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            card.Arrange(new Rect(new Point(0, 0), card.DesiredSize));
            card.UpdateLayout();

            var bmp = new RenderTargetBitmap(
                (int)Math.Ceiling(card.ActualWidth), (int)Math.Ceiling(card.ActualHeight),
                96, 96, PixelFormats.Pbgra32);
            bmp.Render(card);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            var path = Path.Combine(ShotDir, "81-avatar-menu.png");
            using var fs = File.Create(path);
            encoder.Save(fs);
            Console.WriteLine($"rendered: {path}");
        }

        popup.IsOpen = false;
        win.Close();
    }

    // ---------------- 模型设置：API Key / 接口地址必填 ----------------

    private static void ProbeSettingsDialog()
    {
        var store = new SettingsStore();
        store.Load(); // 只读；本探针不会触发保存（校验必然失败）

        var dlg = new SettingsDialog(store) { Left = -4000, Top = -4000, ShowActivated = false };
        dlg.Show();
        Pump();

        // 清空两个必填项
        var apiKey = (HandyControl.Controls.PasswordBox)dlg.FindName("ApiKeyBox")!;
        apiKey.Password = string.Empty;
        var endpoint = (TextBox)dlg.FindName("EndpointBox")!;
        endpoint.Text = string.Empty;
        Pump();

        ClickButton(dlg, "保存");
        Pump();
        Save(dlg, "70-settings-required-empty");

        // 只填 API Key，接口地址仍为空
        apiKey.Password = "sk-test-123";
        Pump();
        ClickButton(dlg, "保存");
        Pump();
        Save(dlg, "71-settings-required-endpoint-only");

        dlg.Close();
    }

    // ---------------- 技能编辑：三个输入框必填 ----------------

    private static void ProbeSkillEditorDialog()
    {
        var editor = new SkillEditorDialog(new Skill()) { Left = -4000, Top = -4000, ShowActivated = false };
        editor.Show();
        Pump();

        ClickButton(editor, "保存");
        Pump();
        Save(editor, "72-skill-editor-required-empty");

        // 只填名称
        ((TextBox)editor.FindName("NameBox")!).Text = "翻译助手";
        Pump();
        ClickButton(editor, "保存");
        Pump();
        Save(editor, "73-skill-editor-required-partial");

        editor.Close();
    }

    // ---------------- 工具方法 ----------------

    private static void ClickButton(DependencyObject root, string content)
    {
        foreach (var btn in Descendants(root).OfType<Button>())
        {
            if (btn.Content as string == content)
            {
                btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                return;
            }
        }
        Console.WriteLine($"未找到按钮：{content}");
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var grand in Descendants(child)) yield return grand;
        }
    }

    /// <summary>把窗口内容渲染为 PNG。</summary>
    private static void Save(Window window, string name)
    {
        window.UpdateLayout();

        var width = (int)Math.Ceiling(window.ActualWidth);
        var height = (int)Math.Ceiling(window.ActualHeight);
        if (width <= 0 || height <= 0)
        {
            Console.WriteLine($"窗口尺寸异常，跳过：{name}");
            return;
        }

        var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        var path = Path.Combine(ShotDir, name + ".png");
        using var fs = File.Create(path);
        encoder.Save(fs);
        Console.WriteLine($"rendered: {path} ({width}x{height})");
    }

    /// <summary>跑一轮消息队列，保证布局与绑定刷新。</summary>
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
