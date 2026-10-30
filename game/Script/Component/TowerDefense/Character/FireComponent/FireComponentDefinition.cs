using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class FireComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[ExportGroup("Owner-relative References", "")]
	[Export(PropertyHint.None, "")]
	public Array<NodePath> firePosMarkerPaths { get; set; } = new Array<NodePath>();

	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public Array<NodePath> spliceSpritePaths { get; set; } = new Array<NodePath>();

	[Export(PropertyHint.None, "")]
	public string preExtendInstanceId { get; set; } = string.Empty;

	[ExportGroup("Target Geometry", "")]
	[Export(PropertyHint.None, "")]
	public Array<AabbRay2DResource> checkRayResources { get; set; } = new Array<AabbRay2DResource>();

	[Export(PropertyHint.None, "")]
	public Array<AabbShape2DResource> checkShapeResources { get; set; } = new Array<AabbShape2DResource>();

	[Export(PropertyHint.None, "")]
	public bool checkUse { get; set; } = true;

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string fireAudioName { get; set; } = "ProjectileThrow";

	[Export(PropertyHint.None, "")]
	public string fireEventName { get; set; } = "fire";

	[Export(PropertyHint.None, "")]
	public StringName attackStateEvent { get; set; } = "ToAttack";

	[Export(PropertyHint.None, "")]
	public StringName restoreStateEvent { get; set; } = "ToRestore";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	[Export(PropertyHint.None, "")]
	public string fireOverEventName { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public Array<string> fireAnimeClipsArray { get; set; } = new Array<string> { "Fire" };

	[Export(PropertyHint.None, "")]
	public string fireAnimeClips { get; set; } = "Fire";

	[Export(PropertyHint.None, "")]
	public float fireAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string restoreAnimeClips { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public float restoreTime { get; set; } = 10f;

	[Export(PropertyHint.None, "")]
	public string spliceIdleAnimeClips { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public float spliceIdleAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public bool isSpliceSprite { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<string> spliceSpriteFireAnimeClipsList { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> spliceSpriteIdleAnimeClipsList { get; set; } = new Array<string>();

	[ExportGroup("Fire", "")]
	[Export(PropertyHint.None, "")]
	public bool onlyEmitSignal { get; set; }

	[Export(PropertyHint.None, "")]
	public float fireLength { get; set; } = -1f;

	[Export(PropertyHint.None, "")]
	public bool fireDirect { get; set; }

	[Export(PropertyHint.None, "")]
	public float fireIntervalBase { get; set; } = 1.5f;

	[Export(PropertyHint.None, "")]
	public float fireInterval { get; set; } = 1.5f;

	[Export(PropertyHint.None, "")]
	public float fireIntervalOffset { get; set; } = 0.1f;

	[Export(PropertyHint.None, "")]
	public int fireNum { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public bool fireNumAtOnce { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<FireComponentCheckConfig> fireCheckList { get; set; } = new Array<FireComponentCheckConfig>();

	[Export(PropertyHint.None, "")]
	public Array<FireComponentFireProjectileConfig> fireProjectileList { get; set; } = new Array<FireComponentFireProjectileConfig>();

	[Export(PropertyHint.None, "")]
	public bool useCollisionEveryPos { get; set; }

	[Export(PropertyHint.None, "")]
	public bool readyConfirmProjectile { get; set; }

	[Export(PropertyHint.None, "")]
	public bool lockProjectileGridY { get; set; }

	[Export(PropertyHint.None, "")]
	public bool snapStraightProjectileToSameCellTarget { get; set; }

	[Export(PropertyHint.None, "")]
	public float offscreenTargetMarginColumns { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public float defaultCheckLengthWorld { get; set; } = 2000f;

	[Export(PropertyHint.None, "")]
	public float fireAnimationStartPosition { get; set; } = 0.2f;

	[Export(PropertyHint.None, "")]
	public float repeatFireAnimationStartPosition { get; set; } = 0.1f;

	[Export(PropertyHint.None, "")]
	public float spliceAnimationStartPosition { get; set; } = 0.2f;

	[Export(PropertyHint.None, "")]
	public float restoreAnimationStartPosition { get; set; } = 0.2f;

	[Export(PropertyHint.None, "")]
	public float offsetLineTweenDuration { get; set; } = 0.15f;

	[Export(PropertyHint.None, "")]
	public float blockedOffsetLineX { get; set; } = 25f;

	[ExportGroup("Targeting", "")]
	[Export(PropertyHint.None, "")]
	public bool checkAllLine { get; set; }

	[Export(PropertyHint.None, "")]
	public float checkLength { get; set; } = -1f;

	[Export(PropertyHint.None, "")]
	public int checkIntervalMax { get; set; } = 2;

	[Export(PropertyHint.None, "")]
	public bool checkHeight { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool airFirst { get; set; }

	[Export(PropertyHint.None, "")]
	public bool catapultFirstFar { get; set; }

	[Export(PropertyHint.None, "")]
	public bool randomChoose { get; set; }

	[Export(PropertyHint.None, "")]
	public bool canTargetGargantuar { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool checkGravestone { get; set; } = true;

	public int CollisionPreviewShapeCount => checkShapeResources?.Count ?? 0;

	public int CollisionPreviewRayCount => checkRayResources?.Count ?? 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index < 0 || index >= (checkShapeResources?.Count ?? 0))
		{
			return null;
		}
		return checkShapeResources[index];
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		if (index < 0 || index >= (checkRayResources?.Count ?? 0))
		{
			return null;
		}
		return checkRayResources[index];
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new FireComponent();
	}
}
