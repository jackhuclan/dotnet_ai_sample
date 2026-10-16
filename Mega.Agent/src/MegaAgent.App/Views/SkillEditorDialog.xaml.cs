using System.Windows;
using MegaAgent.Core.Models;

namespace MegaAgent.App;

public partial class SkillEditorDialog : Window
{
    private readonly Skill _skill;
    private readonly RequiredField _nameField;
    private readonly RequiredField _descField;
    private readonly RequiredField _promptField;

    public SkillEditorDialog(Skill skill)
    {
        InitializeComponent();
        _skill = skill;
        NameBox.Text = skill.Name;
        DescBox.Text = skill.Description;
        PromptBox.Text = skill.Prompt;

        // 三个输入框均为必填：为空时标红 + 显示「必填」提示
        _nameField = new RequiredField(NameBox, NameError, "技能名称", () => NameBox.Text);
        _descField = new RequiredField(DescBox, DescError, "技能描述", () => DescBox.Text);
        _promptField = new RequiredField(PromptBox, PromptError, "系统提示词", () => PromptBox.Text);

        NameBox.TextChanged += (_, _) => _nameField.Clear();
        DescBox.TextChanged += (_, _) => _descField.Clear();
        PromptBox.TextChanged += (_, _) => _promptField.Clear();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var nameOk = _nameField.Validate();
        var descOk = _descField.Validate();
        var promptOk = _promptField.Validate();

        if (!nameOk) NameBox.Focus();
        else if (!descOk) DescBox.Focus();
        else if (!promptOk) PromptBox.Focus();

        if (!(nameOk && descOk && promptOk)) return;

        _skill.Name = NameBox.Text.Trim();
        _skill.Description = DescBox.Text.Trim();
        _skill.Prompt = PromptBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
