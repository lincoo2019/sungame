using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleRegistry : RefCounted
{
	public static bool IsInit = false;

	public static System.Collections.Generic.Dictionary<StringName, TowerDefenseBattleFeature> BattleFeatureDictionary = new System.Collections.Generic.Dictionary<StringName, TowerDefenseBattleFeature>();

	public static System.Collections.Generic.Dictionary<StringName, TowerDefenseBattleProcess> BattleProcessDictionary = new System.Collections.Generic.Dictionary<StringName, TowerDefenseBattleProcess>();

	private static readonly System.Collections.Generic.Dictionary<StringName, Func<TowerDefenseBattleFeature>> BattleFeatureFactoryDictionary = new System.Collections.Generic.Dictionary<StringName, Func<TowerDefenseBattleFeature>>();

	private static readonly System.Collections.Generic.Dictionary<StringName, Func<TowerDefenseBattleProcess>> BattleProcessFactoryDictionary = new System.Collections.Generic.Dictionary<StringName, Func<TowerDefenseBattleProcess>>();

	public static void Init()
	{
		if (!IsInit)
		{
			IsInit = true;
			RegisterInit();
		}
	}

	public static void RegisterInit()
	{
		RegisterFeature<TowerDefenseBattleFeatureCamera>("Camera");
		RegisterFeature<TowerDefenseBattleFeatureMap>("Map");
		RegisterFeature<TowerDefenseBattleFeatureShovel>("Shovel");
		RegisterFeature<TowerDefenseBattleFeatureGlove>("Glove");
		RegisterFeature<TowerDefenseBattleFeaturePacketPick>("PacketPick");
		RegisterFeature<TowerDefenseBattleFeatureMower>("Mower");
		RegisterFeature<TowerDefenseBattleFeatureBrain>("Brain");
		RegisterFeature<TowerDefenseBattleFeatureNpcTalk>("NpcTalk");
		RegisterFeature<TowerDefenseBattleFeatureSun>("Sun");
		RegisterFeature<TowerDefenseBattleFeatureProgress>("Progress");
		RegisterFeature<TowerDefenseBattleFeatureRainMode>("RainMode");
		RegisterFeature<TowerDefenseBattleFeatureConveyorBelt>("ConveyorBelt");
		RegisterFeature<TowerDefenseBattleFeaturePacketBank>("PacketBank");
		RegisterFeature<TowerDefenseBattleFeatureSeedBank>("SeedBank");
		RegisterFeature<TowerDefenseBattleFeaturePreSpawn>("PreSpawn");
		RegisterFeature<TowerDefenseBattleFeatureBGM>("BGM");
		RegisterFeature<TowerDefenseBattleFeatureFog>("Fog");
		RegisterFeature<TowerDefenseBattleFeatureLookStar>("LookStar");
		RegisterFeature<TowerDefenseBattleFeatureScreenEffect>("ScreenEffect");
		RegisterFeature<TowerDefenseBattleFeatureWarningLine>("WarningLine");
		RegisterFeature<TowerDefenseBattleFeaturePortal>("Portal");
		RegisterFeature<TowerDefenseBattleFeatureTutorial>("Tutorial", 200);
		RegisterFeature<TowerDefenseBattleFeatureEvent>("Event");
		RegisterFeature<TowerDefenseBattleFeatureHammer>("Hammer");
		RegisterFeature<TowerDefenseBattleFeatureGemMatch>("GemMatch");
		RegisterFeature<TowerDefenseBattleFeatureSlotMachine>("SlotMachine");
		RegisterFeature<TowerDefenseBattleFeatureWave>("Wave");
		RegisterProcess<TowerDefenseBattleProcessWave>("Wave");
		RegisterProcess<TowerDefenseBattleProcessVase>("Vase");
		RegisterProcess<TowerDefenseBattleProcessIZM>("IZM");
		RegisterProcess<TowerDefenseBattleProcessIZM2>("IZM2");
		RegisterProcess<TowerDefenseBattleProcessQuiz>("Quiz");
		RegisterProcess<TowerDefenseBattleProcessEmpty>("Empty");
		RegisterDependence();
	}

	public static void RegisterDependence()
	{
		SetFeatureDependence("Camera", new Array<StringName> { "Map" });
		SetFeatureDependence("Map", new Array<StringName>());
		SetFeatureDependence("Shovel", new Array<StringName> { "Map", "PacketPick" });
		SetFeatureDependence("Glove", new Array<StringName> { "Map", "PacketPick" });
		SetFeatureDependence("PacketPick", new Array<StringName> { "Map" });
		SetFeatureDependence("Mower", new Array<StringName> { "Map" });
		SetFeatureDependence("Brain", new Array<StringName> { "Map" }, new Array<StringName> { "Progress" });
		SetFeatureDependence("NpcTalk", new Array<StringName>(), new Array<StringName> { "BGM" });
		SetFeatureDependence("Sun", new Array<StringName> { "Map" });
		SetFeatureDependence("Progress", new Array<StringName>());
		SetFeatureDependence("RainMode", new Array<StringName> { "Map", "PacketPick" }, new Array<StringName> { "Sun" });
		SetFeatureDependence("ConveyorBelt", new Array<StringName> { "PacketPick" }, new Array<StringName> { "Wave" });
		SetFeatureDependence("PacketBank", new Array<StringName> { "SeedBank" });
		SetFeatureDependence("SeedBank", new Array<StringName> { "PacketPick" });
		SetFeatureDependence("PreSpawn", new Array<StringName> { "Map" });
		SetFeatureDependence("BGM", new Array<StringName>());
		SetFeatureDependence("Fog", new Array<StringName> { "Map" });
		SetFeatureDependence("LookStar", new Array<StringName> { "Map" });
		SetFeatureDependence("ScreenEffect", new Array<StringName>());
		SetFeatureDependence("WarningLine", new Array<StringName> { "Map" });
		SetFeatureDependence("Portal", new Array<StringName> { "Map" });
		SetFeatureDependence("Tutorial", new Array<StringName>());
		SetFeatureDependence("Event", new Array<StringName> { "Map" });
		SetFeatureDependence("Hammer", new Array<StringName>());
		SetFeatureDependence("GemMatch", new Array<StringName> { "Map", "Sun", "SeedBank" });
		SetFeatureDependence("SlotMachine", new Array<StringName> { "Sun" });
		SetFeatureDependence("Wave", new Array<StringName> { "Map", "Progress" }, new Array<StringName> { "LookStar", "GemMatch", "PacketPick" });
		SetProcessDependence("Wave", new Array<StringName> { "Map", "Progress", "Camera", "Wave" }, new Array<StringName> { "Mower", "Sun", "SeedBank", "PacketBank", "SlotMachine" });
		SetProcessDependence("Vase", new Array<StringName> { "Map" }, new Array<StringName> { "Mower", "Progress", "SeedBank", "PacketBank" });
		SetProcessDependence("IZM", new Array<StringName> { "Map", "Brain", "Progress", "Sun" }, new Array<StringName> { "SeedBank", "PacketBank", "ConveyorBelt" });
		SetProcessDependence("IZM2", new Array<StringName> { "Mower", "Progress", "Camera", "Wave" }, new Array<StringName> { "SeedBank", "PacketBank" });
		SetProcessDependence("Quiz", new Array<StringName> { "Map", "Brain", "Progress" });
		SetProcessDependence("Empty", new Array<StringName>(), new Array<StringName> { "SeedBank" });
	}

	public static void RegisterFeature<T>(StringName featureName, int gameStartPriority = 0) where T : TowerDefenseBattleFeature, new()
	{
		BattleFeatureDictionary[featureName] = new T
		{
			gameStartPriority = gameStartPriority
		};
		BattleFeatureFactoryDictionary[featureName] = () => new T();
	}

	public static void RegisterProcess<T>(StringName processName, int gameStartPriority = 100) where T : TowerDefenseBattleProcess, new()
	{
		BattleProcessDictionary[processName] = new T
		{
			gameStartPriority = gameStartPriority
		};
		BattleProcessFactoryDictionary[processName] = () => new T();
	}

	public static void RegisterFeature(StringName featureName, TowerDefenseBattleFeature feature, int gameStartPriority = 0)
	{
		feature.gameStartPriority = gameStartPriority;
		SetFeaturePrototype(featureName, feature);
	}

	public static void RegisterProcess(StringName processName, TowerDefenseBattleProcess process, int gameStartPriority = 100)
	{
		process.gameStartPriority = gameStartPriority;
		SetProcessPrototype(processName, process);
	}

	internal static void SetFeaturePrototype(StringName key, TowerDefenseBattleFeature prototype)
	{
		BattleFeatureDictionary[key] = prototype;
		BattleFeatureFactoryDictionary.Remove(key);
	}

	internal static void SetProcessPrototype(StringName key, TowerDefenseBattleProcess prototype)
	{
		BattleProcessDictionary[key] = prototype;
		BattleProcessFactoryDictionary.Remove(key);
	}

	internal static bool RemoveFeatureRegistration(StringName key)
	{
		BattleFeatureFactoryDictionary.Remove(key);
		return BattleFeatureDictionary.Remove(key);
	}

	internal static bool RemoveProcessRegistration(StringName key)
	{
		BattleProcessFactoryDictionary.Remove(key);
		return BattleProcessDictionary.Remove(key);
	}

	internal static Action CaptureFeatureRegistration(StringName key)
	{
		return CaptureRegistration(key, BattleFeatureDictionary, BattleFeatureFactoryDictionary);
	}

	internal static Action CaptureProcessRegistration(StringName key)
	{
		return CaptureRegistration(key, BattleProcessDictionary, BattleProcessFactoryDictionary);
	}

	private static Action CaptureRegistration<T>(StringName key, System.Collections.Generic.Dictionary<StringName, T> prototypes, System.Collections.Generic.Dictionary<StringName, Func<T>> factories)
	{
		T prototype;
		bool hadPrototype = prototypes.TryGetValue(key, out prototype);
		Func<T> factory;
		bool hadFactory = factories.TryGetValue(key, out factory);
		return delegate
		{
			if (hadPrototype)
			{
				prototypes[key] = prototype;
			}
			else
			{
				prototypes.Remove(key);
			}
			if (hadFactory)
			{
				factories[key] = factory;
			}
			else
			{
				factories.Remove(key);
			}
		};
	}

	public static void SetFeatureDependence(StringName featureName, Array<StringName> _featureNames, Array<StringName> _optionalFeatureNames = null)
	{
		if (BattleFeatureDictionary.ContainsKey(featureName))
		{
			TowerDefenseBattleDependenceData towerDefenseBattleDependenceData = new TowerDefenseBattleDependenceData();
			towerDefenseBattleDependenceData.FeatureNames = _featureNames ?? new Array<StringName>();
			towerDefenseBattleDependenceData.ConfiguredOptionalFeatureNames = _optionalFeatureNames ?? new Array<StringName>();
			BattleFeatureDictionary[featureName].dependenceData = towerDefenseBattleDependenceData;
		}
	}

	public static void SetProcessDependence(StringName processName, Array<StringName> _featureNames, Array<StringName> _optionalFeatureNames = null)
	{
		if (BattleProcessDictionary.ContainsKey(processName))
		{
			TowerDefenseBattleDependenceData towerDefenseBattleDependenceData = new TowerDefenseBattleDependenceData();
			towerDefenseBattleDependenceData.FeatureNames = _featureNames ?? new Array<StringName>();
			towerDefenseBattleDependenceData.ConfiguredOptionalFeatureNames = _optionalFeatureNames ?? new Array<StringName>();
			BattleProcessDictionary[processName].dependenceData = towerDefenseBattleDependenceData;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "Mod 动态注册的类型走 Activator 回退路径，内置类型使用工厂字典无反射。")]
	public static TowerDefenseBattleFeature GetFeature(StringName featureName)
	{
		if (!BattleFeatureDictionary.ContainsKey(featureName))
		{
			GD.PushError($"[GetFeature] 未注册的 feature: {featureName}");
			return null;
		}
		TowerDefenseBattleFeature towerDefenseBattleFeature = BattleFeatureDictionary[featureName];
		try
		{
			Func<TowerDefenseBattleFeature> value;
			TowerDefenseBattleFeature towerDefenseBattleFeature2 = ((!BattleFeatureFactoryDictionary.TryGetValue(featureName, out value)) ? ((TowerDefenseBattleFeature)CreateInstanceDynamically(towerDefenseBattleFeature.GetType())) : value());
			towerDefenseBattleFeature2.dependenceData = towerDefenseBattleFeature.dependenceData;
			towerDefenseBattleFeature2.gameStartPriority = towerDefenseBattleFeature.gameStartPriority;
			return towerDefenseBattleFeature2;
		}
		catch (Exception value2)
		{
			GD.PushError($"[GetFeature] 创建实例失败: {featureName}, 类型: {towerDefenseBattleFeature.GetType().Name}, 错误: {value2}");
			return null;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "Mod 动态注册的类型走 Activator 回退路径，内置类型使用工厂字典无反射。")]
	public static TowerDefenseBattleProcess GetProcess(StringName processName)
	{
		if (processName == null || processName.IsEmpty)
		{
			GD.PushError("[GetProcess] Process name is empty.");
			return null;
		}
		if (!BattleProcessDictionary.ContainsKey(processName))
		{
			GD.PushError($"[GetProcess] 未注册的 process: {processName}");
			return null;
		}
		TowerDefenseBattleProcess towerDefenseBattleProcess = BattleProcessDictionary[processName];
		try
		{
			Func<TowerDefenseBattleProcess> value;
			TowerDefenseBattleProcess towerDefenseBattleProcess2 = ((!BattleProcessFactoryDictionary.TryGetValue(processName, out value)) ? ((TowerDefenseBattleProcess)CreateInstanceDynamically(towerDefenseBattleProcess.GetType())) : value());
			towerDefenseBattleProcess2.dependenceData = towerDefenseBattleProcess.dependenceData;
			towerDefenseBattleProcess2.gameStartPriority = towerDefenseBattleProcess.gameStartPriority;
			return towerDefenseBattleProcess2;
		}
		catch (Exception value2)
		{
			GD.PushError($"[GetProcess] 创建实例失败: {processName}, 类型: {towerDefenseBattleProcess.GetType().Name}, 错误: {value2}");
			return null;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "Mod 动态注册的类型走 Activator 回退路径，内置类型使用工厂字典无反射。")]
	private static object CreateInstanceDynamically([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
	{
		return Activator.CreateInstance(type);
	}
}
