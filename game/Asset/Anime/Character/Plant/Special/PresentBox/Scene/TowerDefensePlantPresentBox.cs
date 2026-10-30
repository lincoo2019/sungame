using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPresentBox : TowerDefensePlant
{
	private static PackedScene _IMITATER_CLOUD;

	private ExplodeComponent _explodeComponent;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	[Export(PropertyHint.None, "")]
	public string packetBank { get; set; } = "";

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		_explodeComponent.CreateParticlesEffect();
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefensePacketBankData towerDefensePacketBankData = ((!(packetBank == "PlantPresentBox")) ? TowerDefenseManager.GetPacketBankData(packetBank) : TowerDefenseManager.GetPacketBankData(((double)GD.Randf() > 0.5) ? packetBank : "GeneralPlant"));
		if (!GodotObject.IsInstanceValid(towerDefensePacketBankData) || !GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		Array plantList = towerDefensePacketBankData.GetPlantList();
		if (plantList.Count == 0)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(packet))
		{
			plantList.Remove(packet.saveKey);
		}
		if (plantList.Count == 0)
		{
			return;
		}
		string text = plantList.PickRandom().AsString();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		cell.RemoveCharacter(this);
		while (plantList.Count > 1 && GodotObject.IsInstanceValid(packetConfig) && ((instance.hypnoses && (packetConfig.characterConfig.unUseBuffFlags & 8) != 0) || !cell.CanPacketPlant(packetConfig)))
		{
			plantList.Remove(text);
			text = plantList.PickRandom().AsString();
			packetConfig = TowerDefenseManager.GetPacketConfig(text);
		}
		if (plantList.Count <= 0 || !GodotObject.IsInstanceValid(packetConfig) || !cell.CanPacketPlant(packetConfig) || (instance.hypnoses && (packetConfig.characterConfig.unUseBuffFlags & 8) != 0))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return;
		}
		towerDefenseCharacter.CallDeferred("WakeUp");
		if (instance.hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(text, base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, instance.hypnoses);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "packetBank", packetBank } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		packetBank = data.GetValueOrDefault("packetBank", Variant.From<string>("")).AsString();
	}
}
