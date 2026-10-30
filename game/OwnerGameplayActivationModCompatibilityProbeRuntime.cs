public sealed class OwnerGameplayActivationModCompatibilityProbeRuntime : CharacterComponentRuntime
{
	public int CallCount { get; private set; }

	protected override void OnOwnerGameplayActivated()
	{
		CallCount++;
	}
}
