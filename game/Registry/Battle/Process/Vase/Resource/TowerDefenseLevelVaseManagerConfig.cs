using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelVaseManagerConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool shuffle = true;

	[Export(PropertyHint.None, "")]
	public bool mowerUse;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.LEVEL_SEEDBANK_METHOD packetBankMethod;

	[Export(PropertyHint.Range, "0,5,0.05")]
	public double packetBankExitDelay = 0.5;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelVaseConfig> vaseList = new Array<TowerDefenseLevelVaseConfig>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelVaseFillConfig> vaseFillList = new Array<TowerDefenseLevelVaseFillConfig>();

	public void Init(Dictionary data)
	{
		vaseList.Clear();
		vaseFillList.Clear();
		shuffle = data.GetValueOrDefault("Shuffle", false).AsBool();
		mowerUse = data.GetValueOrDefault("MowerUse", false).AsBool();
		Variant valueOrDefault = data.GetValueOrDefault("PacketBankMethod", 0);
		packetBankMethod = ((valueOrDefault.VariantType == Variant.Type.String && Enum.TryParse<TowerDefenseEnum.LEVEL_SEEDBANK_METHOD>(valueOrDefault.AsString(), ignoreCase: true, out var result)) ? result : ((TowerDefenseEnum.LEVEL_SEEDBANK_METHOD)valueOrDefault.AsInt32()));
		double num = data.GetValueOrDefault("PacketBankExitDelay", 0.5).AsDouble();
		packetBankExitDelay = (double.IsFinite(num) ? Math.Clamp(num, 0.0, 5.0) : 0.5);
		foreach (Variant item in data.ContainsKey("Vase") ? ((Godot.Collections.Array)data["Vase"]) : new Godot.Collections.Array())
		{
			Dictionary data2 = item.AsGodotDictionary();
			TowerDefenseLevelVaseConfig towerDefenseLevelVaseConfig = new TowerDefenseLevelVaseConfig();
			towerDefenseLevelVaseConfig.Init(data2);
			vaseList.Add(towerDefenseLevelVaseConfig);
		}
		foreach (Variant item2 in data.ContainsKey("VaseFill") ? ((Godot.Collections.Array)data["VaseFill"]) : new Godot.Collections.Array())
		{
			Dictionary data3 = item2.AsGodotDictionary();
			TowerDefenseLevelVaseFillConfig towerDefenseLevelVaseFillConfig = new TowerDefenseLevelVaseFillConfig();
			towerDefenseLevelVaseFillConfig.Init(data3);
			vaseFillList.Add(towerDefenseLevelVaseFillConfig);
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Shuffle"] = shuffle,
			["MowerUse"] = mowerUse,
			["PacketBankMethod"] = (int)packetBankMethod,
			["PacketBankExitDelay"] = packetBankExitDelay,
			["Vase"] = new Godot.Collections.Array(),
			["VaseFill"] = new Godot.Collections.Array()
		};
		foreach (TowerDefenseLevelVaseConfig vase in vaseList)
		{
			((Godot.Collections.Array)dictionary["Vase"]).Add(vase.Export());
		}
		foreach (TowerDefenseLevelVaseFillConfig vaseFill in vaseFillList)
		{
			((Godot.Collections.Array)dictionary["VaseFill"]).Add(vaseFill.Export());
		}
		return dictionary;
	}
}
