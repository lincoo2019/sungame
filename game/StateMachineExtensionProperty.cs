using Godot;

public readonly struct StateMachineExtensionProperty(StringName name, Variant.Type declaredType, Variant value)
{
	public readonly StringName Name = name;

	public readonly Variant.Type DeclaredType = declaredType;

	public readonly Variant Value = value;
}
