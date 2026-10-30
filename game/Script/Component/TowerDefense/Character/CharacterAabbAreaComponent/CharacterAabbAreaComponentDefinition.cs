using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class CharacterAabbAreaComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[ExportGroup("Area", "")]
	[Export(PropertyHint.None, "")]
	public bool enabled { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Array<AabbShape2DResource> shapeResources { get; set; } = new Array<AabbShape2DResource>();

	[Export(PropertyHint.Layers2DPhysics, "")]
	public uint collisionLayer { get; set; } = 1u;

	[Export(PropertyHint.Layers2DPhysics, "")]
	public uint collisionMask { get; set; } = 1u;

	[Export(PropertyHint.None, "")]
	public bool monitoring { get; set; } = true;

	[ExportGroup("Owner-relative Transform", "")]
	[Export(PropertyHint.None, "")]
	public NodePath anchorPath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public Transform2D localTransform { get; set; } = Transform2D.Identity;

	public int CollisionPreviewShapeCount => shapeResources?.Count ?? 0;

	public int CollisionPreviewRayCount => 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index < 0 || index >= (shapeResources?.Count ?? 0))
		{
			return null;
		}
		return shapeResources[index];
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new CharacterAabbAreaComponent();
	}
}
