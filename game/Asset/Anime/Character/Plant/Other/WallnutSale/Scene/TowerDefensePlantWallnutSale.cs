using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantWallnutSale : TowerDefensePlant
{
	public bool canCopy = true;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(packet) && packet.GetCost() == 0)
		{
			CallDeferred("_DeferredReady");
		}
	}

	private async void _DeferredReady()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (instance.hologram)
		{
			canCopy = false;
		}
		if (canCopy)
		{
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(base.config.name);
			TowerDefenseCharacterOverride towerDefenseCharacterOverride = new TowerDefenseCharacterOverride();
			TowerDefenseCharacterPropertyChangeConfig towerDefenseCharacterPropertyChangeConfig = new TowerDefenseCharacterPropertyChangeConfig();
			towerDefenseCharacterPropertyChangeConfig.propertyName = "canCopy";
			towerDefenseCharacterPropertyChangeConfig.value = false;
			towerDefenseCharacterOverride.propertyChange = new Array<TowerDefenseCharacterPropertyChangeConfig> { towerDefenseCharacterPropertyChangeConfig };
			TowerDefensePacketOverride towerDefensePacketOverride = new TowerDefensePacketOverride();
			towerDefensePacketOverride.characterOverride = towerDefenseCharacterOverride;
			packetConfig._override = towerDefensePacketOverride;
			SpawnPacket(packetConfig, GetLogicalGlobalPosition(), 15.0, isFall: false);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "canCopy", canCopy } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		canCopy = data.GetValueOrDefault("canCopy", Variant.From<bool>(true)).AsBool();
	}
}
