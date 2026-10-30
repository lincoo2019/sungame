using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class StateMachineGuardDefinition : Resource
{
	[Export(PropertyHint.None, "")]
	public StateMachineGuardKind Kind { get; set; }

	[Export(PropertyHint.None, "")]
	public StringName ComparedProperty { get; set; } = new StringName();

	[Export(PropertyHint.None, "")]
	public StateMachineComparisonOperator Operator { get; set; }

	[Export(PropertyHint.None, "")]
	public Variant ExpectedValue { get; set; }

	[Export(PropertyHint.None, "")]
	public bool Negate { get; set; }

	[Export(PropertyHint.None, "")]
	public StringName CallbackKey { get; set; } = new StringName();

	[Export(PropertyHint.None, "")]
	public Array<Resource> Children { get; set; } = new Array<Resource>();
}
