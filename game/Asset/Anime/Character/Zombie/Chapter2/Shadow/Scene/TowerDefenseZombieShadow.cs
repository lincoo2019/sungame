using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieShadow : TowerDefenseZombie
{
	public bool over;

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DieEntered()
	{
		base.DieEntered();
		DestroySet();
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			over = true;
			return;
		}
		over = true;
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieShadowBlack");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter shadow = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, logicalGlobalPosition, base.gridPos) : packetConfig.Create(logicalGlobalPosition, base.gridPos));
		if (!GodotObject.IsInstanceValid(shadow))
		{
			return;
		}
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", shadow);
		shadow.CallDeferred("WalkReady");
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				shadow.Hypnoses();
			}).CallDeferred();
		}
		double hitpointScale = instance.hitpointScale;
		Vector2 scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(shadow))
			{
				if (GodotObject.IsInstanceValid(shadow.instance))
				{
					shadow.instance.hitpointScale = hitpointScale;
				}
				if (GodotObject.IsInstanceValid(shadow.transformPoint))
				{
					shadow.transformPoint.Scale = scale;
				}
			}
		}).CallDeferred();
		shadow.SetDeferred("invisible", base.invisible);
		Dictionary spawnState = new Dictionary
		{
			["spawn_invisible"] = base.invisible,
			["walk_ready_after_spawn"] = true
		};
		TowerDefenseManager.PublishSpawnedCharacter("ZombieShadowBlack", shadow, useCreate: true, 0.0, walkAfterSpawn: false, "", spawnState);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
