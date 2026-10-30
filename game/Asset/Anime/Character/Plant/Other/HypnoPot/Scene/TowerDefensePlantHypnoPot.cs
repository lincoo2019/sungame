using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHypnoPot : TowerDefensePlant
{
	private const string HypnoShroomPacketName = "PlantHypnoShroom";

	private const string HypnoSeedPacketName = "PlantHypnoSeed";

	private const double SpawnInterval = 25.0;

	private double _spawnTimer;

	private bool _over;

	public override void _Ready()
	{
		base._Ready();
		Engine.IsEditorHint();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && inGame && GodotObject.IsInstanceValid(instance) && !instance.sleep)
		{
			_spawnTimer += (float)delta;
			if (_spawnTimer >= 25.0)
			{
				_spawnTimer -= 25.0;
				TrySpawnHypnoShroom();
			}
		}
	}

	private void TrySpawnHypnoShroom()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		Vector2I vector2I = base.gridPos;
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantHypnoShroom");
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(vector2I);
			if (GodotObject.IsInstanceValid(mapCell) && mapCell.CanPacketPlant(packetConfig))
			{
				TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(vector2I);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter) && instance.hypnoses)
				{
					towerDefenseCharacter.Hypnoses();
				}
				return;
			}
		}
		TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig("PlantHypnoSeed");
		if (!GodotObject.IsInstanceValid(packetConfig2))
		{
			return;
		}
		if (instance.hypnoses)
		{
			if (!GodotObject.IsInstanceValid(packetConfig2._override))
			{
				packetConfig2._override = new TowerDefensePacketOverride();
			}
			packetConfig2._override.hypnoses = true;
		}
		SpawnPacket(packetConfig2, GetLogicalGlobalPosition(), 15.0, isFall: false);
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (instance.sleep || _over)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(character) && (character.instance.unUseBuffFlags & 8) != 0)
		{
			SkipInvincibleHurt(num);
		}
		else if (type == "Eat")
		{
			_over = true;
			if (GodotObject.IsInstanceValid(character))
			{
				character.Hypnoses();
			}
			Destroy();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["over"] = _over,
			["spawnTimer"] = _spawnTimer
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_over = data.GetValueOrDefault("over", false).AsBool();
		_spawnTimer = data.GetValueOrDefault("spawnTimer", 0.0).AsDouble();
	}
}
