using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieAlien : TowerDefenseZombie
{
	private const double ShootDamage = 80.0;

	private const int BrainSunCount = 3;

	private const long BrainSunValue = 25L;

	private AttackComponent _shootAttack;

	public override Dictionary ExportVariantSave()
	{
		return base.ExportVariantSave();
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_shootAttack = componentManager?.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent = componentManager?.GetRuntime<AttackComponent>("character.attack.1");
			base.useAttackDps = true;
			base.walkSpeedScale = 1.5;
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			base.WalkProcessing(delta);
		}
		else if (!die && !nearDie && GodotObject.IsInstanceValid(base.sprite) && !base.sprite.pause && !(timeScale <= 0.0) && !(base.sprite.timeScale <= 0.0))
		{
			TryFireLaser();
			base.WalkProcessing(delta);
		}
	}

	public override void AttackProcessing(double delta)
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			TryFireLaser();
		}
		base.AttackProcessing(delta);
	}

	private void TryFireLaser()
	{
		AttackComponent shootAttack = _shootAttack;
		if (shootAttack != null && !shootAttack.IsReleased && _shootAttack.timer <= 0.0 && _shootAttack.CanAttack())
		{
			FireLaser();
			_shootAttack.Refresh();
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetAnimation("Shooting", loop: false, 0.2);
			}
		}
	}

	private void FireLaser()
	{
		AudioManager.Instance.AudioPlay("Spike");
		foreach (TowerDefenseCharacter target in _shootAttack.GetTargetList())
		{
			if (!GodotObject.IsInstanceValid(target))
			{
				continue;
			}
			if (target is TowerDefensePlant towerDefensePlant)
			{
				TowerDefenseEnum.CHARACTER_HEIGHT height = towerDefensePlant.instance.height;
				if (height == TowerDefenseEnum.CHARACTER_HEIGHT.LOW || height == TowerDefenseEnum.CHARACTER_HEIGHT.GROUND)
				{
					continue;
				}
			}
			target.AttackDeal(this, "Smash", 80.0);
			if (GodotObject.IsInstanceValid(target.cell))
			{
				target.cell.AttackDeal(this, "Smash", 80.0);
			}
			target.Hurt(80.0, playSplatAudio: true, Vector2.Zero);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!die && clip == "Shooting")
		{
			if (die)
			{
				SendStateEvent("ToDie");
			}
			else if (GodotObject.IsInstanceValid(base.sprite))
			{
				bool flag = base.AttackStateHandle?.IsActive ?? false;
				base.sprite.SetAnimation(flag ? base.attackAnimeClip : base.walkAnimeClip, loop: true, 0.2);
			}
		}
	}

	public override void DestroySet()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		for (int i = 0; i < 3; i++)
		{
			Vector2 vector = new Vector2(GD.RandRange(-40, 40), GD.RandRange(-30, 30));
			if (instance.hypnoses)
			{
				SunCreate(logicalGlobalPosition + vector, 25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2(GD.RandRange(-50, 50), -400f));
			}
			else
			{
				BrainSunCreate(logicalGlobalPosition + vector, 25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2(GD.RandRange(-50, 50), -400f));
			}
		}
		base.DestroySet();
	}
}
