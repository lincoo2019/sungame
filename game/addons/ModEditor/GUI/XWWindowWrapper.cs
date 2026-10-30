using System;
using Godot;

namespace PVZHE.ModEditor.Layout;

public partial class XWWindowWrapper : Window
{
	private Control _content;

	public new event Action CloseRequested;

	public override void _Ready()
	{
		base.WrapControls = true;
		base.Transient = false;
		base.Borderless = false;
		base.Unresizable = false;
		base.MinSize = new Vector2I(320, 240);
	}

	public void SetContent(Control content)
	{
		_content = content;
		AddChild(content, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1006)
		{
			this.CloseRequested?.Invoke();
			QueueFree();
		}
	}
}
