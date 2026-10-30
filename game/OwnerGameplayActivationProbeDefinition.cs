public partial class OwnerGameplayActivationProbeDefinition : CharacterComponentDefinition
{
	public int OrderId { get; set; }

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new OwnerGameplayActivationProbeRuntime
		{
			OrderId = OrderId
		};
	}
}
