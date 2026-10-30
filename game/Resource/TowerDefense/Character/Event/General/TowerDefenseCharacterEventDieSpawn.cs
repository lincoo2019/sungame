using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventDieSpawn : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

	[Export(PropertyHint.None, "")]
	public double percentage = 1.0;

	[Export(PropertyHint.None, "")]
	public bool useGridPos = true;

	[Export(PropertyHint.None, "")]
	public bool byCamp;

	[Export(PropertyHint.None, "")]
	public Vector2 offset = Vector2.Zero;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, packetName, percentage, useGridPos, byCamp, offset);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, packetName, percentage, useGridPos, byCamp, offset);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target.GetLogicalGlobalPosition(), target, packetName, percentage, useGridPos, byCamp, offset);
	}

	public override void Init(Dictionary valueDictionary)
	{
		packetName = valueDictionary.GetValueOrDefault("PacketName", Variant.From<string>("")).AsString();
		percentage = valueDictionary.GetValueOrDefault("Percentage", Variant.From<double>(1.0)).AsDouble();
		useGridPos = valueDictionary.GetValueOrDefault("UseGridPos", Variant.From<bool>(true)).AsBool();
		byCamp = valueDictionary.GetValueOrDefault("ByCamp", Variant.From<bool>(false)).AsBool();
		offset = valueDictionary.GetValueOrDefault("Offset", Variant.From<Vector2>(Vector2.Zero)).AsVector2();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "DieSpawn",
			["Value"] = new Dictionary
			{
				["PacketName"] = packetName,
				["Percentage"] = percentage,
				["UseGridPos"] = useGridPos,
				["ByCamp"] = byCamp,
				["Offset"] = offset
			}
		};
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, string packetName, double percentage, bool useGridPos, bool byCamp, Vector2 offset)
	{
		if ((double)GD.Randf() > percentage || string.IsNullOrEmpty(packetName))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2 pos2 = pos + offset;
		if (useGridPos)
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(target.gridPos);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && byCamp && target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				towerDefenseCharacter.Hypnoses();
			}
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter2 = packetConfig.Create(pos2, target.gridPos, target.groundHeight);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter2))
		{
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseCharacter2, forceReadableName: false, Node.InternalMode.Disabled);
			if (byCamp && target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				towerDefenseCharacter2.Hypnoses();
			}
		}
	}
}
