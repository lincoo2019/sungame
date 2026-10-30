using System;
using Godot;

public sealed partial class TowerDefenseCharacterSpriteGroup : Node2D
{
	public event Action TransformChanged;

	public override void _EnterTree()
	{
		base._EnterTree();
		SetNotifyTransform(enable: true);
		SetNotifyLocalTransform(enable: true);
	}

	public override void _ExitTree()
	{
		SetNotifyTransform(enable: false);
		SetNotifyLocalTransform(enable: false);
		base._ExitTree();
	}

	public override void _Notification(int what)
	{
		if ((long)what == 2000 || (long)what == 35)
		{
			this.TransformChanged?.Invoke();
		}
	}
}
