using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelPreSpawnConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

	[Export(PropertyHint.None, "")]
	public Vector2I gridPos;

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterOverride characterOverride;

	public void Init(Dictionary spawnDictionary)
	{
		packetName = spawnDictionary.GetValueOrDefault("Name", Variant.From<string>("")).AsString();
		Array array = spawnDictionary.GetValueOrDefault("GridPos", new Array { 0, 0 }).AsGodotArray();
		gridPos = new Vector2I(array[0].AsInt32(), array[1].AsInt32());
		characterOverride = new TowerDefenseCharacterOverride();
		characterOverride.Init(spawnDictionary.GetValueOrDefault("CharacterOverride", new Dictionary()).AsGodotDictionary());
	}

	public TowerDefenseCharacter SpawnCharacter(Vector2I _gridPos = default(Vector2I), bool editorPreviewMode = false)
	{
		if (_gridPos == default(Vector2I))
		{
			_gridPos = gridPos;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return null;
		}
		bool editorPreviewMode2;
		if (packetConfig.characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.PLANT))
		{
			TowerDefenseCharacter towerDefenseCharacter = FindPreSpawnPlantTarget(TowerDefenseManager.GetMapCell(_gridPos), packetConfig);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				return null;
			}
			editorPreviewMode2 = editorPreviewMode;
			return packetConfig.PlantOnPlant(towerDefenseCharacter, playAudio: false, default(EconomyAccountId), editorPreviewMode2);
		}
		Vector2I vector2I = _gridPos;
		editorPreviewMode2 = editorPreviewMode;
		return packetConfig.Plant(vector2I, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: false, editorPreviewMode2);
	}

	private static TowerDefenseCharacter FindPreSpawnPlantTarget(TowerDefenseCellInstance cell, TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(cell) || !GodotObject.IsInstanceValid(packet))
		{
			return null;
		}
		cell.ClearEmpty();
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (GodotObject.IsInstanceValid(character) && character is TowerDefensePlant && character.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && !character.die && !character.nearDie && GodotObject.IsInstanceValid(character.instance) && !character.instance.invincible && !character.instance.hologram && character.instance.canBeCollection && !character.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.PLANT))
			{
				return character;
			}
		}
		return null;
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Name"] = packetName,
			["GridPos"] = new Array { gridPos.X, gridPos.Y }
		};
		if (GodotObject.IsInstanceValid(characterOverride))
		{
			dictionary["CharacterOverride"] = characterOverride.Export();
		}
		return dictionary;
	}
}
