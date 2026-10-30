using Godot;

[GlobalClass]
[Tool]
public partial class PlantAnimeComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public bool fallbackToIdle = true;

	[Export(PropertyHint.None, "")]
	public bool syncSpriteTimeScale = true;

	[Export(PropertyHint.None, "")]
	public bool loopAnimation;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new PlantAnimeComponent();
	}
}
