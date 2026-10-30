public partial class CharacterComponentCreationPlanProbeDefinition : CharacterComponentDefinition
{
	public override CharacterComponentRuntime CreateRuntime()
	{
		return new CharacterComponentCreationPlanProbeRuntime();
	}
}
