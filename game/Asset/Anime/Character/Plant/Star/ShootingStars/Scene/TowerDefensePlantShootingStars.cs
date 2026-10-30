using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantShootingStars : TowerDefensePlant
{
	private static PackedScene _shootingStarsEffectScene;

	public string projectileName = "StarFull";

	private bool _effectStarted;

	private TowerDefenseProjectileEffectShootingStars _activeEffect;

	private static PackedScene ShootingStarsEffectScene => _shootingStarsEffectScene ?? (_shootingStarsEffectScene = GD.Load<PackedScene>("res://Prefab/ProjectileEffect/ShootingStars/TowerDefenseProjectileEffectShootingStars.tscn"));

	public override void _Ready()
	{
		base._Ready();
		Engine.IsEditorHint();
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (!Engine.IsEditorHint() && !_effectStarted && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && inGame)
		{
			_effectStarted = true;
			TowerDefenseProjectileEffectShootingStars towerDefenseProjectileEffectShootingStars = (_activeEffect = ShootingStarsEffectScene.Instantiate<TowerDefenseProjectileEffectShootingStars>(PackedScene.GenEditState.Disabled));
			towerDefenseProjectileEffectShootingStars.ConfigureNetworkVariant(projectileName);
			towerDefenseProjectileEffectShootingStars.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
			towerDefenseProjectileEffectShootingStars.GlobalPosition = GetLogicalGlobalPosition();
			if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
			{
				towerDefenseProjectileEffectShootingStars.OnCompleted += CompleteEffect;
			}
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseProjectileEffectShootingStars, forceReadableName: false, InternalMode.Disabled);
			HitBoxDestroy();
			instance.invincible = true;
		}
	}

	private void CompleteEffect()
	{
		_activeEffect = null;
		if (GodotObject.IsInstanceValid(this) && IsInsideTree() && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			Destroy();
		}
	}

	public override void DestroySet()
	{
		CancelActiveEffect();
		base.DestroySet();
	}

	public override void _ExitTree()
	{
		CancelActiveEffect();
		base._ExitTree();
	}

	private void CancelActiveEffect()
	{
		TowerDefenseProjectileEffectShootingStars activeEffect = _activeEffect;
		_activeEffect = null;
		if (GodotObject.IsInstanceValid(activeEffect))
		{
			activeEffect.OnCompleted -= CompleteEffect;
			if (!activeEffect.IsQueuedForDeletion())
			{
				activeEffect.QueueFree();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "projectileName", projectileName } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		projectileName = data.GetValueOrDefault("projectileName", Variant.From<string>("StarFull")).AsString();
	}
}
