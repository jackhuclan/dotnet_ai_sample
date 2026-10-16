using MegaAgent.Core.Models;

namespace MegaAgent.App.ViewModels;

/// <summary>
/// View 层提供给 ViewModel 的对话框交互能力。
/// 让 ViewModel 只依赖抽象，而不直接引用具体的 Window 类型（保持 MVVM 分层），
/// 也便于在单元测试中替换为无 UI 的替身。
/// 实现见 <see cref="MegaAgent.App.DialogService"/>（由 DI 容器注入）。
/// </summary>
public interface IDialogService
{
    /// <summary>打开技能编辑器；返回 true 表示用户点了保存。</summary>
    bool ShowSkillEditor(Skill skill);

    /// <summary>显示二次确认；返回 true 表示用户点了确认。</summary>
    bool Confirm(string message, string title, string confirmText = "确定");

    /// <summary>显示信息提示（只有一个「确定」按钮）。</summary>
    void ShowInformation(string message, string title);

    /// <summary>打开「模型设置」；返回 true 表示用户保存了配置。</summary>
    bool ShowSettings();

    /// <summary>打开「技能管理」。</summary>
    bool ShowSkills();
}
