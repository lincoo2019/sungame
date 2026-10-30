using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffMagicRootHaste : TowerDefenseCharacterBuffConfig
{
	public const double CoverHasteTime = 30.0;

	public const double CoverHasteTimeScale = 2.0;

	private const string CircleSceneUid = "uid://bocq4s513vjga";

	private static PackedScene _circleScene;

	[Export(PropertyHint.None, "")]
	public double timeScaleValue = 2.0;

	[Export(PropertyHint.None, "")]
	public double time = 30.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public AdobeAnimateSprite circleSprite;

	private static PackedScene CircleScene => _circleScene ?? (_circleScene = GD.Load<PackedScene>("uid://bocq4s513vjga"));

	public override void _Init()
	{
		if (key == null)
		{
			key = "MagicRootHaste";
		}
	}

	public override TowerDefenseCharacterBuffConfig CreateRuntimeInstance()
	{
		return new TowerDefenseCharacterBuffMagicRootHaste
		{
			key = "MagicRootHaste",
			refresh = refresh,
			canFliter = canFliter,
			timeScaleValue = timeScaleValue,
			time = time,
			currentTime = currentTime
		};
	}

	public override void Enter()
	{
		ShowCircleVisual();
	}

	public override void EnterReadOnlyClient()
	{
		ShowCircleVisual();
	}

	private void ShowCircleVisual()
	{
		if (GodotObject.IsInstanceValid(circleSprite) || !GodotObject.IsInstanceValid(character))
		{
			return;
		}
		PackedScene circleScene = CircleScene;
		if (circleScene != null && GodotObject.IsInstanceValid(character.backEffectNode))
		{
			circleSprite = circleScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(circleSprite))
			{
				character.backEffectNode.AddChild(circleSprite, forceReadableName: false, Node.InternalMode.Disabled);
				circleSprite.Position = (GodotObject.IsInstanceValid(character.shadowSprite) ? character.shadowSprite.Position : Vector2.Zero);
				circleSprite.ZIndex = -1;
			}
		}
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		if (character.nearDie || character.die || currentTime >= time)
		{
			return true;
		}
		character.timeScale *= timeScaleValue;
		return false;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
		HideCircleVisual();
	}

	public override void ExitReadOnlyClient()
	{
		HideCircleVisual();
	}

	private void HideCircleVisual()
	{
		if (GodotObject.IsInstanceValid(circleSprite))
		{
			circleSprite.QueueFree();
		}
		circleSprite = null;
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		if (config is TowerDefenseCharacterBuffMagicRootHaste towerDefenseCharacterBuffMagicRootHaste)
		{
			timeScaleValue = towerDefenseCharacterBuffMagicRootHaste.timeScaleValue;
			time = Mathf.Max(time, currentTime + towerDefenseCharacterBuffMagicRootHaste.time);
		}
	}
}
