using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MegaAgent.Core.Models;
using MegaAgent.Core.Services;

namespace MegaAgent.App;

public partial class SkillsDialog : Window
{
    private readonly SkillStore _store;
    private readonly ObservableCollection<Skill> _skills = new();

    public SkillsDialog(SkillStore store)
    {
        InitializeComponent();
        _store = store;
        foreach (var s in store.Skills)
            _skills.Add(s);
        SkillList.ItemsSource = _skills;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var skill = new Skill { Name = "新技能", Description = "", Prompt = "" };
        var editor = new SkillEditorDialog(skill) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            _skills.Add(skill);
            Persist();
            SkillList.SelectedItem = skill;
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (SkillList.SelectedItem is not Skill selected)
        {
            HandyControl.Controls.MessageBox.Show("请先选择一个技能。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var editor = new SkillEditorDialog(selected) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            var index = _skills.IndexOf(selected);
            _skills[index] = selected;
            Persist();
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SkillList.SelectedItem is not Skill selected)
        {
            HandyControl.Controls.MessageBox.Show("请先选择一个技能。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (HandyControl.Controls.MessageBox.Show($"确定删除技能「{selected.Name}」吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _skills.Remove(selected);
        Persist();
    }

    private void SkillList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SkillList.SelectedItem is Skill)
            EditButton_Click(sender, e);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Persist()
    {
        _store.Skills = _skills.ToList();
        _store.Save();
    }
}
