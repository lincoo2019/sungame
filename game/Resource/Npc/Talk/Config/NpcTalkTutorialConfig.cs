using Godot;
using Godot.Collections;

[GlobalClass]
public partial class NpcTalkTutorialConfig : NpcTalkBaseConfig
{
	[Export(PropertyHint.None, "")]
	public TutorialConfig tutorial;

	public override void Init(Dictionary data)
	{
		base.Init(data);
		Array array = data["Arg"].AsGodotArray();
		tutorial = GD.Load<TutorialConfig>(array[0].AsString());
	}
}
