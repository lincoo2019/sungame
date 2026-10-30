using Godot;

[Tool]
public partial class TowerDefenseBungiTargetSP : TowerDefenseGravestone
{
	private const string DisableMetaKey = "disabled_by_bungi_target_sp";

	private const string RefCountMetaKey = "character_disabled_ref_count";

	private const string SavedPauseMetaKey = "bungi_target_sp_saved_pause";

	public double lifetime = 15.0;

	private bool _blowStarted;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint() || editorPreviewMode)
		{
			return;
		}
		instance.invincible = true;
		AddToGroup("BungiTargetSP", persistent: true);
		RemoveFromGroup("Gravestone");
		isGround = false;
		base.z = 600.0;
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(this, "z", 0.0, 0.5);
		tween.TweenCallback(Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(this))
			{
				base.z = 0.0;
				base.groundHeight = 0.0;
				isGround = true;
			}
		}));
		Timer timer = new Timer
		{
			WaitTime = lifetime,
			OneShot = true
		};
		AddChild(timer, forceReadableName: false, InternalMode.Disabled);
		timer.Timeout += delegate
		{
			if (GodotObject.IsInstanceValid(this))
			{
				Blow();
			}
		};
		timer.Start();
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnBlowAllEffectEmit += Blow;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnBlowAllEffectEmit -= Blow;
		}
	}

	public override async void Blow()
	{
		if (GodotObject.IsInstanceValid(this) && !_blowStarted)
		{
			_blowStarted = true;
			HitBoxDestroy();
			Tween tween = CreateTween();
			tween.TweenProperty(this, "modulate", Colors.Transparent, 1.0);
			await ToSignal(tween, Tween.SignalName.Finished);
			Destroy();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || editorPreviewMode || !GodotObject.IsInstanceValid(cell) || nearDie || die)
		{
			return;
		}
		foreach (TowerDefenseCharacter character in cell.GetCharacterList())
		{
			if (!(character is TowerDefensePlant towerDefensePlant) || !CanTarget(towerDefensePlant) || towerDefensePlant is TowerDefensePlantBowlingBase || (character.instance.maskFlags & 2) != 0 || towerDefensePlant.instance.invincible)
			{
				continue;
			}
			if (!towerDefensePlant.HasMeta("disabled_by_bungi_target_sp"))
			{
				towerDefensePlant.SetMeta("disabled_by_bungi_target_sp", true);
				int num = towerDefensePlant.GetMeta("character_disabled_ref_count", 0).AsInt32() + 1;
				towerDefensePlant.SetMeta("character_disabled_ref_count", num);
			}
			if (GodotObject.IsInstanceValid(towerDefensePlant.sprite))
			{
				if (!towerDefensePlant.HasMeta("bungi_target_sp_saved_pause"))
				{
					towerDefensePlant.SetMeta("bungi_target_sp_saved_pause", towerDefensePlant.sprite.pause);
				}
				towerDefensePlant.sprite.pause = true;
			}
			towerDefensePlant.characterDisabled = true;
		}
	}

	public override void DestroySet()
	{
		if (GodotObject.IsInstanceValid(cell))
		{
			foreach (TowerDefenseCharacter character in cell.GetCharacterList())
			{
				if (!(character is TowerDefensePlant towerDefensePlant) || towerDefensePlant is TowerDefensePlantBowlingBase || !towerDefensePlant.HasMeta("disabled_by_bungi_target_sp"))
				{
					continue;
				}
				towerDefensePlant.RemoveMeta("disabled_by_bungi_target_sp");
				int num = towerDefensePlant.GetMeta("character_disabled_ref_count", 1).AsInt32() - 1;
				if (num <= 0)
				{
					towerDefensePlant.RemoveMeta("character_disabled_ref_count");
					towerDefensePlant.characterDisabled = false;
					if (GodotObject.IsInstanceValid(towerDefensePlant.sprite) && towerDefensePlant.HasMeta("bungi_target_sp_saved_pause"))
					{
						towerDefensePlant.sprite.pause = towerDefensePlant.GetMeta("bungi_target_sp_saved_pause").AsBool();
						towerDefensePlant.RemoveMeta("bungi_target_sp_saved_pause");
					}
				}
				else
				{
					towerDefensePlant.SetMeta("character_disabled_ref_count", num);
				}
			}
		}
		base.DestroySet();
	}
}
