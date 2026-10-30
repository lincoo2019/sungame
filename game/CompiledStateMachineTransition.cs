using System;
using Godot;

public readonly struct CompiledStateMachineTransition(int sourceIndex, int targetIndex, StateMachineTriggerKind triggerKind, StringName eventName, double delaySeconds, int priority, int declarationOrder, string stableId, CompiledStateMachineGuard guard, StateMachineExtensionProperty[] extensionProperties)
{
	public readonly int SourceIndex = sourceIndex;

	public readonly int TargetIndex = targetIndex;

	public readonly StateMachineTriggerKind TriggerKind = triggerKind;

	public readonly StringName EventName = eventName;

	public readonly double DelaySeconds = delaySeconds;

	public readonly int Priority = priority;

	public readonly int DeclarationOrder = declarationOrder;

	public readonly string StableId = stableId;

	public readonly CompiledStateMachineGuard Guard = guard;

	public readonly StateMachineExtensionProperty[] ExtensionProperties = extensionProperties ?? Array.Empty<StateMachineExtensionProperty>();

	public bool TryGetExtensionProperty(StringName propertyName, out Variant value)
	{
		return StateMachineExtensionProperties.TryGet(ExtensionProperties, propertyName, out value);
	}
}
