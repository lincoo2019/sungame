using Godot;

[GlobalClass]
[Tool]
public partial class BlockDiggerComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[Export(PropertyHint.None, "")]
	public AabbShape2DResource checkShape { get; set; }

	[Export(PropertyHint.None, "")]
	public bool blockSameCamp { get; set; }

	[ExportGroup("Performance", "")]
	[Export(PropertyHint.Range, "1,60,1")]
	public int characterCheckEveryPhysicsFrames { get; set; } = 1;

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
		return new BlockDiggerComponent();
	}
}
