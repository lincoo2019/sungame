using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class ChangeProjectileComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[Export(PropertyHint.None, "")]
	public StringName changeName { get; set; }

	[Export(PropertyHint.None, "")]
	public string changeAudio { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public AabbShape2DResource checkShape { get; set; }

	[Export(PropertyHint.None, "")]
	public bool isStar { get; set; }

	[Export(PropertyHint.None, "")]
	public bool trackToStraight { get; set; }

	[ExportGroup("Star Split", "")]
	[Export(PropertyHint.None, "")]
	public Array<float> starDirections { get; set; } = new Array<float> { 330f, 270f, 180f, 90f, 30f };

	[Export(PropertyHint.None, "")]
	public float starProjectileSpeed { get; set; } = 500f;

	[Export(PropertyHint.Range, "1,60,1")]
	public int exclusionPhysicsFrames { get; set; } = 2;

	[Export(PropertyHint.Range, "1,600,1")]
	public int structExcludeCleanupIntervalFrames { get; set; } = 60;

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
		return new ChangeProjectileComponent();
	}
}
