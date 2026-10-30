using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventPacketSpawn : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

	[Export(PropertyHint.None, "")]
	public double percentage = 1.0;

	[Export(PropertyHint.None, "")]
	public bool dieSpawn;

	[Export(PropertyHint.None, "")]
	public bool byCamp;

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterOverride characterOverride;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, packetName, percentage, dieSpawn, byCamp, target.camp, characterOverride);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, packetName, percentage, dieSpawn, byCamp, target.camp, characterOverride);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, packetName, percentage, dieSpawn, byCamp, projectile.camp, characterOverride);
	}

	public override void Init(Dictionary valueDictionary)
	{
		packetName = valueDictionary.GetValueOrDefault("PacketName", "").AsString();
		percentage = valueDictionary.GetValueOrDefault("Percentage", 1.0).AsDouble();
		dieSpawn = valueDictionary.GetValueOrDefault("DieSpawn", false).AsBool();
		byCamp = valueDictionary.GetValueOrDefault("ByCamp", false).AsBool();
		Dictionary dictionary = valueDictionary.GetValueOrDefault("CharacterOverride", new Dictionary()).AsGodotDictionary();
		if (dictionary.Count > 0)
		{
			characterOverride = new TowerDefenseCharacterOverride();
			characterOverride.Init(dictionary);
		}
	}

	public override Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["EventName"] = "PacketSpawn",
			["Value"] = new Dictionary
			{
				["PacketName"] = packetName,
				["Percentage"] = percentage,
				["DieSpawn"] = dieSpawn,
				["ByCamp"] = byCamp
			}
		};
		if (GodotObject.IsInstanceValid(characterOverride))
		{
			((Dictionary)dictionary["Value"])["CharacterOverride"] = characterOverride.Export();
		}
		return dictionary;
	}

	public static void Run(TowerDefenseCharacter target, string _packetName, double _percentage, bool _dieSpawn = false, bool _byCamp = false, TowerDefenseEnum.CHARACTER_CAMP _sourceCamp = TowerDefenseEnum.CHARACTER_CAMP.PLANT, TowerDefenseCharacterOverride _characterOverride = null)
	{
		if ((double)GD.Randf() > _percentage || (_dieSpawn && (!_dieSpawn || (!target.instance.die && !target.instance.nearDie))))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(_packetName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_characterOverride))
		{
			if (!GodotObject.IsInstanceValid(packetConfig._override))
			{
				packetConfig._override = new TowerDefensePacketOverride();
			}
			packetConfig._override.characterOverride = _characterOverride;
		}
		TowerDefenseCharacter towerDefenseCharacter = ((!(packetConfig.characterConfig is TowerDefenseZombieConfig)) ? packetConfig.Plant(target.gridPos) : SpawnZombieAtTarget(packetConfig, target));
		if (GodotObject.IsInstanceValid(towerDefenseCharacter) && _byCamp && towerDefenseCharacter.camp != _sourceCamp)
		{
			towerDefenseCharacter.Hypnoses();
		}
	}

	private static TowerDefenseCharacter SpawnZombieAtTarget(TowerDefensePacketConfig packetConfig, TowerDefenseCharacter target)
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Create(Vector2.Zero, target.gridPos, target.groundHeight);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return null;
		}
		characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, Node.InternalMode.Disabled);
		if (!(towerDefenseCharacter.gridPos != target.gridPos))
		{
			towerDefenseCharacter.SetLogicalGlobalPosition(target.GetLogicalGlobalPosition());
		}
		if (packetConfig.packetFlip)
		{
			towerDefenseCharacter.Scale = new Vector2(0f - towerDefenseCharacter.Scale.X, towerDefenseCharacter.Scale.Y);
		}
		if (packetConfig.GetHypnoses())
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (GodotObject.IsInstanceValid(packetConfig._override?.characterOverride))
		{
			packetConfig._override.characterOverride.ExecuteCharacter(towerDefenseCharacter);
		}
		packetConfig.IsZombieWalk(towerDefenseCharacter);
		ShadowComponent shadowComponent = towerDefenseCharacter.shadowComponent;
		if (shadowComponent != null && !shadowComponent.IsReleased)
		{
			towerDefenseCharacter.shadowComponent.Init();
			towerDefenseCharacter.shadowComponent.UpdateShadow();
		}
		return towerDefenseCharacter;
	}
}
