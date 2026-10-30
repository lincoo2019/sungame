using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseItemSoccerBall : TowerDefenseItem
{
	private const string IdleClip = "Idle";

	private const string DeathClip = "Death";

	private const string RollClip = "Roll";

	public CharacterMoveComponent moveComponent;

	public WaterEnvironmentComponent waterEnvironmentComponent;

	public BowlingComponent bowlingComponent;

	private bool _destroyAnimePlaying;

	private bool _destroyFreeInstance = true;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			if (GodotObject.IsInstanceValid(componentManager))
			{
				moveComponent = componentManager.GetRuntime<CharacterMoveComponent>();
				waterEnvironmentComponent = componentManager.GetRuntime<WaterEnvironmentComponent>();
				bowlingComponent = componentManager.GetRuntime<BowlingComponent>();
			}
			BowlingComponent obj = bowlingComponent;
			if (obj != null && !obj.IsReleased)
			{
				EnsureRollClipConfigured();
				bowlingComponent.SetAlive(alive: false);
			}
			if (GodotObject.IsInstanceValid(base.sprite) && base.sprite.HasClip("Idle"))
			{
				base.sprite.SetAnimation("Idle");
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
			ShadowComponent obj = shadowComponent;
			if (obj != null && !obj.IsReleased)
			{
				shadowComponent.saveShadowPosition = new Vector2(shadowComponent.saveShadowPosition.X, globalPositionForPhysicsFrame.Y + 30f);
			}
			if (TowerDefenseManager.Instance != null)
			{
				base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(globalPositionForPhysicsFrame);
			}
		}
	}

	public override void IdleEntered()
	{
		BowlingComponent obj = bowlingComponent;
		if (obj == null || obj.IsReleased || !bowlingComponent.isRoll)
		{
			base.IdleEntered();
			BowlingComponent obj2 = bowlingComponent;
			if (obj2 != null && !obj2.IsReleased)
			{
				bowlingComponent.SetAlive(alive: false);
			}
		}
	}

	public void StartKickedRoll()
	{
		useIdleAnimeReset = false;
		EnsureRollClipConfigured();
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.StartRoll();
		}
	}

	private void EnsureRollClipConfigured()
	{
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased && string.IsNullOrEmpty(bowlingComponent.rollAnimeClips))
		{
			bowlingComponent.rollAnimeClips = "Roll";
		}
	}

	public override void InWater()
	{
		base.InWater();
		CreateSplash();
		base.Destroy();
	}

	public override void Destroy(bool freeInstance = true)
	{
		if (isDestroy || _destroyAnimePlaying)
		{
			return;
		}
		if (base.inWater || !GodotObject.IsInstanceValid(base.sprite) || !base.sprite.HasClip("Death"))
		{
			base.Destroy(freeInstance);
			return;
		}
		_destroyAnimePlaying = true;
		_destroyFreeInstance = freeInstance;
		HitBoxDestroy();
		CharacterMoveComponent characterMoveComponent = moveComponent;
		if (characterMoveComponent != null && !characterMoveComponent.IsReleased)
		{
			moveComponent.velocity = Vector2.Zero;
		}
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.SetAlive(alive: false);
			bowlingComponent.isRoll = false;
		}
		base.sprite.SetAnimation("Death", loop: false);
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (_destroyAnimePlaying && !(clip != "Death"))
		{
			_destroyAnimePlaying = false;
			base.Destroy(_destroyFreeInstance);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["destroyAnimePlaying"] = _destroyAnimePlaying,
			["destroyFreeInstance"] = _destroyFreeInstance
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_destroyAnimePlaying = data.GetValueOrDefault("destroyAnimePlaying", _destroyAnimePlaying).AsBool();
		_destroyFreeInstance = data.GetValueOrDefault("destroyFreeInstance", _destroyFreeInstance).AsBool();
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		CharacterMoveComponent characterMoveComponent = moveComponent;
		if (characterMoveComponent != null && !characterMoveComponent.IsReleased)
		{
			moveComponent.moveScale = (instance.hypnoses ? (-1.0) : 1.0);
		}
	}
}
