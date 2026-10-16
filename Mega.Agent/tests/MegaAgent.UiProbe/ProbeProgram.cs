using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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
/// 离屏 UI 探针：不依赖鼠标/键盘模拟，直接实例化窗口与 ViewModel、驱动命令、把结果渲染成 PNG。
/// 覆盖：必填校验（红框 + 必填提示 + 保存按钮禁用）、对话框 MVVM 绑定、消息区滚轮滚动。
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

        DumpPasswordBoxInfo();

        ProbeSettingsDialog();
        ProbeSkillEditorDialog();
        ProbeSkillsDialog();
        ProbeChatScroll();
        ProbeAvatarMenu();
        ProbeEnterSend();
        ProbeConversationMenu();

        Console.WriteLine("probe done");
    }

    // ---------------- 诊断：hc:PasswordBox.Password 是否支持双向绑定 ----------------

    private static void DumpPasswordBoxInfo()
    {
        var type = typeof(HandyControl.Controls.PasswordBox);
        var prop = type.GetProperty("Password");
        var field = type.GetField("PasswordProperty",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Console.WriteLine($"[diag] PasswordBox 基类={type.BaseType?.Name} " +
                          $"Password 属性={prop?.PropertyType.Name} " +
                          $"PasswordProperty 是依赖属性={(field?.GetValue(null) is DependencyProperty)}");
    }

    // ---------------- 模型设置：必填红框 + 保存按钮禁用 + 双向绑定 ----------------

    private static void ProbeSettingsDialog()
    {
        var configBackup = BackupFile(StoragePaths.ConfigPath);

        try
        {
            var store = new SettingsStore();
            store.Load();

            var dlg = new SettingsDialog(store) { Left = -4000, Top = -4000, ShowActivated = false };
            dlg.Show();
            Pump();

            var vm = (SettingsDialogViewModel)dlg.DataContext;
            var apiKeyBox = (HandyControl.Controls.PasswordBox)dlg.FindName("ApiKeyBox")!;
            var endpointBox = (TextBox)dlg.FindName("EndpointBox")!;
            var saveButton = Buttons(dlg).First(b => (b.Content as string) == "保存");
            var testButton = Buttons(dlg).First(b => (b.Content as string) == "测试连接");

            // ---- 1) 两字段清空：不点任何按钮，立即红框 + 提示 + 保存按钮禁用 ----
            vm.ApiKey = string.Empty;
            vm.Endpoint = string.Empty;
            Pump();
            Save(dlg, "70-settings-required-empty");

            Console.WriteLine($"空值：保存可用={saveButton.IsEnabled} 测试可用={testButton.IsEnabled} " +
                              $"红框(APIKey)={IsRed(apiKeyBox)} 红框(接口)={IsRed(endpointBox)} " +
                              $"提示可见(APIKey)={vm.ApiKeyHasError} 提示可见(接口)={vm.EndpointHasError}");
            var emptyState = !saveButton.IsEnabled && !testButton.IsEnabled
                             && IsRed(apiKeyBox) && IsRed(endpointBox)
                             && vm.ApiKeyHasError && vm.EndpointHasError;

            // ---- 2) 只填 API Key：该框恢复，接口地址仍红，保存仍禁用 ----
            vm.ApiKey = "sk-test-123";
            Pump();
            Save(dlg, "71-settings-required-endpoint-only");
            Console.WriteLine($"仅填 Key：保存可用={saveButton.IsEnabled} 红框(APIKey)={IsRed(apiKeyBox)} " +
                              $"红框(接口)={IsRed(endpointBox)}");
            var partialState = !saveButton.IsEnabled && !IsRed(apiKeyBox) && IsRed(endpointBox);

            // ---- 3) 双向绑定：VM → 控件（外层 Password + 模板内部原生控件都要拿到） ----
            var innerBox = FindInnerPasswordBox(apiKeyBox);
            var vmToView = apiKeyBox.Password == "sk-test-123" && innerBox?.Password == "sk-test-123";

            // ---- 4) 双向绑定：控件 → VM（改内部原生 PasswordBox，等价于用户输入） ----
            if (innerBox is not null) innerBox.Password = "sk-typed-by-user";
            Pump();
            var viewToVm = vm.ApiKey == "sk-typed-by-user";
            Console.WriteLine($"双向绑定：VM→控件={vmToView} 控件→VM={viewToVm}（内部控件={innerBox is not null}）");

            // ---- 5) 补全接口地址：保存恢复可用 ----
            vm.Endpoint = "https://api.deepseek.com/v1";
            Pump();
            Save(dlg, "76-settings-save-enabled");
            Console.WriteLine($"补全后：保存可用={saveButton.IsEnabled} 红框(接口)={IsRed(endpointBox)}");
            var filledState = saveButton.IsEnabled && !IsRed(endpointBox);

            // ---- 6) 点保存（走真实命令）：配置落盘 ----
            InvokeButton(saveButton);
            Pump();
            var saved = File.ReadAllText(StoragePaths.ConfigPath).Contains("sk-typed-by-user");
            Console.WriteLine($"保存落盘={saved} 窗口已关闭={!dlg.IsVisible}");

            var pass = emptyState && partialState && vmToView && viewToVm && filledState && saved;
            Console.WriteLine(pass ? "SETTINGS-MVV PASS" : "SETTINGS-MVV FAIL");
            if (dlg.IsVisible) dlg.Close();
        }
        finally
        {
            RestoreFile(StoragePaths.ConfigPath, configBackup);
        }
    }

    /// <summary>在 HC PasswordBox 的模板里找到真正承载输入的原生 PasswordBox。</summary>
    private static System.Windows.Controls.PasswordBox? FindInnerPasswordBox(Control host)
    {
        host.ApplyTemplate();
        return Descendants(host).OfType<System.Windows.Controls.PasswordBox>().FirstOrDefault();
    }

    // ---------------- 技能编辑：必填红框 + 保存按钮禁用 ----------------

    private static void ProbeSkillEditorDialog()
    {
        var skill = new Skill();
        var editor = new SkillEditorDialog(skill) { Left = -4000, Top = -4000, ShowActivated = false };
        editor.Show();
        Pump();

        var vm = (SkillEditorDialogViewModel)editor.DataContext;
        var nameBox = (TextBox)editor.FindName("NameBox")!;
        var descBox = (TextBox)editor.FindName("DescBox")!;
        var promptBox = (TextBox)editor.FindName("PromptBox")!;
        var saveButton = Buttons(editor).First(b => (b.Content as string) == "保存");

        // ---- 1) 三个字段清空：立即红框 + 保存禁用 ----
        vm.Name = string.Empty;
        vm.Description = string.Empty;
        vm.Prompt = string.Empty;
        Pump();
        Save(editor, "72-skill-editor-required-empty");

        Console.WriteLine($"空值：保存可用={saveButton.IsEnabled} " +
                          $"红框={IsRed(nameBox)}/{IsRed(descBox)}/{IsRed(promptBox)}");
        var emptyState = !saveButton.IsEnabled && IsRed(nameBox) && IsRed(descBox) && IsRed(promptBox);

        // ---- 2) 只填名称：名称恢复，保存仍禁用 ----
        vm.Name = "翻译助手";
        Pump();
        Save(editor, "73-skill-editor-required-partial");
        Console.WriteLine($"仅填名称：保存可用={saveButton.IsEnabled} " +
                          $"红框={IsRed(nameBox)}/{IsRed(descBox)}/{IsRed(promptBox)}");
        var partialState = !saveButton.IsEnabled && !IsRed(nameBox) && IsRed(descBox) && IsRed(promptBox);

        // ---- 3) 全部填满：保存恢复可用 ----
        vm.Description = "中英互译，保持原文风格";
        vm.Prompt = "你是一名专业翻译。";
        Pump();
        Save(editor, "77-skill-editor-save-enabled");
        Console.WriteLine($"全部填满：保存可用={saveButton.IsEnabled}");
        var filledState = saveButton.IsEnabled;

        // ---- 4) 走真实命令保存：写回 Skill 模型 ----
        InvokeButton(saveButton);
        Pump();
        var written = skill.Name == "翻译助手" && skill.Description == "中英互译，保持原文风格";
        Console.WriteLine($"保存写回模型：{written}（Name=\"{skill.Name}\"）");

        var pass = emptyState && partialState && filledState && written;
        Console.WriteLine(pass ? "SKILL-EDITOR-MVV PASS" : "SKILL-EDITOR-MVV FAIL");
        if (editor.IsVisible) editor.Close();
    }

    // ---------------- 技能管理：列表绑定 + 新增/取消链路 ----------------

    private static void ProbeSkillsDialog()
    {
        var skillsBackup = BackupFile(StoragePaths.SkillsPath);

        try
        {
            var store = new SkillStore();
            store.Load();

            var dlg = new SkillsDialog(store) { Left = -4000, Top = -4000, ShowActivated = false };
            dlg.Show();
            Pump();

            var vm = (SkillsDialogViewModel)dlg.DataContext;
            var list = (ListBox)dlg.FindName("SkillList")!;
            var addButton = Buttons(dlg).First(b => (b.Content as string) == "新建技能");
            var editButton = Buttons(dlg).First(b => (b.Content as string) == "编辑");
            var deleteButton = Buttons(dlg).First(b => (b.Content as string) == "删除");

            Console.WriteLine($"列表绑定：ItemsSource 同一集合={ReferenceEquals(list.ItemsSource, vm.Skills)} " +
                              $"条目数={vm.Skills.Count} 存储条目数={store.Skills.Count} 默认选中={vm.SelectedSkill?.Name}");
            var listBound = ReferenceEquals(list.ItemsSource, vm.Skills)
                            && vm.Skills.Count == store.Skills.Count
                            && vm.SelectedSkill is not null;

            // 选中项为空 → 编辑/删除禁用
            vm.SelectedSkill = null;
            Pump();
            var disabledWithoutSelection = !editButton.IsEnabled && !deleteButton.IsEnabled;
            vm.SelectedSkill = vm.Skills.FirstOrDefault();
            Pump();
            var enabledWithSelection = editButton.IsEnabled && deleteButton.IsEnabled;
            Console.WriteLine($"选中态：无选中时编辑/删除禁用={disabledWithoutSelection}，" +
                              $"有选中时可用={enabledWithSelection}");

            // 新增 → 在弹出的编辑器里点「取消」：不应新增
            var countBefore = vm.Skills.Count;
            QueueChildAction<SkillEditorDialog>(dlg, ed =>
            {
                ((SkillEditorDialogViewModel)ed.DataContext).CancelCommand.Execute(null);
            });
            InvokeButton(addButton);
            PumpFor(300);
            var cancelKeptCount = vm.Skills.Count == countBefore;

            // 新增 → 填好后点「保存」：应新增并落盘
            QueueChildAction<SkillEditorDialog>(dlg, ed =>
            {
                var evm = (SkillEditorDialogViewModel)ed.DataContext;
                evm.Name = "探针技能";
                evm.Description = "由离屏探针创建";
                evm.Prompt = "你是一个测试用技能。";
                Pump();
                evm.SaveCommand.Execute(null);
            });
            InvokeButton(addButton);
            PumpFor(300);
            var added = vm.Skills.Count == countBefore + 1 && vm.Skills.Any(s => s.Name == "探针技能");
            var persisted = File.ReadAllText(StoragePaths.SkillsPath).Contains("探针技能");
            Console.WriteLine($"取消新增：条目数不变={cancelKeptCount}；保存新增：{added} 落盘={persisted}");

            Save(dlg, "83-skills-dialog");

            var pass = listBound && disabledWithoutSelection && enabledWithSelection
                       && cancelKeptCount && added && persisted;
            Console.WriteLine(pass ? "SKILLS-DIALOG-MVV PASS" : "SKILLS-DIALOG-MVV FAIL");
            if (dlg.IsVisible) dlg.Close();
        }
        finally
        {
            RestoreFile(StoragePaths.SkillsPath, skillsBackup);
        }
    }

    // ---------------- 消息区：滚轮是否带动内容滚动 ----------------

    private static void ProbeChatScroll()
    {
        var backup = BackupConversations();

        var settings = new SettingsStore();
        settings.Load();
        var skills = new SkillStore();
        skills.Load();
        var conversations = new ConversationStore();
        conversations.Load();
        var agent = new AgentService(settings);

        try
        {
            var vm = new MainViewModel(settings, skills, conversations, agent);
            var win = new MainWindow { DataContext = vm, Left = -4000, Top = -4000, ShowActivated = false };
            win.Show();
            Pump();

            vm.NewConversationCommand.Execute(null);
            Pump();
            var conv = vm.CurrentConversation!;
            conv.Messages.Clear();
            for (var i = 1; i <= 20; i++)
            {
                conv.Messages.Add(new MessageViewModel("user",
                    $"第 {i} 条用户消息：这是一段用来把消息区撑出滚动条的文字。"));
                conv.Messages.Add(new MessageViewModel("assistant",
                    $"### 第 {i} 条回复\n\n这是一段 Markdown 回复，包含 **粗体**、`代码` 与列表：\n\n" +
                    "- 第一点说明\n- 第二点说明\n- 第三点说明\n"));
            }
            Pump();
            PumpFor(400);

            var sv = (ScrollViewer)win.FindName("ChatScrollViewer")!;
            Console.WriteLine($"消息数={conv.Messages.Count} 视口高={sv.ViewportHeight:F0} " +
                              $"可滚动高度={sv.ScrollableHeight:F0} 当前偏移={sv.VerticalOffset:F0}");
            var scrollable = sv.ScrollableHeight > 0;

            sv.ScrollToTop();
            Pump();
            var start = sv.VerticalOffset;

            // 鼠标悬停在「助手气泡里的 Markdown 视图」上滚动（这是之前失效的场景）
            var mdView = Descendants(sv).FirstOrDefault(d => d.GetType().Name == "MarkdownScrollViewer");
            var target = mdView as UIElement ?? Descendants(sv).OfType<TextBlock>().FirstOrDefault();
            if (target is null)
            {
                Console.WriteLine("未找到可滚动的目标元素");
                win.Close();
                return;
            }

            RaiseWheel(target, -120);
            Pump();
            var afterAssistant = sv.VerticalOffset;
            Console.WriteLine($"悬停在助手消息上滚动：{start:F0} → {afterAssistant:F0}（目标={target.GetType().Name}）");

            RaiseWheel(target, -120);
            Pump();
            var afterSecond = sv.VerticalOffset;
            Console.WriteLine($"再滚动一格：{afterAssistant:F0} → {afterSecond:F0}");

            // 鼠标悬停在用户气泡（纯文本）上滚动
            var userText = Descendants(sv).OfType<TextBlock>()
                .FirstOrDefault(t => t.Text != null && t.Text.StartsWith("第 1 条用户消息"));
            var userBefore = sv.VerticalOffset;
            if (userText is not null) RaiseWheel(userText, -120);
            Pump();
            Console.WriteLine($"悬停在用户消息上滚动：{userBefore:F0} → {sv.VerticalOffset:F0} " +
                              $"（目标={(userText is null ? "未找到" : "TextBlock")}）");

            Save(win, "84-chat-scroll");

            var pass = scrollable && afterAssistant > start && afterSecond > afterAssistant;
            Console.WriteLine(pass ? "CHAT-SCROLL PASS" : "CHAT-SCROLL FAIL");

            win.Close();
        }
        finally
        {
            RestoreConversations(backup);
        }
    }

    /// <summary>派发一次鼠标滚轮（Preview 隧道阶段，与真实输入路径一致）。</summary>
    private static void RaiseWheel(UIElement target, int delta)
    {
        var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, delta)
        {
            RoutedEvent = UIElement.PreviewMouseWheelEvent,
        };
        target.RaiseEvent(args);
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
            RenderElement(card, "81-avatar-menu");

        popup.IsOpen = false;
        win.Close();
    }

    // ---------------- 会话列表：… 菜单 / 重命名 / 删除 ----------------

    private static void ProbeConversationMenu()
    {
        var backup = BackupConversations();
        var settings = new SettingsStore();
        settings.Load();
        var skills = new SkillStore();
        skills.Load();
        var conversations = new ConversationStore();
        conversations.Load();
        var agent = new AgentService(settings);

        try
        {
            var vm = new MainViewModel(settings, skills, conversations, agent);
            var win = new MainWindow { DataContext = vm, Left = -4000, Top = -4000, ShowActivated = false };
            win.Show();
            Pump();

            // 造两条临时会话用于删除验证
            vm.NewConversationCommand.Execute(null);
            Pump();
            vm.NewConversationCommand.Execute(null);
            Pump();
            var total = vm.Conversations.Count;
            Console.WriteLine($"会话总数 = {total}");

            // 展开抽屉
            ((FrameworkElement)win.FindName("SidebarLayer")!).Visibility = Visibility.Visible;
            Pump();
            Save(win, "90-drawer-with-more");

            var listBox = Descendants(win).OfType<ListBox>().FirstOrDefault();
            if (listBox is null) { Console.WriteLine("未找到会话列表"); win.Close(); return; }

            var moreButtons = Descendants(listBox).OfType<Button>()
                .Where(b => (b.ToolTip as string) == "更多操作").ToList();
            Console.WriteLine($"「…」按钮数量 = {moreButtons.Count}（会话 {total} 条）");

            // 点第 2 条（不要动当前会话）的「…」
            var target = moreButtons.Count > 1 ? moreButtons[1] : moreButtons[0];
            var targetConv = (ConversationViewModel)target.DataContext;
            var currentBefore = vm.CurrentConversation;
            RaiseMouseButton(target, UIElement.PreviewMouseLeftButtonUpEvent);
            Pump();

            var menu = (Popup)win.FindName("ConversationMenu")!;
            Console.WriteLine($"下拉菜单 IsOpen = {menu.IsOpen}；当前会话未变 = {ReferenceEquals(vm.CurrentConversation, currentBefore)}");

            // 渲染下拉菜单内容
            if (menu.Child is FrameworkElement card)
                RenderElement(card, "91-conversation-menu");

            // ---- 重命名：Enter 确认 ----
            var renameItem = Descendants(menu.Child).OfType<Button>()
                .FirstOrDefault(b => Descendants(b).OfType<TextBlock>().Any(t => t.Text == "重命名"));
            if (renameItem is not null)
            {
                renameItem.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Pump();
            }
            var renameBox = Descendants(listBox).OfType<TextBox>()
                .FirstOrDefault(t => ReferenceEquals(t.DataContext, targetConv));
            var titleBefore = targetConv.Title;
            Console.WriteLine($"进入重命名：IsRenaming={targetConv.IsRenaming} 文本框可见={renameBox?.IsVisible} " +
                              $"全选={renameBox?.SelectionStart}/{renameBox?.SelectionLength}（标题长度 {titleBefore.Length}）");
            Save(win, "92-rename-inline");

            const string newTitle = "重命名后的会话标题";
            if (renameBox is not null)
            {
                targetConv.RenameDraft = newTitle;
                Pump();
                // 模拟 Enter（走 PreviewKeyDown，与真实按键同一条事件路径）
                RaiseKey(renameBox, Key.Enter);
                Pump();
            }
            var renamed = targetConv.Title == newTitle && !targetConv.IsRenaming;
            var persisted = File.ReadAllText(StoragePaths.ConversationsPath).Contains(newTitle);
            Console.WriteLine($"Enter 重命名：Title=\"{targetConv.Title}\" 落盘={persisted}");
            Save(win, "94-after-rename");

            // ---- 删除：先取消（应什么都不做） ----
            RaiseMouseButton(target, UIElement.PreviewMouseLeftButtonUpEvent);
            Pump();
            var deleteItem = Descendants(menu.Child).OfType<Button>()
                .FirstOrDefault(b => Descendants(b).OfType<TextBlock>().Any(t => t.Text == "删除对话"));

            using (var watchdog = new Timer(_ => win.Dispatcher.BeginInvoke(new Action(() =>
                   {
                       foreach (var w in Application.Current.Windows.OfType<ConfirmDialog>().ToList())
                           w.Close();
                   })), null, 8000, Timeout.Infinite))
            {
                QueueModalAction(win, "取消", dlg => Save(dlg, "93-confirm-delete"));
                deleteItem?.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Pump();
            }
            var afterCancel = vm.Conversations.Count;
            var cancelKept = vm.Conversations.Contains(targetConv);
            Console.WriteLine($"取消删除：剩余 {afterCancel} 条，目标会话仍存在 = {cancelKept}");

            // ---- 删除：确认（应真删） ----
            RaiseMouseButton(target, UIElement.PreviewMouseLeftButtonUpEvent);
            Pump();
            using (var watchdog = new Timer(_ => win.Dispatcher.BeginInvoke(new Action(() =>
                   {
                       foreach (var w in Application.Current.Windows.OfType<ConfirmDialog>().ToList())
                           w.Close();
                   })), null, 8000, Timeout.Infinite))
            {
                QueueModalAction(win, "删除", null);
                deleteItem?.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Pump();
            }
            var afterConfirm = vm.Conversations.Count;
            var confirmed = !vm.Conversations.Contains(targetConv);
            Console.WriteLine($"确认删除：剩余 {afterConfirm} 条，目标会话已移除 = {confirmed}");
            Save(win, "95-after-delete");

            var pass = moreButtons.Count == total
                       && renamed && persisted
                       && cancelKept && afterCancel == total
                       && confirmed && afterConfirm == total - 1;
            Console.WriteLine(pass ? "CONVERSATION-MENU PASS" : "CONVERSATION-MENU FAIL");

            win.Close();
        }
        finally
        {
            RestoreConversations(backup);
        }
    }

    /// <summary>在模态对话框弹出后于其内部执行动作（点击按钮，可选先截图）。</summary>
    private static void QueueModalAction(Window owner, string buttonContent, Action<ConfirmDialog>? before)
    {
        owner.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        {
            var dlg = Application.Current.Windows.OfType<ConfirmDialog>().FirstOrDefault();
            if (dlg is null)
            {
                Console.WriteLine("未找到确认对话框");
                return;
            }
            Console.WriteLine($"确认对话框：标题=\"{dlg.Title}\" 按钮=[{string.Join("/", Buttons(dlg).Select(b => b.Content))}]");
            before?.Invoke(dlg);
            Pump();
            RaiseClick(dlg, buttonContent);
        }));
    }

    /// <summary>
    /// 在模态子窗口弹出后于其内部执行动作（供 ShowDialog 阻塞期间调用）。
    /// 用 DispatcherTimer 反复探测，规避「队列动作先于窗口注册执行」的时序问题。
    /// </summary>
    private static void QueueChildAction<T>(Window owner, Action<T> action) where T : Window
    {
        var timer = new DispatcherTimer(DispatcherPriority.Normal, owner.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };
        var attempts = 0;
        timer.Tick += (s, _) =>
        {
            var child = Application.Current.Windows.OfType<T>().FirstOrDefault(w => w.IsVisible);
            if (child is null)
            {
                if (++attempts > 100)
                {
                    timer.Stop();
                    Console.WriteLine($"未找到子窗口 {typeof(T).Name}（超时，已强制关闭）");
                    foreach (var w in Application.Current.Windows.OfType<T>().ToList()) w.Close();
                }
                return;
            }
            timer.Stop();
            action(child);
        };
        timer.Start();
    }

    private static string? BackupConversations() => BackupFile(StoragePaths.ConversationsPath);

    private static void RestoreConversations(string? backup) => RestoreFile(StoragePaths.ConversationsPath, backup);

    private static string? BackupFile(string path)
    {
        if (!File.Exists(path)) return null;
        var backup = Path.Combine(Path.GetTempPath(), "mega-probe-" + Path.GetFileName(path) + ".bak");
        File.Copy(path, backup, true);
        return backup;
    }

    private static void RestoreFile(string path, string? backup)
    {
        if (backup is null) return;
        File.Copy(backup, path, true);
        Console.WriteLine($"{Path.GetFileName(path)} 已还原");
    }

    private static void RaiseMouseButton(UIElement target, RoutedEvent routedEvent)
    {
        target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = routedEvent,
        });
    }

    /// <summary>把任意元素渲染为 PNG（用于 Popup 内容等不在窗口可视树里的部分）。</summary>
    private static void RenderElement(FrameworkElement element, string name)
    {
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        element.Arrange(new Rect(new Point(0, 0), element.DesiredSize));
        element.UpdateLayout();

        var bmp = new RenderTargetBitmap(
            (int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight),
            96, 96, PixelFormats.Pbgra32);
        bmp.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        var path = Path.Combine(ShotDir, name + ".png");
        using var fs = File.Create(path);
        encoder.Save(fs);
        Console.WriteLine($"rendered: {path}");
    }

    // ---------------- 工具方法 ----------------

    /// <summary>判断控件当前是否为红色边框（必填校验生效）。直接与 DangerBrush 资源比对。</summary>
    private static bool IsRed(Control control)
    {
        if (Application.Current?.TryFindResource("DangerBrush") is Brush danger
            && ReferenceEquals(control.BorderBrush, danger)) return true;

        return control.BorderBrush is SolidColorBrush b
               && b.Color.R > 0x60 && b.Color.G < 0x90 && b.Color.B < 0x90;
    }

    /// <summary>通过自动化对等体点击按钮：会走 OnClick → 命令执行（与真实点击一致）。</summary>
    private static void InvokeButton(Button button)
    {
        var peer = new ButtonAutomationPeer(button);
        if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
            invoke.Invoke();
        else
            Console.WriteLine("按钮不支持 Invoke");
    }

    /// <summary>直接触发 Click 事件（仅适用于使用 Click 处理器而非命令的按钮，如确认对话框）。</summary>
    private static void RaiseClick(DependencyObject root, string content)
    {
        var btn = Buttons(root).FirstOrDefault(b => (b.Content as string) == content);
        if (btn is null)
        {
            Console.WriteLine($"未找到按钮：{content}");
            return;
        }
        btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

    private static IEnumerable<Button> Buttons(DependencyObject root) => Descendants(root).OfType<Button>();

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
