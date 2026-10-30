using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class ChomperComponentDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
{
	[ExportGroup("Bite", "")]
	[Export(PropertyHint.None, "")]
	public float chewTime { get; set; } = 30f;

	[Export(PropertyHint.None, "")]
	public float chewTimePercentage { get; set; } = -1f;

	[Export(PropertyHint.None, "")]
	public float biteAttack { get; set; } = -1f;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> biteEvent { get; set; } = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public bool biteOnly { get; set; }

	[Export(PropertyHint.None, "")]
	public bool biteNoLimit { get; set; }

	[Export(PropertyHint.None, "")]
	public bool ignoreBiteHurt { get; set; }

	[Export(PropertyHint.None, "")]
	public float healthPercentage { get; set; } = -1f;

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string biteEventName { get; set; } = "attack";

	[Export(PropertyHint.None, "")]
	public string biteStartAnimeClips { get; set; } = "Bite";

	[Export(PropertyHint.None, "")]
	public float biteStartAnimeTimeScale { get; set; } = 1.5f;

	[Export(PropertyHint.None, "")]
	public string biteLoopAnimeClips { get; set; } = "BiteLoop";

	[Export(PropertyHint.None, "")]
	public float biteLoopAnimeTimeScale { get; set; } = 1.5f;

	[Export(PropertyHint.None, "")]
	public string biteEndAnimeClips { get; set; } = "BiteEnd";

	[Export(PropertyHint.None, "")]
	public float biteEndAnimeTimeScale { get; set; } = 1.5f;

	[Export(PropertyHint.None, "")]
	public string chewReadyAnimeClips { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public float chewReadyAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string chewAnimeClips { get; set; } = "Chew";

	[Export(PropertyHint.None, "")]
	public float chewAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string swallowAnimeClips { get; set; } = "Swallow";

	[Export(PropertyHint.None, "")]
	public float swallowAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public bool isPartSprite { get; set; }

	[Export(PropertyHint.None, "")]
	public string partIdleAnimeClips { get; set; } = "Idle";

	[Export(PropertyHint.None, "")]
	public float partIdleAnimeTimeScale { get; set; } = 1.5f;

	[Export(PropertyHint.None, "")]
	public bool playBiteAudio { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public string biteAudioName { get; set; } = "BigChomp";

	[ExportGroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName attackStateEvent { get; set; } = "ToAttack";

	[Export(PropertyHint.None, "")]
	public StringName chewStateEvent { get; set; } = "ToChew";

	[Export(PropertyHint.None, "")]
	public StringName swallowStateEvent { get; set; } = "ToSwallow";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	[ExportGroup("Suction", "")]
	[Export(PropertyHint.None, "")]
	public bool suckUse { get; set; }

	[Export(PropertyHint.None, "")]
	public AabbShape2DResource suckShape { get; set; }

	[ExportGroup("Owner-relative References", "")]
	[Export(PropertyHint.None, "")]
	public NodePath attackComponentPath { get; set; }

	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; }

	public int CollisionPreviewShapeCount
	{
		get
		{
			if (!suckUse || !GodotObject.IsInstanceValid(suckShape))
			{
				return 0;
			}
			return 1;
		}
	}

	public int CollisionPreviewRayCount => 0;

	public AabbShape2DResource GetCollisionPreviewShape(int index)
	{
		if (index != 0 || !suckUse)
		{
			return null;
		}
		return suckShape;
	}

	public AabbRay2DResource GetCollisionPreviewRay(int index)
	{
		return null;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ChomperComponent();
	}
}
