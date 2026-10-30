using Godot;
using Godot.Collections;

[GlobalClass]
public partial class NpcTalkHandConfig : NpcTalkBaseConfig
{
	[Export(PropertyHint.None, "")]
	public PackedScene handScene;

	[Export(PropertyHint.None, "")]
	public PackedScene shoulderScene;

	[Export(PropertyHint.None, "")]
	public PackedScene shoulder2Scene;

	[Export(PropertyHint.None, "")]
	public PackedScene headScene;

	public override void Init(Dictionary data)
	{
		base.Init(data);
		Array array = data["Arg"].AsGodotArray();
		if (array.Count >= 1)
		{
			handScene = GD.Load<PackedScene>(array[0].AsString());
		}
		if (array.Count >= 2)
		{
			shoulderScene = GD.Load<PackedScene>(array[1].AsString());
		}
		if (array.Count >= 3)
		{
			shoulder2Scene = GD.Load<PackedScene>(array[2].AsString());
		}
		if (array.Count >= 4)
		{
			headScene = GD.Load<PackedScene>(array[3].AsString());
		}
	}
}
