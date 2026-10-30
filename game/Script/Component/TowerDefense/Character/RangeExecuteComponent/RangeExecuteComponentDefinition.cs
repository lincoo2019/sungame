using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class RangeExecuteComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.RANGE_TYPE rangeType { get; set; } = TowerDefenseEnum.RANGE_TYPE.AREA;

	[Export(PropertyHint.None, "")]
	public AabbShape2DResource executeShape { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList { get; set; } = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public bool filterGravestones { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool filterVases { get; set; } = true;

	[Export(PropertyHint.Range, "-1,1024,1")]
	public int maxTargets { get; set; } = -1;

	[Export(PropertyHint.None, "")]
	public Vector2 eventOffset { get; set; } = Vector2.Zero;

	public int CollisionPreviewShapeCount => GodotObject.IsInstanceValid(executeShape) ? 1 : 0;

	public int CollisionPreviewRayCount => 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index != 0)
		{
			return null;
		}
		return executeShape;
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new RangeExecuteComponent();
	}
}
