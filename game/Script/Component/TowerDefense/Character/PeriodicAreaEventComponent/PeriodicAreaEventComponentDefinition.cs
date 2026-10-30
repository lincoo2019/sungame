using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class PeriodicAreaEventComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[Export(PropertyHint.Enum, "StaticTime,Dps")]
	public string attackMethod { get; set; } = "StaticTime";

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList { get; set; } = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.Range, "0.001,60,0.001,or_greater")]
	public float staticTime { get; set; } = 1f;

	[Export(PropertyHint.Range, "0.001,10,0.001,or_greater")]
	public float dpsEffectInterval { get; set; } = 0.1f;

	[Export(PropertyHint.Range, "1,16,1")]
	public int maxIntervalTriggersPerFrame { get; set; } = 1;

	[Export(PropertyHint.Enum, "Particles,Sprite,Disabled")]
	public string targetEffectType { get; set; } = "Particles";

	[Export(PropertyHint.None, "")]
	public PackedScene targetEffectScene { get; set; }

	[Export(PropertyHint.None, "")]
	public PeriodicAreaEventComponent.TargetEffectSpawnPolicy targetEffectSpawnPolicy { get; set; }

	[Export(PropertyHint.None, "")]
	public AabbShape2DResource checkShape { get; set; }

	[Export(PropertyHint.None, "")]
	public bool checkAllLine { get; set; }

	[Export(PropertyHint.None, "")]
	public bool skipVases { get; set; } = true;

	public int CollisionPreviewShapeCount => GodotObject.IsInstanceValid(checkShape) ? 1 : 0;

	public int CollisionPreviewRayCount => 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index != 0)
		{
			return null;
		}
		return checkShape;
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new PeriodicAreaEventComponent();
	}
}
