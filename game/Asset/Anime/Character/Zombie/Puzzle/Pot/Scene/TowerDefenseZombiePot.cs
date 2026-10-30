using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombiePot : TowerDefenseZombie
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
		base.sprite.SetFliters(new Array { "anim_Pot" }, open: false);
		DestroySet();
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		base.sprite.SetFliters(new Array { "anim_Pot" }, open: false);
		DestroySet();
	}

	public override async void DestroySet()
	{
		if (!over)
		{
			over = true;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			SpawnVase();
		}
	}

	public void SpawnVase()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("VaseNormal");
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(logicalGlobalPosition);
			if ((base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, logicalGlobalPosition, mapGridPos) : packetConfig.Create(logicalGlobalPosition, mapGridPos)) is TowerDefenseVase towerDefenseVase && GodotObject.IsInstanceValid(towerDefenseVase))
			{
				towerDefenseVase.useEnterAnime = false;
				towerDefenseVase.packetBank = "Original";
				TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseVase, forceReadableName: false, InternalMode.Disabled);
				Dictionary spawnState = new Dictionary
				{
					["vase_use_enter_anime"] = false,
					["vase_packet_bank"] = "Original"
				};
				TowerDefenseManager.PublishSpawnedCharacter("VaseNormal", towerDefenseVase, useCreate: true, 0.0, walkAfterSpawn: false, "", spawnState);
			}
		}
	}
}
