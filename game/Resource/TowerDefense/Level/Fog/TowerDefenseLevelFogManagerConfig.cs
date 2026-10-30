using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseLevelFogManagerConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool open;

	[Export(PropertyHint.None, "")]
	public int beginColumn = 5;

	[Export(PropertyHint.None, "")]
	public int extraColumns = 10;

	[Export(PropertyHint.None, "")]
	public double blowReturnDelay = 25.0;

	[Export(PropertyHint.None, "")]
	public float blowDistance = 1400f;

	[Export(PropertyHint.None, "")]
	public double blowDuration = 1.5;

	[Export(PropertyHint.None, "")]
	public double returnDuration = 3.0;

	[Export(PropertyHint.None, "")]
	public double entryDuration = 3.0;

	[Export(PropertyHint.None, "")]
	public float entryStartX = 1350f;

	[ExportCategory("MagicFog")]
	[Export(PropertyHint.None, "")]
	public bool magicOpen;

	[Export(PropertyHint.None, "")]
	public string magicColor = "Random";

	[Export(PropertyHint.None, "")]
	public double magicChangeInterval = 20.0;

	private const double DefaultMagicDamagePercentPerSecond = 0.05;

	private const double DefaultMagicHealPerSecond = 50.0;

	[Export(PropertyHint.None, "")]
	public double magicDamagePercentPerSecond = 0.05;

	[Export(PropertyHint.None, "")]
	public double magicHealPerSecond = 50.0;

	public void Init(Dictionary fogManagerData)
	{
		open = fogManagerData.GetValueOrDefault("Open", false).AsBool();
		beginColumn = fogManagerData.GetValueOrDefault("BeginColumn", 5).AsInt32();
		extraColumns = Mathf.Max(0, fogManagerData.GetValueOrDefault("ExtraColumns", 10).AsInt32());
		blowReturnDelay = Mathf.Max(0.001, fogManagerData.GetValueOrDefault("BlowReturnDelay", 25.0).AsDouble());
		blowDistance = Mathf.Max(0f, (float)fogManagerData.GetValueOrDefault("BlowDistance", 1400.0).AsDouble());
		blowDuration = Mathf.Max(0.001, fogManagerData.GetValueOrDefault("BlowDuration", 1.5).AsDouble());
		returnDuration = Mathf.Max(0.001, fogManagerData.GetValueOrDefault("ReturnDuration", 3.0).AsDouble());
		entryDuration = Mathf.Max(0.001, fogManagerData.GetValueOrDefault("EntryDuration", 3.0).AsDouble());
		entryStartX = (float)fogManagerData.GetValueOrDefault("EntryStartX", 1350.0).AsDouble();
		magicOpen = fogManagerData.GetValueOrDefault("MagicOpen", false).AsBool();
		magicColor = fogManagerData.GetValueOrDefault("MagicColor", "Random").AsString();
		magicDamagePercentPerSecond = Mathf.Max(0.0, fogManagerData.GetValueOrDefault("MagicDamagePercentPerSecond", 0.05).AsDouble());
		magicHealPerSecond = Mathf.Max(0.0, fogManagerData.GetValueOrDefault("MagicHealPerSecond", 50.0).AsDouble());
		magicChangeInterval = Mathf.Max(0.0, fogManagerData.GetValueOrDefault("MagicChangeInterval", 20.0).AsDouble());
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["Open"] = open,
			["BeginColumn"] = beginColumn,
			["ExtraColumns"] = extraColumns,
			["BlowReturnDelay"] = blowReturnDelay,
			["BlowDistance"] = blowDistance,
			["BlowDuration"] = blowDuration,
			["ReturnDuration"] = returnDuration,
			["EntryDuration"] = entryDuration,
			["EntryStartX"] = entryStartX,
			["MagicOpen"] = magicOpen,
			["MagicColor"] = magicColor,
			["MagicChangeInterval"] = magicChangeInterval,
			["MagicDamagePercentPerSecond"] = magicDamagePercentPerSecond,
			["MagicHealPerSecond"] = magicHealPerSecond
		};
	}
}
