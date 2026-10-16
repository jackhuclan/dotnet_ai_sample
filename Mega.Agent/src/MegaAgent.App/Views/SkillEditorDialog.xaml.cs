using System.Windows;
using MegaAgent.Core.Models;

namespace MegaAgent.App;

public partial class SkillEditorDialog : Window
{
    private readonly Skill _skill;

    public SkillEditorDialog(Skill skill)
    {
        InitializeComponent();
        _skill = skill;
        NameBox.Text = skill.Name;
        DescBox.Text = skill.Description;
        PromptBox.Text = skill.Prompt;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            HandyControl.Controls.MessageBox.Show("技能名称不能为空。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _skill.Name = NameBox.Text.Trim();
        _skill.Description = DescBox.Text.Trim();
        _skill.Prompt = PromptBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
