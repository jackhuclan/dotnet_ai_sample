using System.Windows;
using MegaAgent.App.ViewModels;
using MegaAgent.Core.Services;

namespace MegaAgent.App;

public partial class App : Application
{
    public static SettingsStore SettingsStore { get; private set; } = null!;
    public static SkillStore SkillStore { get; private set; } = null!;
    public static ConversationStore ConversationStore { get; private set; } = null!;
    public static AgentService AgentService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SettingsStore = new SettingsStore();
        SettingsStore.Load();
        SkillStore = new SkillStore();
        SkillStore.Load();
        ConversationStore = new ConversationStore();
        ConversationStore.Load();
        AgentService = new AgentService(SettingsStore);

        var main = new MainWindow
        {
            DataContext = new MainViewModel(SettingsStore, SkillStore, ConversationStore, AgentService),
        };
        MainWindow = main;
        main.Show();
    }
}
