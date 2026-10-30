using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseLevelLookStarManagerConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool open;

	[Export(PropertyHint.Range, "0.05,10.0,0.05")]
	public double checkInterval = 0.5;

	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float previewOpacity = 0.5f;

	[Export(PropertyHint.None, "")]
	public int previewLayer = 2;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelLookStarCheckConfig> checkList = new Array<TowerDefenseLevelLookStarCheckConfig>();

	public void Init(Dictionary lookStarManagerData)
	{
		open = lookStarManagerData.GetValueOrDefault("Open", false).AsBool();
		checkInterval = Math.Max(0.05, lookStarManagerData.GetValueOrDefault("CheckInterval", 0.5).AsDouble());
		previewOpacity = Mathf.Clamp((float)lookStarManagerData.GetValueOrDefault("PreviewOpacity", 0.5).AsDouble(), 0f, 1f);
		previewLayer = lookStarManagerData.GetValueOrDefault("PreviewLayer", 2).AsInt32();
		checkList.Clear();
		foreach (Variant item in lookStarManagerData.ContainsKey("Check") ? ((Godot.Collections.Array)lookStarManagerData["Check"]) : new Godot.Collections.Array())
		{
			Dictionary data = item.AsGodotDictionary();
			TowerDefenseLevelLookStarCheckConfig towerDefenseLevelLookStarCheckConfig = new TowerDefenseLevelLookStarCheckConfig();
			towerDefenseLevelLookStarCheckConfig.Init(data);
			checkList.Add(towerDefenseLevelLookStarCheckConfig);
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Open"] = open,
			["CheckInterval"] = checkInterval,
			["PreviewOpacity"] = previewOpacity,
			["PreviewLayer"] = previewLayer,
			["Check"] = new Godot.Collections.Array()
		};
		foreach (TowerDefenseLevelLookStarCheckConfig check in checkList)
		{
			((Godot.Collections.Array)dictionary["Check"]).Add(check.Export());
		}
		return dictionary;
	}
}
