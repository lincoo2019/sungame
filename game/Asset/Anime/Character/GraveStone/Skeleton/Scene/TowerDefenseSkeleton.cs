using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseSkeleton : TowerDefenseGravestone
{
	private static PackedScene _SKELETON_EXPLOSION;

	public bool over;

	public double timer;

	public double time = 30.0;

	private static PackedScene SKELETON_EXPLOSION => _SKELETON_EXPLOSION ?? (_SKELETON_EXPLOSION = GD.Load<PackedScene>("uid://dykgix28gj14b"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			RemoveFromGroup("Gravestone");
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning || over || base.sprite.pause)
		{
			return;
		}
		if (timer < time)
		{
			timer += delta * timeScale;
			return;
		}
		over = true;
		if (nearDie || die)
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieSkeletuar");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseZombieSkeletuar zombie = (TowerDefenseZombieSkeletuar)packetConfig.Create(logicalGlobalPosition, base.gridPos);
		zombie.over = true;
		Dictionary dictionary = zombie.ExportNetworkSpawnState();
		zombie.ImportNetworkSpawnState(dictionary);
		TowerDefenseManager.GetCharacterNode().AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				if (GodotObject.IsInstanceValid(zombie.instance))
				{
					zombie.instance.hitpointScale = _hitpointScale;
				}
				if (GodotObject.IsInstanceValid(zombie.transformPoint))
				{
					zombie.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		zombie.SetDeferred("invisible", base.invisible);
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieSkeletuar", base.gridPos.X, base.gridPos.Y, nextSyncId, _hitpointScale, _scale.X, hypnoses: false, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: true, 0.0, "", dictionary);
			}
		}
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "timer", timer } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		timer = Math.Max(0.0, data.GetValueOrDefault("timer", timer).AsDouble());
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		AudioManager.Instance.AudioPlay("ZamboniExplosion");
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(SKELETON_EXPLOSION, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
	}
}
