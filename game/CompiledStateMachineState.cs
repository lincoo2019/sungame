using System;
using Godot;

public readonly struct CompiledStateMachineState(string stableId, StringName displayName, StateMachineStateKind kind, int parentIndex, int initialChildIndex, int depth, StateMachineProcessFlags processFlags, StringName callbackKey, StringName enterCallbackKey, StringName exitCallbackKey, StringName processCallbackKey, StringName physicsProcessCallbackKey, StateMachineExtensionProperty[] extensionProperties)
{
	public readonly string StableId = stableId;

	public readonly StringName DisplayName = displayName;

	public readonly StateMachineStateKind Kind = kind;

	public readonly int ParentIndex = parentIndex;

	public readonly int InitialChildIndex = initialChildIndex;

	public readonly int Depth = depth;

	public readonly StateMachineProcessFlags ProcessFlags = processFlags;

	public readonly StringName CallbackKey = callbackKey;

	public readonly StringName EnterCallbackKey = enterCallbackKey;

	public readonly StringName ExitCallbackKey = exitCallbackKey;

	public readonly StringName ProcessCallbackKey = processCallbackKey;

	public readonly StringName PhysicsProcessCallbackKey = physicsProcessCallbackKey;

	public readonly StateMachineExtensionProperty[] ExtensionProperties = extensionProperties ?? Array.Empty<StateMachineExtensionProperty>();

	public StringName GetLifecycleCallbackKey(StateMachineCallbackPhase phase)
	{
		StringName stringName = phase switch
		{
			StateMachineCallbackPhase.Enter => EnterCallbackKey, 
			StateMachineCallbackPhase.Exit => ExitCallbackKey, 
			StateMachineCallbackPhase.Process => ProcessCallbackKey, 
			StateMachineCallbackPhase.PhysicsProcess => PhysicsProcessCallbackKey, 
			_ => new StringName(), 
		};
		if (!string.IsNullOrWhiteSpace(stringName.ToString()))
		{
			return stringName;
		}
		return CallbackKey;
	}

	public bool TryGetExtensionProperty(StringName propertyName, out Variant value)
	{
		return StateMachineExtensionProperties.TryGet(ExtensionProperties, propertyName, out value);
	}
}
