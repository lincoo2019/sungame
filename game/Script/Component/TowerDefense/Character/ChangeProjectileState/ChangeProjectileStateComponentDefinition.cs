using Godot;

[GlobalClass]
[Tool]
public partial class ChangeProjectileStateComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[Export(PropertyHint.None, "")]
	public AabbShape2DResource checkShape { get; set; }

	[Export(PropertyHint.None, "")]
	public ChangeProjectileStateComponent.ProjectileCampFilter campFilter { get; set; }

	[Export(PropertyHint.None, "")]
	public bool followCamp { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool replaceDamageFlags { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool replaceFireMethodFlags { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool enableTrackingFromFireMethod { get; set; } = true;

	[Export(PropertyHint.Flags, "HITBODY,HITSHIELD,FIRE,ICE,EXPLOSION,CHILD,PIERCE,SMASH")]
	public int damageFlags { get; set; } = 3;

	[Export(PropertyHint.Flags, "SHOOTER,CATAPULT,THROW,TRACK")]
	public int fireMethodFlags { get; set; } = 1;

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
		return new ChangeProjectileStateComponent();
	}
}
