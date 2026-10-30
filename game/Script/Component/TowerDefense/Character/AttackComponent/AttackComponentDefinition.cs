using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class AttackComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[Export(PropertyHint.Enum, "Default,Eat,Smash,Chomp")]
	public string attackType = "Eat";

	[ExportGroup("Collision", "")]
	[Export(PropertyHint.None, "")]
	public bool useParentHitBox;

	[Export(PropertyHint.Range, "0,120,1")]
	public int checkIntreval = 5;

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public NodePath spritePath = new NodePath();

	[Export(PropertyHint.None, "")]
	public Array<string> attackAnimeClipsArray = new Array<string> { "Attack" };

	[Export(PropertyHint.None, "")]
	public string attackAnimeClips = "Attack";

	[Export(PropertyHint.None, "")]
	public string attackEventName = "attack";

	[Export(PropertyHint.None, "")]
	public double attackAnimeTimeScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double attackIntervalBase = 1.5;

	[Export(PropertyHint.None, "")]
	public double attackInterval = 1.5;

	[Export(PropertyHint.None, "")]
	public double attackIntervalOffset = 0.1;

	[Export(PropertyHint.None, "")]
	public string spliceIdleAnimeClips = "";

	[Export(PropertyHint.None, "")]
	public double spliceIdleAnimeTimeScale = 1.0;

	[ExportGroup("Attack Events", "")]
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	[ExportGroup("Targeting", "")]
	[Export(PropertyHint.None, "")]
	public bool useZombieAttackCheck = true;

	[Export(PropertyHint.None, "")]
	public bool checkGrid = true;

	[Export(PropertyHint.None, "")]
	public bool useCheckAreaGridColumn;

	[Export(PropertyHint.None, "")]
	public bool checkEachShape;

	[Export(PropertyHint.None, "")]
	public bool checkLine;

	[Export(PropertyHint.None, "")]
	public bool checkTall;

	[Export(PropertyHint.None, "")]
	public bool checkVase;

	[Export(PropertyHint.None, "")]
	public bool checkBowling;

	[Export(PropertyHint.None, "")]
	public bool checkGravestone = true;

	[Export(PropertyHint.None, "")]
	public bool checkAll;

	[Export(PropertyHint.None, "")]
	public bool fliterLadder;

	[ExportGroup("Audio", "")]
	[Export(PropertyHint.None, "")]
	public string eatAudio = "Chomp";

	[Export(PropertyHint.None, "")]
	public string plantDefeatAudio = "Gulp";

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double eatAudioInterval = 1.0;

	[Export(PropertyHint.None, "")]
	public bool playPlantDefeatAudio = true;

	[ExportGroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName attackStateEvent = "ToAttack";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent = "ToIdle";

	[Export(PropertyHint.None, "")]
	public Array<AabbShape2DResource> checkShapeResources { get; set; } = new Array<AabbShape2DResource>();

	public int CollisionPreviewShapeCount => checkShapeResources?.Count ?? 0;

	public int CollisionPreviewRayCount => 0;

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
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new AttackComponent();
	}
}
