using Godot;

[GlobalClass]
[Tool]
public partial class BlowBackComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "0,30,0.05")]
	public double defaultDuration = 1.0;

	[Export(PropertyHint.None, "")]
	public bool allowStacking = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new BlowBackComponent();
	}
}
