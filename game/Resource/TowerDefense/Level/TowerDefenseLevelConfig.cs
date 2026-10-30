using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseLevelConfig : TowerDefenseLevelBaseConfig
{
	private static class EnumCache<T> where T : struct, Enum
	{
		private static readonly System.Collections.Generic.Dictionary<string, T> _map = BuildMap();

		private static System.Collections.Generic.Dictionary<string, T> BuildMap()
		{
			System.Collections.Generic.Dictionary<string, T> dictionary = new System.Collections.Generic.Dictionary<string, T>(StringComparer.Ordinal);
			T[] values = Enum.GetValues<T>();
			for (int i = 0; i < values.Length; i++)
			{
				T value = values[i];
				dictionary[value.ToString().ToUpperInvariant()] = value;
			}
			return dictionary;
		}

		public static T Parse(string value, T defaultValue)
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			if (!_map.TryGetValue(value.ToUpperInvariant(), out var value2))
			{
				return defaultValue;
			}
			return value2;
		}
	}

	private bool _canExport;

	private bool _eventDataEditedFromEditor;

	private bool _preSpawnDataEditedFromEditor;

	private Json _data;

	[Export(PropertyHint.None, "")]
	public Godot.Collections.Dictionary<StringName, Dictionary> featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();

	[Export(PropertyHint.None, "")]
	public StringName processName;

	[Export(PropertyHint.None, "")]
	public Dictionary processData = new Dictionary();

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.LEVEL_FINISH_METHOD finishMethod;

	[Export(PropertyHint.None, "")]
	public double baseTimeScale = 1.0;

	[ExportCategory("Mower")]
	[Export(PropertyHint.None, "")]
	public bool mowerUse = true;

	[ExportCategory("Tutorial")]
	[Export(PropertyHint.None, "")]
	public Variant talk = "";

	[Export(PropertyHint.None, "")]
	public Variant tutorial = "";

	[ExportCategory("Map")]
	[Export(PropertyHint.None, "")]
	public string map = "Frontlawn";

	[ExportCategory("BGM")]
	[Export(PropertyHint.None, "")]
	public string backgroundMusic = "Frontlawn";

	[ExportCategory("Reward")]
	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.LEVEL_REWARDTYPE firstRewardType = TowerDefenseEnum.LEVEL_REWARDTYPE.COIN;

	[Export(PropertyHint.None, "")]
	public Variant firstRewardValue = 2000;

	[ExportCategory("Event")]
	[Export(PropertyHint.Range, "0,120,0.1")]
	public double eventRemotePhaseTimeoutSeconds = 10.0;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelEventBase> eventInit = new Array<TowerDefenseLevelEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelEventBase> eventEntry = new Array<TowerDefenseLevelEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelEventBase> eventReady = new Array<TowerDefenseLevelEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelEventBase> eventStart = new Array<TowerDefenseLevelEventBase>();

	[ExportCategory("PreSpawn")]
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelPreSpawnConfig> preSpawnList = new Array<TowerDefenseLevelPreSpawnConfig>();

	[ExportCategory("PacketBank")]
	[Export(PropertyHint.None, "")]
	public int limitGridPlantNum = -1;

	[Export(PropertyHint.None, "")]
	public bool plantColumn;

	[Export(PropertyHint.None, "")]
	public bool packetColdDownStart = true;

	[Export(PropertyHint.None, "")]
	public bool packetColdDownUse = true;

	private TowerDefenseEnum.LEVEL_SEEDBANK_METHOD _packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE;

	[ExportCategory("Effect")]
	[Export(PropertyHint.None, "")]
	public bool stormOpen;

	[ExportCategory("Sun")]
	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelSunManagerConfig sunManager;

	[ExportCategory("Fog")]
	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelFogManagerConfig fogManager;

	[ExportCategory("LookStar")]
	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelLookStarManagerConfig lookStarManager;

	[ExportCategory("Wave")]
	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelWaveManagerConfig waveManager;

	[ExportCategory("Vase")]
	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelVaseManagerConfig vaseManager;

	[ExportCategory("Vase")]
	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelIZMManagerConfig izmManager;

	[ExportCategory("Option")]
	[Export(PropertyHint.None, "")]
	public bool isCustomTalk;

	[Export(PropertyHint.None, "")]
	public bool isCustomTutorial;

	[Export(PropertyHint.None, "")]
	public NpcTalkConfig customTalk;

	[Export(PropertyHint.None, "")]
	public TutorialConfig customTutorial;

	[Export(PropertyHint.None, "")]
	public string packetBank = "GeneralPlant";

	[Export(PropertyHint.None, "")]
	public Godot.Collections.Array packetBankList = new Godot.Collections.Array();

	[Export(PropertyHint.None, "")]
	public TowerDefenseConveyorConfig conveyorData;

	[Export(PropertyHint.None, "")]
	public TowerDefenseRainModeConfig rainData;

	[Export(PropertyHint.None, "")]
	public bool canExport
	{
		get
		{
			return _canExport;
		}
		set
		{
			_canExport = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public Json data
	{
		get
		{
			return _data;
		}
		set
		{
			_data = value;
			Init();
		}
	}

	public bool HasPendingEditorFeatureChanges
	{
		get
		{
			if (!_eventDataEditedFromEditor)
			{
				return _preSpawnDataEditedFromEditor;
			}
			return true;
		}
	}

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.LEVEL_SEEDBANK_METHOD packetBankMethod
	{
		get
		{
			return _packetBankMethod;
		}
		set
		{
			_packetBankMethod = value;
			NotifyPropertyListChanged();
		}
	}

	private static bool HasProcessName(StringName value)
	{
		if (value != null)
		{
			return !value.IsEmpty;
		}
		return false;
	}

	private static bool TryGetFinishMethodFromProcessName(string processName, out TowerDefenseEnum.LEVEL_FINISH_METHOD finishMethod)
	{
		switch (processName)
		{
		case "Wave":
			finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE;
			return true;
		case "Vase":
			finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE;
			return true;
		case "IZM":
			finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM;
			return true;
		case "Quiz":
			finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ;
			return true;
		case "IZM2":
			finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2;
			return true;
		case "Empty":
			finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.EMPTY;
			return true;
		default:
			finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE;
			return false;
		}
	}

	private static Dictionary GetDict(Dictionary src, string key)
	{
		if (src != null && src.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Dictionary)
		{
			return value.AsGodotDictionary();
		}
		return new Dictionary();
	}

	private static Godot.Collections.Array GetArray(Dictionary src, string key)
	{
		if (src != null && src.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Array)
		{
			return (Godot.Collections.Array)value;
		}
		return new Godot.Collections.Array();
	}

	private Godot.Collections.Array ExportPacketBankList()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Variant packetBank in packetBankList)
		{
			if (packetBank.VariantType == Variant.Type.Object)
			{
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = packetBank.As<TowerDefenseLevelPacketConfig>();
				if (towerDefenseLevelPacketConfig != null)
				{
					array.Add(towerDefenseLevelPacketConfig.Export());
				}
			}
			else if (packetBank.VariantType == Variant.Type.String)
			{
				array.Add(packetBank);
			}
		}
		return array;
	}

	public bool SetFinishMethodFromEditor(TowerDefenseEnum.LEVEL_FINISH_METHOD value)
	{
		if (finishMethod == value)
		{
			return false;
		}
		finishMethod = value;
		if (value != TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE && value != TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2)
		{
			featureData?.Remove("Wave");
		}
		processName = "";
		processData = new Dictionary();
		return true;
	}

	public bool SetMapFromEditor(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		bool result = !string.Equals(map, value, StringComparison.Ordinal) || !HasSynchronizedMapFeature(value);
		map = value;
		SynchronizeMapFeature();
		return result;
	}

	public bool SetPacketBankMethodFromEditor(TowerDefenseEnum.LEVEL_SEEDBANK_METHOD value)
	{
		if (packetBankMethod == value)
		{
			return false;
		}
		packetBankMethod = value;
		featureData?.Remove("RainMode");
		featureData?.Remove("ConveyorBelt");
		featureData?.Remove("SeedBank");
		featureData?.Remove("PacketBank");
		if (featureData != null && featureData.TryGetValue(new StringName("ScreenEffect"), out var value2) && value2 != null)
		{
			value2["PacketBankMethod"] = (int)value;
		}
		if (processData != null && processData.ContainsKey("PacketBankMethod"))
		{
			processData["PacketBankMethod"] = (int)value;
		}
		return true;
	}

	public void MarkEventDataEditedFromEditor()
	{
		_eventDataEditedFromEditor = true;
		ApplyEditedEventFeatureData();
	}

	public void MarkPreSpawnDataEditedFromEditor()
	{
		_preSpawnDataEditedFromEditor = true;
		ApplyEditedPreSpawnFeatureData();
	}

	public void PrepareForEditorSave()
	{
		ApplyEditedFeatureData();
		ExportToFeatureProcess(refreshEditorValues: true);
		SynchronizeMapFeature();
		ApplyEditedFeatureData();
		RefreshEditorDataSnapshot();
		_eventDataEditedFromEditor = false;
		_preSpawnDataEditedFromEditor = false;
	}

	private void RefreshEditorDataSnapshot()
	{
		Dictionary dictionary = Export();
		Json json = new Json();
		if (json.Parse(Json.Stringify(dictionary)) != Error.Ok)
		{
			GD.PushError("[TowerDefenseLevelConfig] Failed to refresh editor JSON snapshot.");
		}
		else
		{
			_data = json;
		}
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 6L;
		Array<Dictionary> array = new Array<Dictionary>();
		if (packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE)
		{
			array.Add(new Dictionary
			{
				["name"] = "PacketBank/Name",
				["type"] = 4,
				["usage"] = num
			});
		}
		return array;
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property.ToString() == "PacketBank/Name")
		{
			packetBank = value.AsString();
			return true;
		}
		return false;
	}

	public override Variant _Get(StringName property)
	{
		if (property.ToString() == "PacketBank/Name")
		{
			return packetBank;
		}
		return default(Variant);
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		if (property.ToString() == "PacketBank/Name")
		{
			return true;
		}
		return false;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		if (property.ToString() == "PacketBank/Name")
		{
			return "";
		}
		return default(Variant);
	}

	public void Clear()
	{
		homeWorld = GeneralEnum.HOMEWORLD.NOONE;
		finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE;
		packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE;
		eventInit.Clear();
		eventEntry.Clear();
		eventReady.Clear();
		eventStart.Clear();
		sunManager = null;
		fogManager = null;
		lookStarManager = null;
		waveManager = null;
		vaseManager = null;
		izmManager = null;
		isCustomTalk = false;
		isCustomTutorial = false;
		customTalk = null;
		customTutorial = null;
		packetBankList.Clear();
		conveyorData = null;
		rainData = null;
		plantColumn = false;
		limitGridPlantNum = -1;
		packetColdDownStart = true;
		packetColdDownUse = true;
		featureData.Clear();
		processName = "";
		processData = new Dictionary();
	}

	public override void Init()
	{
		if (!GodotObject.IsInstanceValid(data))
		{
			ExportToFeatureProcess();
			return;
		}
		Clear();
		Dictionary dictionary = data.Data.AsGodotDictionary();
		featureData.Clear();
		foreach (Variant item in GetArray(dictionary, "Feature"))
		{
			Dictionary dictionary2 = item.AsGodotDictionary();
			StringName key = new StringName(dictionary2.GetValueOrDefault("Name", "").AsString());
			Dictionary dict = GetDict(dictionary2, "Data");
			featureData[key] = dict;
		}
		Dictionary dict2 = GetDict(dictionary, "Process");
		if (dict2.Count > 0)
		{
			processName = dict2.GetValueOrDefault("Name", "").AsString();
			processData = GetDict(dict2, "Data");
		}
		name = dictionary.GetValueOrDefault("Name", "").AsString();
		levelName = dictionary.GetValueOrDefault("LevelName", "").AsString();
		description = dictionary.GetValueOrDefault("Description", "").AsString();
		levelNumber = dictionary.GetValueOrDefault("LevelNumber", 0).AsInt32();
		nextLevel = dictionary.GetValueOrDefault("NextLevel", "").AsString();
		homeWorld = EnumCache<GeneralEnum.HOMEWORLD>.Parse(dictionary.GetValueOrDefault("HomeWorld", "NOONE").AsString(), GeneralEnum.HOMEWORLD.NOONE);
		finishMethod = EnumCache<TowerDefenseEnum.LEVEL_FINISH_METHOD>.Parse(dictionary.GetValueOrDefault("FinishMethod", "WAVE").AsString(), TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE);
		if (!dictionary.ContainsKey("FinishMethod") && TryGetFinishMethodFromProcessName(processName.ToString(), out var lEVEL_FINISH_METHOD))
		{
			finishMethod = lEVEL_FINISH_METHOD;
		}
		baseTimeScale = dictionary.GetValueOrDefault("BaseTimeScale", 1.0).AsDouble();
		mowerUse = (processData.TryGetValue("MowerUse", out var value) ? value.AsBool() : dictionary.GetValueOrDefault("MowerUse", true).AsBool());
		if (featureData.TryGetValue(new StringName("NpcTalk"), out var value2) && value2 != null && value2.Count > 0)
		{
			if (value2.ContainsKey("TalkName"))
			{
				isCustomTalk = false;
				talk = value2.GetValueOrDefault("TalkName", "").AsString();
			}
			else
			{
				isCustomTalk = true;
				customTalk = new NpcTalkConfig();
				customTalk.Load(value2);
			}
		}
		else
		{
			Variant valueOrDefault = dictionary.GetValueOrDefault("Talk", "");
			if (valueOrDefault.VariantType == Variant.Type.Dictionary)
			{
				isCustomTalk = true;
				customTalk = new NpcTalkConfig();
				customTalk.Load(valueOrDefault.AsGodotDictionary());
			}
			else
			{
				isCustomTalk = false;
				talk = valueOrDefault;
			}
		}
		if (featureData.TryGetValue(new StringName("Tutorial"), out var value3) && value3 != null && value3.Count > 0)
		{
			if (value3.ContainsKey("TutorialName"))
			{
				isCustomTutorial = false;
				tutorial = value3.GetValueOrDefault("TutorialName", "").AsString();
			}
			else
			{
				isCustomTutorial = true;
				customTutorial = new TutorialConfig();
				customTutorial.Load(value3);
			}
		}
		else
		{
			Variant valueOrDefault2 = dictionary.GetValueOrDefault("Tutorial", "");
			if (valueOrDefault2.VariantType == Variant.Type.Dictionary)
			{
				isCustomTutorial = true;
				customTutorial = new TutorialConfig();
				customTutorial.Load(valueOrDefault2.AsGodotDictionary());
			}
			else
			{
				isCustomTutorial = false;
				tutorial = valueOrDefault2;
			}
		}
		if (featureData.TryGetValue(new StringName("Map"), out var value4) && value4 != null && value4.TryGetValue("MapName", out var value5))
		{
			map = value5.AsString();
		}
		else
		{
			map = dictionary.GetValueOrDefault("Map", map).AsString();
		}
		if (featureData.TryGetValue(new StringName("BGM"), out var value6) && value6 != null && value6.TryGetValue("BackgroundMusic", out var value7))
		{
			backgroundMusic = value7.AsString();
		}
		else
		{
			backgroundMusic = dictionary.GetValueOrDefault("BGM", backgroundMusic).AsString();
		}
		if (featureData.TryGetValue(new StringName("ScreenEffect"), out var value8) && value8 != null && value8.TryGetValue("StormOpen", out var value9))
		{
			stormOpen = value9.AsBool();
		}
		else
		{
			stormOpen = dictionary.GetValueOrDefault("StormOpen", false).AsBool();
		}
		Dictionary dict3 = GetDict(dictionary, "Reward");
		firstRewardType = EnumCache<TowerDefenseEnum.LEVEL_REWARDTYPE>.Parse(dict3.GetValueOrDefault("RewardType", "NOONE").AsString(), TowerDefenseEnum.LEVEL_REWARDTYPE.NOONE);
		firstRewardValue = dict3.GetValueOrDefault("RewardFirst", 2000);
		Dictionary dictionary3 = GetDict(dictionary, "Event");
		if (featureData.TryGetValue(new StringName("Event"), out var value10) && value10 != null && value10.Count > 0)
		{
			dictionary3 = value10;
		}
		eventRemotePhaseTimeoutSeconds = Math.Clamp(dictionary3.GetValueOrDefault("RemotePhaseTimeoutSeconds", 10.0).AsDouble(), 0.0, 120.0);
		foreach (Variant item2 in GetArray(dictionary3, "EventInit"))
		{
			Dictionary dictionary4 = item2.AsGodotDictionary();
			string text = dictionary4.GetValueOrDefault("EventName", "").AsString();
			if (text != "")
			{
				TowerDefenseLevelEventBase towerDefenseLevelEventBase = TowerDefenseLevelEventRegistry.Create(text);
				if (towerDefenseLevelEventBase == null)
				{
					GD.PushError("[TowerDefenseLevelConfig] EventInit 未知的 EventName: " + text);
					continue;
				}
				Dictionary dict4 = GetDict(dictionary4, "Value");
				towerDefenseLevelEventBase.Init(dict4);
				eventInit.Add(towerDefenseLevelEventBase);
			}
		}
		foreach (Variant item3 in GetArray(dictionary3, "EventEntry"))
		{
			Dictionary dictionary5 = item3.AsGodotDictionary();
			string text2 = dictionary5.GetValueOrDefault("EventName", "").AsString();
			if (text2 != "")
			{
				TowerDefenseLevelEventBase towerDefenseLevelEventBase2 = TowerDefenseLevelEventRegistry.Create(text2);
				if (towerDefenseLevelEventBase2 == null)
				{
					GD.PushError("[TowerDefenseLevelConfig] EventEntry 未知的 EventName: " + text2);
					continue;
				}
				Dictionary dict5 = GetDict(dictionary5, "Value");
				towerDefenseLevelEventBase2.Init(dict5);
				eventEntry.Add(towerDefenseLevelEventBase2);
			}
		}
		foreach (Variant item4 in GetArray(dictionary3, "EventReady"))
		{
			Dictionary dictionary6 = item4.AsGodotDictionary();
			string text3 = dictionary6.GetValueOrDefault("EventName", "").AsString();
			if (text3 != "")
			{
				TowerDefenseLevelEventBase towerDefenseLevelEventBase3 = TowerDefenseLevelEventRegistry.Create(text3);
				if (towerDefenseLevelEventBase3 == null)
				{
					GD.PushError("[TowerDefenseLevelConfig] EventReady 未知的 EventName: " + text3);
					continue;
				}
				Dictionary dict6 = GetDict(dictionary6, "Value");
				towerDefenseLevelEventBase3.Init(dict6);
				eventReady.Add(towerDefenseLevelEventBase3);
			}
		}
		foreach (Variant item5 in GetArray(dictionary3, "EventStart"))
		{
			Dictionary dictionary7 = item5.AsGodotDictionary();
			string text4 = dictionary7.GetValueOrDefault("EventName", "").AsString();
			if (text4 != "")
			{
				TowerDefenseLevelEventBase towerDefenseLevelEventBase4 = TowerDefenseLevelEventRegistry.Create(text4);
				if (towerDefenseLevelEventBase4 == null)
				{
					GD.PushError("[TowerDefenseLevelConfig] EventStart 未知的 EventName: " + text4);
					continue;
				}
				Dictionary dict7 = GetDict(dictionary7, "Value");
				towerDefenseLevelEventBase4.Init(dict7);
				eventStart.Add(towerDefenseLevelEventBase4);
			}
		}
		preSpawnList = new Array<TowerDefenseLevelPreSpawnConfig>();
		Dictionary src = GetDict(dictionary, "PreSpawn");
		if (featureData.TryGetValue(new StringName("PreSpawn"), out var value11) && value11 != null && value11.Count > 0)
		{
			src = value11;
		}
		foreach (Variant item6 in GetArray(src, "Packet"))
		{
			if (!GodotObject.IsInstanceValid(item6.AsGodotObject()) && item6.VariantType != Variant.Type.Nil)
			{
				TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = new TowerDefenseLevelPreSpawnConfig();
				towerDefenseLevelPreSpawnConfig.Init(item6.AsGodotDictionary());
				preSpawnList.Add(towerDefenseLevelPreSpawnConfig);
			}
		}
		Dictionary dict8 = GetDict(dictionary, "PacketBank");
		Godot.Collections.Array array = new Godot.Collections.Array();
		Dictionary value13;
		Dictionary value14;
		if (featureData.TryGetValue(new StringName("SeedBank"), out var value12) && value12 != null && value12.Count > 0)
		{
			limitGridPlantNum = value12.GetValueOrDefault("LimitGridPlantNum", -1).AsInt32();
			packetColdDownStart = value12.GetValueOrDefault("ColdDownStart", true).AsBool();
			packetColdDownUse = value12.GetValueOrDefault("ColdDownUse", true).AsBool();
			packetBankMethod = EnumCache<TowerDefenseEnum.LEVEL_SEEDBANK_METHOD>.Parse(value12.GetValueOrDefault("Method", "NOONE").AsString(), TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE);
			plantColumn = value12.GetValueOrDefault("PlantColumn", false).AsBool();
			array = GetArray(value12, "Packet");
		}
		else if (featureData.TryGetValue(new StringName("ConveyorBelt"), out value13) && value13 != null && value13.Count > 0)
		{
			packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR;
			conveyorData = new TowerDefenseConveyorConfig();
			conveyorData.Init(value13);
			plantColumn = value13.GetValueOrDefault("PlantColumn", false).AsBool();
		}
		else if (featureData.TryGetValue(new StringName("RainMode"), out value14) && value14 != null && value14.Count > 0)
		{
			packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN;
			rainData = new TowerDefenseRainModeConfig();
			rainData.Init(value14);
			plantColumn = value14.GetValueOrDefault("PlantColumn", false).AsBool();
		}
		else
		{
			limitGridPlantNum = dict8.GetValueOrDefault("LimitGridPlantNum", -1).AsInt32();
			packetColdDownStart = dict8.GetValueOrDefault("ColdDownStart", true).AsBool();
			packetColdDownUse = dict8.GetValueOrDefault("ColdDownUse", true).AsBool();
			packetBankMethod = EnumCache<TowerDefenseEnum.LEVEL_SEEDBANK_METHOD>.Parse(dict8.GetValueOrDefault("Method", "NOONE").AsString(), TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE);
			plantColumn = dict8.GetValueOrDefault("PlantColumn", false).AsBool();
			array = GetArray(dict8, "Value");
		}
		switch (packetBankMethod)
		{
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET:
			foreach (Variant item7 in array)
			{
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = new TowerDefenseLevelPacketConfig();
				towerDefenseLevelPacketConfig.Init(item7);
				packetBankList.Add(towerDefenseLevelPacketConfig);
			}
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE:
		{
			foreach (Variant item8 in array)
			{
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig3 = new TowerDefenseLevelPacketConfig();
				towerDefenseLevelPacketConfig3.Init(item8);
				packetBankList.Add(towerDefenseLevelPacketConfig3);
			}
			if (featureData.TryGetValue(new StringName("PacketBank"), out var value15) && value15 != null && value15.Count > 0)
			{
				packetBank = value15.GetValueOrDefault("PacketBankName", "").AsString();
			}
			else
			{
				packetBank = dict8.GetValueOrDefault("Type", "").AsString();
			}
			break;
		}
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR:
			if (conveyorData != null)
			{
				break;
			}
			conveyorData = new TowerDefenseConveyorConfig();
			if (array.Count == 1 && array[0].VariantType == Variant.Type.Dictionary && array[0].AsGodotDictionary().ContainsKey("Packet"))
			{
				conveyorData.Init(array[0].AsGodotDictionary());
				break;
			}
			conveyorData.Init(GetDict(dict8, "ConveyorPreset"));
			foreach (Variant item9 in array)
			{
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig2 = new TowerDefenseLevelPacketConfig();
				towerDefenseLevelPacketConfig2.Init(item9);
				conveyorData.packetPrioritySpawnList.Add(towerDefenseLevelPacketConfig2);
			}
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN:
			if (rainData == null)
			{
				rainData = new TowerDefenseRainModeConfig();
				if (array.Count == 1 && array[0].VariantType == Variant.Type.Dictionary && array[0].AsGodotDictionary().ContainsKey("Packet"))
				{
					rainData.Init(array[0].AsGodotDictionary());
				}
				else
				{
					rainData.Init(GetDict(dict8, "RainPreset"));
				}
			}
			break;
		}
		Dictionary sunManagerData = GetDict(dictionary, "SunManager");
		if (featureData.TryGetValue(new StringName("Sun"), out var value16) && value16 != null && value16.Count > 0)
		{
			sunManagerData = value16;
		}
		sunManager = new TowerDefenseLevelSunManagerConfig();
		sunManager.Init(sunManagerData);
		Dictionary fogManagerData = GetDict(dictionary, "FogManager");
		if (featureData.TryGetValue(new StringName("Fog"), out var value17) && value17 != null && value17.Count > 0)
		{
			fogManagerData = value17;
		}
		fogManager = new TowerDefenseLevelFogManagerConfig();
		fogManager.Init(fogManagerData);
		Dictionary lookStarManagerData = GetDict(dictionary, "LookStarManager");
		if (featureData.TryGetValue(new StringName("LookStar"), out var value18) && value18 != null && value18.Count > 0)
		{
			lookStarManagerData = value18;
		}
		lookStarManager = new TowerDefenseLevelLookStarManagerConfig();
		lookStarManager.Init(lookStarManagerData);
		switch (finishMethod)
		{
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE:
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2:
		{
			Dictionary waveManagerData = GetDict(dictionary, "WaveManager");
			if (featureData.TryGetValue(new StringName("Wave"), out var value19) && value19 != null && value19.Count > 0)
			{
				waveManagerData = value19;
			}
			waveManager = new TowerDefenseLevelWaveManagerConfig();
			waveManager.Init(waveManagerData);
			break;
		}
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE:
		{
			Dictionary dict10 = GetDict(dictionary, "VaseManager");
			if (processName == (StringName)"Vase" && processData.Count > 0)
			{
				dict10 = processData;
			}
			vaseManager = new TowerDefenseLevelVaseManagerConfig();
			vaseManager.Init(dict10);
			break;
		}
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
		{
			Dictionary dict9 = GetDict(dictionary, "IZMManager");
			if (processName == (StringName)"IZM" && processData.Count > 0)
			{
				dict9 = processData;
			}
			izmManager = new TowerDefenseLevelIZMManagerConfig();
			izmManager.Init(dict9);
			break;
		}
		}
		if (packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR)
		{
			featureData.Remove(new StringName("SeedBank"));
			featureData.Remove(new StringName("PacketBank"));
		}
		if (featureData.Count == 0)
		{
			BuildBaseFeatureData();
			if (isCustomTalk && GodotObject.IsInstanceValid(customTalk))
			{
				Dictionary dictionary8 = GetDict(dictionary, "Talk").Duplicate();
				dictionary8["isCustom"] = true;
				featureData["NpcTalk"] = dictionary8;
			}
			else if (talk.AsString() != "")
			{
				featureData["NpcTalk"] = new Dictionary { ["TalkName"] = talk };
			}
			Dictionary dict11 = GetDict(dictionary, "SunManager");
			if (dict11.Count > 0)
			{
				featureData["Sun"] = dict11;
			}
			BuildPacketBankFeatureData();
			featureData["BGM"] = new Dictionary { ["BackgroundMusic"] = backgroundMusic };
			Dictionary dict12 = GetDict(dictionary, "FogManager");
			if (dict12.Count > 0 && dict12.GetValueOrDefault("Open", false).AsBool())
			{
				featureData["Fog"] = dict12;
			}
			Dictionary dict13 = GetDict(dictionary, "LookStarManager");
			if (dict13.Count > 0 && dict13.GetValueOrDefault("Open", false).AsBool())
			{
				featureData["LookStar"] = dict13;
			}
			featureData["ScreenEffect"] = new Dictionary
			{
				["StormOpen"] = stormOpen,
				["PacketBankMethod"] = (int)packetBankMethod
			};
			if (isCustomTutorial && GodotObject.IsInstanceValid(customTutorial))
			{
				Dictionary dictionary9 = GetDict(dictionary, "Tutorial").Duplicate();
				dictionary9["isCustom"] = true;
				featureData["Tutorial"] = dictionary9;
			}
			else if (tutorial.AsString() != "")
			{
				featureData["Tutorial"] = new Dictionary { ["TutorialName"] = tutorial };
			}
			Dictionary dict14 = GetDict(dictionary, "Event");
			if (dict14.Count > 0)
			{
				featureData["Event"] = dict14;
			}
			if (finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE || finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2)
			{
				featureData["Wave"] = GetDict(dictionary, "WaveManager");
			}
		}
		MergeLegacyPreSpawnFeatureData(dictionary);
		if (!HasProcessName(processName))
		{
			switch (finishMethod)
			{
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE:
				processName = "Wave";
				processData["StormOpen"] = stormOpen;
				processData["MowerUse"] = mowerUse;
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2:
				processName = "IZM2";
				processData["StormOpen"] = stormOpen;
				processData["MowerUse"] = mowerUse;
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE:
				processName = "Vase";
				processData = GetDict(dictionary, "VaseManager").Duplicate();
				processData["PacketBankMethod"] = (int)packetBankMethod;
				processData["MowerUse"] = mowerUse;
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
				processName = "IZM";
				processData = GetDict(dictionary, "IZMManager").Duplicate();
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ:
				processName = "Quiz";
				break;
			}
		}
	}

	public Dictionary Export()
	{
		ExportToFeatureProcess();
		Dictionary dictionary = new Dictionary
		{
			["Name"] = name,
			["LevelName"] = levelName,
			["Description"] = description,
			["LevelNumber"] = levelNumber,
			["NextLevel"] = nextLevel,
			["HomeWorld"] = Enum.GetName(typeof(GeneralEnum.HOMEWORLD), homeWorld),
			["Version"] = "1.0",
			["Reward"] = new Dictionary
			{
				["RewardType"] = Enum.GetName(typeof(TowerDefenseEnum.LEVEL_REWARDTYPE), firstRewardType),
				["RewardFirst"] = firstRewardValue
			},
			["BaseTimeScale"] = baseTimeScale,
			["Feature"] = new Godot.Collections.Array(),
			["Process"] = new Dictionary()
		};
		foreach (StringName key14 in featureData.Keys)
		{
			if (!(key14 == (StringName)""))
			{
				((Godot.Collections.Array)dictionary["Feature"]).Add(new Dictionary
				{
					["Name"] = key14,
					["Data"] = featureData[key14]?.Duplicate(deep: true) ?? new Dictionary()
				});
			}
		}
		if (HasProcessName(processName))
		{
			dictionary["Process"] = new Dictionary
			{
				["Name"] = processName,
				["Data"] = processData?.Duplicate(deep: true) ?? new Dictionary()
			};
		}
		return dictionary;
	}

	private void BuildBaseFeatureData()
	{
		featureData["Camera"] = new Dictionary();
		featureData["Map"] = new Dictionary { ["MapName"] = map };
		featureData["Shovel"] = new Dictionary();
		featureData["PacketPick"] = new Dictionary();
		featureData["Mower"] = new Dictionary();
		featureData["Brain"] = new Dictionary();
		featureData["Progress"] = new Dictionary();
	}

	private Dictionary BuildPreSpawnFeatureData()
	{
		if (preSpawnList == null || preSpawnList.Count == 0)
		{
			return null;
		}
		Dictionary dictionary = new Dictionary { ["Packet"] = new Godot.Collections.Array() };
		foreach (TowerDefenseLevelPreSpawnConfig preSpawn in preSpawnList)
		{
			if (GodotObject.IsInstanceValid(preSpawn))
			{
				((Godot.Collections.Array)dictionary["Packet"]).Add(preSpawn.Export());
			}
		}
		return dictionary;
	}

	private Dictionary BuildEventFeatureData()
	{
		Dictionary obj = new Dictionary
		{
			["RemotePhaseTimeoutSeconds"] = eventRemotePhaseTimeoutSeconds,
			["EventInit"] = new Godot.Collections.Array(),
			["EventEntry"] = new Godot.Collections.Array(),
			["EventReady"] = new Godot.Collections.Array(),
			["EventStart"] = new Godot.Collections.Array()
		};
		AppendEventExports(obj, "EventInit", eventInit);
		AppendEventExports(obj, "EventEntry", eventEntry);
		AppendEventExports(obj, "EventReady", eventReady);
		AppendEventExports(obj, "EventStart", eventStart);
		return obj;
	}

	private static void AppendEventExports(Dictionary target, string key, Array<TowerDefenseLevelEventBase> events)
	{
		if (events == null)
		{
			return;
		}
		foreach (TowerDefenseLevelEventBase @event in events)
		{
			if (GodotObject.IsInstanceValid(@event))
			{
				((Godot.Collections.Array)target[key]).Add(@event.Export());
			}
		}
	}

	private static bool HasEventEntries(Dictionary eventFeatureData)
	{
		if (((Godot.Collections.Array)eventFeatureData["EventInit"]).Count <= 0 && ((Godot.Collections.Array)eventFeatureData["EventEntry"]).Count <= 0 && ((Godot.Collections.Array)eventFeatureData["EventReady"]).Count <= 0)
		{
			return ((Godot.Collections.Array)eventFeatureData["EventStart"]).Count > 0;
		}
		return true;
	}

	private void ApplyEditedFeatureData()
	{
		if (_eventDataEditedFromEditor)
		{
			ApplyEditedEventFeatureData();
		}
		if (_preSpawnDataEditedFromEditor)
		{
			ApplyEditedPreSpawnFeatureData();
		}
	}

	private void ApplyEditedEventFeatureData()
	{
		if (featureData == null)
		{
			featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();
		}
		StringName key = new StringName("Event");
		Dictionary dictionary = BuildEventFeatureData();
		Dictionary value;
		Dictionary dictionary2 = ((featureData.TryGetValue(key, out value) && value != null) ? value.Duplicate(deep: true) : new Dictionary());
		dictionary2["EventInit"] = dictionary["EventInit"];
		dictionary2["EventReady"] = dictionary["EventReady"];
		dictionary2["EventStart"] = dictionary["EventStart"];
		if ((eventEntry?.Count ?? 0) > 0 || !dictionary2.ContainsKey("EventEntry"))
		{
			dictionary2["EventEntry"] = dictionary["EventEntry"];
		}
		if (!dictionary2.ContainsKey("RemotePhaseTimeoutSeconds"))
		{
			dictionary2["RemotePhaseTimeoutSeconds"] = dictionary["RemotePhaseTimeoutSeconds"];
		}
		if (!HasEventEntries(dictionary2))
		{
			featureData.Remove(new StringName("Event"));
		}
		else
		{
			featureData[key] = dictionary2;
		}
	}

	private void ApplyEditedPreSpawnFeatureData()
	{
		if (featureData == null)
		{
			featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();
		}
		StringName key = new StringName("PreSpawn");
		Dictionary dictionary = BuildPreSpawnFeatureData();
		Dictionary value;
		Dictionary dictionary2 = ((featureData.TryGetValue(key, out value) && value != null) ? value.Duplicate(deep: true) : new Dictionary());
		if (dictionary == null)
		{
			dictionary2.Remove("Packet");
		}
		else
		{
			dictionary2["Packet"] = dictionary["Packet"];
		}
		if (dictionary2.Count == 0)
		{
			featureData.Remove(key);
		}
		else
		{
			featureData[key] = dictionary2;
		}
	}

	private void MergeLegacyPreSpawnFeatureData(Dictionary levelData)
	{
		if (featureData.ContainsKey("PreSpawn"))
		{
			return;
		}
		Godot.Collections.Array array = GetArray(GetDict(levelData, "PreSpawn"), "Packet");
		if (array.Count == 0)
		{
			return;
		}
		Dictionary dictionary = new Dictionary { ["Packet"] = new Godot.Collections.Array() };
		foreach (Variant item in array)
		{
			((Godot.Collections.Array)dictionary["Packet"]).Add(item);
		}
		featureData["PreSpawn"] = dictionary;
	}

	private void BuildPacketBankFeatureData()
	{
		if (packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN && GodotObject.IsInstanceValid(rainData))
		{
			Dictionary dictionary = rainData.Export();
			dictionary["PlantColumn"] = plantColumn;
			featureData["RainMode"] = dictionary;
		}
		if (packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR && GodotObject.IsInstanceValid(conveyorData))
		{
			Dictionary dictionary2 = conveyorData.Export();
			dictionary2["PlantColumn"] = plantColumn;
			featureData["ConveyorBelt"] = dictionary2;
		}
		if (packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET || packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE)
		{
			featureData["PacketBank"] = new Dictionary { ["PacketBankName"] = packetBank };
			Dictionary dictionary3 = new Dictionary();
			dictionary3["Method"] = Enum.GetName(typeof(TowerDefenseEnum.LEVEL_SEEDBANK_METHOD), packetBankMethod);
			dictionary3["PlantColumn"] = plantColumn;
			dictionary3["LimitGridPlantNum"] = limitGridPlantNum;
			dictionary3["ColdDownStart"] = packetColdDownStart;
			dictionary3["ColdDownUse"] = packetColdDownUse;
			dictionary3["Packet"] = ExportPacketBankList();
			featureData["SeedBank"] = dictionary3;
		}
	}

	public void ExportToFeatureProcess()
	{
		ExportToFeatureProcess(refreshEditorValues: false);
	}

	private void ExportToFeatureProcess(bool refreshEditorValues)
	{
		if (!GodotObject.IsInstanceValid(data) && HasProcessName(processName) && TryGetFinishMethodFromProcessName(processName.ToString(), out var lEVEL_FINISH_METHOD))
		{
			finishMethod = lEVEL_FINISH_METHOD;
		}
		bool preferExplicitData = !refreshEditorValues && HasExplicitSection("Feature");
		bool preferExplicitData2 = !refreshEditorValues && HasExplicitSection("Process");
		Godot.Collections.Dictionary<StringName, Dictionary> preserved = DuplicateFeatureData(featureData);
		StringName preservedName = processName;
		Dictionary preservedData = processData?.Duplicate(deep: true) ?? new Dictionary();
		if (featureData == null)
		{
			featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();
		}
		featureData.Clear();
		BuildBaseFeatureData();
		Dictionary dictionary = BuildPreSpawnFeatureData();
		if (dictionary != null)
		{
			featureData["PreSpawn"] = dictionary;
		}
		if (isCustomTalk && GodotObject.IsInstanceValid(customTalk))
		{
			if (GodotObject.IsInstanceValid(customTalk.data) && customTalk.data.Data.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary2 = customTalk.data.Data.AsGodotDictionary().Duplicate();
				dictionary2["isCustom"] = true;
				featureData["NpcTalk"] = dictionary2;
			}
		}
		else if (talk.AsString() != "")
		{
			featureData["NpcTalk"] = new Dictionary { ["TalkName"] = talk };
		}
		if (GodotObject.IsInstanceValid(sunManager))
		{
			featureData["Sun"] = sunManager.Export();
		}
		BuildPacketBankFeatureData();
		featureData["BGM"] = new Dictionary { ["BackgroundMusic"] = backgroundMusic };
		if (GodotObject.IsInstanceValid(fogManager) && fogManager.open)
		{
			featureData["Fog"] = fogManager.Export();
		}
		if (GodotObject.IsInstanceValid(lookStarManager) && lookStarManager.open)
		{
			featureData["LookStar"] = lookStarManager.Export();
		}
		featureData["ScreenEffect"] = new Dictionary
		{
			["StormOpen"] = stormOpen,
			["PacketBankMethod"] = (int)packetBankMethod
		};
		if (isCustomTutorial && GodotObject.IsInstanceValid(customTutorial))
		{
			if (GodotObject.IsInstanceValid(customTutorial.data) && customTutorial.data.Data.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary3 = customTutorial.data.Data.AsGodotDictionary().Duplicate();
				dictionary3["isCustom"] = true;
				featureData["Tutorial"] = dictionary3;
			}
		}
		else if (tutorial.AsString() != "")
		{
			featureData["Tutorial"] = new Dictionary { ["TutorialName"] = tutorial };
		}
		Dictionary dictionary4 = BuildEventFeatureData();
		if (HasEventEntries(dictionary4))
		{
			featureData["Event"] = dictionary4;
		}
		if ((finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE || finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2) && GodotObject.IsInstanceValid(waveManager))
		{
			featureData["Wave"] = waveManager.Export();
		}
		processName = "";
		processData = new Dictionary();
		switch (finishMethod)
		{
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE:
			processName = "Wave";
			processData["StormOpen"] = stormOpen;
			processData["MowerUse"] = mowerUse;
			break;
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2:
			processName = "IZM2";
			processData["StormOpen"] = stormOpen;
			processData["MowerUse"] = mowerUse;
			break;
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE:
			processName = "Vase";
			if (GodotObject.IsInstanceValid(vaseManager))
			{
				processData = vaseManager.Export();
			}
			processData["PacketBankMethod"] = (int)packetBankMethod;
			processData["MowerUse"] = mowerUse;
			break;
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
			processName = "IZM";
			if (GodotObject.IsInstanceValid(izmManager))
			{
				processData = izmManager.Export();
			}
			break;
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ:
			processName = "Quiz";
			break;
		}
		MergePreservedFeatureData(preserved, preferExplicitData);
		MergePreservedProcessData(preservedName, preservedData, preferExplicitData2);
	}

	private bool HasExplicitSection(string sectionName)
	{
		if (!GodotObject.IsInstanceValid(data) || data.Data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		if (!data.Data.AsGodotDictionary().TryGetValue(sectionName, out var value))
		{
			return false;
		}
		return value.VariantType switch
		{
			Variant.Type.Array => value.AsGodotArray().Count > 0, 
			Variant.Type.Dictionary => value.AsGodotDictionary().Count > 0, 
			_ => false, 
		};
	}

	private void SynchronizeMapFeature()
	{
		if (featureData == null)
		{
			featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();
		}
		StringName key = new StringName("Map");
		if (!featureData.TryGetValue(key, out var value) || value == null)
		{
			value = new Dictionary();
			featureData[key] = value;
		}
		value["MapName"] = map;
	}

	private bool HasSynchronizedMapFeature(string value)
	{
		StringName key = new StringName("Map");
		if (featureData != null && featureData.TryGetValue(key, out var value2) && value2 != null)
		{
			return string.Equals(value2.GetValueOrDefault("MapName", "").AsString(), value, StringComparison.Ordinal);
		}
		return false;
	}

	private static Godot.Collections.Dictionary<StringName, Dictionary> DuplicateFeatureData(Godot.Collections.Dictionary<StringName, Dictionary> source)
	{
		Godot.Collections.Dictionary<StringName, Dictionary> dictionary = new Godot.Collections.Dictionary<StringName, Dictionary>();
		if (source == null)
		{
			return dictionary;
		}
		foreach (StringName key in source.Keys)
		{
			dictionary[key] = source[key]?.Duplicate(deep: true) ?? new Dictionary();
		}
		return dictionary;
	}

	private void MergePreservedFeatureData(Godot.Collections.Dictionary<StringName, Dictionary> preserved, bool preferExplicitData)
	{
		foreach (StringName key in preserved.Keys)
		{
			Dictionary dictionary = preserved[key];
			if (featureData.TryGetValue(key, out var value) && value != null)
			{
				if (preferExplicitData)
				{
					Dictionary dictionary2 = dictionary;
					dictionary = value.Duplicate(deep: true);
					foreach (Variant key2 in dictionary2.Keys)
					{
						dictionary[key2] = dictionary2[key2];
					}
				}
				else
				{
					foreach (Variant key3 in value.Keys)
					{
						dictionary[key3] = value[key3];
					}
				}
			}
			featureData[key] = dictionary;
		}
	}

	private void MergePreservedProcessData(StringName preservedName, Dictionary preservedData, bool preferExplicitData)
	{
		if (!HasProcessName(preservedName))
		{
			return;
		}
		if (processName != preservedName)
		{
			if (preferExplicitData)
			{
				processName = preservedName;
				processData = preservedData;
			}
			return;
		}
		if (preferExplicitData)
		{
			foreach (Variant key in preservedData.Keys)
			{
				processData[key] = preservedData[key];
			}
			return;
		}
		foreach (Variant key2 in processData.Keys)
		{
			preservedData[key2] = processData[key2];
		}
		processData = preservedData;
	}

	public void ConveyorPreset()
	{
		packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR;
		waveManager.minNextWaveHealthPercentage = 0.45;
		waveManager.maxNextWaveHealthPercentage = 0.35;
		waveManager.beginCol = 9.0;
		waveManager.spawnColEnd = 15.0;
		waveManager.spawnColStart = 6.0;
		sunManager.open = false;
	}

	public void RainPreset()
	{
		packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN;
		sunManager.open = false;
	}
}
