using Godot;

namespace PVZHE.ModEditor.GUI;

public partial class XWAboutDialog : AcceptDialog
{
	private RichTextLabel _content;

	public override void _Ready()
	{
		_content = GetNode<RichTextLabel>("%AboutContent");
		_content.Text = "[center][b]PVZ Mod 编辑器[/b][/center]\n\n版本：v0.1\n基于 Godot 4 + C#\n移植自虚无编辑器 (XWEditor)\n\n用于创建和管理植物大战僵尸杂交版 Mod。";
	}
}
