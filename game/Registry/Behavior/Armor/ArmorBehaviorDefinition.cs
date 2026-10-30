using Godot;

[Tool]
[GlobalClass]
public abstract partial class ArmorBehaviorDefinition : BehaviorDefinitionBase
{
	public ArmorBehaviorDefinition()
	{
		base.BehaviorTypeId = new StringName("armor");
	}

	public abstract ArmorBehaviorRuntime CreateRuntime();
}
