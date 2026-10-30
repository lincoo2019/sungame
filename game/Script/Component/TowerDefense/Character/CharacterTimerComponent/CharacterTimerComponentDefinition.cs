using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class CharacterTimerComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public Dictionary timerDictionary { get; set; } = new Dictionary();

	[Export(PropertyHint.None, "")]
	public double timeScale { get; set; } = 1.0;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new CharacterTimerComponent();
	}
}
