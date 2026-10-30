using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class ExplodeComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public string explodeAnimeClips { get; set; } = "Explode";

	[Export(PropertyHint.None, "")]
	public float explodeAnimeTimeScale { get; set; } = 1f;

	[ExportGroup("Explosion", "")]
	[Export(PropertyHint.None, "")]
	public bool hostAuthoritative { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool skipGameRunning { get; set; }

	[Export(PropertyHint.None, "")]
	public bool checkIZM { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public string explodeMethod { get; set; } = "Range";

	[Export(PropertyHint.None, "")]
	public bool explodeUse { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool reload { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool explodeOnce { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public string reverseAudio { get; set; } = "ReverseExplosion";

	[Export(PropertyHint.None, "")]
	public string explodeAudio { get; set; } = "ExplodeCherrybomb";

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> explodeEvent { get; set; } = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public PackedScene explodeEffect { get; set; }

	[Export(PropertyHint.None, "")]
	public AabbShape2DResource explodeShape { get; set; }

	[Export(PropertyHint.None, "")]
	public bool scaleExplodeShapeToMapGrid { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Vector2 explodeRange { get; set; } = new Vector2(1.5f, 1.5f);

	[Export(PropertyHint.None, "")]
	public StringName explodeStateEvent { get; set; } = "ToExplode";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	[ExportGroup("Jalapeno", "")]
	[Export(PropertyHint.None, "")]
	public string explodeJalaFireType { get; set; } = "Fire";

	[Export(PropertyHint.None, "")]
	public float explodeJalaNum { get; set; } = 1800f;

	[Export(PropertyHint.None, "")]
	public Array<int> explodeJalaOffset { get; set; } = new Array<int>();

	[ExportGroup("Camera", "")]
	[Export(PropertyHint.None, "")]
	public bool cameraShakeUse { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Vector2 cameraShakeOffset { get; set; } = Vector2.One;

	[Export(PropertyHint.None, "")]
	public float cameraShakeForce { get; set; } = 5f;

	[Export(PropertyHint.None, "")]
	public float cameraShakeInterval { get; set; } = 0.05f;

	[Export(PropertyHint.None, "")]
	public int cameraShakeTime { get; set; } = 4;

	[ExportGroup("Screen", "")]
	[Export(PropertyHint.None, "")]
	public bool screenColorBlinkUse { get; set; }

	[Export(PropertyHint.None, "")]
	public Color screenColorBlinkColor { get; set; } = Colors.DarkSlateBlue;

	[Export(PropertyHint.None, "")]
	public float screenColorBlinkDuration { get; set; } = 0.5f;

	[Export(PropertyHint.None, "")]
	public bool screenColorBlinkRise { get; set; }

	[ExportGroup("Extension", "")]
	[Export(PropertyHint.None, "")]
	public bool craterCreateUse { get; set; }

	[Export(PropertyHint.None, "")]
	public string craterCreatePacketName { get; set; } = "CraterDayGround";

	public int CollisionPreviewShapeCount => GodotObject.IsInstanceValid(explodeShape) ? 1 : 0;

	public int CollisionPreviewRayCount => 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index != 0)
		{
			return null;
		}
		return explodeShape;
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ExplodeComponent();
	}
}
