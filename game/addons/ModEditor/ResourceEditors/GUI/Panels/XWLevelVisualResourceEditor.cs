using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.OutPutPanel;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWLevelVisualResourceEditor : XWGenericVisualResourceEditor
{
	[Flags]
	private enum LevelRefreshDomain
	{
		None = 0,
		Summary = 1,
		Feature = 2,
		Process = 4,
		Events = 8,
		Wave = 0x10,
		Hud = 0x20,
		PreSpawn = 0x40,
		All = 0x7F
	}

	private sealed class LevelOptionItem
	{
		public string Name { get; set; } = "";

		public string DisplayName { get; set; } = "";

		public string SourceLabel { get; set; } = "";

		public string SourcePath { get; set; } = "";

		public string Description { get; set; } = "";

		public string DetailText
		{
			get
			{
				string text = (string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName);
				if (!string.IsNullOrWhiteSpace(SourceLabel))
				{
					text = text + " / " + SourceLabel;
				}
				if (!string.IsNullOrWhiteSpace(SourcePath))
				{
					text = text + "\n" + SourcePath;
				}
				if (!string.IsNullOrWhiteSpace(Description))
				{
					text = text + "\n" + Description;
				}
				return text;
			}
		}
	}

	private readonly struct LevelDataPathSegment
	{
		public bool IsArrayIndex { get; }

		public int ArrayIndex { get; }

		public Variant DictionaryKey { get; }

		public string DisplayName { get; }

		public LevelDataPathSegment(Variant dictionaryKey, string displayName)
		{
			IsArrayIndex = false;
			ArrayIndex = -1;
			DictionaryKey = dictionaryKey;
			DisplayName = (string.IsNullOrWhiteSpace(displayName) ? dictionaryKey.AsString() : displayName);
		}

		public LevelDataPathSegment(int arrayIndex)
		{
			IsArrayIndex = true;
			ArrayIndex = arrayIndex;
			DictionaryKey = default(Variant);
			DisplayName = $"[{arrayIndex}]";
		}
	}

	private sealed class LevelDataEditorBinding
	{
		public List<LevelDataPathSegment> Path { get; } = new List<LevelDataPathSegment>();

		public LineEdit Editor { get; set; }

		public bool WasPresent { get; set; } = true;

		public bool Dirty { get; set; }

		public Variant.Type ExpectedType { get; set; }
	}

	private const string CardPacketSelectorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelCardPacketSelectorWindow.tscn";

	private const string CompactPacketSelectorRowScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketSelectorRow.tscn";

	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelVisualEditorLayout.tscn";

	private const string LevelGameCanvasScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelGameCanvas.tscn";

	private const string LevelDataEditorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelDataEditorWindow.tscn";

	private const string WaveRuntimePreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelWaveRuntimePreview.tscn";

	private static PackedScene _cardPacketSelectorScene;

	private static PackedScene _compactPacketSelectorRowScene;

	private static PackedScene _editorLayoutScene;

	private static PackedScene _levelGameCanvasScene;

	private static PackedScene _levelDataEditorScene;

	private static PackedScene _waveRuntimePreviewScene;

	private static readonly HashSet<string> LevelInspectorEditableProperties = new HashSet<string>
	{
		"resource_name", "resource_local_to_scene", "data", "featureData", "processName", "processData", "eventInit", "eventReady", "eventStart", "preSpawnList",
		"packetBankList", "sunManager", "fogManager", "lookStarManager", "waveManager", "vaseManager", "izmManager", "customTalk", "customTutorial"
	};

	private Resource _editingLevel;

	private XWLevelGameCanvas _levelGameCanvas;

	private ItemList _featureList;

	private ItemList _featureDataList;

	private ItemList _processDataList;

	private ItemList _eventInitList;

	private ItemList _eventReadyList;

	private ItemList _eventStartList;

	private ItemList _preSpawnList;

	private ItemList _packetBankList;

	private LineEdit _featureNameEdit;

	private CheckBox _featureEnabledCheck;

	private LineEdit _featureDataKeyEdit;

	private LineEdit _featureDataValueEdit;

	private LineEdit _processNameEdit;

	private LineEdit _processDataKeyEdit;

	private LineEdit _processDataValueEdit;

	private Label _summaryLabel;

	private Label _featureSummaryLabel;

	private Label _processSummaryLabel;

	private Label _waveRuntimeStatusLabel;

	private TextureRect _waveRuntimeBackground;

	private Node2D _waveRuntimeZombieRoot;

	private Control _waveRuntimeSpawnControlRoot;

	private VBoxContainer _waveRuntimeEventOverlay;

	private Label _waveRuntimeBanner;

	private Label _waveRuntimeSpawnSummary;

	private ProgressBar _waveRuntimeProgress;

	private Timer _waveRuntimeTimer;

	private Button _waveRuntimeAutoButton;

	private LineEdit _waveRuntimeLevelNameEdit;

	private LineEdit _waveRuntimeDescriptionEdit;

	private Label _waveRuntimeSunLabel;

	private Label _waveRuntimeWaveLabel;

	private Label _waveRuntimeMowerLabel;

	private Label _waveRuntimeFogLabel;

	private ColorRect _waveRuntimeFogOverlay;

	private HBoxContainer _waveRuntimeSeedBankSlots;

	private ProgressBar _waveRuntimeHudProgress;

	private int _waveRuntimeIndex;

	private int _waveRuntimeSpawnGroupCount;

	private int _waveRuntimeEventCount;

	private bool _waveRuntimePlaying;

	private string _waveRuntimeSeedBankSignature = "";

	private readonly List<Node2D> _waveRuntimeZombies = new List<Node2D>();

	private readonly System.Collections.Generic.Dictionary<Node2D, Button> _waveRuntimeZombieButtons = new System.Collections.Generic.Dictionary<Node2D, Button>();

	private readonly System.Collections.Generic.Dictionary<string, Stack<TowerDefenseCharacter>> _waveRuntimeZombiePool = new System.Collections.Generic.Dictionary<string, Stack<TowerDefenseCharacter>>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<TowerDefenseCharacter, string> _waveRuntimeZombiePoolKeys = new System.Collections.Generic.Dictionary<TowerDefenseCharacter, string>();

	private readonly System.Collections.Generic.Dictionary<string, List<LineEdit>> _levelAtomicTextControls = new System.Collections.Generic.Dictionary<string, List<LineEdit>>(StringComparer.OrdinalIgnoreCase);

	private int _waveRuntimeBattlefieldRebuildCount;

	private int _waveRuntimeCharacterInstantiationCount;

	private int _waveRuntimeCharacterReuseCount;

	private int _waveRuntimeProcessTickCount;

	private int _levelAtomicCommitCount;

	private int _levelContinuousInputCount;

	private LineEdit _preSpawnPacketNameEdit;

	private Label _preSpawnMapLabel;

	private Label _preSpawnSelectionLabel;

	private SubViewport _preSpawnMapViewport;

	private Node2D _preSpawnMapPreviewRoot;

	private Control _preSpawnPreviewStack;

	private Control _preSpawnGridOverlay;

	private TowerDefenseMapConfig _preSpawnMapConfig;

	private int _selectedPreSpawnIndex = -1;

	private int _selectedFeatureIndex = -1;

	private string _selectedFeatureName = "";

	private bool _updatingControls;

	private Timer _levelSaveTimer;

	private bool PreSpawnCanvasPan;

	private bool _preSpawnRightClickArmed;

	private bool _preSpawnCanvasMovedDuringPan;

	private Vector2 PreSpawnCanvasLastPointer = Vector2.Zero;

	private float PreSpawnCanvasZoom = 1f;

	private Vector2 PreSpawnCanvasOffset = Vector2.Zero;

	private Window _featureDataEditorWindow;

	private Window _processDataEditorWindow;

	private Window _eventEditorWindow;

	private VBoxContainer _featureDataEditorFields;

	private VBoxContainer _processDataEditorFields;

	private VBoxContainer _eventEditorFields;

	private Label _featureDataEditorTitle;

	private Label _processDataEditorTitle;

	private Label _eventEditorTitle;

	private readonly System.Collections.Generic.Dictionary<string, LineEdit> _featureDataEditorInputs = new System.Collections.Generic.Dictionary<string, LineEdit>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<string, LineEdit> _processDataEditorInputs = new System.Collections.Generic.Dictionary<string, LineEdit>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<string, LineEdit> _eventEditorInputs = new System.Collections.Generic.Dictionary<string, LineEdit>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<string, LevelDataEditorBinding> _featureDataEditorBindings = new System.Collections.Generic.Dictionary<string, LevelDataEditorBinding>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<string, LevelDataEditorBinding> _processDataEditorBindings = new System.Collections.Generic.Dictionary<string, LevelDataEditorBinding>(StringComparer.OrdinalIgnoreCase);

	private string _editingEventPropertyName = "";

	private int _editingEventIndex = -1;

	private XWGameplayResourcePickerWindow _levelOptionSelectorWindow;

	private readonly List<XWVisualSegmentedOption> _levelSegmentedOptions = new List<XWVisualSegmentedOption>();

	private readonly List<LevelOptionItem> _levelOptionSelectorOptions = new List<LevelOptionItem>();

	private Action<LevelOptionItem> _levelOptionSelectorConfirmed;

	private Window _cardPacketSelectorWindow;

	private LineEdit _cardPacketSelectorFilter;

	private GridContainer _cardPacketSelectorGrid;

	private Label _cardPacketSelectorDescription;

	private Button _cardPacketSelectorConfirmButton;

	private readonly List<LevelOptionItem> _cardPacketSelectorOptions = new List<LevelOptionItem>();

	private readonly List<LevelOptionItem> _filteredCardPacketSelectorOptions = new List<LevelOptionItem>();

	private LevelOptionItem _selectedCardPacketSelectorItem;

	private Action<LevelOptionItem> _cardPacketSelectorConfirmed;

	private const string MapResourceRegistryPath = "res://Asset/Config/Map/MapResource.json";

	private const string BgmResourceRegistryPath = "res://Asset/Config/BGM/BGMResource.json";

	private const string PacketBankResourceRegistryPath = "res://Asset/Config/PacketBank/PacketBankResource.json";

	private const string LevelResourceRegistryPath = "res://Asset/Config/Level/LevelResource.json";

	private const string TalkResourceRegistryPath = "res://Asset/Config/Npc/TalkResource.json";

	private const string TutorialResourceRegistryPath = "res://Asset/Config/Tutorial/TutorialResource.json";

	private const string CharacterResourceRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string DefaultFrontlawnMapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres";

	private static TowerDefenseMapConfig _defaultFrontlawnMapConfig;

	private const float PreSpawnCanvasMinZoom = 0.05f;

	private const float PreSpawnCanvasMaxZoom = 12f;

	private static readonly System.Collections.Generic.Dictionary<string, string[]> FeatureEditableFieldDefaults = new System.Collections.Generic.Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
	{
		{
			"Map",
			new string[1] { "MapName" }
		},
		{
			"BGM",
			new string[1] { "BackgroundMusic" }
		},
		{
			"PacketBank",
			new string[1] { "PacketBankName" }
		},
		{
			"SeedBank",
			new string[2] { "PacketBankName", "PacketList" }
		},
		{
			"Wave",
			new string[3] { "WaveName", "WaveConfig", "WaveManager" }
		},
		{
			"Mower",
			new string[1] { "MowerUse" }
		},
		{
			"Sun",
			new string[3] { "SunNum", "SunDrop", "StartSun" }
		},
		{
			"Fog",
			new string[2] { "FogName", "FogManager" }
		},
		{
			"LookStar",
			new string[2] { "StarCount", "LookStarManager" }
		},
		{
			"Vase",
			new string[2] { "VaseManager", "VaseConfig" }
		},
		{
			"IZM",
			new string[2] { "IZMManager", "ZombiePool" }
		},
		{
			"Tutorial",
			new string[2] { "TutorialName", "customTutorial" }
		},
		{
			"NpcTalk",
			new string[2] { "TalkName", "customTalk" }
		},
		{
			"Progress",
			new string[14]
			{
				"Mode", "LevelName", "DifficultyText", "SurvivalText", "ProgressText", "ProgressValue", "ProgressMax", "AutoProgressResponse", "ProgressItemsVisibility", "DifficultyVisibility",
				"LevelNameVisibility", "SurvivalVisibility", "ProgressVisibility", "ProgressTextVisibility"
			}
		}
	};

	private static readonly System.Collections.Generic.Dictionary<string, IReadOnlyDictionary<string, Variant>> FeatureEditableFieldDefaultValues = new System.Collections.Generic.Dictionary<string, IReadOnlyDictionary<string, Variant>>(StringComparer.OrdinalIgnoreCase) { ["Progress"] = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.OrdinalIgnoreCase)
	{
		["Mode"] = Variant.From<string>("Auto"),
		["LevelName"] = Variant.From<string>(""),
		["DifficultyText"] = Variant.From<string>(""),
		["SurvivalText"] = Variant.From<string>(""),
		["ProgressText"] = Variant.From<string>("{value}/{max}"),
		["ProgressValue"] = Variant.From<double>(0.0),
		["ProgressMax"] = Variant.From<double>(1.0),
		["AutoProgressResponse"] = Variant.From<double>(-1.0),
		["ProgressItemsVisibility"] = Variant.From<string>("Auto"),
		["DifficultyVisibility"] = Variant.From<string>("Auto"),
		["LevelNameVisibility"] = Variant.From<string>("Auto"),
		["SurvivalVisibility"] = Variant.From<string>("Auto"),
		["ProgressVisibility"] = Variant.From<string>("Auto"),
		["ProgressTextVisibility"] = Variant.From<string>("Auto")
	} };

	private static readonly System.Collections.Generic.Dictionary<string, string[]> ProcessEditableFieldDefaults = new System.Collections.Generic.Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
	{
		{
			"Wave",
			new string[4] { "WaveManager", "WaveConfig", "SpawnConfig", "Flags" }
		},
		{
			"Vase",
			new string[2] { "VaseManager", "VaseConfig" }
		},
		{
			"IZM",
			new string[3] { "IZMManager", "ZombiePool", "SpawnConfig" }
		},
		{
			"IZM2",
			new string[3] { "IZMManager", "ZombiePool", "WaveConfig" }
		},
		{
			"Quiz",
			new string[3] { "QuestionSet", "AnswerKey", "Reward" }
		},
		{
			"Empty",
			new string[2] { "Delay", "NextProcess" }
		}
	};

	public int WaveRuntimeBattlefieldRebuildCount => _waveRuntimeBattlefieldRebuildCount;

	public int WaveRuntimeCharacterInstantiationCount => _waveRuntimeCharacterInstantiationCount;

	public int WaveRuntimeCharacterReuseCount => _waveRuntimeCharacterReuseCount;

	public int WaveRuntimeProcessTickCount => _waveRuntimeProcessTickCount;

	public int WaveRuntimeIndex => _waveRuntimeIndex;

	public int LevelAtomicCommitCount => _levelAtomicCommitCount;

	public int LevelContinuousInputCount => _levelContinuousInputCount;

	public bool IsWaveRuntimePlaying => _waveRuntimePlaying;

	public bool IsWaveRuntimeProcessing => IsProcessing();

	public int WaveRuntimePooledCharacterCount
	{
		get
		{
			int num = 0;
			foreach (Stack<TowerDefenseCharacter> value in _waveRuntimeZombiePool.Values)
			{
				num += value.Count;
			}
			return num;
		}
	}

	public int WaveRuntimeUntrackedCharacterCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_waveRuntimeZombieRoot))
			{
				return 0;
			}
			HashSet<TowerDefenseCharacter> hashSet = new HashSet<TowerDefenseCharacter>();
			foreach (Node2D waveRuntimeZombie in _waveRuntimeZombies)
			{
				if (waveRuntimeZombie is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					hashSet.Add(towerDefenseCharacter);
				}
			}
			foreach (Stack<TowerDefenseCharacter> value in _waveRuntimeZombiePool.Values)
			{
				foreach (TowerDefenseCharacter item2 in value)
				{
					if (GodotObject.IsInstanceValid(item2))
					{
						hashSet.Add(item2);
					}
				}
			}
			int num = 0;
			foreach (Node child in _waveRuntimeZombieRoot.GetChildren())
			{
				if (child is TowerDefenseCharacter item && !hashSet.Contains(item))
				{
					num++;
				}
			}
			return num;
		}
	}

	private void BindLevelBasicInfo(VBoxContainer root, Resource level)
	{
		_summaryLabel = root.GetNode<Label>("%SummaryLabel");
		_summaryLabel.Text = BuildSummary(level);
		BindBasicTextField(root, "%LevelNameKeyLineEdit", ReadString(level, "name"), "name", (string from) => Variant.From(in from), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
		BindBasicTextField(root, "%LevelDisplayNameLineEdit", ReadString(level, "levelName"), "levelName", (string from) => Variant.From(in from), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
		BindBasicTextField(root, "%DescriptionLineEdit", ReadString(level, "description"), "description", (string from) => Variant.From(in from), LevelRefreshDomain.Hud);
		BindBasicSpinField(root, "%LevelNumberSpinBox", ReadDouble(level, "levelNumber", 0.0), "levelNumber", (double value) => Variant.From<int>((int)Math.Round(value)), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
		BindBasicSelectableField(root, "%NextLevelLineEdit", "%NextLevelSelectButton", ReadString(level, "nextLevel"), "level", "nextLevel", (string from) => Variant.From(in from), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
		BindHomeWorldCatalog(root, ReadEnum(level, "homeWorld", GeneralEnum.HOMEWORLD.NOONE));
		bool flag = level is TowerDefenseLevelNewConfig;
		root.GetNode<Control>("%ModernVersionRow").Visible = flag;
		BindBasicTextField(root, "%VersionLineEdit", ReadString(level, "version"), "version", (string name) => Variant.From<StringName>(new StringName(name)), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
		string[] array = new string[10] { "FinishMethodRow", "BaseTimeScaleRow", "MapRow", "BackgroundMusicRow", "MowerUseRow", "StormOpenRow", "FirstRewardTypeRow", "FirstRewardValueRow", "PacketBankRow", "LimitGridPlantNumRow" };
		foreach (string text in array)
		{
			root.GetNode<Control>("%" + text).Visible = !flag;
		}
		if (!flag)
		{
			OptionButton option = BindBasicEnumField(root, "%FinishMethodOption", ReadEnum(level, "finishMethod", TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE), delegate(TowerDefenseEnum.LEVEL_FINISH_METHOD value)
			{
				SetLevelDiscreteProperty("finishMethod", Variant.From<int>((int)(object)value), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
			});
			MountLevelSegmentedOption(option, root.GetNode<HFlowContainer>("%FinishMethodSegments"));
			BindBasicSpinField(root, "%BaseTimeScaleSpinBox", ReadDouble(level, "baseTimeScale", 1.0), "baseTimeScale", (double value) => Variant.From(in value), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
			BindBasicSelectableField(root, "%MapLineEdit", "%MapSelectButton", ReadString(level, "map"), "map", "map", (string from) => Variant.From(in from), LevelRefreshDomain.Summary | LevelRefreshDomain.Wave | LevelRefreshDomain.Hud | LevelRefreshDomain.PreSpawn);
			BindBasicSelectableField(root, "%BackgroundMusicLineEdit", "%BackgroundMusicSelectButton", ReadString(level, "backgroundMusic"), "bgm", "backgroundMusic", (string from) => Variant.From(in from), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
			BindBasicBoolField(root, "%MowerUseCheck", ReadBool(level, "mowerUse", fallback: true), delegate(bool value)
			{
				SetLevelDiscreteProperty("mowerUse", Variant.From(in value), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
			});
			BindBasicBoolField(root, "%StormOpenCheck", ReadBool(level, "stormOpen", fallback: false), delegate(bool value)
			{
				SetLevelDiscreteProperty("stormOpen", Variant.From(in value), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
			});
			OptionButton option2 = BindBasicEnumField(root, "%FirstRewardTypeOption", ReadEnum(level, "firstRewardType", TowerDefenseEnum.LEVEL_REWARDTYPE.NOONE), delegate(TowerDefenseEnum.LEVEL_REWARDTYPE value)
			{
				SetLevelDiscreteProperty("firstRewardType", Variant.From<int>((int)(object)value), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
			});
			MountLevelSegmentedOption(option2, root.GetNode<HFlowContainer>("%FirstRewardTypeSegments"));
			BindBasicTextField(root, "%FirstRewardValueLineEdit", FormatVariant(ReadVariant(level, "firstRewardValue")), "firstRewardValue", (string text2) => ParseVariantText(text2, Variant.Type.Nil), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
			BindBasicSelectableField(root, "%PacketBankLineEdit", "%PacketBankSelectButton", ReadString(level, "packetBank"), "packetBank", "packetBank", (string from) => Variant.From(in from), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
			BindBasicSpinField(root, "%LimitGridPlantNumSpinBox", ReadDouble(level, "limitGridPlantNum", -1.0), "limitGridPlantNum", (double value) => Variant.From<int>((int)Math.Round(value)), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
		}
	}

	private void BindBasicTextField(VBoxContainer root, string nodePath, string value, string propertyName, Func<string, Variant> convert, LevelRefreshDomain domains)
	{
		LineEdit node = root.GetNode<LineEdit>(nodePath);
		node.Text = value ?? "";
		BindAtomicTextControl(node, propertyName, convert, domains);
	}

	private void BindAtomicTextControl(LineEdit edit, string propertyName, Func<string, Variant> convert, LevelRefreshDomain domains)
	{
		if (!GodotObject.IsInstanceValid(edit) || string.IsNullOrWhiteSpace(propertyName) || convert == null)
		{
			return;
		}
		if (!_levelAtomicTextControls.TryGetValue(propertyName, out var value))
		{
			value = new List<LineEdit>();
			_levelAtomicTextControls[propertyName] = value;
		}
		value.Add(edit);
		bool submitted = false;
		edit.TextChanged += delegate(string text)
		{
			if (!_updatingControls)
			{
				submitted = false;
				_levelContinuousInputCount++;
				SyncAtomicTextControls(propertyName, text, edit);
				RefreshWaveRuntimeHudTextOnly(propertyName, text);
			}
		};
		edit.TextSubmitted += delegate(string text)
		{
			SetLevelDiscreteProperty(propertyName, convert(text), domains);
			submitted = true;
		};
		edit.FocusExited += delegate
		{
			if (submitted)
			{
				submitted = false;
			}
			else
			{
				SetLevelDiscreteProperty(propertyName, convert(edit.Text), domains);
			}
		};
	}

	private void SyncAtomicTextControls(string propertyName, string text, LineEdit source = null)
	{
		if (!_levelAtomicTextControls.TryGetValue(propertyName, out var value))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			foreach (LineEdit item in value)
			{
				if (GodotObject.IsInstanceValid(item) && item != source)
				{
					item.Text = text ?? "";
				}
			}
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RefreshWaveRuntimeHudTextOnly(string propertyName, string text)
	{
		if (propertyName == "levelName" && GodotObject.IsInstanceValid(_waveRuntimeLevelNameEdit) && !_waveRuntimeLevelNameEdit.HasFocus())
		{
			_waveRuntimeLevelNameEdit.Text = text ?? "";
		}
		else if (propertyName == "description" && GodotObject.IsInstanceValid(_waveRuntimeDescriptionEdit) && !_waveRuntimeDescriptionEdit.HasFocus())
		{
			_waveRuntimeDescriptionEdit.Text = text ?? "";
		}
	}

	private void BindBasicSelectableField(VBoxContainer root, string editPath, string buttonPath, string value, string selectorKind, string propertyName, Func<string, Variant> convert, LevelRefreshDomain domains)
	{
		LineEdit edit = root.GetNode<LineEdit>(editPath);
		BindBasicTextField(root, editPath, value, propertyName, convert, domains);
		root.GetNode<Button>(buttonPath).Pressed += delegate
		{
			ShowKeySelectionForLineEdit(edit, selectorKind, delegate(string text)
			{
				SetLevelDiscreteProperty(propertyName, convert(text), domains);
			});
		};
	}

	private void BindBasicSpinField(VBoxContainer root, string nodePath, double value, string propertyName, Func<double, Variant> convert, LevelRefreshDomain domains)
	{
		SpinBox spin = root.GetNode<SpinBox>(nodePath);
		spin.Value = value;
		spin.ValueChanged += delegate
		{
			if (!_updatingControls)
			{
				_levelContinuousInputCount++;
			}
		};
		spin.FocusExited += Commit;
		spin.GetLineEdit().TextSubmitted += delegate
		{
			Commit();
		};
		void Commit()
		{
			SetLevelDiscreteProperty(propertyName, convert(spin.Value), domains);
		}
	}

	private void BindBasicBoolField(VBoxContainer root, string nodePath, bool value, Action<bool> changed)
	{
		CheckBox node = root.GetNode<CheckBox>(nodePath);
		node.ButtonPressed = value;
		node.Toggled += delegate(bool newValue)
		{
			if (!_updatingControls)
			{
				changed?.Invoke(newValue);
			}
		};
	}

	private OptionButton BindBasicEnumField<T>(VBoxContainer root, string nodePath, T value, Action<T> changed) where T : struct, Enum
	{
		OptionButton option = root.GetNode<OptionButton>(nodePath);
		option.Clear();
		T[] values = Enum.GetValues<T>();
		for (int i = 0; i < values.Length; i++)
		{
			T val = values[i];
			option.AddItem(val.ToString(), Convert.ToInt32(val));
			if (EqualityComparer<T>.Default.Equals(val, value))
			{
				option.Select(option.ItemCount - 1);
			}
		}
		option.ItemSelected += delegate(long index)
		{
			if (!_updatingControls)
			{
				changed?.Invoke((T)Enum.ToObject(typeof(T), option.GetItemId((int)index)));
			}
		};
		return option;
	}

	private void BindHomeWorldCatalog(VBoxContainer root, GeneralEnum.HOMEWORLD value)
	{
		OptionButton option = BindBasicEnumField(root, "%HomeWorldOption", value, delegate(GeneralEnum.HOMEWORLD next)
		{
			XWLevelVisualResourceEditor xWLevelVisualResourceEditor = this;
			int from = (int)next;
			xWLevelVisualResourceEditor.SetLevelDiscreteProperty("homeWorld", Variant.From(in from));
		});
		Button catalogButton = root.GetNode<Button>("%HomeWorldCatalogButton");
		catalogButton.Text = $"世界图鉴 · {value}";
		catalogButton.Pressed += delegate
		{
			EnsureLevelOptionSelectorWindow();
			if (GodotObject.IsInstanceValid(_levelOptionSelectorWindow))
			{
				List<XWGameplayResourceChoice> list = new List<XWGameplayResourceChoice>();
				GeneralEnum.HOMEWORLD[] values = Enum.GetValues<GeneralEnum.HOMEWORLD>();
				for (int i = 0; i < values.Length; i++)
				{
					GeneralEnum.HOMEWORLD hOMEWORLD = values[i];
					int num = (int)hOMEWORLD;
					list.Add(new XWGameplayResourceChoice(num.ToString(), hOMEWORLD.ToString(), $"HOMEWORLD/{hOMEWORLD}", ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceMap.svg", null, ResourceLoader.CacheMode.Reuse), XWGameplayResourceKind.Resource, IsModResource: false));
				}
				_levelOptionSelectorWindow.OpenChoices("世界图鉴", "搜索并选择关卡所属世界；隐藏枚举仍保留原始 ID 语义", option.GetSelectedId().ToString(), list, delegate(XWGameplayResourceChoice choice)
				{
					if (int.TryParse(choice?.Key, out var result))
					{
						int itemIndex = option.GetItemIndex(result);
						if (itemIndex >= 0)
						{
							option.Select(itemIndex);
							catalogButton.Text = $"世界图鉴 · {(GeneralEnum.HOMEWORLD)result}";
							option.EmitSignal(OptionButton.SignalName.ItemSelected, itemIndex);
						}
					}
				});
			}
		};
	}

	private void MountLevelSegmentedOption(OptionButton option, HFlowContainer host)
	{
		if (!GodotObject.IsInstanceValid(option) || !GodotObject.IsInstanceValid(host) || option.ItemCount < 2 || option.ItemCount > 8)
		{
			if (GodotObject.IsInstanceValid(option))
			{
				option.Visible = true;
			}
		}
		else
		{
			XWVisualSegmentedOption xWVisualSegmentedOption = new XWVisualSegmentedOption(option, host);
			xWVisualSegmentedOption.Rebuild();
			_levelSegmentedOptions.Add(xWVisualSegmentedOption);
		}
	}

	private void DisposeLevelVisualChoices()
	{
		foreach (XWVisualSegmentedOption levelSegmentedOption in _levelSegmentedOptions)
		{
			levelSegmentedOption?.Dispose();
		}
		_levelSegmentedOptions.Clear();
	}

	private void BindFeatureProcessLayout(VBoxContainer root, Resource level)
	{
		_featureSummaryLabel = root.GetNode<Label>("%FeatureSummaryLabel");
		_featureNameEdit = root.GetNode<LineEdit>("%FeatureNameLineEdit");
		root.GetNode<Button>("%ChooseFeatureButton").Pressed += delegate
		{
			ShowLevelOptionSelector("选择关卡功能 Feature", BuildFeatureSelectionOptions(), AddFeatureFromSelection);
		};
		root.GetNode<Button>("%AddFeatureButton").Pressed += AddFeatureEntry;
		root.GetNode<Button>("%RemoveFeatureButton").Pressed += RemoveSelectedFeatureEntry;
		_featureEnabledCheck = root.GetNode<CheckBox>("%FeatureEnabledCheck");
		_featureEnabledCheck.Toggled += SetFeatureEnabled;
		_featureList = root.GetNode<ItemList>("%FeatureList");
		_featureList.ItemSelected += delegate(long index)
		{
			SelectFeature((int)index);
		};
		_featureDataKeyEdit = root.GetNode<LineEdit>("%FeatureDataKeyLineEdit");
		_featureDataValueEdit = root.GetNode<LineEdit>("%FeatureDataValueLineEdit");
		root.GetNode<Button>("%WriteFeatureDataButton").Pressed += delegate
		{
			SetFeatureDataValue(_featureDataKeyEdit.Text, _featureDataValueEdit.Text);
		};
		root.GetNode<Button>("%EditFeatureDataButton").Pressed += ShowFeatureDataEditor;
		_featureDataList = root.GetNode<ItemList>("%FeatureDataList");
		_featureDataList.ItemSelected += delegate(long index)
		{
			SelectFeatureDataRow((int)index);
		};
		_featureDataList.ItemActivated += delegate
		{
			ShowFeatureDataEditor();
		};
		_processSummaryLabel = root.GetNode<Label>("%ProcessSummaryLabel");
		_processNameEdit = root.GetNode<LineEdit>("%ProcessNameLineEdit");
		_processNameEdit.Text = ReadString(level, "processName");
		root.GetNode<Button>("%ChooseProcessButton").Pressed += delegate
		{
			ShowLevelOptionSelector("选择关卡流程 Process", BuildProcessSelectionOptions(), AddProcessFromSelection);
		};
		root.GetNode<Button>("%SetProcessButton").Pressed += delegate
		{
			SetProcessName(_processNameEdit.Text);
		};
		string[] array = new string[6] { "Wave", "Vase", "IZM", "IZM2", "Quiz", "Empty" };
		foreach (string process in array)
		{
			root.GetNode<Button>("%" + process + "ProcessButton").Pressed += delegate
			{
				SetProcessName(process);
			};
		}
		_processDataKeyEdit = root.GetNode<LineEdit>("%ProcessDataKeyLineEdit");
		_processDataValueEdit = root.GetNode<LineEdit>("%ProcessDataValueLineEdit");
		root.GetNode<Button>("%WriteProcessDataButton").Pressed += delegate
		{
			SetProcessDataValue(_processDataKeyEdit.Text, _processDataValueEdit.Text);
		};
		root.GetNode<Button>("%EditProcessDataButton").Pressed += ShowProcessDataEditor;
		_processDataList = root.GetNode<ItemList>("%ProcessDataList");
		_processDataList.ItemSelected += delegate(long index)
		{
			SelectProcessDataRow((int)index);
		};
		_processDataList.ItemActivated += delegate
		{
			ShowProcessDataEditor();
		};
	}

	private void BindEventLayout(VBoxContainer root, Resource level)
	{
		root.GetNode<Button>("%AddInitEventButton").Pressed += delegate
		{
			ShowLevelOptionSelector("选择初始化事件", BuildEventSelectionOptions(), delegate(LevelOptionItem item)
			{
				AddLevelEventFromSelection("eventInit", item);
			});
		};
		root.GetNode<Button>("%AddReadyEventButton").Pressed += delegate
		{
			ShowLevelOptionSelector("选择准备事件", BuildEventSelectionOptions(), delegate(LevelOptionItem item)
			{
				AddLevelEventFromSelection("eventReady", item);
			});
		};
		root.GetNode<Button>("%AddStartEventButton").Pressed += delegate
		{
			ShowLevelOptionSelector("选择开始事件", BuildEventSelectionOptions(), delegate(LevelOptionItem item)
			{
				AddLevelEventFromSelection("eventStart", item);
			});
		};
		_preSpawnMapLabel = root.GetNode<Label>("%PreSpawnMapLabel");
		_preSpawnPacketNameEdit = root.GetNode<LineEdit>("%PreSpawnPacketNameLineEdit");
		root.GetNode<Button>("%ChoosePreSpawnPacketButton").Pressed += delegate
		{
			ShowKeySelectionForLineEdit(_preSpawnPacketNameEdit, "card", delegate(string selected)
			{
				if (_selectedPreSpawnIndex >= 0 && _editingLevel is TowerDefenseLevelConfig { preSpawnList: not null } towerDefenseLevelConfig && _selectedPreSpawnIndex < towerDefenseLevelConfig.preSpawnList.Count)
				{
					TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = towerDefenseLevelConfig.preSpawnList[_selectedPreSpawnIndex];
					if (GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig))
					{
						towerDefenseLevelPreSpawnConfig.packetName = selected;
						towerDefenseLevelPreSpawnConfig.EmitChanged();
						towerDefenseLevelConfig.MarkPreSpawnDataEditedFromEditor();
						SaveCurrentEditedResource(towerDefenseLevelConfig);
						RefreshEventPanel();
					}
				}
			});
		};
		root.GetNode<Button>("%SyncPreSpawnMapButton").Pressed += RefreshPreSpawnMapSync;
		root.GetNode<Button>("%ResetPreSpawnViewButton").Pressed += ResetPreSpawnMapPreviewView;
		root.GetNode<Button>("%DeletePreSpawnButton").Pressed += DeleteSelectedPreSpawn;
		_preSpawnPreviewStack = root.GetNode<Control>("%PreSpawnPreviewStack");
		_preSpawnPreviewStack.Resized += delegate
		{
			UpdatePreSpawnViewportSize();
			UpdatePreSpawnCanvasTransform();
			QueuePreSpawnGridRedraw();
		};
		_preSpawnMapViewport = root.GetNode<SubViewport>("%PreSpawnMapViewport");
		_preSpawnMapPreviewRoot = root.GetNode<Node2D>("%PreSpawnMapPreviewRoot");
		_preSpawnGridOverlay = root.GetNode<Control>("%PreSpawnGridOverlay");
		_preSpawnGridOverlay.Draw += delegate
		{
			DrawPreSpawnGridOverlay(_preSpawnGridOverlay);
		};
		_preSpawnGridOverlay.GuiInput += OnPreSpawnGridOverlayGuiInput;
		_preSpawnSelectionLabel = root.GetNode<Label>("%PreSpawnSelectionLabel");
		_preSpawnSelectionLabel.Text = BuildPreSpawnSelectionText();
		_eventInitList = root.GetNode<ItemList>("%EventInitList");
		_eventReadyList = root.GetNode<ItemList>("%EventReadyList");
		_eventStartList = root.GetNode<ItemList>("%EventStartList");
		_eventInitList.ItemActivated += delegate(long index)
		{
			ShowEventEditor("eventInit", (int)index);
		};
		_eventReadyList.ItemActivated += delegate(long index)
		{
			ShowEventEditor("eventReady", (int)index);
		};
		_eventStartList.ItemActivated += delegate(long index)
		{
			ShowEventEditor("eventStart", (int)index);
		};
		_preSpawnList = root.GetNode<ItemList>("%PreSpawnList");
		_preSpawnList.ItemSelected += delegate(long index)
		{
			SelectPreSpawn((int)index);
		};
		_preSpawnList.ItemActivated += delegate(long index)
		{
			SelectPreSpawn((int)index);
		};
		_packetBankList = root.GetNode<ItemList>("%PacketBankList");
		RefreshPreSpawnMapSync();
	}

	public override void _Ready()
	{
		base._Ready();
		_levelSaveTimer = new Timer
		{
			WaitTime = 0.35,
			OneShot = true
		};
		_levelSaveTimer.Timeout += SavePendingLevelResource;
		AddChild(_levelSaveTimer, forceReadableName: false, InternalMode.Disabled);
		EnableVisibilityGatedProcessing();
	}

	public override void _ExitTree()
	{
		DisposeLevelVisualChoices();
		StopWaveRuntimePreview();
		DisposeWaveRuntimePool();
		_levelAtomicTextControls.Clear();
		if (GodotObject.IsInstanceValid(_levelSaveTimer))
		{
			_levelSaveTimer.Stop();
		}
		base._ExitTree();
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeLevelVisualChoices();
		StopWaveRuntimePreview();
		DisposeWaveRuntimePool();
		_levelAtomicTextControls.Clear();
		_editingLevel = null;
		_levelGameCanvas = null;
		_preSpawnMapConfig = null;
		if (base.CurrentResource is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			_editingLevel = towerDefenseLevelConfig;
			RenderLevelEditor(towerDefenseLevelConfig);
		}
		else if (base.CurrentResource is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig)
		{
			_editingLevel = towerDefenseLevelNewConfig;
			RenderLevelEditor(towerDefenseLevelNewConfig);
		}
	}

	protected override HashSet<string> GetEmbeddedInspectorAllowedProperties(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (resource is TowerDefenseLevelConfig)
		{
			return null;
		}
		if (!IsLevelResource(resource))
		{
			return base.GetEmbeddedInspectorAllowedProperties(resource, path, descriptor);
		}
		return LevelInspectorEditableProperties;
	}

	protected override void OnEmbeddedInspectorPropertyChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if (obj is Resource resource && IsLevelResource(resource))
		{
			_editingLevel = resource;
			if (property == (StringName)"map")
			{
				RefreshPreSpawnMapSync();
			}
			RefreshAll();
		}
	}

	private void ShowFeatureDataEditor()
	{
		if (_editingLevel == null)
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(_selectedFeatureName) && GetFeatureData(_editingLevel).Count > 0)
		{
			SelectFeature(0);
		}
		Dictionary selectedFeatureDictionary = GetSelectedFeatureDictionary();
		if (selectedFeatureDictionary == null)
		{
			AddLevelEditorOutput("请先选择 Feature", XWOutputPanel.MessageType.Warning);
			return;
		}
		EnsureFeatureDataEditorWindow();
		if (GodotObject.IsInstanceValid(_featureDataEditorWindow))
		{
			_featureDataEditorTitle.Text = "FeatureData: " + _selectedFeatureName;
			BuildDictionaryDataEditorFields(_featureDataEditorFields, _featureDataEditorInputs, _featureDataEditorBindings, selectedFeatureDictionary, GetFeatureEditableDefaults(_selectedFeatureName), GetFeatureEditableDefaultValues(_selectedFeatureName, selectedFeatureDictionary));
			_featureDataEditorWindow.PopupCentered(new Vector2I(560, 460));
		}
	}

	private void ShowProcessDataEditor()
	{
		if (_editingLevel != null)
		{
			EnsureProcessDataEditorWindow();
			if (GodotObject.IsInstanceValid(_processDataEditorWindow))
			{
				string text = ReadString(_editingLevel, "processName");
				_processDataEditorTitle.Text = "ProcessData: " + EmptyToPlaceholder(text);
				BuildDictionaryDataEditorFields(_processDataEditorFields, _processDataEditorInputs, _processDataEditorBindings, GetProcessData(_editingLevel), GetProcessEditableDefaults(text));
				_processDataEditorWindow.PopupCentered(new Vector2I(560, 460));
			}
		}
	}

	private void ShowEventEditor(string eventPropertyName, int eventIndex)
	{
		Array<TowerDefenseLevelEventBase> levelEventArray = GetLevelEventArray(_editingLevel, eventPropertyName);
		if (levelEventArray == null || eventIndex < 0 || eventIndex >= levelEventArray.Count)
		{
			return;
		}
		TowerDefenseLevelEventBase towerDefenseLevelEventBase = levelEventArray[eventIndex];
		if (GodotObject.IsInstanceValid(towerDefenseLevelEventBase))
		{
			EnsureEventEditorWindow();
			if (GodotObject.IsInstanceValid(_eventEditorWindow))
			{
				_editingEventPropertyName = eventPropertyName;
				_editingEventIndex = eventIndex;
				_eventEditorTitle.Text = $"Event: {eventPropertyName}[{eventIndex}] {FormatResource(towerDefenseLevelEventBase)}";
				BuildEventEditorFields(towerDefenseLevelEventBase);
				_eventEditorWindow.PopupCentered(new Vector2I(560, 500));
			}
		}
	}

	private void EnsureFeatureDataEditorWindow()
	{
		if (!GodotObject.IsInstanceValid(_featureDataEditorWindow))
		{
			_featureDataEditorWindow = CreateLevelDataEditorWindow("FeatureDataEditorWindow", "编辑 Feature Data", out _featureDataEditorTitle, out _featureDataEditorFields, delegate
			{
				CommitDictionaryDataEditor(GetSelectedFeatureDictionary(), _featureDataEditorInputs, _featureDataEditorBindings, RefreshFeatureDataList, "保存 FeatureData");
				_featureDataEditorWindow.Hide();
			});
		}
	}

	private void EnsureProcessDataEditorWindow()
	{
		if (!GodotObject.IsInstanceValid(_processDataEditorWindow))
		{
			_processDataEditorWindow = CreateLevelDataEditorWindow("ProcessDataEditorWindow", "编辑 Process Data", out _processDataEditorTitle, out _processDataEditorFields, delegate
			{
				CommitDictionaryDataEditor(GetProcessData(_editingLevel), _processDataEditorInputs, _processDataEditorBindings, RefreshProcessDataList, "保存 ProcessData");
				_processDataEditorWindow.Hide();
			});
		}
	}

	private void EnsureEventEditorWindow()
	{
		if (!GodotObject.IsInstanceValid(_eventEditorWindow))
		{
			_eventEditorWindow = CreateLevelDataEditorWindow("EventEditorWindow", "编辑 Event", out _eventEditorTitle, out _eventEditorFields, delegate
			{
				CommitEventEditor();
				_eventEditorWindow.Hide();
			});
		}
	}

	private Window CreateLevelDataEditorWindow(string nodeName, string title, out Label titleLabel, out VBoxContainer fieldsRoot, Action saveAction)
	{
		if (_levelDataEditorScene == null)
		{
			_levelDataEditorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelDataEditorWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		Window window = _levelDataEditorScene?.Instantiate<Window>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(window))
		{
			titleLabel = null;
			fieldsRoot = null;
			return null;
		}
		window.Name = nodeName;
		window.Title = title;
		window.CloseRequested += delegate
		{
			window.Hide();
		};
		AddChild(window, forceReadableName: false, InternalMode.Disabled);
		titleLabel = window.GetNode<Label>("Layout/Title");
		fieldsRoot = window.GetNode<VBoxContainer>("Layout/FieldScroll/Fields");
		window.GetNode<Button>("Layout/Controls/CancelButton").Pressed += delegate
		{
			window.Hide();
		};
		window.GetNode<Button>("Layout/Controls/SaveButton").Pressed += saveAction;
		return window;
	}

	private void BuildDictionaryDataEditorFields(VBoxContainer fieldsRoot, System.Collections.Generic.Dictionary<string, LineEdit> inputs, System.Collections.Generic.Dictionary<string, LevelDataEditorBinding> bindings, Dictionary data, IEnumerable<string> defaultFields, IReadOnlyDictionary<string, Variant> defaultValues = null)
	{
		ClearChildren(fieldsRoot);
		inputs.Clear();
		bindings.Clear();
		List<string> list = ApplyEditableDefaults(data, defaultFields);
		if (list.Count == 0)
		{
			fieldsRoot.AddChild(new Label
			{
				Text = "没有可编辑字段",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		Action action = null;
		action = delegate
		{
			BuildDictionaryDataEditorFields(fieldsRoot, inputs, bindings, data, defaultFields, defaultValues);
		};
		foreach (string item in list)
		{
			Variant dictionaryKey;
			Variant value;
			bool flag = TryGetDictionaryEntryByText(data, item, out dictionaryKey, out value);
			Variant.Type variantType = value.VariantType;
			if (!flag && defaultValues != null && defaultValues.TryGetValue(item, out var value2))
			{
				value = value2;
				variantType = value2.VariantType;
			}
			List<LevelDataPathSegment> path = new List<LevelDataPathSegment>
			{
				new LevelDataPathSegment(dictionaryKey, item)
			};
			fieldsRoot.AddChild(BuildLevelDataEditorNode(inputs, bindings, data, path, item, value, 0, action, flag, variantType), forceReadableName: false, InternalMode.Disabled);
		}
	}

	private Control BuildLevelDataEditorNode(System.Collections.Generic.Dictionary<string, LineEdit> inputs, System.Collections.Generic.Dictionary<string, LevelDataEditorBinding> bindings, Dictionary rootData, List<LevelDataPathSegment> path, string label, Variant value, int depth, Action rebuild, bool wasPresent = true, Variant.Type expectedType = Variant.Type.Nil)
	{
		return value.VariantType switch
		{
			Variant.Type.Dictionary => BuildDictionaryDataEditorNode(inputs, bindings, rootData, path, label, value.AsGodotDictionary(), depth, rebuild), 
			Variant.Type.Array => BuildArrayDataEditorNode(inputs, bindings, rootData, path, label, value.AsGodotArray(), depth, rebuild), 
			_ => BuildScalarDataEditorRow(inputs, bindings, rootData, path, label, value, depth, rebuild, wasPresent, expectedType), 
		};
	}

	private Control BuildDictionaryDataEditorNode(System.Collections.Generic.Dictionary<string, LineEdit> inputs, System.Collections.Generic.Dictionary<string, LevelDataEditorBinding> bindings, Dictionary rootData, List<LevelDataPathSegment> path, string label, Dictionary dictionary, int depth, Action rebuild)
	{
		if (dictionary == null)
		{
			dictionary = new Dictionary();
		}
		VBoxContainer vBoxContainer = CreateNestedDataBox(depth);
		HBoxContainer hBoxContainer = CreateNestedDataHeader(label, $"Dictionary({dictionary.Count})", depth);
		hBoxContainer.AddChild(CreateToolbarButton("+字段", delegate
		{
			AddDictionaryDataField(dictionary);
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("+元素", delegate
		{
			string text2 = AddDictionaryDataField(dictionary);
			dictionary[text2] = Variant.From<Godot.Collections.Array>(new Godot.Collections.Array());
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("删除", delegate
		{
			RemoveDictionaryDataValueAtPath(rootData, path);
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		if (dictionary.Count == 0)
		{
			vBoxContainer.AddChild(new Label
			{
				Text = "空字典",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			}, forceReadableName: false, InternalMode.Disabled);
			return vBoxContainer;
		}
		foreach (Variant key in dictionary.Keys)
		{
			string text = key.AsString();
			List<LevelDataPathSegment> list = CopyDataPath(path);
			list.Add(new LevelDataPathSegment(key, text));
			vBoxContainer.AddChild(BuildLevelDataEditorNode(inputs, bindings, rootData, list, text, dictionary[key], depth + 1, rebuild, wasPresent: true, Variant.Type.Nil), forceReadableName: false, InternalMode.Disabled);
		}
		return vBoxContainer;
	}

	private Control BuildArrayDataEditorNode(System.Collections.Generic.Dictionary<string, LineEdit> inputs, System.Collections.Generic.Dictionary<string, LevelDataEditorBinding> bindings, Dictionary rootData, List<LevelDataPathSegment> path, string label, Godot.Collections.Array array, int depth, Action rebuild)
	{
		if (array == null)
		{
			array = new Godot.Collections.Array();
		}
		VBoxContainer vBoxContainer = CreateNestedDataBox(depth);
		HBoxContainer hBoxContainer = CreateNestedDataHeader(label, $"Array({array.Count})", depth);
		hBoxContainer.AddChild(CreateToolbarButton("+元素", delegate
		{
			AddArrayDataElement(array);
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("+字典", delegate
		{
			AddArrayDataElement(array, new Dictionary());
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("删除", delegate
		{
			RemoveDictionaryDataValueAtPath(rootData, path);
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		if (array.Count == 0)
		{
			vBoxContainer.AddChild(new Label
			{
				Text = "空数组",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			}, forceReadableName: false, InternalMode.Disabled);
			return vBoxContainer;
		}
		for (int num = 0; num < array.Count; num++)
		{
			List<LevelDataPathSegment> list = CopyDataPath(path);
			list.Add(new LevelDataPathSegment(num));
			vBoxContainer.AddChild(BuildLevelDataEditorNode(inputs, bindings, rootData, list, $"[{num}]", array[num], depth + 1, rebuild, wasPresent: true, Variant.Type.Nil), forceReadableName: false, InternalMode.Disabled);
		}
		return vBoxContainer;
	}

	private Control BuildScalarDataEditorRow(System.Collections.Generic.Dictionary<string, LineEdit> inputs, System.Collections.Generic.Dictionary<string, LevelDataEditorBinding> bindings, Dictionary rootData, List<LevelDataPathSegment> path, string label, Variant value, int depth, Action rebuild, bool wasPresent, Variant.Type expectedType)
	{
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = SanitizeNodeName(BuildDataPathId(path)) + "Row",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddThemeConstantOverride("separation", 6);
		hBoxContainer.AddChild(new Control
		{
			CustomMinimumSize = new Vector2(Mathf.Max(0, depth) * 18, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(128f, 0f),
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = true
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit edit = new LineEdit
		{
			Name = SanitizeNodeName(BuildDataPathId(path)) + "LineEdit",
			Text = FormatVariant(value),
			PlaceholderText = label,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
		string key = BuildDataPathId(path);
		LevelDataEditorBinding binding = new LevelDataEditorBinding
		{
			Editor = edit,
			WasPresent = wasPresent,
			ExpectedType = ((expectedType == Variant.Type.Nil) ? value.VariantType : expectedType)
		};
		binding.Path.AddRange(CopyDataPath(path));
		edit.TextChanged += delegate
		{
			binding.Dirty = true;
		};
		inputs[key] = edit;
		bindings[key] = binding;
		string selectorKind = GetSelectableKindForProperty(label);
		if (!string.IsNullOrWhiteSpace(selectorKind))
		{
			hBoxContainer.AddChild(CreateToolbarButton("选择", delegate
			{
				ShowKeySelectionForLineEdit(edit, selectorKind);
			}), forceReadableName: false, InternalMode.Disabled);
		}
		hBoxContainer.AddChild(CreateToolbarButton("字典", delegate
		{
			SetDictionaryDataValueAtPath(rootData, path, Variant.From<Dictionary>(new Dictionary()));
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("数组", delegate
		{
			SetDictionaryDataValueAtPath(rootData, path, Variant.From<Godot.Collections.Array>(new Godot.Collections.Array()));
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("删除", delegate
		{
			RemoveDictionaryDataValueAtPath(rootData, path);
			rebuild?.Invoke();
		}), forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static VBoxContainer CreateNestedDataBox(int depth)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		vBoxContainer.AddThemeConstantOverride("separation", 4);
		return vBoxContainer;
	}

	private static HBoxContainer CreateNestedDataHeader(string label, string typeText, int depth)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 6);
		hBoxContainer.AddChild(new Control
		{
			CustomMinimumSize = new Vector2(Mathf.Max(0, depth) * 18, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = label + "  " + typeText,
			ClipText = true,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private void CommitDictionaryDataEditor(Dictionary data, System.Collections.Generic.Dictionary<string, LineEdit> inputs, System.Collections.Generic.Dictionary<string, LevelDataEditorBinding> bindings, Action refreshAction, string outputLabel)
	{
		if (_editingLevel == null || data == null)
		{
			return;
		}
		foreach (LevelDataEditorBinding value2 in bindings.Values)
		{
			if (value2 != null && GodotObject.IsInstanceValid(value2.Editor) && (value2.WasPresent || value2.Dirty))
			{
				if (!TryParseVariantText(value2.Editor.Text, value2.ExpectedType, out var value))
				{
					AddLevelEditorOutput($"字段 {BuildDataPathId(value2.Path)} 的值与类型 {value2.ExpectedType} 不匹配", XWOutputPanel.MessageType.Warning);
				}
				else
				{
					SetDictionaryDataValueAtPath(data, value2.Path, value);
				}
			}
		}
		_editingLevel.EmitChanged();
		SaveCurrentEditedResource(_editingLevel);
		refreshAction?.Invoke();
		AddLevelEditorOutput(outputLabel);
	}

	private static List<LevelDataPathSegment> CopyDataPath(IEnumerable<LevelDataPathSegment> path)
	{
		if (path != null)
		{
			return new List<LevelDataPathSegment>(path);
		}
		return new List<LevelDataPathSegment>();
	}

	private static string BuildDataPathId(IEnumerable<LevelDataPathSegment> path)
	{
		List<string> list = new List<string>();
		if (path != null)
		{
			foreach (LevelDataPathSegment item in path)
			{
				list.Add(item.DisplayName.Replace("/", "_"));
			}
		}
		if (list.Count != 0)
		{
			return string.Join("/", list);
		}
		return "root";
	}

	private static bool TryGetDictionaryEntryByText(Dictionary data, string keyText, out Variant dictionaryKey, out Variant value)
	{
		string from = keyText ?? "";
		dictionaryKey = Variant.From(in from);
		value = default(Variant);
		if (data == null)
		{
			return false;
		}
		foreach (Variant key in data.Keys)
		{
			if (string.Equals(key.AsString(), keyText ?? "", StringComparison.OrdinalIgnoreCase))
			{
				dictionaryKey = key;
				value = data[key];
				return true;
			}
		}
		return false;
	}

	private static string AddDictionaryDataField(Dictionary dictionary)
	{
		if (dictionary == null)
		{
			return "";
		}
		string text = "new_key";
		string text2 = text;
		int num = 1;
		Variant dictionaryKey;
		Variant value;
		while (TryGetDictionaryEntryByText(dictionary, text2, out dictionaryKey, out value))
		{
			text2 = $"{text}_{num++}";
		}
		dictionary[text2] = "";
		return text2;
	}

	private static void AddArrayDataElement(Godot.Collections.Array array)
	{
		AddArrayDataElement(array, Variant.From<string>(""));
	}

	private static void AddArrayDataElement(Godot.Collections.Array array, Variant value)
	{
		array?.Add(DuplicateLevelDataValue(value));
	}

	private static void RemoveDictionaryDataValueAtPath(Dictionary rootData, IReadOnlyList<LevelDataPathSegment> path)
	{
		if (rootData == null || path == null || path.Count == 0)
		{
			return;
		}
		object container;
		if (path.Count == 1)
		{
			if (!path[0].IsArrayIndex)
			{
				RemoveDictionaryKey(rootData, path[0].DictionaryKey);
			}
		}
		else if (TryGetContainerAtPath(rootData, path, path.Count - 1, out container))
		{
			LevelDataPathSegment levelDataPathSegment = path[path.Count - 1];
			if (container is Dictionary dictionary && !levelDataPathSegment.IsArrayIndex)
			{
				RemoveDictionaryKey(dictionary, levelDataPathSegment.DictionaryKey);
			}
			else if (container is Godot.Collections.Array array && levelDataPathSegment.IsArrayIndex && levelDataPathSegment.ArrayIndex >= 0 && levelDataPathSegment.ArrayIndex < array.Count)
			{
				array.RemoveAt(levelDataPathSegment.ArrayIndex);
			}
		}
	}

	private static bool SetDictionaryDataValueAtPath(Dictionary rootData, IReadOnlyList<LevelDataPathSegment> path, Variant value)
	{
		if (rootData == null || path == null || path.Count == 0)
		{
			return false;
		}
		if (path.Count == 1)
		{
			LevelDataPathSegment levelDataPathSegment = path[0];
			if (levelDataPathSegment.IsArrayIndex)
			{
				return false;
			}
			SetDictionaryValue(rootData, levelDataPathSegment.DictionaryKey, DuplicateLevelDataValue(value));
			return true;
		}
		if (!TryGetContainerAtPath(rootData, path, path.Count - 1, out var container))
		{
			return false;
		}
		LevelDataPathSegment levelDataPathSegment2 = path[path.Count - 1];
		if (container is Dictionary dictionary && !levelDataPathSegment2.IsArrayIndex)
		{
			SetDictionaryValue(dictionary, levelDataPathSegment2.DictionaryKey, DuplicateLevelDataValue(value));
			return true;
		}
		if (container is Godot.Collections.Array array && levelDataPathSegment2.IsArrayIndex && levelDataPathSegment2.ArrayIndex >= 0 && levelDataPathSegment2.ArrayIndex < array.Count)
		{
			array[levelDataPathSegment2.ArrayIndex] = DuplicateLevelDataValue(value);
			return true;
		}
		return false;
	}

	private static bool TryGetContainerAtPath(Dictionary rootData, IReadOnlyList<LevelDataPathSegment> path, int segmentCount, out object container)
	{
		container = rootData;
		if (rootData == null || path == null)
		{
			return false;
		}
		for (int i = 0; i < segmentCount; i++)
		{
			LevelDataPathSegment levelDataPathSegment = path[i];
			bool flag = i + 1 < path.Count && path[i + 1].IsArrayIndex;
			if (container is Dictionary dictionary)
			{
				if (levelDataPathSegment.IsArrayIndex)
				{
					return false;
				}
				Variant key = ResolveDictionaryKey(dictionary, levelDataPathSegment.DictionaryKey);
				if (!dictionary.ContainsKey(key) || !TryGetVariantContainer(dictionary[key], out container))
				{
					container = (flag ? ((object)new Godot.Collections.Array()) : ((object)new Dictionary()));
					dictionary[key] = VariantFromLevelDataContainer(container);
				}
				continue;
			}
			if (container is Godot.Collections.Array array)
			{
				if (!levelDataPathSegment.IsArrayIndex)
				{
					return false;
				}
				while (array.Count <= levelDataPathSegment.ArrayIndex)
				{
					array.Add(flag ? Variant.From<Godot.Collections.Array>(new Godot.Collections.Array()) : Variant.From<Dictionary>(new Dictionary()));
				}
				if (!TryGetVariantContainer(array[levelDataPathSegment.ArrayIndex], out container))
				{
					container = (flag ? ((object)new Godot.Collections.Array()) : ((object)new Dictionary()));
					array[levelDataPathSegment.ArrayIndex] = VariantFromLevelDataContainer(container);
				}
				continue;
			}
			return false;
		}
		object obj = container;
		if (obj is Dictionary || obj is Godot.Collections.Array)
		{
			return true;
		}
		return false;
	}

	private static bool TryGetVariantContainer(Variant value, out object container)
	{
		container = null;
		if (value.VariantType == Variant.Type.Dictionary)
		{
			container = value.AsGodotDictionary();
			return true;
		}
		if (value.VariantType == Variant.Type.Array)
		{
			container = value.AsGodotArray();
			return true;
		}
		return false;
	}

	private static Variant VariantFromLevelDataContainer(object container)
	{
		Dictionary from = container as Dictionary;
		if (from == null)
		{
			Godot.Collections.Array from2 = container as Godot.Collections.Array;
			if (from2 != null)
			{
				return Variant.From(in from2);
			}
			return default(Variant);
		}
		return Variant.From(in from);
	}

	private static Variant ResolveDictionaryKey(Dictionary dictionary, Variant requestedKey)
	{
		if (dictionary == null)
		{
			return requestedKey;
		}
		if (dictionary.ContainsKey(requestedKey))
		{
			return requestedKey;
		}
		string b = requestedKey.AsString();
		foreach (Variant key in dictionary.Keys)
		{
			if (string.Equals(key.AsString(), b, StringComparison.OrdinalIgnoreCase))
			{
				return key;
			}
		}
		return requestedKey;
	}

	private static void SetDictionaryValue(Dictionary dictionary, Variant key, Variant value)
	{
		if (dictionary != null)
		{
			dictionary[ResolveDictionaryKey(dictionary, key)] = value;
		}
	}

	private static void RemoveDictionaryKey(Dictionary dictionary, Variant key)
	{
		dictionary?.Remove(ResolveDictionaryKey(dictionary, key));
	}

	private static Variant DuplicateLevelDataValue(Variant value)
	{
		return value.VariantType switch
		{
			Variant.Type.Dictionary => Variant.From<Dictionary>(value.AsGodotDictionary().Duplicate(deep: true)), 
			Variant.Type.Array => Variant.From<Godot.Collections.Array>(value.AsGodotArray().Duplicate(deep: true)), 
			_ => value, 
		};
	}

	private void BuildEventEditorFields(Resource levelEvent)
	{
		ClearChildren(_eventEditorFields);
		_eventEditorInputs.Clear();
		List<string> list = CollectEditableEventProperties(levelEvent);
		if (list.Count == 0)
		{
			_eventEditorFields.AddChild(new Label
			{
				Text = "这个 Event 没有可编辑属性",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		foreach (string item in list)
		{
			LineEdit lineEdit = new LineEdit
			{
				Name = SanitizeNodeName(item) + "LineEdit",
				Text = FormatVariant(levelEvent.Get(item)),
				PlaceholderText = item,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_eventEditorInputs[item] = lineEdit;
			string selectableKindForProperty = GetSelectableKindForProperty(item);
			_eventEditorFields.AddChild(string.IsNullOrWhiteSpace(selectableKindForProperty) ? WrapField(item, lineEdit) : CreateSelectableFieldRow(item, lineEdit, selectableKindForProperty), forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void CommitEventEditor()
	{
		Resource editingEventResource = GetEditingEventResource();
		if (!GodotObject.IsInstanceValid(editingEventResource))
		{
			return;
		}
		foreach (KeyValuePair<string, LineEdit> eventEditorInput in _eventEditorInputs)
		{
			if (GodotObject.IsInstanceValid(eventEditorInput.Value))
			{
				editingEventResource.Set(eventEditorInput.Key, ParseVariantText(eventEditorInput.Value.Text, Variant.Type.Nil));
			}
		}
		editingEventResource.EmitChanged();
		if (_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			towerDefenseLevelConfig.MarkEventDataEditedFromEditor();
		}
		SaveCurrentEditedResource(_editingLevel);
		RefreshEventPanel();
		AddLevelEditorOutput($"保存 Event: {_editingEventPropertyName}[{_editingEventIndex}]");
	}

	private static IEnumerable<string> GetFeatureEditableDefaults(string featureName)
	{
		if (string.IsNullOrWhiteSpace(featureName))
		{
			return System.Array.Empty<string>();
		}
		if (FeatureEditableFieldDefaults.TryGetValue(featureName, out var value))
		{
			return value;
		}
		foreach (KeyValuePair<string, string[]> featureEditableFieldDefault in FeatureEditableFieldDefaults)
		{
			if (featureName.Contains(featureEditableFieldDefault.Key, StringComparison.OrdinalIgnoreCase))
			{
				return featureEditableFieldDefault.Value;
			}
		}
		return System.Array.Empty<string>();
	}

	private static IReadOnlyDictionary<string, Variant> GetFeatureEditableDefaultValues(string featureName, Dictionary data)
	{
		if (string.IsNullOrWhiteSpace(featureName))
		{
			return null;
		}
		IReadOnlyDictionary<string, Variant> readOnlyDictionary = null;
		if (FeatureEditableFieldDefaultValues.TryGetValue(featureName, out var value))
		{
			readOnlyDictionary = value;
		}
		else
		{
			foreach (KeyValuePair<string, IReadOnlyDictionary<string, Variant>> featureEditableFieldDefaultValue in FeatureEditableFieldDefaultValues)
			{
				if (featureName.Contains(featureEditableFieldDefaultValue.Key, StringComparison.OrdinalIgnoreCase))
				{
					readOnlyDictionary = featureEditableFieldDefaultValue.Value;
					break;
				}
			}
		}
		if (readOnlyDictionary == null || data == null || data.ContainsKey("Mode") || !data.GetValueOrDefault("ManualProgress", false).AsBool())
		{
			return readOnlyDictionary;
		}
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, Variant> item in readOnlyDictionary)
		{
			dictionary[item.Key] = item.Value;
		}
		dictionary["Mode"] = Variant.From<string>("Manual");
		return dictionary;
	}

	private static IEnumerable<string> GetProcessEditableDefaults(string processName)
	{
		if (string.IsNullOrWhiteSpace(processName))
		{
			return System.Array.Empty<string>();
		}
		if (ProcessEditableFieldDefaults.TryGetValue(processName, out var value))
		{
			return value;
		}
		foreach (KeyValuePair<string, string[]> processEditableFieldDefault in ProcessEditableFieldDefaults)
		{
			if (processName.Contains(processEditableFieldDefault.Key, StringComparison.OrdinalIgnoreCase))
			{
				return processEditableFieldDefault.Value;
			}
		}
		return System.Array.Empty<string>();
	}

	private static List<string> ApplyEditableDefaults(Dictionary data, IEnumerable<string> defaultFields)
	{
		List<string> keys = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (defaultFields != null)
		{
			foreach (string defaultField in defaultFields)
			{
				AddKey(defaultField);
			}
		}
		if (data != null)
		{
			foreach (Variant key in data.Keys)
			{
				AddKey(key.AsString());
			}
		}
		return keys;
		void AddKey(string key)
		{
			key = key?.StripEdges() ?? "";
			if (!string.IsNullOrWhiteSpace(key) && seen.Add(key))
			{
				keys.Add(key);
			}
		}
	}

	private static List<string> CollectEditableEventProperties(Resource levelEvent)
	{
		List<string> list = new List<string>();
		if (!GodotObject.IsInstanceValid(levelEvent))
		{
			return list;
		}
		foreach (Dictionary property in levelEvent.GetPropertyList())
		{
			if (!property.TryGetValue("name", out var value))
			{
				continue;
			}
			string text = value.AsString();
			if (!string.IsNullOrWhiteSpace(text) && !IsInternalEventProperty(text))
			{
				PropertyUsageFlags propertyUsageFlags = PropertyUsageFlags.None;
				if (property.TryGetValue("usage", out var value2))
				{
					propertyUsageFlags = (PropertyUsageFlags)value2.AsInt64();
				}
				if ((propertyUsageFlags & PropertyUsageFlags.Editor) != PropertyUsageFlags.None && (propertyUsageFlags & PropertyUsageFlags.ReadOnly) == PropertyUsageFlags.None && property.TryGetValue("type", out var value3) && IsLineEditableEventPropertyType((Variant.Type)value3.AsInt64()))
				{
					list.Add(text);
				}
			}
		}
		return list;
	}

	private static bool IsLineEditableEventPropertyType(Variant.Type type)
	{
		if ((ulong)(type - 1) <= 3uL || (ulong)(type - 21) <= 1uL)
		{
			return true;
		}
		return false;
	}

	private Resource GetEditingEventResource()
	{
		Array<TowerDefenseLevelEventBase> levelEventArray = GetLevelEventArray(_editingLevel, _editingEventPropertyName);
		if (levelEventArray == null || _editingEventIndex < 0 || _editingEventIndex >= levelEventArray.Count)
		{
			return null;
		}
		return levelEventArray[_editingEventIndex];
	}

	private static Array<TowerDefenseLevelEventBase> GetLevelEventArray(Resource level, string eventPropertyName)
	{
		if (!(level is TowerDefenseLevelConfig towerDefenseLevelConfig))
		{
			return null;
		}
		return eventPropertyName switch
		{
			"eventInit" => towerDefenseLevelConfig.eventInit, 
			"eventReady" => towerDefenseLevelConfig.eventReady, 
			"eventStart" => towerDefenseLevelConfig.eventStart, 
			_ => null, 
		};
	}

	private static bool IsInternalEventProperty(string propertyName)
	{
		switch (propertyName)
		{
		default:
			return propertyName.StartsWith("metadata/", StringComparison.OrdinalIgnoreCase);
		case "resource_name":
		case "resource_local_to_scene":
		case "script":
			return true;
		}
	}

	private static void ClearChildren(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		foreach (Node child in node.GetChildren())
		{
			child.QueueFree();
		}
	}

	private static string SanitizeNodeName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "Property";
		}
		char[] array = value.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (!char.IsLetterOrDigit(array[i]))
			{
				array[i] = '_';
			}
		}
		return new string(array);
	}

	private void AddFeatureEntry()
	{
		AddFeatureEntry(_featureNameEdit?.Text, "");
	}

	private void AddFeatureEntry(string featureName, string sourcePath)
	{
		if (_editingLevel == null)
		{
			return;
		}
		featureName = featureName?.StripEdges() ?? "";
		if (!string.IsNullOrWhiteSpace(featureName))
		{
			Godot.Collections.Dictionary<StringName, Dictionary> featureData = GetFeatureData(_editingLevel);
			StringName key = new StringName(featureName);
			if (!featureData.ContainsKey(key))
			{
				featureData[key] = new Dictionary();
			}
			_selectedFeatureName = featureName;
			_selectedFeatureIndex = GetFeatureIndex(featureName);
			if (GodotObject.IsInstanceValid(_featureNameEdit))
			{
				_featureNameEdit.Text = featureName;
			}
			SaveCurrentEditedResource(_editingLevel);
			RegisterProjectSelectionPath(sourcePath);
			SyncProjectFeatures();
			AddLevelEditorOutput("添加 Feature: " + featureName);
			RefreshFeatureLists();
		}
	}

	private void AddFeatureFromSelection(LevelOptionItem item)
	{
		if (item != null)
		{
			AddFeatureEntry(item.Name, item.SourcePath);
		}
	}

	private void RemoveSelectedFeatureEntry()
	{
		if (_editingLevel != null && !string.IsNullOrWhiteSpace(_selectedFeatureName))
		{
			GetFeatureData(_editingLevel).Remove(new StringName(_selectedFeatureName));
			_selectedFeatureName = "";
			_selectedFeatureIndex = -1;
			SaveCurrentEditedResource(_editingLevel);
			RefreshFeatureLists();
		}
	}

	private void SetFeatureEnabled(bool enabled)
	{
		if (!_updatingControls && _editingLevel != null)
		{
			if (!enabled)
			{
				RemoveSelectedFeatureEntry();
			}
			else
			{
				AddFeatureEntry();
			}
		}
	}

	private void SetFeatureDataValue(string key, string value)
	{
		if (_editingLevel != null && !string.IsNullOrWhiteSpace(_selectedFeatureName) && !string.IsNullOrWhiteSpace(key))
		{
			Godot.Collections.Dictionary<StringName, Dictionary> featureData = GetFeatureData(_editingLevel);
			StringName key2 = new StringName(_selectedFeatureName);
			if (!featureData.ContainsKey(key2))
			{
				featureData[key2] = new Dictionary();
			}
			featureData[key2][key] = ParseVariantText(value, Variant.Type.Nil);
			SaveCurrentEditedResource(_editingLevel);
			RefreshFeatureDataList();
		}
	}

	private void SetProcessName(string processName)
	{
		if (_editingLevel != null)
		{
			SetLevelProperty("processName", Variant.From<StringName>(new StringName(processName?.StripEdges() ?? "")), refresh: false);
			if (GodotObject.IsInstanceValid(_processNameEdit))
			{
				_processNameEdit.Text = processName ?? "";
			}
			RefreshProcessPanel();
		}
	}

	private void AddProcessFromSelection(LevelOptionItem item)
	{
		if (item != null)
		{
			SetProcessName(item.Name);
			RegisterProjectSelectionPath(item.SourcePath);
			AddLevelEditorOutput("设置 Process: " + item.Name);
		}
	}

	private void SetProcessDataValue(string key, string value)
	{
		if (_editingLevel != null && !string.IsNullOrWhiteSpace(key))
		{
			GetProcessData(_editingLevel)[key] = ParseVariantText(value, Variant.Type.Nil);
			SaveCurrentEditedResource(_editingLevel);
			RefreshProcessDataList();
		}
	}

	private void AddLevelEventFromSelection(string eventPropertyName, LevelOptionItem item)
	{
		if (!(_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig) || item == null || string.IsNullOrWhiteSpace(eventPropertyName))
		{
			return;
		}
		TowerDefenseLevelEventBase towerDefenseLevelEventBase = TowerDefenseLevelEventRegistry.Create(item.Name);
		if (!GodotObject.IsInstanceValid(towerDefenseLevelEventBase))
		{
			AddLevelEditorOutput("无法创建 Event: " + item.Name, XWOutputPanel.MessageType.Warning);
			return;
		}
		towerDefenseLevelEventBase.ResourceName = item.Name;
		switch (eventPropertyName)
		{
		default:
			return;
		case "eventInit":
		{
			TowerDefenseLevelConfig towerDefenseLevelConfig2 = towerDefenseLevelConfig;
			if (towerDefenseLevelConfig2.eventInit == null)
			{
				towerDefenseLevelConfig2.eventInit = new Array<TowerDefenseLevelEventBase>();
			}
			towerDefenseLevelConfig.eventInit.Add(towerDefenseLevelEventBase);
			break;
		}
		case "eventReady":
		{
			TowerDefenseLevelConfig towerDefenseLevelConfig2 = towerDefenseLevelConfig;
			if (towerDefenseLevelConfig2.eventReady == null)
			{
				towerDefenseLevelConfig2.eventReady = new Array<TowerDefenseLevelEventBase>();
			}
			towerDefenseLevelConfig.eventReady.Add(towerDefenseLevelEventBase);
			break;
		}
		case "eventStart":
		{
			TowerDefenseLevelConfig towerDefenseLevelConfig2 = towerDefenseLevelConfig;
			if (towerDefenseLevelConfig2.eventStart == null)
			{
				towerDefenseLevelConfig2.eventStart = new Array<TowerDefenseLevelEventBase>();
			}
			towerDefenseLevelConfig.eventStart.Add(towerDefenseLevelEventBase);
			break;
		}
		}
		towerDefenseLevelConfig.MarkEventDataEditedFromEditor();
		SaveCurrentEditedResource(towerDefenseLevelConfig);
		RefreshEventPanel();
		AddLevelEditorOutput("添加 Event: " + item.Name + " -> " + eventPropertyName);
	}

	private void SelectFeature(int index)
	{
		_selectedFeatureIndex = index;
		_selectedFeatureName = GetFeatureNameAt(index);
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_featureNameEdit))
			{
				_featureNameEdit.Text = _selectedFeatureName;
			}
			if (GodotObject.IsInstanceValid(_featureEnabledCheck))
			{
				_featureEnabledCheck.ButtonPressed = !string.IsNullOrWhiteSpace(_selectedFeatureName);
			}
		}
		finally
		{
			_updatingControls = false;
		}
		RefreshFeatureDataList();
	}

	private void SelectFeatureDataRow(int index)
	{
		if (_editingLevel == null || string.IsNullOrWhiteSpace(_selectedFeatureName))
		{
			return;
		}
		Dictionary selectedFeatureDictionary = GetSelectedFeatureDictionary();
		if (selectedFeatureDictionary == null || index < 0 || index >= selectedFeatureDictionary.Count)
		{
			return;
		}
		int num = 0;
		foreach (Variant key in selectedFeatureDictionary.Keys)
		{
			if (num++ == index)
			{
				if (GodotObject.IsInstanceValid(_featureDataKeyEdit))
				{
					_featureDataKeyEdit.Text = key.AsString();
				}
				if (GodotObject.IsInstanceValid(_featureDataValueEdit))
				{
					_featureDataValueEdit.Text = FormatVariant(selectedFeatureDictionary[key]);
				}
				break;
			}
		}
	}

	private void SelectProcessDataRow(int index)
	{
		Dictionary processData = GetProcessData(_editingLevel);
		if (processData == null || index < 0 || index >= processData.Count)
		{
			return;
		}
		int num = 0;
		foreach (Variant key in processData.Keys)
		{
			if (num++ == index)
			{
				if (GodotObject.IsInstanceValid(_processDataKeyEdit))
				{
					_processDataKeyEdit.Text = key.AsString();
				}
				if (GodotObject.IsInstanceValid(_processDataValueEdit))
				{
					_processDataValueEdit.Text = FormatVariant(processData[key]);
				}
				break;
			}
		}
	}

	private void SelectPreSpawn(int index)
	{
		if (!(_editingLevel is TowerDefenseLevelConfig { preSpawnList: not null } towerDefenseLevelConfig) || index < 0 || index >= towerDefenseLevelConfig.preSpawnList.Count)
		{
			_selectedPreSpawnIndex = -1;
			UpdatePreSpawnSelectionLabel();
			QueuePreSpawnGridRedraw();
			return;
		}
		_selectedPreSpawnIndex = index;
		TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = towerDefenseLevelConfig.preSpawnList[index];
		if (GodotObject.IsInstanceValid(_preSpawnPacketNameEdit) && GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig))
		{
			_preSpawnPacketNameEdit.Text = towerDefenseLevelPreSpawnConfig.packetName ?? "";
		}
		UpdatePreSpawnSelectionLabel();
		QueuePreSpawnGridRedraw();
	}

	private static bool IsLevelResource(Resource resource)
	{
		if (resource is TowerDefenseLevelConfig || resource is TowerDefenseLevelNewConfig)
		{
			return true;
		}
		return false;
	}

	private static string BuildSummary(Resource level)
	{
		return $"关卡: {EmptyToPlaceholder(ReadString(level, "name"))} / {EmptyToPlaceholder(ReadString(level, "levelName"))}  Feature={ReadFeatureCount(level)}  Process={EmptyToPlaceholder(ReadString(level, "processName"))}";
	}

	private static int ReadFeatureCount(Resource level)
	{
		if (!(level is TowerDefenseLevelConfig towerDefenseLevelConfig))
		{
			if (level is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig)
			{
				return towerDefenseLevelNewConfig.featureData?.Count ?? 0;
			}
			return 0;
		}
		return towerDefenseLevelConfig.featureData?.Count ?? 0;
	}

	private static string ReadString(Resource resource, string propertyName)
	{
		Variant variant = ReadVariant(resource, propertyName);
		if (variant.VariantType != Variant.Type.Nil)
		{
			return variant.AsString();
		}
		return "";
	}

	private static bool ReadBool(Resource resource, string propertyName, bool fallback)
	{
		Variant variant = ReadVariant(resource, propertyName);
		if (variant.VariantType != Variant.Type.Bool)
		{
			return fallback;
		}
		return variant.AsBool();
	}

	private static double ReadDouble(Resource resource, string propertyName, double fallback)
	{
		Variant variant = ReadVariant(resource, propertyName);
		Variant.Type variantType = variant.VariantType;
		if ((ulong)(variantType - 2) > 1uL || 1 == 0)
		{
			return fallback;
		}
		return variant.AsDouble();
	}

	private static T ReadEnum<T>(Resource resource, string propertyName, T fallback) where T : struct, Enum
	{
		Variant variant = ReadVariant(resource, propertyName);
		Variant.Type variantType = variant.VariantType;
		if (((ulong)(variantType - 2) <= 1uL) ? true : false)
		{
			Type underlyingType = Enum.GetUnderlyingType(typeof(T));
			object value;
			try
			{
				value = Convert.ChangeType(variant.AsInt64(), underlyingType);
			}
			catch
			{
				return fallback;
			}
			if (!Enum.IsDefined(typeof(T), value))
			{
				return fallback;
			}
			return (T)Enum.ToObject(typeof(T), value);
		}
		if (!Enum.TryParse<T>(variant.AsString(), out var result))
		{
			return fallback;
		}
		return result;
	}

	private static Variant ReadVariant(Resource resource, string propertyName)
	{
		if (!GodotObject.IsInstanceValid(resource) || string.IsNullOrWhiteSpace(propertyName))
		{
			return default(Variant);
		}
		try
		{
			return resource.Get(propertyName);
		}
		catch
		{
			return default(Variant);
		}
	}

	private static string ReadCount(Resource resource, string propertyName)
	{
		Variant variant = ReadVariant(resource, propertyName);
		return variant.VariantType switch
		{
			Variant.Type.Array => variant.AsGodotArray().Count.ToString(), 
			Variant.Type.Dictionary => variant.AsGodotDictionary().Count.ToString(), 
			_ => "0", 
		};
	}

	private static Variant ParseVariantText(string text, Variant.Type expectedType = Variant.Type.Nil)
	{
		switch (expectedType)
		{
		case Variant.Type.String:
		{
			string from = text ?? "";
			return Variant.From(in from);
		}
		case Variant.Type.StringName:
			return Variant.From<StringName>(new StringName(text ?? ""));
		case Variant.Type.NodePath:
			return Variant.From<NodePath>(new NodePath(text ?? ""));
		case Variant.Type.Bool:
		{
			if (bool.TryParse(text, out var result))
			{
				return Variant.From(in result);
			}
			break;
		}
		}
		if (expectedType == Variant.Type.Int && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
		{
			return Variant.From(in result2);
		}
		if (expectedType == Variant.Type.Float && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
		{
			return Variant.From(in result3);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return Variant.From<string>("");
		}
		string from2 = text.StripEdges();
		if (bool.TryParse(from2, out var result4))
		{
			return Variant.From(in result4);
		}
		if (int.TryParse(from2, out var result5))
		{
			return Variant.From(in result5);
		}
		if (double.TryParse(from2, out var result6))
		{
			return Variant.From(in result6);
		}
		return Variant.From(in from2);
	}

	private static bool TryParseVariantText(string text, Variant.Type expectedType, out Variant value)
	{
		Variant.Type num = expectedType - 1;
		if ((ulong)num <= 2uL)
		{
			switch ((int)num)
			{
			case 0:
			{
				if (bool.TryParse(text, out var result3))
				{
					value = Variant.From(in result3);
					return true;
				}
				value = default(Variant);
				return false;
			}
			case 1:
			{
				if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
				{
					value = Variant.From(in result2);
					return true;
				}
				value = default(Variant);
				return false;
			}
			case 2:
			{
				if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					value = Variant.From(in result);
					return true;
				}
				value = default(Variant);
				return false;
			}
			}
		}
		value = ParseVariantText(text, expectedType);
		return true;
	}

	private Control CreateSelectableFieldRow(string label, LineEdit edit, string selectorKind, Action<string> changed = null)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(112f, 0f),
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = true
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(CreateToolbarButton("选择", delegate
		{
			ShowKeySelectionForLineEdit(edit, selectorKind, changed);
		}), forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private void ShowKeySelectionForLineEdit(LineEdit edit, string selectorKind, Action<string> changed = null)
	{
		if (!GodotObject.IsInstanceValid(edit))
		{
			return;
		}
		selectorKind = (selectorKind ?? "").ToLowerInvariant();
		if (selectorKind == "card")
		{
			ShowCardPacketSelector(edit, changed);
			return;
		}
		List<LevelOptionItem> options = BuildSelectableKeyOptions(selectorKind);
		ShowLevelOptionSelector("选择 " + GetSelectableKindDisplayName(selectorKind), options, delegate(LevelOptionItem item)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Name) && GodotObject.IsInstanceValid(edit))
			{
				_updatingControls = true;
				try
				{
					edit.Text = item.Name;
				}
				finally
				{
					_updatingControls = false;
				}
				changed?.Invoke(item.Name);
				RegisterProjectSelectionPath(item.SourcePath);
				AddLevelEditorOutput("选择 " + GetSelectableKindDisplayName(selectorKind) + ": " + item.Name);
			}
		});
	}

	private static string GetSelectableKindForProperty(string propertyName)
	{
		if (string.IsNullOrWhiteSpace(propertyName))
		{
			return "";
		}
		string text = propertyName.Replace("_", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
		switch (text)
		{
		case "map":
		case "mapname":
			return "map";
		case "bgm":
		case "bgmname":
		case "backgroundmusic":
			return "bgm";
		case "packetbank":
		case "packetbankname":
		case "seedbank":
		case "seedbankname":
			return "packetBank";
		case "levelkey":
		case "nextlevel":
			return "level";
		case "packetname":
		case "cardname":
		case "packet":
		case "card":
			return "card";
		case "customtutorial":
		case "tutorial":
		case "tutorialname":
			return "tutorial";
		case "npctalk":
		case "customtalk":
		case "talkname":
		case "talk":
			return "talk";
		case "zombiename":
		case "character":
		case "plantname":
		case "zombie":
		case "charactername":
		case "plant":
			return "character";
		default:
			return text.Contains("packetlist", StringComparison.OrdinalIgnoreCase) ? "card" : "";
		}
	}

	private static string GetSelectableKindDisplayName(string selectorKind)
	{
		return (selectorKind ?? "").ToLowerInvariant() switch
		{
			"map" => "地图", 
			"bgm" => "BGM", 
			"packetbank" => "卡牌库", 
			"level" => "关卡", 
			"card" => "卡片/角色", 
			"character" => "角色", 
			"talk" => "对话", 
			"tutorial" => "教程", 
			_ => "资源", 
		};
	}

	private static Button CreateToolbarButton(string text, Action pressed)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(54f, 28f);
		button.Pressed += delegate
		{
			pressed?.Invoke();
		};
		return button;
	}

	private static Control WrapField(string label, Control editor)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(112f, 0f),
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = true
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static void PopulateObjectArray<[MustBeVariant] T>(ItemList list, Array<T> array)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.Clear();
		if (array == null || array.Count == 0)
		{
			list.AddItem("未配置");
			return;
		}
		for (int i = 0; i < array.Count; i++)
		{
			list.AddItem($"{i}: {FormatObject(array[i])}");
		}
	}

	private void PopulatePreSpawnArray(ItemList list, Array<TowerDefenseLevelPreSpawnConfig> array)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.Clear();
		if (array == null || array.Count == 0)
		{
			_selectedPreSpawnIndex = -1;
			list.AddItem("未配置");
			return;
		}
		if (_selectedPreSpawnIndex >= array.Count)
		{
			_selectedPreSpawnIndex = -1;
		}
		for (int i = 0; i < array.Count; i++)
		{
			TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = array[i];
			list.AddItem(GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig) ? $"{i}: {towerDefenseLevelPreSpawnConfig.packetName} @ ({towerDefenseLevelPreSpawnConfig.gridPos.X}, {towerDefenseLevelPreSpawnConfig.gridPos.Y})" : $"{i}: 空");
		}
		if (_selectedPreSpawnIndex >= 0)
		{
			list.Select(_selectedPreSpawnIndex);
		}
	}

	private static void PopulateVariantArray(ItemList list, Godot.Collections.Array array)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.Clear();
		if (array == null || array.Count == 0)
		{
			list.AddItem("未配置");
			return;
		}
		for (int i = 0; i < array.Count; i++)
		{
			list.AddItem($"{i}: {FormatVariant(array[i])}");
		}
	}

	private static string FormatObject(object value)
	{
		if (value is Resource resource)
		{
			return FormatResource(resource);
		}
		if (!(value is GodotObject godotObject))
		{
			return value?.ToString() ?? "空";
		}
		return godotObject.GetType().Name;
	}

	private static string FormatVariant(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 1:
				return value.AsBool() ? "true" : "false";
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			case 4:
				goto IL_00bb;
			}
		}
		Variant.Type num = variantType - 21;
		if ((ulong)num > 7uL)
		{
			goto IL_0158;
		}
		switch ((int)num)
		{
		case 0:
		case 1:
			break;
		case 7:
			return $"数组({value.AsGodotArray().Count})";
		case 6:
			return $"字典({value.AsGodotDictionary().Count})";
		case 3:
			return FormatResource(value.AsGodotObject() as Resource);
		default:
			goto IL_0158;
		}
		goto IL_00bb;
		IL_0158:
		return value.ToString();
		IL_00bb:
		return value.AsString();
	}

	private static string FormatResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "未配置";
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourcePath))
		{
			return resource.ResourcePath.GetFile();
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourceName))
		{
			return resource.ResourceName;
		}
		return resource.GetType().Name;
	}

	private static void AddItemIfMissing(ItemList list, string text)
	{
		if (!GodotObject.IsInstanceValid(list) || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			if (list.GetItemText(i) == text)
			{
				return;
			}
		}
		list.AddItem(text);
	}

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未配置";
	}

	private void ShowLevelOptionSelector(string title, List<LevelOptionItem> options, Action<LevelOptionItem> confirmed)
	{
		EnsureLevelOptionSelectorWindow();
		if (GodotObject.IsInstanceValid(_levelOptionSelectorWindow))
		{
			_levelOptionSelectorConfirmed = confirmed;
			_levelOptionSelectorOptions.Clear();
			if (options != null)
			{
				_levelOptionSelectorOptions.AddRange(options);
			}
			_levelOptionSelectorWindow.OpenChoices(title, "按紧凑图鉴浏览游戏内与当前 Mod 的可用项", "", BuildLevelOptionChoices(), SelectLevelOptionChoice, "添加此项");
		}
	}

	private void EnsureLevelOptionSelectorWindow()
	{
		if (!GodotObject.IsInstanceValid(_levelOptionSelectorWindow))
		{
			_levelOptionSelectorWindow = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_levelOptionSelectorWindow))
			{
				AddChild(_levelOptionSelectorWindow, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private List<XWGameplayResourceChoice> BuildLevelOptionChoices()
	{
		List<XWGameplayResourceChoice> list = new List<XWGameplayResourceChoice>();
		foreach (LevelOptionItem levelOptionSelectorOption in _levelOptionSelectorOptions)
		{
			if (levelOptionSelectorOption != null && !string.IsNullOrWhiteSpace(levelOptionSelectorOption.Name))
			{
				string displayName = (string.IsNullOrWhiteSpace(levelOptionSelectorOption.DisplayName) ? levelOptionSelectorOption.Name : (levelOptionSelectorOption.DisplayName + " · " + levelOptionSelectorOption.Name));
				list.Add(new XWGameplayResourceChoice(levelOptionSelectorOption.Name, displayName, levelOptionSelectorOption.SourcePath ?? "", null, XWGameplayResourceKind.Resource, (levelOptionSelectorOption.SourceLabel ?? "").Contains("Mod", StringComparison.OrdinalIgnoreCase)));
			}
		}
		return list;
	}

	private static bool MatchesLevelOption(LevelOptionItem option, string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return true;
		}
		if (!(option.Name ?? "").ToLowerInvariant().Contains(query) && !(option.DisplayName ?? "").ToLowerInvariant().Contains(query) && !(option.SourceLabel ?? "").ToLowerInvariant().Contains(query) && !(option.SourcePath ?? "").ToLowerInvariant().Contains(query))
		{
			return (option.Description ?? "").ToLowerInvariant().Contains(query);
		}
		return true;
	}

	private void SelectLevelOptionChoice(XWGameplayResourceChoice choice)
	{
		if (!(choice == null))
		{
			LevelOptionItem levelOptionItem = _levelOptionSelectorOptions.Find((LevelOptionItem option) => option != null && string.Equals(option.Name, choice.Key, StringComparison.OrdinalIgnoreCase) && string.Equals(option.SourcePath ?? "", choice.ResourcePath ?? "", StringComparison.OrdinalIgnoreCase)) ?? _levelOptionSelectorOptions.Find((LevelOptionItem option) => option != null && string.Equals(option.Name, choice.Key, StringComparison.OrdinalIgnoreCase));
			if (levelOptionItem != null)
			{
				_levelOptionSelectorConfirmed?.Invoke(levelOptionItem);
			}
		}
	}

	private void ShowCardPacketSelector(LineEdit edit, Action<string> changed = null)
	{
		if (!GodotObject.IsInstanceValid(edit))
		{
			return;
		}
		EnsureCardPacketSelectorWindow();
		if (!GodotObject.IsInstanceValid(_cardPacketSelectorWindow))
		{
			return;
		}
		_cardPacketSelectorConfirmed = delegate(LevelOptionItem item)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Name) && GodotObject.IsInstanceValid(edit))
			{
				_updatingControls = true;
				try
				{
					edit.Text = item.Name;
				}
				finally
				{
					_updatingControls = false;
				}
				changed?.Invoke(item.Name);
				RegisterProjectSelectionPath(item.SourcePath);
				AddLevelEditorOutput("选择 卡牌: " + item.Name);
			}
		};
		_cardPacketSelectorOptions.Clear();
		_cardPacketSelectorOptions.AddRange(BuildCardKeySelectionOptions());
		_selectedCardPacketSelectorItem = null;
		_cardPacketSelectorWindow.Title = "选择卡牌";
		_cardPacketSelectorFilter.Text = "";
		FilterCardPacketSelector("");
		_cardPacketSelectorWindow.PopupCenteredClamped(new Vector2I(860, 620), 0.9f);
	}

	private void EnsureCardPacketSelectorWindow()
	{
		if (GodotObject.IsInstanceValid(_cardPacketSelectorWindow))
		{
			return;
		}
		if (_cardPacketSelectorScene == null)
		{
			_cardPacketSelectorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelCardPacketSelectorWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_cardPacketSelectorWindow = _cardPacketSelectorScene?.Instantiate<Window>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(_cardPacketSelectorWindow))
		{
			_cardPacketSelectorWindow.CloseRequested += delegate
			{
				_cardPacketSelectorWindow.Hide();
			};
			AddChild(_cardPacketSelectorWindow, forceReadableName: false, InternalMode.Disabled);
			_cardPacketSelectorFilter = _cardPacketSelectorWindow.GetNode<LineEdit>("Layout/Filter");
			_cardPacketSelectorFilter.TextChanged += FilterCardPacketSelector;
			_cardPacketSelectorGrid = _cardPacketSelectorWindow.GetNode<GridContainer>("Layout/PacketScroll/PacketGrid");
			_cardPacketSelectorDescription = _cardPacketSelectorWindow.GetNode<Label>("Layout/Description");
			_cardPacketSelectorWindow.GetNode<Button>("Layout/Controls/CancelButton").Pressed += delegate
			{
				_cardPacketSelectorWindow.Hide();
			};
			_cardPacketSelectorConfirmButton = _cardPacketSelectorWindow.GetNode<Button>("Layout/Controls/SelectButton");
			_cardPacketSelectorConfirmButton.Pressed += ConfirmCardPacketSelection;
		}
	}

	private void FilterCardPacketSelector(string query)
	{
		if (!GodotObject.IsInstanceValid(_cardPacketSelectorGrid))
		{
			return;
		}
		foreach (Node child in _cardPacketSelectorGrid.GetChildren())
		{
			child.QueueFree();
		}
		string query2 = (query ?? "").StripEdges().ToLowerInvariant();
		_filteredCardPacketSelectorOptions.Clear();
		_selectedCardPacketSelectorItem = null;
		foreach (LevelOptionItem cardPacketSelectorOption in _cardPacketSelectorOptions)
		{
			if (cardPacketSelectorOption != null && !string.IsNullOrWhiteSpace(cardPacketSelectorOption.Name) && MatchesLevelOption(cardPacketSelectorOption, query2))
			{
				_filteredCardPacketSelectorOptions.Add(cardPacketSelectorOption);
				_cardPacketSelectorGrid.AddChild(CreateCardPacketSelectorItem(cardPacketSelectorOption), forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (_filteredCardPacketSelectorOptions.Count > 0)
		{
			SelectCardPacketSelectorItem(_filteredCardPacketSelectorOptions[0]);
			return;
		}
		_cardPacketSelectorGrid.AddChild(new Label
		{
			Text = "没有可用卡牌",
			CustomMinimumSize = new Vector2(240f, 60f)
		}, forceReadableName: false, InternalMode.Disabled);
		UpdateCardPacketSelectorDescription(null);
	}

	private Control CreateCardPacketSelectorItem(LevelOptionItem option)
	{
		if (_compactPacketSelectorRowScene == null)
		{
			_compactPacketSelectorRowScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketSelectorRow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		Control control = _compactPacketSelectorRowScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(control))
		{
			throw new InvalidOperationException("Cannot instantiate selector row: res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketSelectorRow.tscn");
		}
		control.Name = "CardPacketSelectorItem";
		Button node = control.GetNode<Button>("%SelectSurface");
		node.ToggleMode = false;
		node.Pressed += delegate
		{
			ConfirmCardPacketSelection(option);
		};
		node.MouseEntered += delegate
		{
			SelectCardPacketSelectorItem(option);
		};
		control.GetNode<Label>("%PacketName").Text = option.Name;
		control.GetNode<Label>("%PacketCost").Text = option.SourceLabel;
		Label node2 = control.GetNode<Label>("%LoadingBadge");
		node2.Text = "卡";
		node2.Visible = true;
		Control node3 = control.GetNode<Control>("%PreviewHost");
		TowerDefensePacketConfig towerDefensePacketConfig = LoadPacketConfigForSelector(option);
		if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
			try
			{
				towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
				{
					towerDefenseInGamePacketShow.Name = "CardPacketSelectorPacketShow";
					towerDefenseInGamePacketShow.Position = new Vector2(16f, 18f);
					towerDefenseInGamePacketShow.Scale = new Vector2(0.23f, 0.23f);
					towerDefenseInGamePacketShow.MouseFilter = MouseFilterEnum.Ignore;
					node3.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
					ConfigureCardPacketSelectorPacketShow(towerDefenseInGamePacketShow, towerDefensePacketConfig);
					node2.Visible = false;
				}
			}
			catch (Exception ex)
			{
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
				{
					towerDefenseInGamePacketShow.QueueFree();
				}
				GD.PushWarning("Card selector PacketShow failed: " + option.Name + " " + ex.Message);
			}
		}
		return control;
	}

	private void SelectCardPacketSelectorItem(LevelOptionItem option)
	{
		_selectedCardPacketSelectorItem = option;
		UpdateCardPacketSelectorDescription(option);
	}

	private void UpdateCardPacketSelectorDescription(LevelOptionItem option)
	{
		if (GodotObject.IsInstanceValid(_cardPacketSelectorDescription))
		{
			_cardPacketSelectorDescription.Text = ((option != null) ? option.DetailText : "没有选择卡牌");
			if (GodotObject.IsInstanceValid(_cardPacketSelectorConfirmButton))
			{
				_cardPacketSelectorConfirmButton.Disabled = option == null;
			}
		}
	}

	private void ConfirmCardPacketSelection()
	{
		ConfirmCardPacketSelection(_selectedCardPacketSelectorItem);
	}

	private void ConfirmCardPacketSelection(LevelOptionItem option)
	{
		if (option != null)
		{
			_cardPacketSelectorWindow.Hide();
			_cardPacketSelectorConfirmed?.Invoke(option);
		}
	}

	private static TowerDefensePacketConfig LoadPacketConfigForSelector(LevelOptionItem option)
	{
		if (option == null || string.IsNullOrWhiteSpace(option.Name))
		{
			return null;
		}
		if (!string.IsNullOrWhiteSpace(option.SourcePath))
		{
			string text = Path.GetExtension(option.SourcePath).ToLowerInvariant();
			if ((text == ".tres" || text == ".res") ? true : false)
			{
				try
				{
					TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(option.SourcePath, null, ResourceLoader.CacheMode.Reuse);
					if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
					{
						return towerDefensePacketConfig;
					}
				}
				catch (Exception ex)
				{
					GD.PushWarning("Card selector resource load failed: " + option.SourcePath + " " + ex.Message);
				}
			}
		}
		try
		{
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(option.Name);
			if (GodotObject.IsInstanceValid(packetConfigReadOnly))
			{
				return packetConfigReadOnly;
			}
		}
		catch
		{
		}
		try
		{
			TowerDefensePacketConfig packetConfigReadOnlyByCharacterName = TowerDefenseManager.GetPacketConfigReadOnlyByCharacterName(option.Name);
			if (GodotObject.IsInstanceValid(packetConfigReadOnlyByCharacterName))
			{
				return packetConfigReadOnlyByCharacterName;
			}
		}
		catch
		{
		}
		return null;
	}

	private static void ConfigureCardPacketSelectorPacketShow(TowerDefenseInGamePacketShow packetShow, TowerDefensePacketConfig packet)
	{
		if (GodotObject.IsInstanceValid(packetShow) && GodotObject.IsInstanceValid(packet))
		{
			packetShow.onlyDraw = true;
			packetShow.setPcLayout = true;
			packetShow.showLove = false;
			packetShow.select = false;
			packetShow.alive = true;
			packetShow.@lock = false;
			packetShow.MouseFilter = MouseFilterEnum.Ignore;
			packetShow.Init(packet);
			packetShow.onlyDraw = true;
			packetShow.RefreshPreview();
		}
	}

	private List<LevelOptionItem> BuildFeatureSelectionOptions()
	{
		SyncProjectFeatures();
		TowerDefenseBattleRegistry.Init();
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		foreach (StringName key in TowerDefenseBattleRegistry.BattleFeatureDictionary.Keys)
		{
			AddLevelOption(options, key.ToString(), "内置", "", "内置战斗 Feature");
		}
		CollectProjectBattleOptions(options, "Battle/Features", "工程 Feature");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildProcessSelectionOptions()
	{
		TowerDefenseBattleRegistry.Init();
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		foreach (StringName key in TowerDefenseBattleRegistry.BattleProcessDictionary.Keys)
		{
			AddLevelOption(options, key.ToString(), "内置", "", "内置关卡流程 Process");
		}
		CollectProjectBattleOptions(options, "Battle/Processes", "工程 Process");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildEventSelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		foreach (TowerDefenseLevelEventDefinition definition in TowerDefenseLevelEventRegistry.Definitions)
		{
			AddLevelOption(options, definition.Id, "内置事件表", "", definition.EventType.Name);
		}
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildSelectableKeyOptions(string selectorKind)
	{
		return (selectorKind ?? "").ToLowerInvariant() switch
		{
			"map" => BuildMapKeySelectionOptions(), 
			"bgm" => BuildBgmKeySelectionOptions(), 
			"packetbank" => BuildPacketBankKeySelectionOptions(), 
			"level" => BuildLevelKeySelectionOptions(), 
			"card" => BuildCardKeySelectionOptions(), 
			"character" => BuildCharacterKeySelectionOptions(), 
			"talk" => BuildTalkKeySelectionOptions(), 
			"tutorial" => BuildTutorialKeySelectionOptions(), 
			_ => new List<LevelOptionItem>(), 
		};
	}

	private List<LevelOptionItem> BuildMapKeySelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/Map/MapResource.json", "游戏内地图", "内置 Map");
		CollectProjectResourceKeyOptions(options, "Resources/Maps", "Mod 地图");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildBgmKeySelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/BGM/BGMResource.json", "游戏内 BGM", "内置 BGM");
		CollectProjectResourceKeyOptions(options, "Resources/BGMConfigs", "Mod BGM");
		CollectProjectResourceKeyOptions(options, "Assets/Audio/BGM", "Mod BGM 音频");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildPacketBankKeySelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/PacketBank/PacketBankResource.json", "游戏内卡牌库", "内置 PacketBank");
		CollectProjectResourceKeyOptions(options, "Resources/PacketBank", "Mod 卡牌库");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildLevelKeySelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/Level/LevelResource.json", "游戏内关卡", "内置 Level");
		CollectProjectResourceKeyOptions(options, "Resources/Levels", "Mod 关卡");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildCardKeySelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/Character/CharacterResource.json", "游戏内卡片/角色", "内置 Character Packet");
		CollectProjectResourceKeyOptions(options, "Resources/Cards", "Mod 卡片");
		CollectProjectResourceKeyOptions(options, "Resources/Characters", "Mod 角色");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildCharacterKeySelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/Character/CharacterResource.json", "游戏内角色", "内置 Character");
		CollectProjectResourceKeyOptions(options, "Resources/Characters", "Mod 角色");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildTalkKeySelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/Npc/TalkResource.json", "游戏内对话", "内置 NpcTalk");
		CollectProjectResourceKeyOptions(options, "Resources/NpcTalks", "Mod 对话");
		return SortLevelOptions(options);
	}

	private List<LevelOptionItem> BuildTutorialKeySelectionOptions()
	{
		List<LevelOptionItem> options = new List<LevelOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/Tutorial/TutorialResource.json", "游戏内教程", "内置 Tutorial");
		CollectProjectResourceKeyOptions(options, "Resources/Tutorials", "Mod 教程");
		return SortLevelOptions(options);
	}

	private static void LoadRegisteredKeysFromJson(List<LevelOptionItem> options, string registryPath, string sourceLabel, string description)
	{
		if (options == null || string.IsNullOrWhiteSpace(registryPath) || !ResourceLoader.Exists(registryPath))
		{
			return;
		}
		try
		{
			Json json = ResourceLoader.Load<Json>(registryPath, null, ResourceLoader.CacheMode.Reuse);
			if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
			{
				return;
			}
			foreach (Variant key in json.Data.AsGodotDictionary().Keys)
			{
				AddLevelOption(options, key.AsString(), sourceLabel, registryPath, description);
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("Level editor key registry load failed: " + registryPath + " " + ex.Message);
		}
	}

	private void CollectProjectBattleOptions(List<LevelOptionItem> options, string relativeFolder, string sourceLabel)
	{
		string currentProjectPathForLevelEditor = GetCurrentProjectPathForLevelEditor();
		if (string.IsNullOrWhiteSpace(currentProjectPathForLevelEditor))
		{
			return;
		}
		string path = Path.Combine(currentProjectPathForLevelEditor, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
		if (!Directory.Exists(path))
		{
			return;
		}
		foreach (string item in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
		{
			if (IsBattleOptionFile(item))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
				AddLevelOption(options, fileNameWithoutExtension, sourceLabel, item, ToProjectRelativeDisplayPath(item, currentProjectPathForLevelEditor));
			}
		}
	}

	private void CollectProjectResourceKeyOptions(List<LevelOptionItem> options, string relativeFolder, string sourceLabel)
	{
		string currentProjectPathForLevelEditor = GetCurrentProjectPathForLevelEditor();
		if (string.IsNullOrWhiteSpace(currentProjectPathForLevelEditor) || string.IsNullOrWhiteSpace(relativeFolder))
		{
			return;
		}
		string path = Path.Combine(currentProjectPathForLevelEditor, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
		if (!Directory.Exists(path))
		{
			return;
		}
		foreach (string item in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
		{
			switch (Path.GetExtension(item).ToLowerInvariant())
			{
			case ".tres":
			case ".res":
			case ".tscn":
			case ".json":
			case ".wav":
			case ".ogg":
			case ".mp3":
			case ".flac":
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
				AddLevelOption(options, fileNameWithoutExtension, sourceLabel, item, ToProjectRelativeDisplayPath(item, currentProjectPathForLevelEditor));
				break;
			}
			}
		}
	}

	public static bool IsBattleOptionFile(string filePath)
	{
		string text = Path.GetExtension(filePath).ToLowerInvariant();
		bool flag = XWCSharpOnlyPolicy.IsSupportedCSharpScriptPath(filePath);
		if (!flag)
		{
			bool flag2 = ((text == ".tres" || text == ".res") ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private static void AddLevelOption(List<LevelOptionItem> options, string name, string sourceLabel, string sourcePath, string description)
	{
		if (options == null || string.IsNullOrWhiteSpace(name))
		{
			return;
		}
		foreach (LevelOptionItem option in options)
		{
			if (string.Equals(option.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrWhiteSpace(sourcePath))
				{
					option.SourcePath = sourcePath;
				}
				if (!string.IsNullOrWhiteSpace(sourceLabel) && !option.SourceLabel.Contains(sourceLabel, StringComparison.OrdinalIgnoreCase))
				{
					option.SourceLabel = (string.IsNullOrWhiteSpace(option.SourceLabel) ? sourceLabel : (option.SourceLabel + "+" + sourceLabel));
				}
				if (!string.IsNullOrWhiteSpace(description) && !option.Description.Contains(description, StringComparison.OrdinalIgnoreCase))
				{
					option.Description = (string.IsNullOrWhiteSpace(option.Description) ? description : (option.Description + "; " + description));
				}
				return;
			}
		}
		options.Add(new LevelOptionItem
		{
			Name = name,
			DisplayName = name,
			SourceLabel = (sourceLabel ?? ""),
			SourcePath = (sourcePath ?? ""),
			Description = (description ?? "")
		});
	}

	private static List<LevelOptionItem> SortLevelOptions(List<LevelOptionItem> options)
	{
		options.Sort((LevelOptionItem a, LevelOptionItem b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase));
		return options;
	}

	private static void SyncProjectFeatures()
	{
		string currentProjectPathForLevelEditor = GetCurrentProjectPathForLevelEditor();
		if (!string.IsNullOrWhiteSpace(currentProjectPathForLevelEditor))
		{
			string path = Path.Combine(currentProjectPathForLevelEditor, "Battle", "Features");
			Directory.CreateDirectory(path);
			XWModManifestSyncService.RegisterPath(currentProjectPathForLevelEditor, path);
		}
	}

	private static void RegisterProjectSelectionPath(string sourcePath)
	{
		string currentProjectPathForLevelEditor = GetCurrentProjectPathForLevelEditor();
		if (!string.IsNullOrWhiteSpace(currentProjectPathForLevelEditor) && !string.IsNullOrWhiteSpace(sourcePath))
		{
			XWModManifestSyncService.RegisterPath(currentProjectPathForLevelEditor, sourcePath);
		}
	}

	private static string GetCurrentProjectPathForLevelEditor()
	{
		string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
		{
			return modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
		}
		return "";
	}

	private static string ToProjectRelativeDisplayPath(string filePath, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(projectPath))
		{
			return filePath ?? "";
		}
		try
		{
			return Path.GetRelativePath(projectPath, filePath).Replace('\\', '/');
		}
		catch
		{
			return filePath;
		}
	}

	private static void AddLevelEditorOutput(string message, XWOutputPanel.MessageType type = XWOutputPanel.MessageType.Editor)
	{
		XWEditorInterface.Instance?.AddOutputMessage("关卡编辑器: " + message, type);
	}

	private void SetLevelDiscreteProperty(string propertyName, Variant value, LevelRefreshDomain domains = LevelRefreshDomain.All)
	{
		if (_updatingControls || !GodotObject.IsInstanceValid(_editingLevel) || string.IsNullOrWhiteSpace(propertyName))
		{
			return;
		}
		Variant variant = _editingLevel.Get(propertyName);
		if (!variant.Equals(value))
		{
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				ApplyLevelDiscretePropertyFromHistory(propertyName, value, (int)domains);
				return;
			}
			xWUndoRedoManager.CreateAction("修改关卡 " + propertyName);
			xWUndoRedoManager.AddDoMethod(this, "ApplyLevelDiscretePropertyFromHistory", propertyName, value, (int)domains);
			xWUndoRedoManager.AddUndoMethod(this, "ApplyLevelDiscretePropertyFromHistory", propertyName, variant, (int)domains);
			xWUndoRedoManager.CommitAction();
		}
	}

	public void ApplyLevelDiscretePropertyFromHistory(string propertyName, Variant value, int refreshDomains)
	{
		if (!GodotObject.IsInstanceValid(_editingLevel) || string.IsNullOrWhiteSpace(propertyName))
		{
			return;
		}
		_editingLevel.Set(propertyName, value);
		if (_editingLevel is TowerDefenseLevelConfig level)
		{
			switch (propertyName)
			{
			case "map":
			case "backgroundMusic":
			case "packetBank":
				UpdateLegacyFeatureMirrors(level, propertyName, value);
				break;
			}
		}
		_editingLevel.EmitChanged();
		_editingLevel.NotifyPropertyListChanged();
		_levelAtomicCommitCount++;
		if (GodotObject.IsInstanceValid(_levelSaveTimer))
		{
			_levelSaveTimer.Start();
		}
		RefreshDomains((LevelRefreshDomain)refreshDomains);
		NotifyCurrentResourceEdited();
	}

	public void RefreshLevelDiscreteSurfaceFromHistory()
	{
		RefreshDomains(LevelRefreshDomain.All);
	}

	private void SetLevelProperty(string propertyName, Variant value, bool refresh = true)
	{
		if (_editingLevel == null || string.IsNullOrWhiteSpace(propertyName))
		{
			return;
		}
		_editingLevel.Set(propertyName, value);
		if (_editingLevel is TowerDefenseLevelConfig level)
		{
			switch (propertyName)
			{
			case "map":
			case "backgroundMusic":
			case "packetBank":
				UpdateLegacyFeatureMirrors(level, propertyName, value);
				break;
			}
		}
		_editingLevel.EmitChanged();
		_editingLevel.NotifyPropertyListChanged();
		if (GodotObject.IsInstanceValid(_levelSaveTimer))
		{
			_levelSaveTimer.Start();
		}
		if (propertyName == "map")
		{
			RefreshPreSpawnMapSync();
		}
		if (refresh)
		{
			RefreshAll();
		}
	}

	private void SavePendingLevelResource()
	{
		if (GodotObject.IsInstanceValid(_editingLevel))
		{
			SaveCurrentEditedResource(_editingLevel);
		}
	}

	private void UpdateLegacyFeatureMirrors(TowerDefenseLevelConfig level, string propertyName, Variant value)
	{
		if (level.featureData == null)
		{
			level.featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();
		}
		switch (propertyName)
		{
		case "map":
			EnsureFeature(level, "Map")["MapName"] = value.AsString();
			break;
		case "backgroundMusic":
			EnsureFeature(level, "BGM")["BackgroundMusic"] = value.AsString();
			break;
		case "packetBank":
			EnsureFeature(level, "PacketBank")["PacketBankName"] = value.AsString();
			break;
		}
	}

	private static Dictionary EnsureFeature(TowerDefenseLevelConfig level, string name)
	{
		StringName key = new StringName(name);
		if (!level.featureData.ContainsKey(key))
		{
			level.featureData[key] = new Dictionary();
		}
		return level.featureData[key];
	}

	private void RefreshAll()
	{
		RefreshDomains(LevelRefreshDomain.All);
	}

	private void RefreshDomains(LevelRefreshDomain domains)
	{
		if (_editingLevel == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			RefreshAtomicTextControlsFromResource();
			if ((domains & LevelRefreshDomain.Summary) != LevelRefreshDomain.None && GodotObject.IsInstanceValid(_summaryLabel))
			{
				_summaryLabel.Text = BuildSummary(_editingLevel);
			}
			if ((domains & LevelRefreshDomain.Feature) != LevelRefreshDomain.None)
			{
				RefreshFeatureLists();
			}
			if ((domains & LevelRefreshDomain.Process) != LevelRefreshDomain.None)
			{
				RefreshProcessPanel();
			}
			if ((domains & LevelRefreshDomain.Events) != LevelRefreshDomain.None)
			{
				RefreshEventPanel();
			}
			if ((domains & LevelRefreshDomain.PreSpawn) != LevelRefreshDomain.None)
			{
				RefreshPreSpawnMapSync();
			}
			if ((domains & LevelRefreshDomain.Wave) != LevelRefreshDomain.None)
			{
				RefreshWaveRuntimePreview();
			}
			else if ((domains & LevelRefreshDomain.Hud) != LevelRefreshDomain.None)
			{
				RefreshWaveRuntimeHud();
			}
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RefreshAtomicTextControlsFromResource()
	{
		if (!GodotObject.IsInstanceValid(_editingLevel))
		{
			return;
		}
		foreach (KeyValuePair<string, List<LineEdit>> levelAtomicTextControl in _levelAtomicTextControls)
		{
			levelAtomicTextControl.Deconstruct(out var key, out var value);
			string propertyName = key;
			List<LineEdit> list = value;
			string text = ReadVariant(_editingLevel, propertyName).AsString();
			foreach (LineEdit item in list)
			{
				if (GodotObject.IsInstanceValid(item) && !item.HasFocus())
				{
					item.Text = text;
				}
			}
		}
	}

	private void RefreshFeatureLists()
	{
		if (_editingLevel == null || !GodotObject.IsInstanceValid(_featureList))
		{
			return;
		}
		Godot.Collections.Dictionary<StringName, Dictionary> featureData = GetFeatureData(_editingLevel);
		_featureList.Clear();
		int num = 0;
		int num2 = -1;
		foreach (StringName key in featureData.Keys)
		{
			Dictionary dictionary = featureData[key];
			_featureList.AddItem($"{key} ({dictionary?.Count ?? 0})");
			if (key.ToString() == _selectedFeatureName)
			{
				num2 = num;
			}
			num++;
		}
		if (featureData.Count == 0)
		{
			_featureList.AddItem("未配置 Feature");
		}
		else if (num2 >= 0)
		{
			_featureList.Select(num2);
		}
		else if (string.IsNullOrWhiteSpace(_selectedFeatureName))
		{
			SelectFeature(0);
		}
		if (GodotObject.IsInstanceValid(_featureSummaryLabel))
		{
			_featureSummaryLabel.Text = $"已启用 Feature: {featureData.Count}";
		}
		RefreshFeatureDataList();
	}

	private void RefreshFeatureDataList()
	{
		if (!GodotObject.IsInstanceValid(_featureDataList))
		{
			return;
		}
		_featureDataList.Clear();
		Dictionary selectedFeatureDictionary = GetSelectedFeatureDictionary();
		if (selectedFeatureDictionary == null || selectedFeatureDictionary.Count == 0)
		{
			_featureDataList.AddItem("未配置 Data");
			return;
		}
		foreach (Variant key in selectedFeatureDictionary.Keys)
		{
			_featureDataList.AddItem(key.AsString() + ": " + FormatVariant(selectedFeatureDictionary[key]));
		}
	}

	private void RefreshProcessPanel()
	{
		if (_editingLevel != null)
		{
			if (GodotObject.IsInstanceValid(_processNameEdit))
			{
				_processNameEdit.Text = ReadString(_editingLevel, "processName");
			}
			if (GodotObject.IsInstanceValid(_processSummaryLabel))
			{
				_processSummaryLabel.Text = "当前流程: " + EmptyToPlaceholder(ReadString(_editingLevel, "processName"));
			}
			RefreshProcessDataList();
		}
	}

	private void RefreshProcessDataList()
	{
		if (!GodotObject.IsInstanceValid(_processDataList))
		{
			return;
		}
		Dictionary processData = GetProcessData(_editingLevel);
		_processDataList.Clear();
		if (processData == null || processData.Count == 0)
		{
			_processDataList.AddItem("未配置 Process Data");
			return;
		}
		foreach (Variant key in processData.Keys)
		{
			_processDataList.AddItem(key.AsString() + ": " + FormatVariant(processData[key]));
		}
	}

	private void RefreshEventPanel()
	{
		if (!(_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig))
		{
			PopulateVariantArray(_eventInitList, null);
			PopulateVariantArray(_eventReadyList, null);
			PopulateVariantArray(_eventStartList, null);
			PopulateVariantArray(_preSpawnList, null);
			PopulateVariantArray(_packetBankList, null);
		}
		else
		{
			PopulateObjectArray(_eventInitList, towerDefenseLevelConfig.eventInit);
			PopulateObjectArray(_eventReadyList, towerDefenseLevelConfig.eventReady);
			PopulateObjectArray(_eventStartList, towerDefenseLevelConfig.eventStart);
			PopulatePreSpawnArray(_preSpawnList, towerDefenseLevelConfig.preSpawnList);
			PopulateVariantArray(_packetBankList, towerDefenseLevelConfig.packetBankList);
			UpdatePreSpawnSelectionLabel();
			QueuePreSpawnGridRedraw();
		}
	}

	private void SaveCurrentEditedResource(Resource resource)
	{
		string currentResourcePath = base.CurrentResourcePath;
		if (string.IsNullOrWhiteSpace(currentResourcePath) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		if (resource is TowerDefenseLevelConfig { HasPendingEditorFeatureChanges: not false } towerDefenseLevelConfig)
		{
			towerDefenseLevelConfig.PrepareForEditorSave();
		}
		Error error = ResourceSaver.Save(resource, currentResourcePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			GD.PushWarning($"Level editor save failed: {error} {currentResourcePath}");
		}
		else
		{
			string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				XWModManifestSyncService.RegisterPath(text, currentResourcePath);
			}
		}
	}

	private Godot.Collections.Dictionary<StringName, Dictionary> GetFeatureData(Resource level)
	{
		if (level is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			TowerDefenseLevelConfig towerDefenseLevelConfig2 = towerDefenseLevelConfig;
			if (towerDefenseLevelConfig2.featureData == null)
			{
				towerDefenseLevelConfig2.featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();
			}
			return towerDefenseLevelConfig.featureData;
		}
		if (level is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig)
		{
			TowerDefenseLevelNewConfig towerDefenseLevelNewConfig2 = towerDefenseLevelNewConfig;
			if (towerDefenseLevelNewConfig2.featureData == null)
			{
				towerDefenseLevelNewConfig2.featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();
			}
			return towerDefenseLevelNewConfig.featureData;
		}
		return new Godot.Collections.Dictionary<StringName, Dictionary>();
	}

	private Dictionary GetProcessData(Resource level)
	{
		if (level is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			TowerDefenseLevelConfig towerDefenseLevelConfig2 = towerDefenseLevelConfig;
			if (towerDefenseLevelConfig2.processData == null)
			{
				towerDefenseLevelConfig2.processData = new Dictionary();
			}
			return towerDefenseLevelConfig.processData;
		}
		if (level is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig)
		{
			TowerDefenseLevelNewConfig towerDefenseLevelNewConfig2 = towerDefenseLevelNewConfig;
			if (towerDefenseLevelNewConfig2.processData == null)
			{
				towerDefenseLevelNewConfig2.processData = new Dictionary();
			}
			return towerDefenseLevelNewConfig.processData;
		}
		return new Dictionary();
	}

	private Dictionary GetSelectedFeatureDictionary()
	{
		if (_editingLevel == null || string.IsNullOrWhiteSpace(_selectedFeatureName))
		{
			return null;
		}
		Godot.Collections.Dictionary<StringName, Dictionary> featureData = GetFeatureData(_editingLevel);
		StringName key = new StringName(_selectedFeatureName);
		if (!featureData.ContainsKey(key))
		{
			return null;
		}
		return featureData[key];
	}

	private string GetFeatureNameAt(int index)
	{
		if (_editingLevel == null || index < 0)
		{
			return "";
		}
		int num = 0;
		foreach (StringName key in GetFeatureData(_editingLevel).Keys)
		{
			if (num++ == index)
			{
				return key.ToString();
			}
		}
		return "";
	}

	private int GetFeatureIndex(string featureName)
	{
		if (_editingLevel == null || string.IsNullOrWhiteSpace(featureName))
		{
			return -1;
		}
		int num = 0;
		foreach (StringName key in GetFeatureData(_editingLevel).Keys)
		{
			if (key.ToString() == featureName)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	private void AddSummaryRows(Resource level)
	{
		AddItemIfMissing(base.PreviewList, BuildSummary(level));
		AddItemIfMissing(base.PreviewList, "map -> " + ReadString(level, "map"));
		AddItemIfMissing(base.PreviewList, "backgroundMusic -> " + ReadString(level, "backgroundMusic"));
		AddItemIfMissing(base.TimelineList, "processName -> " + ReadString(level, "processName"));
		AddItemIfMissing(base.TimelineList, $"Feature 数量 -> {GetFeatureData(level).Count}");
		AddItemIfMissing(base.GraphList, "关卡 -> 基础信息 -> Feature -> Process -> 事件/波次");
		AddItemIfMissing(base.GraphList, "Feature -> " + string.Join(", ", GetFeatureData(level).Keys));
		AddItemIfMissing(base.GraphList, "Process -> " + ReadString(level, "processName"));
		AddItemIfMissing(base.ReferenceList, "关卡资源 -> " + base.CurrentResourcePath);
		AddItemIfMissing(base.ReferenceList, "map -> " + ReadString(level, "map"));
		AddItemIfMissing(base.ReferenceList, "packetBankList -> " + ReadCount(level, "packetBankList"));
	}

	private void RefreshPreSpawnMapSync()
	{
		_preSpawnMapConfig = ResolveLevelMapConfig(_editingLevel);
		if (GodotObject.IsInstanceValid(_preSpawnMapLabel))
		{
			_preSpawnMapLabel.Text = BuildPreSpawnMapLabel(_editingLevel, _preSpawnMapConfig);
		}
		RebuildPreSpawnMapPreview();
		ResetPreSpawnMapPreviewView();
		QueuePreSpawnGridRedraw();
	}

	private void RebuildPreSpawnMapPreview()
	{
		if (!GodotObject.IsInstanceValid(_preSpawnMapPreviewRoot))
		{
			return;
		}
		foreach (Node child in _preSpawnMapPreviewRoot.GetChildren())
		{
			child.QueueFree();
		}
		if (_preSpawnMapConfig == null)
		{
			_preSpawnMapConfig = ResolveLevelMapConfig(_editingLevel);
		}
		UpdatePreSpawnViewportSize();
		UpdatePreSpawnCanvasTransform();
		if (_preSpawnMapConfig == null)
		{
			_preSpawnMapPreviewRoot.AddChild(new Label
			{
				Text = "未找到地图配置"
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		UpdatePreSpawnCanvasTransform();
		Node node = null;
		PackedScene mapScene = _preSpawnMapConfig.GetMapScene(cache: false);
		if (GodotObject.IsInstanceValid(mapScene))
		{
			try
			{
				node = mapScene.Instantiate(PackedScene.GenEditState.Disabled);
			}
			catch (Exception ex)
			{
				GD.PushWarning("Level editor PreSpawn map preview failed: " + ex.Message);
			}
		}
		if (node != null)
		{
			node.Name = "PreSpawnConfiguredMapScenePreview";
			_preSpawnMapPreviewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		Texture2D mapTexture = _preSpawnMapConfig.GetMapTexture(cache: false);
		if (GodotObject.IsInstanceValid(mapTexture))
		{
			_preSpawnMapPreviewRoot.AddChild(new Sprite2D
			{
				Name = "PreSpawnMapTexturePreview",
				Texture = mapTexture,
				Centered = false
			}, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			_preSpawnMapPreviewRoot.AddChild(new Label
			{
				Text = "地图没有 mapScene 或 mapTexture"
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void ResetPreSpawnMapPreviewView()
	{
		if (_preSpawnMapConfig == null)
		{
			_preSpawnMapConfig = ResolveLevelMapConfig(_editingLevel);
		}
		if (_preSpawnMapConfig != null)
		{
			Vector2 preSpawnPreviewViewSize = GetPreSpawnPreviewViewSize();
			Vector2 vector = new Vector2(Mathf.Max(1f, _preSpawnMapConfig.mapSize.X), Mathf.Max(1f, _preSpawnMapConfig.mapSize.Y));
			float value = Mathf.Min(preSpawnPreviewViewSize.X / vector.X, preSpawnPreviewViewSize.Y / vector.Y) * 0.96f;
			PreSpawnCanvasZoom = Mathf.Clamp(value, 0.05f, 12f);
			PreSpawnCanvasOffset = (preSpawnPreviewViewSize - vector * PreSpawnCanvasZoom) * 0.5f;
			UpdatePreSpawnCanvasTransform();
			QueuePreSpawnGridRedraw();
		}
	}

	private void SetPreSpawnCanvasZoom(float zoom, Vector2 pivot)
	{
		float num = Mathf.Clamp(zoom, 0.05f, 12f);
		if (!Mathf.IsEqualApprox(num, PreSpawnCanvasZoom))
		{
			Vector2 vector = PreSpawnViewToWorld(pivot);
			PreSpawnCanvasZoom = num;
			PreSpawnCanvasOffset = pivot - vector * PreSpawnCanvasZoom;
			UpdatePreSpawnCanvasTransform();
			QueuePreSpawnGridRedraw();
		}
	}

	private void UpdatePreSpawnCanvasTransform()
	{
		UpdatePreSpawnViewportSize();
		if (_preSpawnMapConfig != null && GodotObject.IsInstanceValid(_preSpawnMapPreviewRoot))
		{
			_preSpawnMapPreviewRoot.Position = PreSpawnWorldToView(_preSpawnMapConfig.mapOffset);
			_preSpawnMapPreviewRoot.Scale = Vector2.One * PreSpawnCanvasZoom;
		}
	}

	private Vector2 PreSpawnWorldToView(Vector2 world)
	{
		return world * PreSpawnCanvasZoom + PreSpawnCanvasOffset;
	}

	private Vector2 PreSpawnViewToWorld(Vector2 view)
	{
		float num = Mathf.Max(PreSpawnCanvasZoom, 0.001f);
		return (view - PreSpawnCanvasOffset) / num;
	}

	private void UpdatePreSpawnViewportSize()
	{
		if (GodotObject.IsInstanceValid(_preSpawnMapViewport))
		{
			Vector2 preSpawnPreviewViewSize = GetPreSpawnPreviewViewSize();
			_preSpawnMapViewport.Size = new Vector2I(Mathf.Max(1, Mathf.RoundToInt(preSpawnPreviewViewSize.X)), Mathf.Max(1, Mathf.RoundToInt(preSpawnPreviewViewSize.Y)));
		}
	}

	private Vector2 GetPreSpawnPreviewViewSize()
	{
		if (GodotObject.IsInstanceValid(_preSpawnGridOverlay) && _preSpawnGridOverlay.Size.X > 1f && _preSpawnGridOverlay.Size.Y > 1f)
		{
			return _preSpawnGridOverlay.Size;
		}
		if (GodotObject.IsInstanceValid(_preSpawnPreviewStack) && _preSpawnPreviewStack.Size.X > 1f && _preSpawnPreviewStack.Size.Y > 1f)
		{
			return _preSpawnPreviewStack.Size;
		}
		return new Vector2(700f, 280f);
	}

	private void DrawPreSpawnGridOverlay(Control overlay)
	{
		if (!GodotObject.IsInstanceValid(overlay))
		{
			return;
		}
		if (_preSpawnMapConfig == null)
		{
			_preSpawnMapConfig = ResolveLevelMapConfig(_editingLevel);
		}
		if (_preSpawnMapConfig == null)
		{
			return;
		}
		Vector2 begin = PreSpawnWorldToView(_preSpawnMapConfig.mapOffset + _preSpawnMapConfig.gridBeginPos);
		Vector2 cellSize = _preSpawnMapConfig.gridSize * PreSpawnCanvasZoom;
		int num = Math.Clamp(_preSpawnMapConfig.gridNum.X, 1, 50);
		int num2 = Math.Clamp(_preSpawnMapConfig.gridNum.Y, 1, 50);
		Color color = new Color(0.12f, 0.42f, 0.22f, 0.12f);
		Color color2 = new Color(0.56f, 0.92f, 0.64f, 0.62f);
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Rect2 preSpawnCellRect = GetPreSpawnCellRect(j, i, begin, cellSize);
				overlay.DrawRect(preSpawnCellRect, color);
				overlay.DrawRect(preSpawnCellRect, color2, filled: false, 1f);
			}
		}
		if (!(_editingLevel is TowerDefenseLevelConfig { preSpawnList: not null } towerDefenseLevelConfig))
		{
			return;
		}
		for (int k = 0; k < towerDefenseLevelConfig.preSpawnList.Count; k++)
		{
			TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = towerDefenseLevelConfig.preSpawnList[k];
			if (GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig))
			{
				Rect2 preSpawnCellRect2 = GetPreSpawnCellRect(towerDefenseLevelPreSpawnConfig.gridPos.X, towerDefenseLevelPreSpawnConfig.gridPos.Y, begin, cellSize);
				Color color3 = ((k == _selectedPreSpawnIndex) ? new Color(1f, 0.74f, 0.2f, 0.6f) : new Color(0.35f, 0.7f, 1f, 0.44f));
				overlay.DrawRect(preSpawnCellRect2, color3);
				overlay.DrawRect(preSpawnCellRect2, color3.Lightened(0.35f), filled: false, (k == _selectedPreSpawnIndex) ? 3f : 1.5f);
			}
		}
	}

	private void OnPreSpawnGridOverlayGuiInput(InputEvent inputEvent)
	{
		if (!(_editingLevel is TowerDefenseLevelConfig) || _preSpawnMapConfig == null || !GodotObject.IsInstanceValid(_preSpawnGridOverlay))
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton)
		{
			if (inputEventMouseButton.ButtonIndex == MouseButton.WheelUp && inputEventMouseButton.Pressed)
			{
				SetPreSpawnCanvasZoom(PreSpawnCanvasZoom * 1.1f, inputEventMouseButton.Position);
				_preSpawnGridOverlay.AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.WheelDown && inputEventMouseButton.Pressed)
			{
				SetPreSpawnCanvasZoom(PreSpawnCanvasZoom / 1.1f, inputEventMouseButton.Position);
				_preSpawnGridOverlay.AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Middle)
			{
				PreSpawnCanvasPan = inputEventMouseButton.Pressed;
				PreSpawnCanvasLastPointer = inputEventMouseButton.Position;
				_preSpawnGridOverlay.AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Right)
			{
				if (inputEventMouseButton.Pressed)
				{
					PreSpawnCanvasPan = true;
					_preSpawnRightClickArmed = true;
					_preSpawnCanvasMovedDuringPan = false;
					PreSpawnCanvasLastPointer = inputEventMouseButton.Position;
					_preSpawnGridOverlay.AcceptEvent();
					return;
				}
				PreSpawnCanvasPan = false;
				if (_preSpawnRightClickArmed && !_preSpawnCanvasMovedDuringPan)
				{
					Vector2I preSpawnCellAtPosition = GetPreSpawnCellAtPosition(inputEventMouseButton.Position);
					if (preSpawnCellAtPosition.X >= 0)
					{
						DeletePreSpawnAtCell(preSpawnCellAtPosition);
					}
				}
				_preSpawnRightClickArmed = false;
				_preSpawnGridOverlay.AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed)
			{
				Vector2I preSpawnCellAtPosition2 = GetPreSpawnCellAtPosition(inputEventMouseButton.Position);
				if (preSpawnCellAtPosition2.X >= 0)
				{
					AddOrMovePreSpawnAtCell(preSpawnCellAtPosition2);
					_preSpawnGridOverlay.AcceptEvent();
				}
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && PreSpawnCanvasPan)
		{
			PanPreSpawnCanvasTo(inputEventMouseMotion.Position);
		}
	}

	private void PanPreSpawnCanvasTo(Vector2 position)
	{
		Vector2 vector = position - PreSpawnCanvasLastPointer;
		PreSpawnCanvasOffset += position - PreSpawnCanvasLastPointer;
		if (vector.LengthSquared() > 4f)
		{
			_preSpawnCanvasMovedDuringPan = true;
		}
		PreSpawnCanvasLastPointer = position;
		UpdatePreSpawnCanvasTransform();
		QueuePreSpawnGridRedraw();
		_preSpawnGridOverlay.AcceptEvent();
	}

	private void AddOrMovePreSpawnAtCell(Vector2I cell)
	{
		if (!(_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig) || cell.X < 0 || cell.Y < 0)
		{
			return;
		}
		TowerDefenseLevelConfig towerDefenseLevelConfig2 = towerDefenseLevelConfig;
		if (towerDefenseLevelConfig2.preSpawnList == null)
		{
			towerDefenseLevelConfig2.preSpawnList = new Array<TowerDefenseLevelPreSpawnConfig>();
		}
		int num = FindPreSpawnAtCell(cell);
		if (_selectedPreSpawnIndex < 0 || _selectedPreSpawnIndex >= towerDefenseLevelConfig.preSpawnList.Count)
		{
			if (num >= 0)
			{
				SelectPreSpawn(num);
				return;
			}
			TowerDefenseLevelPreSpawnConfig item = new TowerDefenseLevelPreSpawnConfig
			{
				ResourceName = $"PreSpawn_{cell.X}_{cell.Y}",
				packetName = (GodotObject.IsInstanceValid(_preSpawnPacketNameEdit) ? _preSpawnPacketNameEdit.Text.StripEdges() : ""),
				gridPos = cell
			};
			towerDefenseLevelConfig.preSpawnList.Add(item);
			_selectedPreSpawnIndex = towerDefenseLevelConfig.preSpawnList.Count - 1;
		}
		else
		{
			TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = towerDefenseLevelConfig.preSpawnList[_selectedPreSpawnIndex];
			if (!GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig))
			{
				return;
			}
			towerDefenseLevelPreSpawnConfig.packetName = (GodotObject.IsInstanceValid(_preSpawnPacketNameEdit) ? _preSpawnPacketNameEdit.Text.StripEdges() : towerDefenseLevelPreSpawnConfig.packetName);
			towerDefenseLevelPreSpawnConfig.gridPos = cell;
			towerDefenseLevelPreSpawnConfig.ResourceName = (string.IsNullOrWhiteSpace(towerDefenseLevelPreSpawnConfig.ResourceName) ? $"PreSpawn_{cell.X}_{cell.Y}" : towerDefenseLevelPreSpawnConfig.ResourceName);
			towerDefenseLevelPreSpawnConfig.EmitChanged();
		}
		towerDefenseLevelConfig.MarkPreSpawnDataEditedFromEditor();
		SaveCurrentEditedResource(towerDefenseLevelConfig);
		RefreshEventPanel();
		if (GodotObject.IsInstanceValid(_preSpawnList) && _selectedPreSpawnIndex >= 0 && _selectedPreSpawnIndex < _preSpawnList.ItemCount)
		{
			_preSpawnList.Select(_selectedPreSpawnIndex);
		}
		AddLevelEditorOutput($"设置预生成: {cell}");
	}

	private void DeleteSelectedPreSpawn()
	{
		if (_editingLevel is TowerDefenseLevelConfig { preSpawnList: not null } towerDefenseLevelConfig && _selectedPreSpawnIndex >= 0 && _selectedPreSpawnIndex < towerDefenseLevelConfig.preSpawnList.Count)
		{
			towerDefenseLevelConfig.preSpawnList.RemoveAt(_selectedPreSpawnIndex);
			_selectedPreSpawnIndex = -1;
			towerDefenseLevelConfig.MarkPreSpawnDataEditedFromEditor();
			SaveCurrentEditedResource(towerDefenseLevelConfig);
			RefreshEventPanel();
			AddLevelEditorOutput("删除预生成");
		}
	}

	private void DeletePreSpawnAtCell(Vector2I cell)
	{
		if (_editingLevel is TowerDefenseLevelConfig { preSpawnList: not null } towerDefenseLevelConfig)
		{
			int num = FindPreSpawnAtCell(cell);
			if (num >= 0)
			{
				towerDefenseLevelConfig.preSpawnList.RemoveAt(num);
				_selectedPreSpawnIndex = -1;
				towerDefenseLevelConfig.MarkPreSpawnDataEditedFromEditor();
				SaveCurrentEditedResource(towerDefenseLevelConfig);
				RefreshEventPanel();
				AddLevelEditorOutput($"删除预生成: {cell}");
			}
		}
	}

	private int FindPreSpawnAtCell(Vector2I cell)
	{
		if (!(_editingLevel is TowerDefenseLevelConfig { preSpawnList: not null } towerDefenseLevelConfig))
		{
			return -1;
		}
		for (int i = 0; i < towerDefenseLevelConfig.preSpawnList.Count; i++)
		{
			TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = towerDefenseLevelConfig.preSpawnList[i];
			if (GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig) && towerDefenseLevelPreSpawnConfig.gridPos == cell)
			{
				return i;
			}
		}
		return -1;
	}

	private Vector2I GetPreSpawnCellAtPosition(Vector2 position)
	{
		if (_preSpawnMapConfig == null)
		{
			_preSpawnMapConfig = ResolveLevelMapConfig(_editingLevel);
		}
		if (_preSpawnMapConfig == null || !GodotObject.IsInstanceValid(_preSpawnGridOverlay))
		{
			return new Vector2I(-1, -1);
		}
		if (Mathf.IsZeroApprox(PreSpawnCanvasZoom) || Mathf.IsZeroApprox(_preSpawnMapConfig.gridSize.X) || Mathf.IsZeroApprox(_preSpawnMapConfig.gridSize.Y))
		{
			return new Vector2I(-1, -1);
		}
		Vector2 vector = PreSpawnViewToWorld(position) - _preSpawnMapConfig.mapOffset - _preSpawnMapConfig.gridBeginPos;
		int num = Mathf.FloorToInt(vector.X / _preSpawnMapConfig.gridSize.X);
		int num2 = Mathf.FloorToInt(vector.Y / _preSpawnMapConfig.gridSize.Y);
		int num3 = Math.Clamp(_preSpawnMapConfig.gridNum.X, 1, 50);
		int num4 = Math.Clamp(_preSpawnMapConfig.gridNum.Y, 1, 50);
		if (num < 0 || num2 < 0 || num >= num3 || num2 >= num4)
		{
			return new Vector2I(-1, -1);
		}
		return new Vector2I(num, num2);
	}

	private static Rect2 GetPreSpawnCellRect(int x, int y, Vector2 begin, Vector2 cellSize)
	{
		return new Rect2(begin + new Vector2((float)x * cellSize.X, (float)y * cellSize.Y), cellSize);
	}

	private static Vector2I GetPreSpawnViewportSize(TowerDefenseMapConfig map)
	{
		if (map == null)
		{
			return new Vector2I(1400, 600);
		}
		return new Vector2I(Mathf.Max(1, Mathf.RoundToInt(map.mapSize.X)), Mathf.Max(1, Mathf.RoundToInt(map.mapSize.Y)));
	}

	private static TowerDefenseMapConfig ResolveLevelMapConfig(Resource level)
	{
		string text = ReadString(level, "map").StripEdges();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "Frontlawn";
		}
		TowerDefenseMapConfig towerDefenseMapConfig = TryLoadMapConfigPath(text);
		if (GodotObject.IsInstanceValid(towerDefenseMapConfig))
		{
			return towerDefenseMapConfig;
		}
		try
		{
			if (ResourceManager.Instance?.MAPS != null && ResourceManager.Instance.MAPS.TryGetValue(text, out var value) && value is TowerDefenseMapConfig result)
			{
				return result;
			}
		}
		catch
		{
		}
		TowerDefenseMapConfig towerDefenseMapConfig2 = LoadMapConfigFromMapResource(text);
		if (GodotObject.IsInstanceValid(towerDefenseMapConfig2))
		{
			return towerDefenseMapConfig2;
		}
		return LoadDefaultFrontlawnMapConfig();
	}

	private static TowerDefenseMapConfig LoadMapConfigFromMapResource(string mapName)
	{
		if (string.IsNullOrWhiteSpace(mapName) || !ResourceLoader.Exists("res://Asset/Config/Map/MapResource.json"))
		{
			return null;
		}
		try
		{
			Json json = ResourceLoader.Load<Json>("res://Asset/Config/Map/MapResource.json", null, ResourceLoader.CacheMode.Reuse);
			if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
			{
				return null;
			}
			Dictionary dictionary = json.Data.AsGodotDictionary();
			if (dictionary == null || !dictionary.ContainsKey(mapName))
			{
				return null;
			}
			return TryLoadMapConfigPath(dictionary[mapName].AsString());
		}
		catch
		{
			return null;
		}
	}

	private static TowerDefenseMapConfig LoadDefaultFrontlawnMapConfig()
	{
		if (GodotObject.IsInstanceValid(_defaultFrontlawnMapConfig))
		{
			return _defaultFrontlawnMapConfig;
		}
		_defaultFrontlawnMapConfig = TryLoadMapConfigPath("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres");
		return _defaultFrontlawnMapConfig;
	}

	private static TowerDefenseMapConfig TryLoadMapConfigPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		try
		{
			if (ResourceLoader.Exists(path) || File.Exists(path))
			{
				return ResourceLoader.Load<TowerDefenseMapConfig>(path, null, ResourceLoader.CacheMode.Reuse);
			}
		}
		catch
		{
		}
		return null;
	}

	private static string BuildPreSpawnMapLabel(Resource level, TowerDefenseMapConfig map)
	{
		string value = ReadString(level, "map");
		return "map: " + EmptyToPlaceholder(value) + " / " + FormatResource(map);
	}

	private void UpdatePreSpawnSelectionLabel()
	{
		if (GodotObject.IsInstanceValid(_preSpawnSelectionLabel))
		{
			_preSpawnSelectionLabel.Text = BuildPreSpawnSelectionText();
		}
	}

	private string BuildPreSpawnSelectionText()
	{
		if (!(_editingLevel is TowerDefenseLevelConfig { preSpawnList: not null } towerDefenseLevelConfig) || towerDefenseLevelConfig.preSpawnList.Count == 0)
		{
			return "点击格子可添加预生成；右键格子删除。";
		}
		if (_selectedPreSpawnIndex >= 0 && _selectedPreSpawnIndex < towerDefenseLevelConfig.preSpawnList.Count)
		{
			TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = towerDefenseLevelConfig.preSpawnList[_selectedPreSpawnIndex];
			if (GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig))
			{
				return $"选中: {towerDefenseLevelPreSpawnConfig.packetName} @ ({towerDefenseLevelPreSpawnConfig.gridPos.X}, {towerDefenseLevelPreSpawnConfig.gridPos.Y})";
			}
			return $"预生成数量: {towerDefenseLevelConfig.preSpawnList.Count}";
		}
		return $"预生成数量: {towerDefenseLevelConfig.preSpawnList.Count}。选择列表项后点击格子可移动。";
	}

	private void QueuePreSpawnGridRedraw()
	{
		if (GodotObject.IsInstanceValid(_preSpawnGridOverlay))
		{
			_preSpawnGridOverlay.QueueRedraw();
		}
	}

	private void RenderLevelEditor(TowerDefenseLevelConfig level)
	{
		RenderLevelGameCanvas(level);
	}

	private void RenderLevelEditor(TowerDefenseLevelNewConfig level)
	{
		RenderLevelEditor((Resource)level);
	}

	private void RenderLevelGameCanvas(TowerDefenseLevelConfig level)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = 1;
			if (_levelGameCanvasScene == null)
			{
				_levelGameCanvasScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelGameCanvas.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_levelGameCanvas = _levelGameCanvasScene?.Instantiate<XWLevelGameCanvas>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_levelGameCanvas))
			{
				base.CanvasGrid.AddChild(_levelGameCanvas, forceReadableName: false, InternalMode.Disabled);
				_levelGameCanvas.Connect(XWLevelGameCanvas.SignalName.LevelEdited, new Callable(this, "OnLevelGameCanvasEdited"));
				_levelGameCanvas.EditLevel(level);
				base.CanvasGrid.AddChild(CreateWaveRuntimePreviewPanel(), forceReadableName: false, InternalMode.Disabled);
				AddSummaryRows(level);
			}
		}
	}

	private void OnLevelGameCanvasEdited()
	{
		NotifyCurrentResourceEdited();
	}

	private void RenderLevelEditor(Resource level)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = 1;
			if (_editorLayoutScene == null)
			{
				_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindLevelBasicInfo(vBoxContainer, level);
				vBoxContainer.GetNode<VBoxContainer>("%WavePreviewHost").AddChild(CreateWaveRuntimePreviewPanel(), forceReadableName: false, InternalMode.Disabled);
				BindFeatureProcessLayout(vBoxContainer, level);
				BindEventLayout(vBoxContainer, level);
				RefreshAll();
				AddSummaryRows(level);
			}
		}
	}

	private Control CreateWaveRuntimePreviewPanel()
	{
		if (_waveRuntimePreviewScene == null)
		{
			_waveRuntimePreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelWaveRuntimePreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(_waveRuntimePreviewScene))
		{
			return new Label
			{
				Text = "关卡波次预览场景加载失败"
			};
		}
		Control control = _waveRuntimePreviewScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		_waveRuntimeStatusLabel = control.GetNode<Label>("Layout/Status");
		_waveRuntimeBackground = control.GetNode<TextureRect>("%GameBackground");
		_waveRuntimeZombieRoot = control.GetNode<Node2D>("%ZombieRoot");
		_waveRuntimeSpawnControlRoot = control.GetNode<Control>("%SpawnControlRoot");
		_waveRuntimeEventOverlay = control.GetNode<VBoxContainer>("%EventOverlay");
		_waveRuntimeBanner = control.GetNode<Label>("%WaveBanner");
		_waveRuntimeSpawnSummary = control.GetNode<Label>("%SpawnSummary");
		_waveRuntimeProgress = control.GetNode<ProgressBar>("Layout/Progress");
		_waveRuntimeTimer = control.GetNode<Timer>("Layout/Timer");
		_waveRuntimeAutoButton = control.GetNode<Button>("Layout/Toolbar/AutoButton");
		_waveRuntimeLevelNameEdit = control.GetNode<LineEdit>("%LevelNameHudEdit");
		_waveRuntimeDescriptionEdit = control.GetNode<LineEdit>("%DescriptionHudEdit");
		_waveRuntimeSunLabel = control.GetNode<Label>("%SunHudLabel");
		_waveRuntimeWaveLabel = control.GetNode<Label>("%WaveHudLabel");
		_waveRuntimeMowerLabel = control.GetNode<Label>("%MowerHudLabel");
		_waveRuntimeFogLabel = control.GetNode<Label>("%FogHudLabel");
		_waveRuntimeFogOverlay = control.GetNode<ColorRect>("%FogOverlay");
		_waveRuntimeSeedBankSlots = control.GetNode<HBoxContainer>("%SeedBankSlots");
		_waveRuntimeHudProgress = control.GetNode<ProgressBar>("%InGameWaveProgress");
		_waveRuntimeSeedBankSignature = null;
		control.GetNode<Button>("Layout/Toolbar/ResetButton").Pressed += ResetWaveRuntimePreview;
		control.GetNode<Button>("Layout/Toolbar/PreviousButton").Pressed += delegate
		{
			AdvanceWaveRuntimePreview(-1);
		};
		control.GetNode<Button>("Layout/Toolbar/NextButton").Pressed += delegate
		{
			AdvanceWaveRuntimePreview(1);
		};
		_waveRuntimeAutoButton.Pressed += ToggleWaveRuntimeAutoPlay;
		_waveRuntimeTimer.Timeout += delegate
		{
			AdvanceWaveRuntimePreview(1);
		};
		BindAtomicTextControl(_waveRuntimeLevelNameEdit, "levelName", (string text) => Variant.From(in text), LevelRefreshDomain.Summary | LevelRefreshDomain.Hud);
		BindAtomicTextControl(_waveRuntimeDescriptionEdit, "description", (string text) => Variant.From(in text), LevelRefreshDomain.Hud);
		SetWaveRuntimePlaying(playing: false);
		RefreshWaveRuntimePreview();
		return control;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!_waveRuntimePlaying)
		{
			return;
		}
		_waveRuntimeProcessTickCount++;
		for (int i = 0; i < _waveRuntimeZombies.Count; i++)
		{
			Node2D node2D = _waveRuntimeZombies[i];
			if (GodotObject.IsInstanceValid(node2D))
			{
				node2D.Position += Vector2.Left * (30f + (float)i * 2f) * (float)delta;
				if (node2D.Position.X < 180f)
				{
					node2D.Position = new Vector2(1010f + (float)i * 28f, node2D.Position.Y);
				}
				if (_waveRuntimeZombieButtons.TryGetValue(node2D, out var value) && GodotObject.IsInstanceValid(value))
				{
					value.Position = node2D.Position + new Vector2(-66f, -82f);
				}
			}
		}
	}

	private void ResetWaveRuntimePreview()
	{
		_waveRuntimeIndex = 0;
		RefreshWaveRuntimePreview();
	}

	private void AdvanceWaveRuntimePreview(int delta)
	{
		int waveRuntimeCount = GetWaveRuntimeCount();
		if (waveRuntimeCount <= 0)
		{
			RefreshWaveRuntimePreview();
			return;
		}
		_waveRuntimeIndex = Math.Clamp(_waveRuntimeIndex + delta, 0, waveRuntimeCount - 1);
		RefreshWaveRuntimePreview();
		if (_waveRuntimeIndex >= waveRuntimeCount - 1 && GodotObject.IsInstanceValid(_waveRuntimeTimer) && !_waveRuntimeTimer.IsStopped())
		{
			ToggleWaveRuntimeAutoPlay();
		}
	}

	private void ToggleWaveRuntimeAutoPlay()
	{
		if (!GodotObject.IsInstanceValid(_waveRuntimeTimer))
		{
			return;
		}
		if (!_waveRuntimePlaying)
		{
			if (GetWaveRuntimeCount() <= 0)
			{
				return;
			}
			if (_waveRuntimeIndex >= GetWaveRuntimeCount() - 1)
			{
				_waveRuntimeIndex = 0;
			}
			_waveRuntimeTimer.Start();
			SetWaveRuntimePlaying(playing: true);
		}
		else
		{
			_waveRuntimeTimer.Stop();
			SetWaveRuntimePlaying(playing: false);
		}
		RefreshWaveRuntimePreview();
	}

	private void SetWaveRuntimePlaying(bool playing)
	{
		_waveRuntimePlaying = playing && GetWaveRuntimeCount() > 0;
		if (GodotObject.IsInstanceValid(_waveRuntimeAutoButton))
		{
			_waveRuntimeAutoButton.Text = (_waveRuntimePlaying ? "暂停播放" : "播放波次");
		}
		foreach (Node2D waveRuntimeZombie in _waveRuntimeZombies)
		{
			if (waveRuntimeZombie is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				SetWaveRuntimeCharacterActive(towerDefenseCharacter, _waveRuntimePlaying && IsVisibleInTree());
			}
		}
		RequestVisibilityGatedProcessing(_waveRuntimePlaying);
		RefreshVisibilityGatedProcessing();
	}

	private void RefreshWaveRuntimePreview()
	{
		if (!GodotObject.IsInstanceValid(_waveRuntimeStatusLabel))
		{
			return;
		}
		_waveRuntimeBattlefieldRebuildCount++;
		int waveRuntimeCount = GetWaveRuntimeCount();
		_waveRuntimeIndex = ((waveRuntimeCount > 0) ? Math.Clamp(_waveRuntimeIndex, 0, waveRuntimeCount - 1) : 0);
		int num = Math.Max(1, GetWaveRuntimeFlagInterval());
		bool flag = waveRuntimeCount > 0 && (_waveRuntimeIndex + 1) % num == 0;
		bool flag2 = waveRuntimeCount > 0 && _waveRuntimeIndex == waveRuntimeCount - 1;
		if (GodotObject.IsInstanceValid(_waveRuntimeProgress))
		{
			_waveRuntimeProgress.MaxValue = Math.Max(1, waveRuntimeCount);
			_waveRuntimeProgress.Value = ((waveRuntimeCount > 0) ? (_waveRuntimeIndex + 1) : 0);
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeHudProgress))
		{
			_waveRuntimeHudProgress.MaxValue = Math.Max(1, waveRuntimeCount);
			_waveRuntimeHudProgress.Value = ((waveRuntimeCount > 0) ? (_waveRuntimeIndex + 1) : 0);
		}
		ReleaseWaveRuntimeBattlefield();
		TowerDefenseMapConfig towerDefenseMapConfig = ResolveLevelMapConfig(_editingLevel);
		Texture2D texture2D = (GodotObject.IsInstanceValid(towerDefenseMapConfig) ? towerDefenseMapConfig.GetMapTexture(cache: false) : null);
		if (GodotObject.IsInstanceValid(_waveRuntimeBackground) && GodotObject.IsInstanceValid(texture2D))
		{
			_waveRuntimeBackground.Texture = texture2D;
		}
		if (waveRuntimeCount <= 0)
		{
			SetWaveRuntimePlaying(playing: false);
			_waveRuntimeStatusLabel.Text = "当前关卡没有可预览的 Wave 配置。";
			if (GodotObject.IsInstanceValid(_waveRuntimeBanner))
			{
				_waveRuntimeBanner.Text = "尚未配置波次";
			}
			if (GodotObject.IsInstanceValid(_waveRuntimeSpawnSummary))
			{
				_waveRuntimeSpawnSummary.Text = "在下方 Wave 数据中添加生成项后，这里会直接出现游戏角色。";
			}
			RefreshWaveRuntimeHud();
			return;
		}
		_waveRuntimeStatusLabel.Text = $"第 {_waveRuntimeIndex + 1}/{waveRuntimeCount} 波 · {(flag ? "旗帜大波" : "普通波")} · {(flag2 ? "最终波" : $"每 {num} 波一次大波")}";
		if (GodotObject.IsInstanceValid(_waveRuntimeBanner))
		{
			_waveRuntimeBanner.Text = (flag2 ? "最终波来袭！" : (flag ? "一大波僵尸正在接近！" : $"第 {_waveRuntimeIndex + 1} 波"));
			_waveRuntimeBanner.Modulate = ((flag || flag2) ? Colors.White : new Color(1f, 1f, 1f, 0.82f));
		}
		if (_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig && GodotObject.IsInstanceValid(towerDefenseLevelConfig.waveManager))
		{
			PopulateLegacyWaveRuntimePreview(towerDefenseLevelConfig.waveManager.wave[_waveRuntimeIndex]);
		}
		else
		{
			PopulateDictionaryWaveRuntimePreview(GetModernWaveRuntimeDictionary(_waveRuntimeIndex));
		}
		PopulateLevelLifecycleEventOverlays();
		if (GodotObject.IsInstanceValid(_waveRuntimeSpawnSummary))
		{
			_waveRuntimeSpawnSummary.Text = ((_waveRuntimeSpawnGroupCount == 0) ? "本波没有显式或动态生成项" : $"本波 {_waveRuntimeSpawnGroupCount} 组生成 · 点击角色上方按钮切换路线 · {_waveRuntimeEventCount} 个事件");
		}
		if (_waveRuntimeEventCount == 0)
		{
			AddWaveEventOverlay("本波没有额外事件", null);
		}
		RefreshWaveRuntimeHud();
		SetWaveRuntimePlaying(_waveRuntimePlaying);
	}

	private void ReleaseWaveRuntimeBattlefield()
	{
		foreach (Node2D waveRuntimeZombie in _waveRuntimeZombies)
		{
			if (waveRuntimeZombie is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				SetWaveRuntimeCharacterActive(towerDefenseCharacter, active: false);
				towerDefenseCharacter.Visible = false;
				string valueOrDefault = _waveRuntimeZombiePoolKeys.GetValueOrDefault(towerDefenseCharacter, towerDefenseCharacter.GetType().Name);
				if (!_waveRuntimeZombiePool.TryGetValue(valueOrDefault, out var value))
				{
					value = new Stack<TowerDefenseCharacter>();
					_waveRuntimeZombiePool[valueOrDefault] = value;
				}
				value.Push(towerDefenseCharacter);
			}
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeSpawnControlRoot))
		{
			foreach (Node child in _waveRuntimeSpawnControlRoot.GetChildren())
			{
				child.QueueFree();
			}
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeEventOverlay))
		{
			foreach (Node child2 in _waveRuntimeEventOverlay.GetChildren())
			{
				child2.QueueFree();
			}
		}
		_waveRuntimeZombies.Clear();
		_waveRuntimeZombieButtons.Clear();
		_waveRuntimeSpawnGroupCount = 0;
		_waveRuntimeEventCount = 0;
	}

	private void DisposeWaveRuntimePool()
	{
		HashSet<TowerDefenseCharacter> hashSet = new HashSet<TowerDefenseCharacter>();
		foreach (Stack<TowerDefenseCharacter> value in _waveRuntimeZombiePool.Values)
		{
			foreach (TowerDefenseCharacter item in value)
			{
				if (GodotObject.IsInstanceValid(item) && hashSet.Add(item))
				{
					item.QueueFree();
				}
			}
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeZombieRoot))
		{
			foreach (Node child in _waveRuntimeZombieRoot.GetChildren())
			{
				if (child is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter) && hashSet.Add(towerDefenseCharacter))
				{
					towerDefenseCharacter.QueueFree();
				}
			}
		}
		_waveRuntimeZombies.Clear();
		_waveRuntimeZombieButtons.Clear();
		_waveRuntimeZombiePool.Clear();
		_waveRuntimeZombiePoolKeys.Clear();
	}

	private void StopWaveRuntimePreview()
	{
		if (GodotObject.IsInstanceValid(_waveRuntimeTimer))
		{
			_waveRuntimeTimer.Stop();
			_waveRuntimeTimer.Paused = false;
		}
		SetWaveRuntimePlaying(playing: false);
		ReleaseWaveRuntimeBattlefield();
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		ApplyWaveRuntimeProcessMode(visible);
	}

	private void ApplyWaveRuntimeProcessMode(bool visible)
	{
		bool flag = visible && _waveRuntimePlaying;
		if (GodotObject.IsInstanceValid(_waveRuntimeZombieRoot))
		{
			_waveRuntimeZombieRoot.ProcessMode = (ProcessModeEnum)(flag ? 0 : 4);
		}
		foreach (Node2D waveRuntimeZombie in _waveRuntimeZombies)
		{
			if (waveRuntimeZombie is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				SetWaveRuntimeCharacterActive(towerDefenseCharacter, flag);
			}
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeSpawnControlRoot))
		{
			_waveRuntimeSpawnControlRoot.ProcessMode = (ProcessModeEnum)(visible ? 0 : 4);
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeEventOverlay))
		{
			_waveRuntimeEventOverlay.ProcessMode = (ProcessModeEnum)(visible ? 0 : 4);
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeTimer))
		{
			_waveRuntimeTimer.Paused = !visible && _waveRuntimePlaying;
		}
	}

	private void RefreshWaveRuntimeHud()
	{
		if (!GodotObject.IsInstanceValid(_editingLevel))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeLevelNameEdit) && !_waveRuntimeLevelNameEdit.HasFocus())
		{
			_waveRuntimeLevelNameEdit.Text = ReadString(_editingLevel, "levelName");
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeDescriptionEdit) && !_waveRuntimeDescriptionEdit.HasFocus())
		{
			_waveRuntimeDescriptionEdit.Text = ReadString(_editingLevel, "description");
		}
		Godot.Collections.Dictionary<StringName, Dictionary> featureData = GetFeatureData(_editingLevel);
		featureData.TryGetValue(new StringName("Sun"), out var value);
		long value2 = 0L;
		if (value != null)
		{
			value2 = value.GetValueOrDefault("Begin", value.GetValueOrDefault("StartSun", 0)).AsInt64();
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeSunLabel))
		{
			_waveRuntimeSunLabel.Text = $"☀ {value2}";
		}
		int waveRuntimeCount = GetWaveRuntimeCount();
		if (GodotObject.IsInstanceValid(_waveRuntimeWaveLabel))
		{
			_waveRuntimeWaveLabel.Text = ((waveRuntimeCount > 0) ? $"波次 {_waveRuntimeIndex + 1}/{waveRuntimeCount}" : "波次 0/0");
		}
		bool flag = ((_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig) ? towerDefenseLevelConfig.mowerUse : GetProcessData(_editingLevel).GetValueOrDefault("MowerUse", featureData.ContainsKey("Mower")).AsBool());
		if (GodotObject.IsInstanceValid(_waveRuntimeMowerLabel))
		{
			_waveRuntimeMowerLabel.Text = (flag ? "割草机 ×5 · 就绪" : "割草机 · 已关闭");
		}
		Dictionary value3;
		bool flag2 = ((!(_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig2)) ? (featureData.TryGetValue(new StringName("Fog"), out value3) && value3.GetValueOrDefault("Open", true).AsBool()) : (GodotObject.IsInstanceValid(towerDefenseLevelConfig2.fogManager) && towerDefenseLevelConfig2.fogManager.open));
		if (GodotObject.IsInstanceValid(_waveRuntimeFogOverlay))
		{
			_waveRuntimeFogOverlay.Visible = flag2;
		}
		if (GodotObject.IsInstanceValid(_waveRuntimeFogLabel))
		{
			_waveRuntimeFogLabel.Text = (flag2 ? "雾区 · 开启" : "雾区 · 关闭");
		}
		List<string> list = new List<string>();
		Dictionary value4;
		if (_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig3)
		{
			foreach (Variant packetBank in towerDefenseLevelConfig3.packetBankList)
			{
				list.Add(FormatVariant(packetBank));
			}
			if (list.Count == 0 && !string.IsNullOrWhiteSpace(towerDefenseLevelConfig3.packetBank))
			{
				list.Add("卡牌库 " + towerDefenseLevelConfig3.packetBank);
			}
		}
		else if (featureData.TryGetValue(new StringName("SeedBank"), out value4))
		{
			foreach (Variant item in ReadDictionaryArray(value4, "Packet"))
			{
				list.Add(FormatVariant(item));
			}
			if (list.Count == 0 && featureData.TryGetValue(new StringName("PacketBank"), out var value5))
			{
				list.Add("卡牌库 " + value5.GetValueOrDefault("PacketBankName", "未配置").AsString());
			}
		}
		RefreshWaveRuntimeSeedBank(list);
	}

	private void RefreshWaveRuntimeSeedBank(List<string> entries)
	{
		if (!GodotObject.IsInstanceValid(_waveRuntimeSeedBankSlots))
		{
			return;
		}
		string text = string.Join("\u001f", entries ?? new List<string>());
		if (text == _waveRuntimeSeedBankSignature)
		{
			return;
		}
		_waveRuntimeSeedBankSignature = text;
		foreach (Node child in _waveRuntimeSeedBankSlots.GetChildren())
		{
			child.QueueFree();
		}
		if (entries == null || entries.Count == 0)
		{
			_waveRuntimeSeedBankSlots.AddChild(new Button
			{
				Text = "＋ 配置卡槽",
				Disabled = true,
				CustomMinimumSize = new Vector2(116f, 48f)
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		for (int i = 0; i < Math.Min(8, entries.Count); i++)
		{
			string text2 = EmptyToPlaceholder(entries[i]);
			_waveRuntimeSeedBankSlots.AddChild(new Button
			{
				Name = $"SeedSlot{i}",
				Text = ((text2.Length > 12) ? text2.Substring(0, 12) : text2),
				TooltipText = text2,
				Disabled = true,
				CustomMinimumSize = new Vector2(116f, 48f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void PopulateLegacyWaveRuntimePreview(TowerDefenseLevelWaveConfig wave)
	{
		if (!GodotObject.IsInstanceValid(wave))
		{
			return;
		}
		if (wave.spawn != null)
		{
			foreach (TowerDefenseLevelSpawnConfig spawn in wave.spawn)
			{
				if (GodotObject.IsInstanceValid(spawn))
				{
					AddWaveSpawnGroup(spawn.zombie, spawn.num, spawn.line, delegate(int nextLine)
					{
						spawn.line = nextLine;
						spawn.EmitChanged();
						SaveCurrentEditedResource(_editingLevel);
						NotifyCurrentResourceEdited();
						RefreshWaveRuntimePreview();
					});
				}
			}
		}
		if (wave.gridSpawn != null)
		{
			foreach (TowerDefenseLevelGridSpawnConfig item in wave.gridSpawn)
			{
				if (GodotObject.IsInstanceValid(item))
				{
					AddWaveSpawnGroup(ResolvePacketCharacterName(item.packet), 1, item.gridPos.Y, null, $"格子 {item.gridPos}");
				}
			}
		}
		if (GodotObject.IsInstanceValid(wave.dynamic))
		{
			if (wave.dynamic.points <= 0)
			{
				Array<string> zombiePool = wave.dynamic.zombiePool;
				if (zombiePool == null || zombiePool.Count <= 0)
				{
					goto IL_0218;
				}
			}
			foreach (string item2 in wave.dynamic.zombiePool)
			{
				AddWaveSpawnGroup(item2, 1, -1, null, $"动态池 · {wave.dynamic.points} 点");
			}
		}
		goto IL_0218;
		IL_0218:
		if (wave.eventList == null)
		{
			return;
		}
		for (int num = 0; num < wave.eventList.Count; num++)
		{
			int nestedIndex = num;
			TowerDefenseLevelEventBase evt = wave.eventList[num];
			if (GodotObject.IsInstanceValid(evt))
			{
				AddWaveEventOverlay(FormatResource(evt), delegate
				{
					XWResourceEditContext context = XWResourceEditContext.ForProperty(evt, wave, evt.ResourcePath, base.CurrentResourcePath, "eventList", nestedIndex, "level_editor", base.CurrentEditContext?.IsBuiltInSource ?? false);
					XWEditorInterface.Instance?.EditResource(evt, context);
					XWEditorInterface.Instance?.FocusPanel("gameplay_logic_editor");
				});
			}
		}
	}

	private void PopulateDictionaryWaveRuntimePreview(Dictionary wave)
	{
		if (wave == null)
		{
			return;
		}
		foreach (Variant item in ReadDictionaryArray(wave, "Spawn"))
		{
			if (item.VariantType == Variant.Type.Dictionary)
			{
				Dictionary spawn = item.AsGodotDictionary();
				string characterName = spawn.GetValueOrDefault("Zombie", "").AsString();
				int amount = spawn.GetValueOrDefault("Num", 1).AsInt32();
				int line = spawn.GetValueOrDefault("Line", -1).AsInt32();
				AddWaveSpawnGroup(characterName, amount, line, delegate(int nextLine)
				{
					spawn["Line"] = nextLine;
					SaveCurrentEditedResource(_editingLevel);
					NotifyCurrentResourceEdited();
					RefreshWaveRuntimePreview();
				});
			}
		}
		foreach (Variant item2 in ReadDictionaryArray(wave, "GridSpawn"))
		{
			if (item2.VariantType == Variant.Type.Dictionary)
			{
				string text = item2.AsGodotDictionary().GetValueOrDefault("Packet", "").AsString();
				AddWaveSpawnGroup(ResolvePacketCharacterName(text), 1, -1, null, "格子生成 · " + text);
			}
		}
		if (wave.ContainsKey("Dynamic") && wave["Dynamic"].VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = wave["Dynamic"].AsGodotDictionary();
			int value = dictionary.GetValueOrDefault("Point", 0).AsInt32();
			foreach (Variant item3 in ReadDictionaryArray(dictionary, "ZombiePool"))
			{
				AddWaveSpawnGroup(item3.AsString(), 1, -1, null, $"动态池 · {value} 点");
			}
		}
		foreach (Variant item4 in ReadDictionaryArray(wave, "Event"))
		{
			if (item4.VariantType == Variant.Type.Dictionary)
			{
				Dictionary eventData = item4.AsGodotDictionary();
				AddEditableWaveEventOverlay(eventData);
			}
		}
	}

	private void AddWaveSpawnGroup(string characterName, int amount, int line, Action<int> lineChanged, string badgePrefix = "")
	{
		if (!GodotObject.IsInstanceValid(_waveRuntimeZombieRoot) || !GodotObject.IsInstanceValid(_waveRuntimeSpawnControlRoot))
		{
			return;
		}
		_waveRuntimeSpawnGroupCount++;
		int num = _waveRuntimeSpawnGroupCount - 1;
		string text = (string.IsNullOrWhiteSpace(characterName) ? "ZombieNormal" : characterName);
		PackedScene packedScene = ResourceManager.Instance?.GetCharacterScene(text);
		if (packedScene == null)
		{
			packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return;
		}
		int num2 = Math.Clamp(amount, 1, 3);
		int line2 = ResolveWavePreviewLine(line, num);
		for (int i = 0; i < num2; i++)
		{
			if (_waveRuntimeZombies.Count >= 15)
			{
				break;
			}
			TowerDefenseCharacter towerDefenseCharacter = AcquireWaveRuntimeZombie(text, packedScene);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				continue;
			}
			towerDefenseCharacter.Visible = true;
			towerDefenseCharacter.Scale = Vector2.One * 0.72f;
			towerDefenseCharacter.Position = new Vector2(820f + (float)num * 78f + (float)i * 48f, GetWavePreviewLineY(line2));
			SetWaveRuntimeCharacterActive(towerDefenseCharacter, _waveRuntimePlaying && IsVisibleInTree());
			_waveRuntimeZombies.Add(towerDefenseCharacter);
			if (i != 0)
			{
				continue;
			}
			string value = ((line < 1) ? "随机路线" : $"路线 {line}");
			Button button = new Button
			{
				Text = (string.IsNullOrWhiteSpace(badgePrefix) ? $"{value} · ×{Math.Max(1, amount)}" : $"{badgePrefix} · ×{Math.Max(1, amount)}"),
				CustomMinimumSize = new Vector2(132f, 31f),
				TooltipText = ((lineChanged == null) ? (text + "（由当前波次动态生成）") : (text + "：点击直接切换生成路线"))
			};
			if (lineChanged == null)
			{
				button.MouseFilter = MouseFilterEnum.Ignore;
			}
			else
			{
				button.Pressed += delegate
				{
					lineChanged(GetNextWavePreviewLine(line));
				};
			}
			_waveRuntimeSpawnControlRoot.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			_waveRuntimeZombieButtons[towerDefenseCharacter] = button;
			button.Position = towerDefenseCharacter.Position + new Vector2(-66f, -82f);
		}
	}

	private TowerDefenseCharacter AcquireWaveRuntimeZombie(string poolKey, PackedScene scene)
	{
		if (_waveRuntimeZombiePool.TryGetValue(poolKey, out var value))
		{
			while (value.Count > 0)
			{
				TowerDefenseCharacter towerDefenseCharacter = value.Pop();
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					towerDefenseCharacter.inGame = false;
					towerDefenseCharacter.editorPreviewMode = true;
					towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
					if (towerDefenseCharacter.GetParent() != _waveRuntimeZombieRoot)
					{
						towerDefenseCharacter.Reparent(_waveRuntimeZombieRoot, keepGlobalTransform: false);
					}
					EnforceWaveRuntimeCharacterIsolation(towerDefenseCharacter, reapplySafety: false);
					_waveRuntimeCharacterReuseCount++;
					return towerDefenseCharacter;
				}
			}
		}
		Node node;
		try
		{
			node = scene.Instantiate(PackedScene.GenEditState.Disabled);
		}
		catch (Exception ex)
		{
			GD.PushWarning("Level wave preview character load failed: " + poolKey + " " + ex.Message);
			return null;
		}
		if (!(node is TowerDefenseCharacter towerDefenseCharacter2))
		{
			node.Free();
			return null;
		}
		PrepareWaveRuntimeCharacterBeforeTree(towerDefenseCharacter2);
		_waveRuntimeZombieRoot.AddChild(towerDefenseCharacter2, forceReadableName: false, InternalMode.Disabled);
		EnforceWaveRuntimeCharacterIsolation(towerDefenseCharacter2, reapplySafety: true);
		_waveRuntimeZombiePoolKeys[towerDefenseCharacter2] = poolKey;
		_waveRuntimeCharacterInstantiationCount++;
		return towerDefenseCharacter2;
	}

	private static void PrepareWaveRuntimeCharacterBeforeTree(TowerDefenseCharacter zombie, bool refreshAnimationAdapter = true)
	{
		if (GodotObject.IsInstanceValid(zombie))
		{
			zombie.inGame = false;
			zombie.editorPreviewMode = true;
			zombie.Visible = false;
			zombie.ProcessMode = ProcessModeEnum.Disabled;
			if (refreshAnimationAdapter)
			{
				XWRuntimePreviewSceneAdapter.PrepareAnimationSprites(zombie);
				ConfigureWaveRuntimeAnimationSprites(zombie);
			}
			XWGameplayLogicPreviewSafety.PrepareCharacterBeforeTree(zombie);
		}
	}

	private static void ConfigureWaveRuntimeAnimationSprites(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.refreshEveryFrame = false;
		}
		foreach (Node child in node.GetChildren())
		{
			ConfigureWaveRuntimeAnimationSprites(child);
		}
	}

	private static void EnforceWaveRuntimeCharacterIsolation(TowerDefenseCharacter character, bool reapplySafety)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			if (reapplySafety)
			{
				XWGameplayLogicPreviewSafety.PrepareCharacterBeforeTree(character);
			}
			character.SetMainStateMachineDispatchEnabled(enabled: false);
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(instance.characterRegistry))
			{
				instance.CharacterUnregister(character);
			}
			if (character is TowerDefenseZombie zombie)
			{
				TowerDefenseZombieBatch.Unregister(zombie);
			}
			else
			{
				TowerDefenseCharacterBatch.Unregister(character);
			}
		}
	}

	private static void SetWaveRuntimeCharacterActive(TowerDefenseCharacter character, bool active)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		ProcessModeEnum processModeEnum = (ProcessModeEnum)(active ? 0 : 4);
		if (character.ProcessMode != processModeEnum)
		{
			if (!active)
			{
				XWGameplayLogicPreviewSafety.SetCharacterPreviewAnimationActive(character, active: false);
			}
			character.ProcessMode = processModeEnum;
			if (active)
			{
				XWGameplayLogicPreviewSafety.SetCharacterPreviewAnimationActive(character, active: true);
			}
		}
	}

	private void AddWaveEventOverlay(string text, Action pressed)
	{
		if (GodotObject.IsInstanceValid(_waveRuntimeEventOverlay))
		{
			_waveRuntimeEventCount++;
			Button button = new Button
			{
				Text = "事件 · " + EmptyToPlaceholder(text),
				CustomMinimumSize = new Vector2(0f, 38f),
				TooltipText = ((pressed == null) ? "波次没有额外事件" : "点击在游戏逻辑可视面板编辑这个事件")
			};
			if (pressed == null)
			{
				button.MouseFilter = MouseFilterEnum.Ignore;
			}
			else
			{
				button.Pressed += pressed;
			}
			_waveRuntimeEventOverlay.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void AddEditableWaveEventOverlay(Dictionary eventData, string phase = "波次")
	{
		if (GodotObject.IsInstanceValid(_waveRuntimeEventOverlay) && eventData != null)
		{
			_waveRuntimeEventCount++;
			LineEdit edit = new LineEdit
			{
				Text = eventData.GetValueOrDefault("EventName", "Event").AsString(),
				PlaceholderText = "直接输入事件名称",
				CustomMinimumSize = new Vector2(0f, 38f),
				TooltipText = phase + "事件：直接在游戏事件提示层修改 EventName"
			};
			edit.TextSubmitted += CommitEventName;
			edit.FocusExited += delegate
			{
				CommitEventName(edit.Text);
			};
			_waveRuntimeEventOverlay.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
		}
		void CommitEventName(string value)
		{
			string text = value?.StripEdges() ?? "";
			if (!(eventData.GetValueOrDefault("EventName", "").AsString() == text))
			{
				eventData["EventName"] = text;
				SaveCurrentEditedResource(_editingLevel);
				NotifyCurrentResourceEdited();
			}
		}
	}

	private void PopulateLevelLifecycleEventOverlays()
	{
		if (_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			AddLegacyLifecycleEvents("初始化", "eventInit", towerDefenseLevelConfig, towerDefenseLevelConfig.eventInit);
			AddLegacyLifecycleEvents("准备", "eventReady", towerDefenseLevelConfig, towerDefenseLevelConfig.eventReady);
			AddLegacyLifecycleEvents("开战", "eventStart", towerDefenseLevelConfig, towerDefenseLevelConfig.eventStart);
			return;
		}
		Godot.Collections.Dictionary<StringName, Dictionary> featureData = GetFeatureData(_editingLevel);
		StringName key = new StringName("Event");
		if (featureData != null && featureData.ContainsKey(key))
		{
			Dictionary eventFeature = featureData[key];
			AddDictionaryLifecycleEvents(eventFeature, "EventInit", "初始化");
			AddDictionaryLifecycleEvents(eventFeature, "EventReady", "准备");
			AddDictionaryLifecycleEvents(eventFeature, "EventStart", "开战");
		}
	}

	private void AddLegacyLifecycleEvents(string phase, string propertyName, Resource owner, Array<TowerDefenseLevelEventBase> events)
	{
		if (events == null)
		{
			return;
		}
		for (int i = 0; i < events.Count; i++)
		{
			int nestedIndex = i;
			TowerDefenseLevelEventBase evt = events[i];
			if (GodotObject.IsInstanceValid(evt))
			{
				AddWaveEventOverlay(phase + " · " + FormatResource(evt), delegate
				{
					XWResourceEditContext context = XWResourceEditContext.ForProperty(evt, owner, evt.ResourcePath, base.CurrentResourcePath, propertyName, nestedIndex, "level_editor", base.CurrentEditContext?.IsBuiltInSource ?? false);
					XWEditorInterface.Instance?.EditResource(evt, context);
					XWEditorInterface.Instance?.FocusPanel("gameplay_logic_editor");
				});
			}
		}
	}

	private void AddDictionaryLifecycleEvents(Dictionary eventFeature, string key, string phase)
	{
		foreach (Variant item in ReadDictionaryArray(eventFeature, key))
		{
			if (item.VariantType == Variant.Type.Dictionary)
			{
				AddEditableWaveEventOverlay(item.AsGodotDictionary(), phase);
			}
		}
	}

	private int ResolveWavePreviewLine(int configuredLine, int groupIndex)
	{
		int num = Math.Max(1, ResolveLevelMapConfig(_editingLevel)?.gridNum.Y ?? 5);
		if (configuredLine < 1 || configuredLine > num)
		{
			return groupIndex % num + 1;
		}
		return configuredLine;
	}

	private int GetNextWavePreviewLine(int configuredLine)
	{
		int num = Math.Max(1, ResolveLevelMapConfig(_editingLevel)?.gridNum.Y ?? 5);
		if (configuredLine < 1)
		{
			return 1;
		}
		if (configuredLine < num)
		{
			return configuredLine + 1;
		}
		return -1;
	}

	private static float GetWavePreviewLineY(int line)
	{
		return 57f + (float)Math.Clamp(line, 1, 8) * 62f;
	}

	private static string ResolvePacketCharacterName(string packetName)
	{
		if (string.IsNullOrWhiteSpace(packetName) || ResourceManager.Instance == null)
		{
			return packetName;
		}
		TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(packetName);
		if (!GodotObject.IsInstanceValid(packetConfigReadOnly?.characterConfig) || string.IsNullOrWhiteSpace(packetConfigReadOnly.characterConfig.name))
		{
			return packetName;
		}
		return packetConfigReadOnly.characterConfig.name;
	}

	private int GetWaveRuntimeCount()
	{
		if (_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig && GodotObject.IsInstanceValid(towerDefenseLevelConfig.waveManager))
		{
			return towerDefenseLevelConfig.waveManager.wave?.Count ?? 0;
		}
		return ReadDictionaryArray(GetProcessData(_editingLevel), "Wave").Count;
	}

	private int GetWaveRuntimeFlagInterval()
	{
		if (_editingLevel is TowerDefenseLevelConfig towerDefenseLevelConfig && GodotObject.IsInstanceValid(towerDefenseLevelConfig.waveManager))
		{
			return towerDefenseLevelConfig.waveManager.flagWaveInterval;
		}
		return GetProcessData(_editingLevel)?.GetValueOrDefault("FlagWaveInterval", 10).AsInt32() ?? 10;
	}

	private Dictionary GetModernWaveRuntimeDictionary(int index)
	{
		Godot.Collections.Array array = ReadDictionaryArray(GetProcessData(_editingLevel), "Wave");
		if (index < 0 || index >= array.Count || array[index].VariantType != Variant.Type.Dictionary)
		{
			return new Dictionary();
		}
		return array[index].AsGodotDictionary();
	}

	private static Godot.Collections.Array ReadDictionaryArray(Dictionary dictionary, string key)
	{
		if (dictionary != null && dictionary.ContainsKey(key) && dictionary[key].VariantType == Variant.Type.Array)
		{
			return dictionary[key].AsGodotArray();
		}
		return new Godot.Collections.Array();
	}

	private static string JoinVariantArray(Godot.Collections.Array array)
	{
		if (array == null || array.Count == 0)
		{
			return "空";
		}
		List<string> list = new List<string>();
		foreach (Variant item in array)
		{
			list.Add(item.AsString());
		}
		return string.Join(", ", list);
	}
}
