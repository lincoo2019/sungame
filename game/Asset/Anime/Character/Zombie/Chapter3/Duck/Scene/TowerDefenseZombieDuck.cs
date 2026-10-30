using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieDuck : TowerDefenseZombie
{
	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void HitpointsNearDie()
	{
		base.HitpointsNearDie();
		DestroySet();
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
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
		if (!isSmash && !isExplode && !isChomp)
		{
			SpawnDuckytube();
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public void SpawnDuckytube()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("Duckytube");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter duckytube = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, logicalGlobalPosition, base.gridPos) : packetConfig.Create(logicalGlobalPosition, base.gridPos));
		if (!GodotObject.IsInstanceValid(duckytube))
		{
			return;
		}
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", duckytube);
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(duckytube))
				{
					duckytube.Hypnoses();
				}
			}).CallDeferred();
		}
		TowerDefenseManager.PublishSpawnedCharacter("Duckytube", duckytube, useCreate: true);
	}
}
