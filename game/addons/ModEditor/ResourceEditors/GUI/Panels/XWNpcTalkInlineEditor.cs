using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWNpcTalkInlineEditor : VBoxContainer
{
	[Signal]
	public delegate void TalkEditedEventHandler();

	private const string CrazyDaveScenePath = "res://Prefab/Npc/CrazyDave/NpcCrazyDave.tscn";

	private const string WeiWeiMiScenePath = "res://Prefab/Npc/WeiWeiMi/WeiWeiMi.tscn";

	private const string BubbleTextEditorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkBubbleTextEditor.tscn";

	private const string VisualChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkVisualChoiceCard.tscn";

	private static readonly string[] KnownNpcKeys = new string[2] { "CrazyDave", "WeiWeiMi" };

	private static readonly string[] HandSceneProperties = new string[4] { "handScene", "shoulderScene", "shoulder2Scene", "headScene" };

	private static readonly string[] EntryTypeTitles = new string[4] { "普通气泡", "道具表演", "教程衔接", "购买对话" };

	private static readonly string[] EntryTypeDetails = new string[4] { "NPC、动画、音频和气泡", "额外装配手部、肩部和头部场景", "对话结束后进入教程资源", "商店和购买流程使用的对白" };

	private static readonly string[] EntryTypeIconPaths = new string[4] { "res://addons/ModEditor/Icons/ClassIcon/RichTextLabel.svg", "res://addons/ModEditor/Icons/ClassIcon/PackedScene.svg", "res://addons/ModEditor/Icons/ClassIcon/GraphEdit.svg", "res://addons/ModEditor/Icons/AssetLib.svg" };

	private static PackedScene _bubbleTextEditorScene;

	private static PackedScene _visualChoiceCardScene;

	private NpcTalkConfig _config;

	private Resource _editingRoot;

	private bool _standaloneEntry;

	private int _index;

	private NpcTalkControl _runtimeControl;

	private NpcBase _npc;

	private string _npcSceneKey = "";

	private TextEdit _bubbleEditor;

	private XWVisualPropertyBinding _rootBinding;

	private XWVisualPropertyBinding _entryBinding;

	private XWUndoRedoManager _undoRedo;

	private bool _updatingControls;

	private string _pendingPropProperty = "";

	private int _selectedAddType;

	private LineEdit _saveKeyLineEdit;

	private Button _jsonSourceButton;

	private Button _entryVisualButton;

	private Button _addTypeVisualButton;

	private PopupPanel _entryPickerPopup;

	private PopupPanel _addTypePopup;

	private HFlowContainer _entryPickerGrid;

	private HFlowContainer _addTypeGrid;

	private XWAspectScaledPreviewHost _stageFrame;

	private readonly List<XWNpcTalkVisualChoiceCard> _entryCards = new List<XWNpcTalkVisualChoiceCard>();

	private readonly List<XWNpcTalkVisualChoiceCard> _typeCards = new List<XWNpcTalkVisualChoiceCard>();

	private Button _previousButton;

	private Button _nextButton;

	private Button _addButton;

	private Button _duplicateButton;

	private Button _removeButton;

	private Button _moveUpButton;

	private Button _moveDownButton;

	private Label _emptyLabel;

	private Label _statusLabel;

	private OptionButton _npcOption;

	private XWVisualOptionGallery _npcVisualGallery;

	private OptionButton _animeOption;

	private XWVisualOptionGallery _animeVisualGallery;

	private Button _audioSelectionButton;

	private Button _playAudioButton;

	private XWGameplayResourcePickerWindow _audioPicker;

	private PanelContainer _handPropsPanel;

	private PanelContainer _tutorialPanel;

	private Button _tutorialResourceButton;

	private readonly System.Collections.Generic.Dictionary<string, Button> _propSceneButtons = new System.Collections.Generic.Dictionary<string, Button>();

	private FileDialog _jsonSourceDialog;

	private FileDialog _propSceneDialog;

	private FileDialog _tutorialResourceDialog;

	private AudioStreamPlayer _directAudioPlayer;

	public int EntryCount => (_config?.talkList?.Count).GetValueOrDefault();

	public int CurrentEntryIndex => _index;

	public int EntryVisualCardCount => _entryCards.Count;

	public int EntryTypeCardCount => _typeCards.Count;

	public int SelectedAddType => _selectedAddType;

	public XWAspectScaledPreviewHost ResponsiveStage => _stageFrame;

	public override void _Ready()
	{
		_undoRedo = XWEditorInterface.Instance?.GetUndoRedoManager();
		BindSceneNodes();
		PopulateFixedOptions();
		ConnectSceneSignals();
		RefreshFromConfig();
	}

	public override void _ExitTree()
	{
		_npcVisualGallery?.Dispose();
		_npcVisualGallery = null;
		_animeVisualGallery?.Dispose();
		_animeVisualGallery = null;
		_rootBinding?.Dispose();
		_entryBinding?.Dispose();
		_rootBinding = null;
		_entryBinding = null;
		_directAudioPlayer?.Stop();
		ClearNpc();
		base._ExitTree();
	}

	public void EditConfig(NpcTalkConfig config, Resource editingRoot)
	{
		_config = config;
		_editingRoot = editingRoot;
		_standaloneEntry = editingRoot is NpcTalkBaseConfig;
		_index = 0;
		if (IsNodeReady())
		{
			RefreshFromConfig();
		}
	}

	private void BindSceneNodes()
	{
		_runtimeControl = GetNode<NpcTalkControl>("%RuntimeNpcTalkControl");
		_saveKeyLineEdit = GetNode<LineEdit>("%SaveKeyLineEdit");
		_jsonSourceButton = GetNode<Button>("%JsonSourceButton");
		_entryVisualButton = GetNode<Button>("%EntryVisualButton");
		_addTypeVisualButton = GetNode<Button>("%AddTypeVisualButton");
		_entryPickerPopup = GetNode<PopupPanel>("%EntryPickerPopup");
		_addTypePopup = GetNode<PopupPanel>("%AddTypePopup");
		_entryPickerGrid = GetNode<HFlowContainer>("%EntryPickerGrid");
		_addTypeGrid = GetNode<HFlowContainer>("%AddTypeGrid");
		_stageFrame = GetNode<XWAspectScaledPreviewHost>("%StageFrame");
		_previousButton = GetNode<Button>("%PreviousButton");
		_nextButton = GetNode<Button>("%NextButton");
		_addButton = GetNode<Button>("%AddEntryButton");
		_duplicateButton = GetNode<Button>("%DuplicateEntryButton");
		_removeButton = GetNode<Button>("%RemoveEntryButton");
		_moveUpButton = GetNode<Button>("%MoveEntryUpButton");
		_moveDownButton = GetNode<Button>("%MoveEntryDownButton");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_statusLabel = GetNode<Label>("%Status");
		_npcOption = GetNode<OptionButton>("%NpcOption");
		_animeOption = GetNode<OptionButton>("%AnimeOption");
		_npcVisualGallery = new XWVisualOptionGallery(_npcOption, GetNode<HFlowContainer>("%NpcVisualChoices"), "NpcVisualCatalog", (int _) => ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCharacter.svg", null, ResourceLoader.CacheMode.Reuse));
		_animeVisualGallery = new XWVisualOptionGallery(_animeOption, GetNode<HFlowContainer>("%AnimeVisualChoices"), "NpcAnimationVisualCatalog", (int _) => ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/MainMovieWrite.svg", null, ResourceLoader.CacheMode.Reuse));
		_audioSelectionButton = GetNode<Button>("%AudioSelectionButton");
		_playAudioButton = GetNode<Button>("%PlayAudioButton");
		_handPropsPanel = GetNode<PanelContainer>("%HandPropsPanel");
		_tutorialPanel = GetNode<PanelContainer>("%TutorialPanel");
		_tutorialResourceButton = GetNode<Button>("%TutorialResourceButton");
		_jsonSourceDialog = GetNode<FileDialog>("%JsonSourceDialog");
		_propSceneDialog = GetNode<FileDialog>("%PropSceneDialog");
		_tutorialResourceDialog = GetNode<FileDialog>("%TutorialResourceDialog");
		_directAudioPlayer = GetNode<AudioStreamPlayer>("%DirectAudioPlayer");
		_propSceneButtons["handScene"] = GetNode<Button>("%HandSceneButton");
		_propSceneButtons["shoulderScene"] = GetNode<Button>("%ShoulderSceneButton");
		_propSceneButtons["shoulder2Scene"] = GetNode<Button>("%Shoulder2SceneButton");
		_propSceneButtons["headScene"] = GetNode<Button>("%HeadSceneButton");
	}

	private void PopulateFixedOptions()
	{
		BuildTypeVisualCards();
		SelectAddTypeVisual(0);
		RebuildNpcOptions("");
	}

	private void BuildTypeVisualCards()
	{
		ClearVisualCards(_addTypeGrid, _typeCards);
		for (int i = 0; i < EntryTypeTitles.Length; i++)
		{
			int capturedIndex = i;
			XWNpcTalkVisualChoiceCard xWNpcTalkVisualChoiceCard = CreateVisualChoiceCard();
			if (GodotObject.IsInstanceValid(xWNpcTalkVisualChoiceCard))
			{
				xWNpcTalkVisualChoiceCard.Configure(i, EntryTypeTitles[i], EntryTypeDetails[i], GetEntryTypeIcon(i), i == _selectedAddType);
				xWNpcTalkVisualChoiceCard.Pressed += delegate
				{
					SelectAddTypeVisual(capturedIndex);
				};
				_typeCards.Add(xWNpcTalkVisualChoiceCard);
				_addTypeGrid.AddChild(xWNpcTalkVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void RebuildEntryVisualCards()
	{
		ClearVisualCards(_entryPickerGrid, _entryCards);
		int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
		for (int i = 0; i < valueOrDefault; i++)
		{
			int capturedIndex = i;
			XWNpcTalkVisualChoiceCard xWNpcTalkVisualChoiceCard = CreateVisualChoiceCard();
			if (GodotObject.IsInstanceValid(xWNpcTalkVisualChoiceCard))
			{
				ConfigureEntryCard(xWNpcTalkVisualChoiceCard, i, _config.talkList[i]);
				xWNpcTalkVisualChoiceCard.Pressed += delegate
				{
					SelectEntry(capturedIndex);
					_entryPickerPopup.Hide();
				};
				_entryCards.Add(xWNpcTalkVisualChoiceCard);
				_entryPickerGrid.AddChild(xWNpcTalkVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private XWNpcTalkVisualChoiceCard CreateVisualChoiceCard()
	{
		if (_visualChoiceCardScene == null)
		{
			_visualChoiceCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		return _visualChoiceCardScene?.Instantiate<XWNpcTalkVisualChoiceCard>(PackedScene.GenEditState.Disabled);
	}

	private static void ClearVisualCards(Control host, List<XWNpcTalkVisualChoiceCard> cards)
	{
		foreach (XWNpcTalkVisualChoiceCard card in cards)
		{
			if (GodotObject.IsInstanceValid(card))
			{
				if (card.GetParent() == host)
				{
					host.RemoveChild(card);
				}
				card.QueueFree();
			}
		}
		cards.Clear();
	}

	private void ConfigureEntryCard(XWNpcTalkVisualChoiceCard card, int index, NpcTalkBaseConfig talk)
	{
		int typeIndex = EntryTypeIndex(talk);
		card.Configure(index, $"#{index + 1}  {EntryTypeName(talk)}", BuildEntryVisualDetail(talk), GetEntryTypeIcon(typeIndex), index == _index);
	}

	private void RefreshEntryVisualCard(int index)
	{
		int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
		if (index >= 0 && index < valueOrDefault && index < _entryCards.Count)
		{
			ConfigureEntryCard(_entryCards[index], index, _config.talkList[index]);
		}
	}

	private void RefreshEntryVisualSelection()
	{
		NpcTalkBaseConfig currentTalk = GetCurrentTalk();
		if (GodotObject.IsInstanceValid(currentTalk))
		{
			_entryVisualButton.Text = BuildEntryLabel(_index, currentTalk);
			_entryVisualButton.Icon = GetEntryTypeIcon(EntryTypeIndex(currentTalk));
			for (int i = 0; i < _entryCards.Count; i++)
			{
				_entryCards[i].SetSelected(i == _index);
			}
		}
	}

	private void OpenEntryVisualPicker()
	{
		if (EntryCount > 0)
		{
			_entryPickerPopup.PopupCenteredClamped(new Vector2I(720, 480), 0.88f);
		}
	}

	private void OpenAddTypeVisualPicker()
	{
		if (!_standaloneEntry)
		{
			_addTypePopup.PopupCenteredClamped(new Vector2I(650, 300), 0.88f);
		}
	}

	public void SelectAddTypeVisual(int typeIndex)
	{
		_selectedAddType = Mathf.Clamp(typeIndex, 0, EntryTypeTitles.Length - 1);
		_addTypeVisualButton.Text = "新增类型：" + EntryTypeTitles[_selectedAddType];
		_addTypeVisualButton.Icon = GetEntryTypeIcon(_selectedAddType);
		for (int i = 0; i < _typeCards.Count; i++)
		{
			_typeCards[i].SetSelected(_typeCards[i].ChoiceIndex == _selectedAddType);
		}
		if (GodotObject.IsInstanceValid(_addTypePopup) && _addTypePopup.Visible)
		{
			_addTypePopup.Hide();
		}
	}

	public bool SetCurrentText(string text)
	{
		NpcTalkBaseConfig currentTalk = GetCurrentTalk();
		if (!GodotObject.IsInstanceValid(currentTalk) || _entryBinding == null)
		{
			return false;
		}
		_entryBinding.SetValue(currentTalk, "text", text ?? "", "修改 NPC 气泡文本", this, "RefreshNpcEditorFromHistory");
		ShowCurrent();
		return currentTalk.text == (text ?? "");
	}

	private static Texture2D GetEntryTypeIcon(int typeIndex)
	{
		int num = Mathf.Clamp(typeIndex, 0, EntryTypeIconPaths.Length - 1);
		return ResourceLoader.Load<Texture2D>(EntryTypeIconPaths[num], null, ResourceLoader.CacheMode.Reuse);
	}

	private static int EntryTypeIndex(NpcTalkBaseConfig talk)
	{
		if (!(talk is NpcTalkHandConfig))
		{
			if (!(talk is NpcTalkTutorialConfig))
			{
				if (talk is NpcTalkBuyConfig)
				{
					return 3;
				}
				return 0;
			}
			return 2;
		}
		return 1;
	}

	private static string BuildEntryVisualDetail(NpcTalkBaseConfig talk)
	{
		if (!GodotObject.IsInstanceValid(talk))
		{
			return "空条目";
		}
		string text = (talk.text ?? "").Replace('\n', ' ').Replace('\r', ' ').StripEdges();
		if (text.Length > 32)
		{
			text = text.Substring(0, 32) + "…";
		}
		return DisplayNpc(talk.npc) + " · " + text;
	}

	private void ConnectSceneSignals()
	{
		_previousButton.Pressed += ShowPrevious;
		_nextButton.Pressed += ShowNext;
		_entryVisualButton.Pressed += OpenEntryVisualPicker;
		_addTypeVisualButton.Pressed += OpenAddTypeVisualPicker;
		_addButton.Pressed += delegate
		{
			AddTalkEntry();
		};
		_duplicateButton.Pressed += delegate
		{
			DuplicateTalkEntry();
		};
		_removeButton.Pressed += delegate
		{
			RemoveTalkEntry();
		};
		_moveUpButton.Pressed += delegate
		{
			MoveTalkEntry(-1);
		};
		_moveDownButton.Pressed += delegate
		{
			MoveTalkEntry(1);
		};
		_jsonSourceButton.Pressed += ChooseJsonSource;
		_jsonSourceDialog.FileSelected += ApplyJsonSource;
		_npcOption.ItemSelected += SelectKnownNpc;
		_animeOption.ItemSelected += SelectKnownAnimation;
		_audioSelectionButton.Pressed += ShowAudioPicker;
		_playAudioButton.Pressed += PlayCurrentAudio;
		GetNode<Button>("%ClearAudioButton").Pressed += delegate
		{
			SetEntryString("audio", "", "清除 NPC 对话音效");
		};
		string[] handSceneProperties = HandSceneProperties;
		foreach (string text in handSceneProperties)
		{
			string captured = text;
			_propSceneButtons[text].Pressed += delegate
			{
				ChoosePropScene(captured);
			};
		}
		GetNode<Button>("%ClearHandSceneButton").Pressed += delegate
		{
			SetEntryResource("handScene", null, "清除手部道具");
		};
		GetNode<Button>("%ClearShoulderSceneButton").Pressed += delegate
		{
			SetEntryResource("shoulderScene", null, "清除肩部道具 1");
		};
		GetNode<Button>("%ClearShoulder2SceneButton").Pressed += delegate
		{
			SetEntryResource("shoulder2Scene", null, "清除肩部道具 2");
		};
		GetNode<Button>("%ClearHeadSceneButton").Pressed += delegate
		{
			SetEntryResource("headScene", null, "清除头部道具");
		};
		_propSceneDialog.FileSelected += ApplyPropScene;
		_tutorialResourceButton.Pressed += ChooseTutorialResource;
		GetNode<Button>("%ClearTutorialButton").Pressed += delegate
		{
			SetEntryResource("tutorial", null, "清除教程引用");
		};
		_tutorialResourceDialog.FileSelected += ApplyTutorialResource;
	}

	private void BindRootFields()
	{
		_rootBinding?.Dispose();
		_rootBinding = null;
		bool flag = GodotObject.IsInstanceValid(_config) && !_standaloneEntry;
		_saveKeyLineEdit.Editable = flag;
		_jsonSourceButton.Disabled = !flag;
		_addTypeVisualButton.Disabled = !flag;
		_addButton.Disabled = !flag;
		if (!flag)
		{
			_saveKeyLineEdit.Text = "独立对话条目";
			_jsonSourceButton.Text = "独立条目 · 无 JSON 根来源";
			return;
		}
		_rootBinding = new XWVisualPropertyBinding(_undoRedo, delegate
		{
			EmitEdited();
		});
		_rootBinding.BindText(_saveKeyLineEdit, _config, "saveKey", RefreshRootHeader, this, "RefreshNpcEditorFromHistory");
		RefreshRootHeader();
	}

	private void RefreshRootHeader()
	{
		if (GodotObject.IsInstanceValid(_config) && !_standaloneEntry)
		{
			string text = (GodotObject.IsInstanceValid(_config.data) ? _config.data.ResourcePath.GetFile() : "未设置");
			_jsonSourceButton.Text = "JSON 来源：" + text;
			_jsonSourceButton.TooltipText = (GodotObject.IsInstanceValid(_config.data) ? _config.data.ResourcePath : "选择 JSON 会按游戏加载规则重新生成整段对话");
		}
	}

	private void RefreshFromConfig()
	{
		if (IsNodeReady())
		{
			_entryBinding?.Dispose();
			_entryBinding = null;
			BindRootFields();
			int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
			RebuildEntryVisualCards();
			_emptyLabel.Visible = valueOrDefault == 0;
			_entryVisualButton.Disabled = valueOrDefault == 0;
			_previousButton.Disabled = valueOrDefault == 0;
			_nextButton.Disabled = valueOrDefault == 0;
			_duplicateButton.Disabled = _standaloneEntry || valueOrDefault == 0;
			_removeButton.Disabled = _standaloneEntry || valueOrDefault == 0;
			_moveUpButton.Disabled = _standaloneEntry || valueOrDefault < 2;
			_moveDownButton.Disabled = _standaloneEntry || valueOrDefault < 2;
			if (valueOrDefault == 0)
			{
				_index = 0;
				_entryVisualButton.Text = "当前没有可编辑的句子";
				_entryVisualButton.Icon = GetEntryTypeIcon(0);
				ClearNpc();
				SetEntryPanelsVisible(null);
				_statusLabel.Text = "当前没有对话条目；请在上方选择表现类型并点击“新增”。";
			}
			else
			{
				_index = Mathf.Clamp(_index, 0, valueOrDefault - 1);
				ShowCurrent();
			}
		}
	}

	private void ShowCurrent()
	{
		_entryBinding?.Dispose();
		_entryBinding = null;
		int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
		if (valueOrDefault != 0 && _index >= 0 && _index < valueOrDefault)
		{
			NpcTalkBaseConfig npcTalkBaseConfig = _config.talkList[_index];
			RefreshEntryVisualSelection();
			UpdateToolbarButtons(valueOrDefault);
			if (!GodotObject.IsInstanceValid(npcTalkBaseConfig))
			{
				ClearNpc();
				SetEntryPanelsVisible(null);
				_statusLabel.Text = $"{_index + 1} / {valueOrDefault} · 空对话条目";
			}
			else
			{
				ClearNpc();
				EnsureNpc(npcTalkBaseConfig.npc);
				ApplyTalk(npcTalkBaseConfig, applyProps: true);
				BindCurrentEntry(npcTalkBaseConfig);
				SetEntryPanelsVisible(npcTalkBaseConfig);
				RefreshEntryControlValues(npcTalkBaseConfig);
				_statusLabel.Text = BuildStatus(npcTalkBaseConfig, valueOrDefault);
			}
		}
	}

	private void BindCurrentEntry(NpcTalkBaseConfig talk)
	{
		_entryBinding = new XWVisualPropertyBinding(_undoRedo, delegate
		{
			EmitEdited();
		});
		if (GodotObject.IsInstanceValid(_bubbleEditor))
		{
			_entryBinding.BindText(_bubbleEditor, talk, "text", RefreshEntryVisuals, this, "RefreshNpcEditorFromHistory");
		}
	}

	private void RefreshEntryControlValues(NpcTalkBaseConfig talk)
	{
		_updatingControls = true;
		RebuildNpcOptions(talk.npc);
		RebuildAnimationOptions(talk.anime);
		string text = talk.audio?.StripEdges() ?? "";
		_audioSelectionButton.Text = (string.IsNullOrWhiteSpace(text) ? "选择游戏 / Mod 音效" : ("音效：" + text));
		_playAudioButton.Disabled = string.IsNullOrWhiteSpace(text);
		if (talk is NpcTalkHandConfig npcTalkHandConfig)
		{
			string[] handSceneProperties = HandSceneProperties;
			foreach (string text2 in handSceneProperties)
			{
				_propSceneButtons[text2].Text = ResourceLabel(npcTalkHandConfig.Get(text2).As<Resource>());
			}
		}
		if (talk is NpcTalkTutorialConfig npcTalkTutorialConfig)
		{
			_tutorialResourceButton.Text = "教程：" + ResourceLabel(npcTalkTutorialConfig.tutorial);
		}
		_updatingControls = false;
	}

	private void RefreshEntryVisuals()
	{
		NpcTalkBaseConfig currentTalk = GetCurrentTalk();
		if (GodotObject.IsInstanceValid(currentTalk))
		{
			bool flag = EnsureNpc(currentTalk.npc);
			ApplyTalk(currentTalk, flag);
			if (flag)
			{
				CallDeferred("ShowCurrent");
				return;
			}
			_updatingControls = true;
			RebuildNpcOptions(currentTalk.npc);
			SelectAnimationOption(currentTalk.anime);
			_updatingControls = false;
			RefreshEntryVisualCard(_index);
			RefreshEntryVisualSelection();
			_statusLabel.Text = BuildStatus(currentTalk, _config.talkList.Count);
		}
	}

	public void RefreshNpcEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()))
		{
			RefreshFromConfig();
			EmitEdited();
		}
	}

	public void SelectEntry(long index)
	{
		if (!_updatingControls)
		{
			int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
			if (valueOrDefault != 0)
			{
				_index = Mathf.Clamp((int)index, 0, valueOrDefault - 1);
				ShowCurrent();
			}
		}
	}

	public void ShowPrevious()
	{
		if (_index > 0)
		{
			SelectEntry(_index - 1);
		}
	}

	public void ShowNext()
	{
		int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
		if (_index + 1 < valueOrDefault)
		{
			SelectEntry(_index + 1);
		}
	}

	public bool AddTalkEntry()
	{
		if (_standaloneEntry || !GodotObject.IsInstanceValid(_config) || _rootBinding == null)
		{
			return false;
		}
		int count = _config.talkList.Count;
		NpcTalkBaseConfig npcTalkBaseConfig = _selectedAddType switch
		{
			1 => new NpcTalkHandConfig(), 
			2 => new NpcTalkTutorialConfig(), 
			3 => new NpcTalkBuyConfig(), 
			_ => new NpcTalkBaseConfig(), 
		};
		npcTalkBaseConfig.ResourceLocalToScene = true;
		npcTalkBaseConfig.ResourceName = $"Talk_{_config.talkList.Count + 1}";
		npcTalkBaseConfig.npc = "CrazyDave";
		npcTalkBaseConfig.text = "点击这里编辑新的对话气泡";
		Array<NpcTalkBaseConfig> array = new Array<NpcTalkBaseConfig>(_config.talkList);
		array.Add(npcTalkBaseConfig);
		_index = array.Count - 1;
		ReplaceTalkList(array, "新增 NPC 对话条目");
		return _config.talkList.Count == count + 1;
	}

	public bool DuplicateTalkEntry()
	{
		NpcTalkBaseConfig currentTalk = GetCurrentTalk();
		if (_standaloneEntry || !GodotObject.IsInstanceValid(currentTalk))
		{
			return false;
		}
		int count = _config.talkList.Count;
		NpcTalkBaseConfig npcTalkBaseConfig = currentTalk.Duplicate(deep: true) as NpcTalkBaseConfig;
		if (!GodotObject.IsInstanceValid(npcTalkBaseConfig))
		{
			return false;
		}
		npcTalkBaseConfig.ResourceLocalToScene = true;
		npcTalkBaseConfig.ResourceName = (string.IsNullOrWhiteSpace(currentTalk.ResourceName) ? $"Talk_{_index + 2}" : (currentTalk.ResourceName + "_Copy"));
		Array<NpcTalkBaseConfig> array = new Array<NpcTalkBaseConfig>(_config.talkList);
		_index = Mathf.Clamp(_index + 1, 0, array.Count);
		array.Insert(_index, npcTalkBaseConfig);
		ReplaceTalkList(array, "复制 NPC 对话条目");
		return _config.talkList.Count == count + 1;
	}

	public bool RemoveTalkEntry()
	{
		if (_standaloneEntry || !HasCurrentTalk())
		{
			return false;
		}
		int count = _config.talkList.Count;
		Array<NpcTalkBaseConfig> array = new Array<NpcTalkBaseConfig>(_config.talkList);
		array.RemoveAt(_index);
		_index = ((array.Count != 0) ? Math.Min(_index, array.Count - 1) : 0);
		ReplaceTalkList(array, "移除 NPC 对话条目");
		return _config.talkList.Count == count - 1;
	}

	private void MoveTalkEntry(int direction)
	{
		if (!_standaloneEntry && HasCurrentTalk())
		{
			int num = _index + Math.Sign(direction);
			if (num >= 0 && num < _config.talkList.Count)
			{
				Array<NpcTalkBaseConfig> array = new Array<NpcTalkBaseConfig>(_config.talkList);
				NpcTalkBaseConfig value = array[_index];
				array[_index] = array[num];
				array[num] = value;
				_index = num;
				ReplaceTalkList(array, (direction < 0) ? "上移 NPC 对话条目" : "下移 NPC 对话条目");
			}
		}
	}

	private void ReplaceTalkList(Array<NpcTalkBaseConfig> next, string actionName)
	{
		if (!_standaloneEntry && GodotObject.IsInstanceValid(_config) && _rootBinding != null)
		{
			_rootBinding.SetValue(_config, "talkList", next, actionName, this, "RefreshNpcEditorFromHistory");
			RefreshFromConfig();
		}
	}

	private void ChooseJsonSource()
	{
		if (!_standaloneEntry)
		{
			_jsonSourceDialog.PopupCenteredClamped(new Vector2I(900, 620), 0.9f);
		}
	}

	private void ApplyJsonSource(string path)
	{
		if (!_standaloneEntry && _rootBinding != null && !string.IsNullOrWhiteSpace(path))
		{
			Json json = ResourceLoader.Load<Json>(path, "", ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(json))
			{
				XWEditorInterface.Instance?.ShowToast("无法加载 NPC 对话 JSON：" + path);
				return;
			}
			_index = 0;
			_rootBinding.SetValue(_config, "data", json, "更换 NPC 对话 JSON 来源", this, "RefreshNpcEditorFromHistory");
			RefreshFromConfig();
		}
	}

	private void SelectKnownNpc(long index)
	{
		if (!_updatingControls && index >= 0 && index < _npcOption.ItemCount)
		{
			SetEntryString("npc", _npcOption.GetItemMetadata((int)index).AsString(), "更换 NPC 演员");
		}
	}

	private void SelectKnownAnimation(long index)
	{
		if (!_updatingControls && index >= 0 && index < _animeOption.ItemCount)
		{
			SetEntryString("anime", _animeOption.GetItemMetadata((int)index).AsString(), "更换 NPC 动画");
		}
	}

	private void SetEntryString(StringName property, string value, string actionName)
	{
		NpcTalkBaseConfig currentTalk = GetCurrentTalk();
		if (GodotObject.IsInstanceValid(currentTalk) && _entryBinding != null)
		{
			_entryBinding.SetValue(currentTalk, property, value ?? "", actionName, this, "RefreshNpcEditorFromHistory");
			ShowCurrent();
		}
	}

	private void ShowAudioPicker()
	{
		NpcTalkBaseConfig currentTalk = GetCurrentTalk();
		if (GodotObject.IsInstanceValid(currentTalk))
		{
			EnsureAudioPicker();
			_audioPicker?.Open(XWGameplayResourceKind.Audio, currentTalk.audio, ApplyAudioChoice, (XWGameplayResourceChoice choice) => choice.Kind == XWGameplayResourceKind.Audio);
		}
	}

	private void EnsureAudioPicker()
	{
		if (!GodotObject.IsInstanceValid(_audioPicker))
		{
			_audioPicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_audioPicker))
			{
				AddChild(_audioPicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void ApplyAudioChoice(XWGameplayResourceChoice choice)
	{
		if (!(choice == null) && choice.Kind == XWGameplayResourceKind.Audio)
		{
			SetEntryString("audio", choice.Key, "更换 NPC 对话音效");
		}
	}

	private void PlayCurrentAudio()
	{
		NpcTalkBaseConfig currentTalk = GetCurrentTalk();
		if (!GodotObject.IsInstanceValid(currentTalk))
		{
			return;
		}
		_directAudioPlayer.Stop();
		string text = currentTalk.audio?.StripEdges() ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			_statusLabel.Text = "当前条目没有配置音频。";
			return;
		}
		if (ResourceLoader.Exists(text))
		{
			_directAudioPlayer.Stream = ResourceLoader.Load<AudioStream>(text, null, ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(_directAudioPlayer.Stream))
			{
				_directAudioPlayer.Play();
				_statusLabel.Text = "正在试听：" + text.GetFile();
				return;
			}
		}
		AudioStream audioStream = LoadSelectedAudioStream(text);
		if (GodotObject.IsInstanceValid(audioStream))
		{
			_directAudioPlayer.Stream = audioStream;
			_directAudioPlayer.Play();
			_statusLabel.Text = "正在试听：" + text;
		}
		else if (GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(text, AudioManagerEnum.TYPE.SFX, 0.0, once: true, pauseAlive: true);
			_statusLabel.Text = "正在通过游戏音频表试听：" + text;
		}
		else
		{
			_statusLabel.Text = "无法试听音频：" + text;
		}
	}

	private AudioStream LoadSelectedAudioStream(string audioKey)
	{
		EnsureAudioPicker();
		if (!GodotObject.IsInstanceValid(_audioPicker))
		{
			return null;
		}
		return _audioPicker.LoadAudioStream(audioKey);
	}

	private void ChoosePropScene(string property)
	{
		if (GetCurrentTalk() is NpcTalkHandConfig && System.Array.IndexOf(HandSceneProperties, property) >= 0)
		{
			_pendingPropProperty = property;
			_propSceneDialog.PopupCenteredClamped(new Vector2I(900, 620), 0.9f);
		}
	}

	private void ApplyPropScene(string path)
	{
		if (!string.IsNullOrWhiteSpace(_pendingPropProperty) && !string.IsNullOrWhiteSpace(path))
		{
			PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				XWEditorInterface.Instance?.ShowToast("无法加载 NPC 道具场景：" + path);
				return;
			}
			string pendingPropProperty = _pendingPropProperty;
			_pendingPropProperty = "";
			SetEntryResource(pendingPropProperty, packedScene, "更换 NPC " + pendingPropProperty);
		}
	}

	private void ChooseTutorialResource()
	{
		if (GetCurrentTalk() is NpcTalkTutorialConfig)
		{
			_tutorialResourceDialog.PopupCenteredClamped(new Vector2I(900, 620), 0.9f);
		}
	}

	private void ApplyTutorialResource(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			TutorialConfig tutorialConfig = ResourceLoader.Load<TutorialConfig>(path, "", ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(tutorialConfig))
			{
				XWEditorInterface.Instance?.ShowToast("无法加载教程资源：" + path);
			}
			else
			{
				SetEntryResource("tutorial", tutorialConfig, "更换 NPC 对话教程引用");
			}
		}
	}

	private void SetEntryResource(StringName property, Resource resource, string actionName)
	{
		NpcTalkBaseConfig currentTalk = GetCurrentTalk();
		if (GodotObject.IsInstanceValid(currentTalk) && _entryBinding != null)
		{
			_entryBinding.SetValue(currentTalk, property, resource, actionName, this, "RefreshNpcEditorFromHistory");
			ClearNpc();
			ShowCurrent();
		}
	}

	private bool EnsureNpc(string npcKey)
	{
		string text = (string.Equals(npcKey, "WeiWeiMi", StringComparison.OrdinalIgnoreCase) ? "WeiWeiMi" : "CrazyDave");
		if (GodotObject.IsInstanceValid(_npc) && _npcSceneKey == text)
		{
			return false;
		}
		ClearNpc();
		_npc = ResourceLoader.Load<PackedScene>((text == "WeiWeiMi") ? "res://Prefab/Npc/WeiWeiMi/WeiWeiMi.tscn" : "res://Prefab/Npc/CrazyDave/NpcCrazyDave.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<NpcBase>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_npc))
		{
			return false;
		}
		_npcSceneKey = text;
		_npc.Name = "EditableNpc";
		_npc.Position = Vector2.Zero;
		_npc.Scale = Vector2.One;
		ColorRect nodeOrNull = _npc.GetNodeOrNull<ColorRect>("CanvasLayer/ColorRect");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.MouseFilter = MouseFilterEnum.Ignore;
		}
		_runtimeControl.AddNpc(_npc);
		AttachBubbleEditor();
		return true;
	}

	private void AttachBubbleEditor()
	{
		_bubbleEditor = null;
		if (!GodotObject.IsInstanceValid(_npc))
		{
			return;
		}
		TextureRect nodeOrNull = _npc.GetNodeOrNull<TextureRect>("%TalkBubble");
		Label nodeOrNull2 = _npc.GetNodeOrNull<Label>("%TalkLabel");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			if (GodotObject.IsInstanceValid(nodeOrNull2))
			{
				nodeOrNull2.Visible = false;
			}
			if (_bubbleTextEditorScene == null)
			{
				_bubbleTextEditorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkBubbleTextEditor.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_bubbleEditor = _bubbleTextEditorScene?.Instantiate<TextEdit>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_bubbleEditor))
			{
				nodeOrNull.AddChild(_bubbleEditor, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void ApplyTalk(NpcTalkBaseConfig talk, bool applyProps)
	{
		if (GodotObject.IsInstanceValid(_npc))
		{
			TextureRect nodeOrNull = _npc.GetNodeOrNull<TextureRect>("%TalkBubble");
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.Visible = true;
				nodeOrNull.Scale = Vector2.One;
			}
			if (GodotObject.IsInstanceValid(_bubbleEditor) && _bubbleEditor.Text != (talk.text ?? ""))
			{
				_bubbleEditor.Text = talk.text ?? "";
			}
			if (GodotObject.IsInstanceValid(_npc.sprite) && !string.IsNullOrWhiteSpace(talk.anime))
			{
				_npc.sprite.SetAnimation(talk.anime);
			}
			if (applyProps && talk is NpcTalkHandConfig hand)
			{
				_npc.Hand(hand);
			}
		}
	}

	private void RebuildNpcOptions(string current)
	{
		_npcOption.Clear();
		string[] knownNpcKeys = KnownNpcKeys;
		foreach (string text in knownNpcKeys)
		{
			_npcOption.AddItem(text);
			_npcOption.SetItemMetadata(_npcOption.ItemCount - 1, text);
		}
		if (!string.IsNullOrWhiteSpace(current) && System.Array.IndexOf(KnownNpcKeys, current) < 0)
		{
			_npcOption.AddItem("自定义：" + current);
			_npcOption.SetItemMetadata(_npcOption.ItemCount - 1, current);
		}
		SelectOptionByMetadata(_npcOption, string.IsNullOrWhiteSpace(current) ? "CrazyDave" : current);
		_npcVisualGallery?.Rebuild();
	}

	private void RebuildAnimationOptions(string current)
	{
		_animeOption.Clear();
		_animeOption.AddItem("未设置");
		_animeOption.SetItemMetadata(0, "");
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (GodotObject.IsInstanceValid(_npc?.sprite?.flashAnimeData) && _npc.sprite.flashAnimeData.clips != null)
		{
			foreach (Variant key in _npc.sprite.flashAnimeData.clips.Keys)
			{
				string text = key.AsString();
				if (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
				{
					_animeOption.AddItem(text);
					_animeOption.SetItemMetadata(_animeOption.ItemCount - 1, text);
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(current) && hashSet.Add(current))
		{
			_animeOption.AddItem("自定义：" + current);
			_animeOption.SetItemMetadata(_animeOption.ItemCount - 1, current);
		}
		SelectAnimationOption(current);
		_animeVisualGallery?.Rebuild();
	}

	private void SelectAnimationOption(string current)
	{
		SelectOptionByMetadata(_animeOption, current ?? "");
		_animeVisualGallery?.RefreshSelection();
	}

	private static void SelectOptionByMetadata(OptionButton option, string value)
	{
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemMetadata(i).AsString() == value)
			{
				option.Select(i);
				return;
			}
		}
		if (option.ItemCount > 0)
		{
			option.Select(0);
		}
	}

	private void SetEntryPanelsVisible(NpcTalkBaseConfig talk)
	{
		_handPropsPanel.Visible = talk is NpcTalkHandConfig;
		_tutorialPanel.Visible = talk is NpcTalkTutorialConfig;
	}

	private void UpdateToolbarButtons(int count)
	{
		_previousButton.Disabled = _index <= 0;
		_nextButton.Disabled = _index >= count - 1;
		_duplicateButton.Disabled = _standaloneEntry;
		_removeButton.Disabled = _standaloneEntry;
		_moveUpButton.Disabled = _standaloneEntry || _index <= 0;
		_moveDownButton.Disabled = _standaloneEntry || _index >= count - 1;
	}

	private NpcTalkBaseConfig GetCurrentTalk()
	{
		int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
		if (_index < 0 || _index >= valueOrDefault)
		{
			return null;
		}
		return _config.talkList[_index];
	}

	private bool HasCurrentTalk()
	{
		return GodotObject.IsInstanceValid(GetCurrentTalk());
	}

	private void EmitEdited()
	{
		GetCurrentTalk()?.EmitChanged();
		_config?.EmitChanged();
		EmitSignal(SignalName.TalkEdited);
	}

	private void ClearNpc()
	{
		_bubbleEditor = null;
		if (GodotObject.IsInstanceValid(_npc))
		{
			Node parent = _npc.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(_npc);
			}
			_npc.QueueFree();
		}
		_npc = null;
		_npcSceneKey = "";
	}

	private static string BuildEntryLabel(int index, NpcTalkBaseConfig talk)
	{
		if (GodotObject.IsInstanceValid(talk))
		{
			string text = (talk.text ?? "").Replace('\n', ' ').Replace('\r', ' ').StripEdges();
			if (text.Length > 18)
			{
				text = text.Substring(0, 18) + "…";
			}
			return $"{index + 1}. {EntryTypeName(talk)} · {DisplayNpc(talk.npc)} · {text}";
		}
		return $"{index + 1}. 空条目";
	}

	private static string BuildStatus(NpcTalkBaseConfig talk, int count)
	{
		return $"{EntryTypeName(talk)} · {DisplayNpc(talk.npc)} · 点击气泡改文本 · 动画 {DisplayValue(talk.anime)} · 音频 {DisplayValue(talk.audio)} · {count} 条";
	}

	private static string EntryTypeName(NpcTalkBaseConfig talk)
	{
		if (!(talk is NpcTalkHandConfig))
		{
			if (!(talk is NpcTalkTutorialConfig))
			{
				if (talk is NpcTalkBuyConfig)
				{
					return "购买对话";
				}
				return "普通气泡";
			}
			return "教程衔接";
		}
		return "道具表演";
	}

	private static string ResourceLabel(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "未设置";
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

	private static string DisplayNpc(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "CrazyDave";
	}

	private static string DisplayValue(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未设置";
	}
}
