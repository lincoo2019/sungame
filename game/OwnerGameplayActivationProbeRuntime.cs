using System;

public sealed class OwnerGameplayActivationProbeRuntime : CharacterComponentRuntime
{
	internal override bool HasOwnerGameplayActivationWork => true;

	public static int Sequence { get; private set; }

	public Action Callback { get; set; }

	public int OrderId { get; set; }

	public int CallCount { get; private set; }

	public int LastSequence { get; private set; }

	public static void ResetSequence()
	{
		Sequence = 0;
	}

	protected override void OnOwnerGameplayActivated()
	{
		CallCount++;
		LastSequence = ++Sequence;
		Callback?.Invoke();
	}
}
