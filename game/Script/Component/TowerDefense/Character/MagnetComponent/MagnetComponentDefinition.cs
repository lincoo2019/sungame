using Godot;

[GlobalClass]
[Tool]
public partial class MagnetComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[ExportGroup("Targeting", "")]
	[Export(PropertyHint.None, "")]
	public AabbShape2DResource checkShape { get; set; }

	[Export(PropertyHint.None, "")]
	public bool scaleCheckShapeToMapGrid { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Vector2 checkRange { get; set; } = new Vector2(2.5f, 2.5f);

	[Export(PropertyHint.None, "")]
	public bool checkAll { get; set; }

	[Export(PropertyHint.None, "")]
	public bool hostAuthoritative { get; set; } = true;

	[ExportGroup("Attachment", "")]
	[Export(PropertyHint.None, "")]
	public NodePath posMarkerPath { get; set; } = new NodePath();

	[Export(PropertyHint.Range, "0,120,0.01,or_greater")]
	public float breakDownTime { get; set; } = 15f;

	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public float magnetMoveSpeed { get; set; } = 10f;

	[Export(PropertyHint.Range, "0,100,0.001,or_greater")]
	public float arriveDistance { get; set; } = 0.01f;

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public string drawEventName { get; set; } = "action";

	[Export(PropertyHint.None, "")]
	public string shootBeginAnimeClips { get; set; } = "Begin";

	[Export(PropertyHint.None, "")]
	public float shootBeginAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public float shootBeginAnimeStartPosition { get; set; } = 0.2f;

	[Export(PropertyHint.None, "")]
	public string shootAnimeClips { get; set; } = "Shooting";

	[Export(PropertyHint.None, "")]
	public float shootAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public float shootAnimeStartPosition { get; set; }

	[Export(PropertyHint.None, "")]
	public string shootEndAnimeClips { get; set; } = "End";

	[Export(PropertyHint.None, "")]
	public float shootEndAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public float shootEndAnimeStartPosition { get; set; } = 0.2f;

	[Export(PropertyHint.None, "")]
	public string noActiveAnimeClips { get; set; } = "NonActiveIdle2";

	[Export(PropertyHint.None, "")]
	public float noActiveAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public float noActiveAnimeStartPosition { get; set; } = 0.2f;

	[ExportGroup("Audio", "")]
	[Export(PropertyHint.None, "")]
	public string drawAudioName { get; set; } = "Magnet";

	[ExportGroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName beginStateEvent { get; set; } = "ToBegin";

	[Export(PropertyHint.None, "")]
	public StringName shootStateEvent { get; set; } = "ToShoot";

	[Export(PropertyHint.None, "")]
	public StringName noActiveStateEvent { get; set; } = "ToNoActive";

	[Export(PropertyHint.None, "")]
	public StringName endStateEvent { get; set; } = "ToEnd";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

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
		return new MagnetComponent();
	}
}
