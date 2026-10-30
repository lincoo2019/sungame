using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class ScaredComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[ExportGroup("Detection", "")]
	[Export(PropertyHint.None, "")]
	public AabbShape2DResource scareShape { get; set; }

	[Export(PropertyHint.None, "")]
	public bool scaleScareShapeToMapGrid { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Vector2 scareGridSpan { get; set; } = new Vector2(2.75f, 2.75f);

	[Export(PropertyHint.None, "")]
	public bool hostAuthoritative { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.CHARACTER_HEIGHT scaredHeight { get; set; } = TowerDefenseEnum.CHARACTER_HEIGHT.LOW;

	[ExportGroup("Owner-relative References", "")]
	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<NodePath> controlledNodePaths { get; set; } = new Array<NodePath>();

	[Export(PropertyHint.None, "")]
	public Array<string> controlledRuntimeInstanceIds { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> controlledRuntimeTypeIds { get; set; } = new Array<string>();

	[ExportGroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName scaredDownStateEvent { get; set; } = "ToScaredDown";

	[Export(PropertyHint.None, "")]
	public StringName scaredGrowStateEvent { get; set; } = "ToScaredGrow";

	[Export(PropertyHint.None, "")]
	public StringName scaredIdleStateEvent { get; set; } = "ToScaredIdle";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string scaredDownAnimeClip { get; set; } = "Scared";

	[Export(PropertyHint.None, "")]
	public float scaredDownAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string scaredIdleAnimeClip { get; set; } = "ScaredIdle";

	[Export(PropertyHint.None, "")]
	public float scaredIdleAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string scaredGrowAnimeClip { get; set; } = "Grow";

	[Export(PropertyHint.None, "")]
	public float scaredGrowAnimeTimeScale { get; set; } = 1f;

	public int CollisionPreviewShapeCount => GodotObject.IsInstanceValid(scareShape) ? 1 : 0;

	public int CollisionPreviewRayCount => 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index != 0)
		{
			return null;
		}
		return scareShape;
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ScaredComponent();
	}
}
