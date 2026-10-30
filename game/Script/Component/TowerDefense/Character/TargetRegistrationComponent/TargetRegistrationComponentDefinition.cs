using Godot;

[GlobalClass]
[Tool]
public partial class TargetRegistrationComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public bool allLineCheck;

	[Export(PropertyHint.None, "")]
	public bool canProjectileCheck = true;

	[Export(PropertyHint.None, "")]
	public bool canCarry = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new TargetRegistrationComponent();
	}
}
