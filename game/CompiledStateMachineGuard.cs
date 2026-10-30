using System;
using Godot;

public readonly struct CompiledStateMachineGuard(bool isDefined, StateMachineGuardKind kind, StringName propertyName, StateMachineComparisonOperator comparisonOperator, Variant expectedValue, bool negate, StringName callbackKey, int callbackIndex, CompiledStateMachineGuard[] children = null)
{
	public readonly bool IsDefined = isDefined;

	public readonly StateMachineGuardKind Kind = kind;

	public readonly StringName PropertyName = propertyName;

	public readonly StateMachineComparisonOperator Operator = comparisonOperator;

	public readonly Variant ExpectedValue = expectedValue;

	public readonly bool Negate = negate;

	public readonly StringName CallbackKey = callbackKey;

	public readonly int CallbackIndex = callbackIndex;

	public readonly CompiledStateMachineGuard[] Children = children ?? Array.Empty<CompiledStateMachineGuard>();
}
