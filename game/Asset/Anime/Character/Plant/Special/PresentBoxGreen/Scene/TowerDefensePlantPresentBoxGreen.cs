using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPresentBoxGreen : TowerDefensePlant
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
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData("PlantPresentBox");
		if (!GodotObject.IsInstanceValid(packetBankData) || !GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		Godot.Collections.Array plantList = packetBankData.GetPlantList();
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
		bool flag = false;
		while (plantList.Count > 1 && GodotObject.IsInstanceValid(packetConfig) && ((instance.hypnoses && (packetConfig.characterConfig.unUseBuffFlags & 8) != 0) || !cell.CanPacketPlant(packetConfig)))
		{
			plantList.Remove(text);
			text = plantList.PickRandom().AsString();
			packetConfig = TowerDefenseManager.GetPacketConfig(text);
		}
		if (plantList.Count > 0 && GodotObject.IsInstanceValid(packetConfig) && cell.CanPacketPlant(packetConfig) && (!instance.hypnoses || (packetConfig.characterConfig.unUseBuffFlags & 8) == 0))
		{
			EconomyAccountId accountId = (base.HasEconomyOwner ? base.EconomyOwnerAccountId : EconomyAccountId.Local);
			long num = Math.Max(0, packetConfig.characterConfig.cost);
			SunSpendReceipt receipt = null;
			bool flag2 = num > 0 && TowerDefenseManager.Instance.TryBeginSunSpend(accountId, num, out receipt);
			long value = 0L;
			if (num > 0 && !flag2)
			{
				if (!TowerDefenseManager.Instance.TryGetSun(accountId, out var balance))
				{
					RestoreCellOccupancy();
					return;
				}
				value = ((balance < -9223372036854775808L + num) ? (-9223372036854775808L) : (balance - num));
			}
			TowerDefenseCharacter towerDefenseCharacter = (base.HasEconomyOwner ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos) : packetConfig.Plant(base.gridPos));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				flag = true;
				if (flag2)
				{
					receipt?.TryCommit();
				}
				else if (num > 0)
				{
					TowerDefenseManager.Instance.SetSun(accountId, value, SunTransactionReason.Cost);
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
			else
			{
				receipt?.TryRollback();
			}
		}
		if (!flag)
		{
			RestoreCellOccupancy();
		}
	}

	private void RestoreCellOccupancy()
	{
		if (GodotObject.IsInstanceValid(cell) && GodotObject.IsInstanceValid(this) && !die && !nearDie && GodotObject.IsInstanceValid(packet))
		{
			cell.CharacterPlant(packet, this, noLimit: true);
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
