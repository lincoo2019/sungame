using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class BlockComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[Export(PropertyHint.None, "")]
	public Array<string> blockType { get; set; } = new Array<string> { "General" };

	[Export(PropertyHint.None, "")]
	public Vector2 extendGrid { get; set; } = Vector2.One;

	[Export(PropertyHint.None, "")]
	public AabbShape2DResource checkShape { get; set; }

	[Export(PropertyHint.None, "")]
	public bool checkLadder { get; set; }

	[Export(PropertyHint.None, "")]
	public bool reboundProjectile { get; set; }

	[Export(PropertyHint.None, "")]
	public AabbShape2DResource reboundProjectileShape { get; set; }

	[ExportGroup("Performance", "")]
	[Export(PropertyHint.Range, "1,60,1")]
	public int characterCheckEveryPhysicsFrames { get; set; } = 1;

	public int CollisionPreviewShapeCount => (GodotObject.IsInstanceValid(checkShape) ? 1 : 0) + ((reboundProjectile && GodotObject.IsInstanceValid(reboundProjectileShape)) ? 1 : 0);

	public int CollisionPreviewRayCount => 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index < 0)
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(checkShape))
		{
			if (index == 0)
			{
				return checkShape;
			}
			index--;
		}
		if (!reboundProjectile || index != 0 || !GodotObject.IsInstanceValid(reboundProjectileShape))
		{
			return null;
		}
		return reboundProjectileShape;
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new BlockComponent();
	}
}
