using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseGraveStoneDefault : TowerDefenseGravestone
{
	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		Dictionary clips = base.sprite.flashAnimeData.clips;
		Array<string> array = new Array<string>();
		foreach (Variant key in clips.Keys)
		{
			array.Add(key.AsString());
		}
		idleAnimeClip = string.Join("&", array);
		HitBoxDestroy();
	}

	public override void IdleEntered()
	{
		if (!over)
		{
			over = true;
			base.IdleEntered();
		}
	}
}
