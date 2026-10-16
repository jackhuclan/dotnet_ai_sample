using MegaAgent.Core.Models;

namespace MegaAgent.App.ViewModels;

/// <summary>
/// View 层提供给 ViewModel 的对话框交互能力。
/// 让 ViewModel 只依赖抽象，而不直接引用具体的 Window 类型（保持 MVVM 分层）。
/// </summary>
public interface IDialogService
{
    /// <summary>打开技能编辑器；返回 true 表示用户点了保存。</summary>
    bool ShowSkillEditor(Skill skill);

    /// <summary>显示确认提示；返回 true 表示用户点了确认。</summary>
    bool Confirm(string message, string title);
}
