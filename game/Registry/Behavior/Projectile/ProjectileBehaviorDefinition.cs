using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ProjectileBehaviorDefinition : BehaviorDefinitionBase
{
	public ProjectileBehaviorDefinition()
	{
		base.BehaviorTypeId = new StringName("projectile");
	}

	public virtual ProjectileBehaviorKernel CreateBulletFieldKernel(int capacity)
	{
		return null;
	}

	public static Array<ProjectileBehaviorDefinition> ReadLegacyArray(Variant value)
	{
		Array<ProjectileBehaviorDefinition> array = new Array<ProjectileBehaviorDefinition>();
		if (value.VariantType != Variant.Type.Array)
		{
			return array;
		}
		foreach (Variant item2 in value.AsGodotArray())
		{
			if (item2.VariantType == Variant.Type.Object && item2.AsGodotObject() is ProjectileBehaviorDefinition item)
			{
				array.Add(item);
			}
		}
		return array;
	}
}
