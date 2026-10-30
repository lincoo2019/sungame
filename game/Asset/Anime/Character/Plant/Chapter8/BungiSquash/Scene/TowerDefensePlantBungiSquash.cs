using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBungiSquash : TowerDefensePlant
{
	private SquashComponent _squashComponent;

	private AttackComponent _attackComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_squashComponent = componentManager.GetRuntime<SquashComponent>();
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			if (TowerDefenseManager.Instance.IsGameRunning())
			{
				instance.biteHurt = 0.0;
				base.z = 600.0;
				isGround = false;
				base.sprite.SetAnimation("JumpUp", loop: false);
				base.OnLand += OnLanded;
			}
		}
	}

	public void OnLanded()
	{
		if (GodotObject.IsInstanceValid(this))
		{
			base.OnLand -= OnLanded;
		}
		if (isDestroy || isShovel)
		{
			return;
		}
		base.sprite.SetAnimation("JumpDown", loop: false);
		SummonBungi();
		GetTree().CreateTimer(0.5, processAlways: false).Timeout += delegate
		{
			if (GodotObject.IsInstanceValid(this))
			{
				Destroy();
			}
		};
	}

	public void SummonBungi()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseZombieBungi towerDefenseZombieBungi = (base.EconomyOwnerAccountId.IsValid ? (TowerDefenseCharacter.CreateCharacter(base.EconomyOwnerAccountId, "ZombieBungi", logicalGlobalPosition, base.gridPos, 0.0) as TowerDefenseZombieBungi) : (TowerDefenseCharacter.CreateCharacter("ZombieBungi", logicalGlobalPosition, base.gridPos, 0.0) as TowerDefenseZombieBungi));
		if (GodotObject.IsInstanceValid(towerDefenseZombieBungi))
		{
			Dictionary dictionary = new Dictionary { ["skipBungeeTarget"] = true };
			towerDefenseZombieBungi.ImportNetworkSpawnState(dictionary);
			if (!instance.hypnoses)
			{
				towerDefenseZombieBungi.Hypnoses();
			}
			TowerDefenseManager.PublishSpawnedCharacter("ZombieBungi", towerDefenseZombieBungi, useCreate: true, 0.0, walkAfterSpawn: true, "", dictionary);
			towerDefenseZombieBungi.CallDeferred("Walk");
		}
	}
}
