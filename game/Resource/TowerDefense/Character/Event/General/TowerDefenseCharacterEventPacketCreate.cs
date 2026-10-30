using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventPacketCreate : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public bool usePacketBank;

	[Export(PropertyHint.None, "")]
	public string packetName = "";

	[Export(PropertyHint.None, "")]
	public string packetBankName = "GeneralPlant";

	[Export(PropertyHint.None, "")]
	public string categoryName = "";

	[Export(PropertyHint.None, "")]
	public double aliveTime = 15.0;

	[Export(PropertyHint.None, "")]
	public bool useCost;

	public TowerDefensePacketOverride _override;

	[Export(PropertyHint.None, "")]
	public TowerDefensePacketOverride @override
	{
		get
		{
			return _override;
		}
		set
		{
			_override = value;
		}
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property.ToString() == "_override")
		{
			_override = value.As<TowerDefensePacketOverride>();
			return true;
		}
		return false;
	}

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, usePacketBank, packetName, packetBankName, categoryName, aliveTime, useCost, _override);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, usePacketBank, packetName, packetBankName, categoryName, aliveTime, useCost, _override);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, usePacketBank, packetName, packetBankName, categoryName, aliveTime, useCost, _override);
	}

	public override void Init(Dictionary valueDictionary)
	{
		usePacketBank = valueDictionary.GetValueOrDefault("UsePacketBank", false).AsBool();
		packetName = valueDictionary.GetValueOrDefault("PacketName", "").AsString();
		packetBankName = valueDictionary.GetValueOrDefault("PacketBankName", "GeneralPlant").AsString();
		categoryName = valueDictionary.GetValueOrDefault("CategoryName", "").AsString();
		aliveTime = valueDictionary.GetValueOrDefault("AliveTime", 15.0).AsDouble();
		useCost = valueDictionary.GetValueOrDefault("UseCost", false).AsBool();
		Dictionary dictionary = valueDictionary.GetValueOrDefault("Override", new Dictionary()).AsGodotDictionary();
		if (dictionary.Count > 0)
		{
			_override = new TowerDefensePacketOverride();
			_override.Init(dictionary);
		}
	}

	public override Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["EventName"] = "PacketCreate",
			["Value"] = new Dictionary
			{
				["PacketName"] = packetName,
				["PacketBankName"] = packetBankName,
				["CategoryName"] = categoryName,
				["UsePacketBank"] = usePacketBank,
				["AliveTime"] = aliveTime,
				["UseCost"] = useCost
			}
		};
		if (GodotObject.IsInstanceValid(_override))
		{
			((Dictionary)dictionary["Value"])["Override"] = _override.Export();
		}
		return dictionary;
	}

	public static void Run(TowerDefenseCharacter target, bool _usePacketBank, string _packetName, string _packetBankName, string _categoryName, double _aliveTime = 8.0, bool _useCost = false, TowerDefensePacketOverride _override = null)
	{
		string text;
		if (_usePacketBank)
		{
			TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(_packetBankName);
			Array array = ((!(_categoryName != "")) ? packetBankData.GetPacketList() : packetBankData.GetCategory(_categoryName));
			text = (string)array[(int)((ulong)GD.Randi() % (ulong)array.Count)];
		}
		else
		{
			text = _packetName;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (GodotObject.IsInstanceValid(_override))
		{
			packetConfig._override = _override.Duplicate(deep: true) as TowerDefensePacketOverride;
		}
		TowerDefenseManager.Instance.SpawnPacket(packetConfig, target.GetLogicalGlobalPosition(), _aliveTime, isFall: false, _useCost);
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
		towerDefenseInGamePacketShow.showCost = _useCost;
		towerDefenseInGamePacketShow.aliveTime = _aliveTime;
	}
}
