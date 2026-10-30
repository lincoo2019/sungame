using Godot;

[GlobalClass]
[Tool]
public partial class HypnosesComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public bool playHypnosesAudio = true;

	[Export(PropertyHint.None, "")]
	public string hypnosesAudio = "Floop";

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new HypnosesComponent();
	}
}
