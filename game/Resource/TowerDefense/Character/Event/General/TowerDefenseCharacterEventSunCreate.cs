using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventSunCreate : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double num = 25.0;

	[Export(PropertyHint.None, "")]
	public bool dieCreate;

	[Export(PropertyHint.None, "")]
	public bool mustForzen;

	[Export(PropertyHint.None, "")]
	public bool fromPacket;

	[Export(PropertyHint.None, "")]
	public double fromPacketPercentage = 1.0;

	[Export(PropertyHint.None, "")]
	public bool byCamp;

	[Export(PropertyHint.None, "")]
	public bool forceBrainSun;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, num, dieCreate, mustForzen, fromPacket, fromPacketPercentage, byCamp, forceBrainSun);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, num * delta, dieCreate, mustForzen, fromPacket, fromPacketPercentage, byCamp, forceBrainSun);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, num, dieCreate, mustForzen, fromPacket, fromPacketPercentage, byCamp, forceBrainSun);
	}

	public override void Init(Dictionary valueDictionary)
	{
		num = valueDictionary.GetValueOrDefault("Num", 50.0).AsDouble();
		dieCreate = valueDictionary.GetValueOrDefault("DieCreate", false).AsBool();
		mustForzen = valueDictionary.GetValueOrDefault("MustForzen", false).AsBool();
		fromPacket = valueDictionary.GetValueOrDefault("FromPacket", false).AsBool();
		fromPacketPercentage = valueDictionary.GetValueOrDefault("FromPacketPercentage", 1.0).AsDouble();
		byCamp = valueDictionary.GetValueOrDefault("ByCamp", false).AsBool();
		forceBrainSun = valueDictionary.GetValueOrDefault("ForceBrainSun", false).AsBool();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "SunCreate",
			["Value"] = new Dictionary
			{
				["Num"] = num,
				["DieCreate"] = dieCreate,
				["MustForzen"] = mustForzen,
				["FromPacket"] = fromPacket,
				["FromPacketPercentage"] = fromPacketPercentage,
				["ByCamp"] = byCamp,
				["ForceBrainSun"] = forceBrainSun
			}
		};
	}

	public static void Run(TowerDefenseCharacter target, double _num = 25.0, bool _dieCreate = false, bool _mustForzen = false, bool _fromPacket = false, double _fromPacketPercentage = 1.0, bool _byCamp = false, bool _forceBrainSun = false)
	{
		if (!GodotObject.IsInstanceValid(target))
		{
			return;
		}
		bool flag = target.IsDie() || (target.nearDie && GodotObject.IsInstanceValid(target.instance) && target.instance.die);
		if ((_dieCreate && !flag) || (_mustForzen && !target.buff.BuffHas("Frozen")))
		{
			return;
		}
		long sunNum;
		if (_fromPacket)
		{
			if (!GodotObject.IsInstanceValid(target.packet))
			{
				return;
			}
			sunNum = (long)Math.Floor((double)target.packet.GetCost() * _fromPacketPercentage);
		}
		else
		{
			sunNum = (long)_num;
		}
		bool createBrainSun = _forceBrainSun || (_byCamp && target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		target.ReplicatedEventSunCreate(target.GetLogicalGlobalPosition(), sunNum, createBrainSun, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, Vector2.Zero);
	}
}
