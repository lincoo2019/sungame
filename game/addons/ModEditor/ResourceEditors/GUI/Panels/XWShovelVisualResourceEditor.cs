using System;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWShovelVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string VisualEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWShovelVisualEditorLayout.tscn";

	private static PackedScene _visualEditorLayoutScene;

	private ShovelConfig _editingShovel;

	private XWVisualPropertyBinding _propertyBinding;

	private TextureRect _texturePreview;

	private Label _titleLabel;

	private Label _textSummaryLabel;

	private Label _listSummaryLabel;

	private Label _textureResourceLabel;

	private LineEdit _resourceName;

	private CheckButton _localToScene;

	private LineEdit _saveKey;

	private LineEdit _name;

	private TextEdit _describe;

	private TextEdit _handbookDescribe;

	private TextEdit _handbookStory;

	private XWResourcePicker _texturePicker;

	private XWResourcePicker _unlockPicker;

	private XWResourcePicker _eventPicker;

	private ItemList _unlockList;

	private ItemList _eventList;

	private ItemList _shovelableList;

	private LineEdit _shovelableNameEdit;

	private OptionButton _targetTypePreviewOption;

	private XWVisualSegmentedOption _targetTypeVisualChoices;

	private LineEdit _targetNamePreviewEdit;

	private CheckBox _targetHypnosesPreviewCheck;

	private CheckBox _targetHologramPreviewCheck;

	private Label _targetPreviewResultLabel;

	private Node2D _shovelTargetRoot;

	private bool _shovelPreviewAllowed;

	private int _selectedShovelableIndex = -1;

	private int _selectedUnlockIndex = -1;

	private int _selectedEventIndex = -1;

	private bool _updatingControls;

	public override void _ExitTree()
	{
		DisposeShovelBinding();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeShovelBinding();
		if (base.CurrentResource is ShovelConfig shovelConfig && base.CanvasGrid != null)
		{
			_editingShovel = shovelConfig;
			base.CanvasGrid.Columns = 1;
			if (_visualEditorLayoutScene == null)
			{
				_visualEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWShovelVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _visualEditorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindShovelWorkbench(vBoxContainer);
				BindShovelPropertyControls(shovelConfig);
				PopulateShovelControls();
				UpdateShovelPreview();
				AddSummaryRows(shovelConfig);
			}
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (resource?.GetType() != typeof(ShovelConfig))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private void DisposeShovelBinding()
	{
		_targetTypeVisualChoices?.Dispose();
		_targetTypeVisualChoices = null;
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_editingShovel = null;
	}

	private void BindShovelWorkbench(VBoxContainer root)
	{
		_texturePreview = root.GetNode<TextureRect>("%TexturePreview");
		_titleLabel = root.GetNode<Label>("%TitleLabel");
		_textSummaryLabel = root.GetNode<Label>("%TextSummaryLabel");
		_listSummaryLabel = root.GetNode<Label>("%ListSummaryLabel");
		_textureResourceLabel = root.GetNode<Label>("%TextureResourceLabel");
		_resourceName = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToScene = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_saveKey = root.GetNode<LineEdit>("%SaveKeyLineEdit");
		_name = root.GetNode<LineEdit>("%NameLineEdit");
		_describe = root.GetNode<TextEdit>("%DescribeTextEdit");
		_handbookDescribe = root.GetNode<TextEdit>("%HandbookDescribeTextEdit");
		_handbookStory = root.GetNode<TextEdit>("%HandbookStoryTextEdit");
		_unlockList = root.GetNode<ItemList>("%UnlockList");
		_eventList = root.GetNode<ItemList>("%EventList");
		_shovelableList = root.GetNode<ItemList>("%ShovelableList");
		_shovelableNameEdit = root.GetNode<LineEdit>("%ShovelableNameLineEdit");
		_texturePicker = CreateResourcePicker(root.GetNode<VBoxContainer>("%TexturePickerHost"), "Texture2D");
		_unlockPicker = CreateResourcePicker(root.GetNode<VBoxContainer>("%UnlockPickerHost"), "UnlockConditionBaseConfig");
		_eventPicker = CreateResourcePicker(root.GetNode<VBoxContainer>("%EventPickerHost"), "ShovelEventConfig");
		_texturePicker.ResourceChanged += OnTexturePicked;
		_unlockList.ItemSelected += delegate(long index)
		{
			SelectUnlockCondition((int)index);
		};
		_unlockList.ItemActivated += delegate(long index)
		{
			OpenShovelNestedResource(_editingShovel?.unlockCheckList, "unlockCheckList", (int)index);
		};
		_eventList.ItemSelected += delegate(long index)
		{
			SelectShovelEvent((int)index);
		};
		_eventList.ItemActivated += delegate(long index)
		{
			OpenShovelNestedResource(_editingShovel?.eventList, "eventList", (int)index);
		};
		_shovelableList.ItemSelected += delegate(long index)
		{
			SelectShovelableName((int)index);
		};
		root.GetNode<Button>("%AddShovelableButton").Pressed += AddShovelableName;
		root.GetNode<Button>("%RemoveShovelableButton").Pressed += RemoveSelectedShovelableName;
		root.GetNode<Button>("%AddUnlockButton").Pressed += AddUnlockCondition;
		root.GetNode<Button>("%ReplaceUnlockButton").Pressed += ReplaceSelectedUnlockCondition;
		root.GetNode<Button>("%RemoveUnlockButton").Pressed += RemoveSelectedUnlockCondition;
		root.GetNode<Button>("%OpenUnlockButton").Pressed += delegate
		{
			OpenShovelNestedResource(_editingShovel?.unlockCheckList, "unlockCheckList", _selectedUnlockIndex);
		};
		root.GetNode<Button>("%AddEventButton").Pressed += AddShovelEvent;
		root.GetNode<Button>("%ReplaceEventButton").Pressed += ReplaceSelectedShovelEvent;
		root.GetNode<Button>("%RemoveEventButton").Pressed += RemoveSelectedShovelEvent;
		root.GetNode<Button>("%OpenEventButton").Pressed += delegate
		{
			OpenShovelNestedResource(_editingShovel?.eventList, "eventList", _selectedEventIndex);
		};
		BindShovelTargetPreview(root.GetNode<Control>("%TargetPreview"));
	}

	private static XWResourcePicker CreateResourcePicker(VBoxContainer host, string baseType)
	{
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Setup(baseType);
		xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		host.AddChild(xWResourcePicker, forceReadableName: false, InternalMode.Disabled);
		return xWResourcePicker;
	}

	private void BindShovelPropertyControls(ShovelConfig shovel)
	{
		_propertyBinding?.Dispose();
		_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnShovelVisualPropertyEdited);
		_propertyBinding.BindText(_resourceName, shovel, "resource_name", UpdateShovelPreview, this, "RefreshShovelEditorFromHistory");
		_propertyBinding.BindToggle(_localToScene, shovel, "resource_local_to_scene", UpdateShovelPreview, this, "RefreshShovelEditorFromHistory");
		_propertyBinding.BindText(_saveKey, shovel, "saveKey", UpdateShovelPreview, this, "RefreshShovelEditorFromHistory");
		_propertyBinding.BindText(_name, shovel, "name", UpdateShovelPreview, this, "RefreshShovelEditorFromHistory");
		_propertyBinding.BindText(_describe, shovel, "describe", UpdateShovelPreview, this, "RefreshShovelEditorFromHistory");
		_propertyBinding.BindText(_handbookDescribe, shovel, "handbookDescribe", UpdateShovelPreview, this, "RefreshShovelEditorFromHistory");
		_propertyBinding.BindText(_handbookStory, shovel, "handbookStory", UpdateShovelPreview, this, "RefreshShovelEditorFromHistory");
	}

	private void OnShovelVisualPropertyEdited(bool committed)
	{
		if (base.CurrentResource is ShovelConfig shovelConfig && shovelConfig == _editingShovel)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
				return;
			}
			MarkCurrentResourceDirty();
			shovelConfig.EmitChanged();
		}
	}

	public void RefreshShovelEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && base.CurrentResource is ShovelConfig shovelConfig && shovelConfig == _editingShovel)
		{
			BindShovelPropertyControls(shovelConfig);
			PopulateShovelControls();
			UpdateShovelPreview();
		}
	}

	private void PopulateShovelControls()
	{
		if (!GodotObject.IsInstanceValid(_editingShovel))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			_texturePicker?.SetEditedResource(_editingShovel.texture);
			RefreshShovelLists(_editingShovel);
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void OnTexturePicked(Resource resource)
	{
		if (!_updatingControls && _propertyBinding != null && GodotObject.IsInstanceValid(_editingShovel))
		{
			_propertyBinding.SetValue(_editingShovel, "texture", Variant.From(in resource), "更换铲子图像", this, "RefreshShovelEditorFromHistory");
			UpdateShovelPreview();
		}
	}

	private void AddShovelableName()
	{
		if (!GodotObject.IsInstanceValid(_editingShovel) || _propertyBinding == null)
		{
			return;
		}
		string text = _shovelableNameEdit?.Text.StripEdges() ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			Array<string> array = CloneStringArray(_editingShovel.shovelableNames);
			if (array.Contains(text))
			{
				_selectedShovelableIndex = array.IndexOf(text);
				RefreshShovelLists(_editingShovel);
				return;
			}
			array.Add(text);
			_selectedShovelableIndex = array.Count - 1;
			_propertyBinding.SetValue(_editingShovel, "shovelableNames", array, "添加可铲目标", this, "RefreshShovelEditorFromHistory");
			RefreshAfterListEdit();
		}
	}

	private void RemoveSelectedShovelableName()
	{
		if (GodotObject.IsInstanceValid(_editingShovel) && _propertyBinding != null && _editingShovel.shovelableNames != null && _selectedShovelableIndex >= 0 && _selectedShovelableIndex < _editingShovel.shovelableNames.Count)
		{
			Array<string> array = CloneStringArray(_editingShovel.shovelableNames);
			array.RemoveAt(_selectedShovelableIndex);
			_selectedShovelableIndex = Math.Min(_selectedShovelableIndex, array.Count - 1);
			_propertyBinding.SetValue(_editingShovel, "shovelableNames", array, "删除可铲目标", this, "RefreshShovelEditorFromHistory");
			RefreshAfterListEdit();
		}
	}

	private void SelectShovelableName(int index)
	{
		_selectedShovelableIndex = index;
		if (_editingShovel?.shovelableNames != null && index >= 0 && index < _editingShovel.shovelableNames.Count)
		{
			_shovelableNameEdit.Text = _editingShovel.shovelableNames[index];
		}
	}

	private void SelectUnlockCondition(int index)
	{
		_selectedUnlockIndex = index;
		if (_editingShovel?.unlockCheckList != null && index >= 0 && index < _editingShovel.unlockCheckList.Count)
		{
			_unlockPicker.SetEditedResource(_editingShovel.unlockCheckList[index]);
		}
	}

	private void AddUnlockCondition()
	{
		if (_unlockPicker?.GetEditedResource() is UnlockConditionBaseConfig item && GodotObject.IsInstanceValid(_editingShovel) && _propertyBinding != null)
		{
			Array<UnlockConditionBaseConfig> array = CloneUnlockArray(_editingShovel.unlockCheckList);
			array.Add(item);
			_selectedUnlockIndex = array.Count - 1;
			_propertyBinding.SetValue(_editingShovel, "unlockCheckList", array, "添加铲子解锁条件", this, "RefreshShovelEditorFromHistory");
			RefreshAfterListEdit();
		}
	}

	private void ReplaceSelectedUnlockCondition()
	{
		if (_unlockPicker?.GetEditedResource() is UnlockConditionBaseConfig value && GodotObject.IsInstanceValid(_editingShovel) && _propertyBinding != null && _editingShovel.unlockCheckList != null && _selectedUnlockIndex >= 0 && _selectedUnlockIndex < _editingShovel.unlockCheckList.Count)
		{
			Array<UnlockConditionBaseConfig> array = CloneUnlockArray(_editingShovel.unlockCheckList);
			array[_selectedUnlockIndex] = value;
			_propertyBinding.SetValue(_editingShovel, "unlockCheckList", array, "替换铲子解锁条件", this, "RefreshShovelEditorFromHistory");
			RefreshAfterListEdit();
		}
	}

	private void RemoveSelectedUnlockCondition()
	{
		if (GodotObject.IsInstanceValid(_editingShovel) && _propertyBinding != null && _editingShovel.unlockCheckList != null && _selectedUnlockIndex >= 0 && _selectedUnlockIndex < _editingShovel.unlockCheckList.Count)
		{
			Array<UnlockConditionBaseConfig> array = CloneUnlockArray(_editingShovel.unlockCheckList);
			array.RemoveAt(_selectedUnlockIndex);
			_selectedUnlockIndex = Math.Min(_selectedUnlockIndex, array.Count - 1);
			_propertyBinding.SetValue(_editingShovel, "unlockCheckList", array, "删除铲子解锁条件", this, "RefreshShovelEditorFromHistory");
			RefreshAfterListEdit();
		}
	}

	private void SelectShovelEvent(int index)
	{
		_selectedEventIndex = index;
		if (_editingShovel?.eventList != null && index >= 0 && index < _editingShovel.eventList.Count)
		{
			_eventPicker.SetEditedResource(_editingShovel.eventList[index]);
		}
	}

	private void AddShovelEvent()
	{
		if (_eventPicker?.GetEditedResource() is ShovelEventConfig item && GodotObject.IsInstanceValid(_editingShovel) && _propertyBinding != null)
		{
			Array<ShovelEventConfig> array = CloneEventArray(_editingShovel.eventList);
			array.Add(item);
			_selectedEventIndex = array.Count - 1;
			_propertyBinding.SetValue(_editingShovel, "eventList", array, "添加铲子事件", this, "RefreshShovelEditorFromHistory");
			RefreshAfterListEdit();
		}
	}

	private void ReplaceSelectedShovelEvent()
	{
		if (_eventPicker?.GetEditedResource() is ShovelEventConfig value && GodotObject.IsInstanceValid(_editingShovel) && _propertyBinding != null && _editingShovel.eventList != null && _selectedEventIndex >= 0 && _selectedEventIndex < _editingShovel.eventList.Count)
		{
			Array<ShovelEventConfig> array = CloneEventArray(_editingShovel.eventList);
			array[_selectedEventIndex] = value;
			_propertyBinding.SetValue(_editingShovel, "eventList", array, "替换铲子事件", this, "RefreshShovelEditorFromHistory");
			RefreshAfterListEdit();
		}
	}

	private void RemoveSelectedShovelEvent()
	{
		if (GodotObject.IsInstanceValid(_editingShovel) && _propertyBinding != null && _editingShovel.eventList != null && _selectedEventIndex >= 0 && _selectedEventIndex < _editingShovel.eventList.Count)
		{
			Array<ShovelEventConfig> array = CloneEventArray(_editingShovel.eventList);
			array.RemoveAt(_selectedEventIndex);
			_selectedEventIndex = Math.Min(_selectedEventIndex, array.Count - 1);
			_propertyBinding.SetValue(_editingShovel, "eventList", array, "删除铲子事件", this, "RefreshShovelEditorFromHistory");
			RefreshAfterListEdit();
		}
	}

	private void RefreshAfterListEdit()
	{
		RefreshShovelLists(_editingShovel);
		UpdateShovelPreview();
		RebuildShovelGameTarget();
	}

	private void OpenShovelNestedResource<[MustBeVariant] T>(Array<T> array, string propertyName, int index) where T : Resource
	{
		if (array != null && index >= 0 && index < array.Count)
		{
			Resource resource = array[index];
			if (resource != null && GodotObject.IsInstanceValid(resource))
			{
				XWResourceEditContext context = XWResourceEditContext.ForProperty(resource, base.CurrentResource, resource.ResourcePath, base.CurrentResourcePath, propertyName, index, "shovel_editor", base.CurrentEditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(base.CurrentResourcePath));
				XWEditorInterface.Instance?.EditResource(resource, context);
			}
		}
	}

	private void BindShovelTargetPreview(Control panel)
	{
		_targetTypePreviewOption = panel.GetNode<OptionButton>("Layout/Inputs/TargetType");
		_targetTypeVisualChoices = new XWVisualSegmentedOption(_targetTypePreviewOption, panel.GetNode<HFlowContainer>("%TargetTypeVisualChoices"));
		_targetTypeVisualChoices.Rebuild();
		_targetNamePreviewEdit = panel.GetNode<LineEdit>("Layout/Inputs/TargetName");
		_targetHypnosesPreviewCheck = panel.GetNode<CheckBox>("Layout/Inputs/Hypnoses");
		_targetHologramPreviewCheck = panel.GetNode<CheckBox>("Layout/Inputs/Hologram");
		_targetPreviewResultLabel = panel.GetNode<Label>("Layout/Result");
		_shovelTargetRoot = panel.GetNode<Node2D>("%TargetRoot");
		panel.GetNode<Button>("Layout/UseShovelButton").Pressed += UseShovelOnPreviewTarget;
		_targetTypePreviewOption.ItemSelected += delegate
		{
			RefreshShovelTargetPreview();
			RebuildShovelGameTarget();
		};
		_targetNamePreviewEdit.TextChanged += delegate
		{
			RefreshShovelTargetPreview();
			RebuildShovelGameTarget();
		};
		_targetHypnosesPreviewCheck.Toggled += delegate
		{
			RefreshShovelTargetPreview();
			RebuildShovelGameTarget();
		};
		_targetHologramPreviewCheck.Toggled += delegate
		{
			RefreshShovelTargetPreview();
			RebuildShovelGameTarget();
		};
		RefreshShovelTargetPreview();
		RebuildShovelGameTarget();
	}

	private void RefreshShovelTargetPreview()
	{
		if (GodotObject.IsInstanceValid(_targetPreviewResultLabel) && _editingShovel != null)
		{
			int num = (GodotObject.IsInstanceValid(_targetTypePreviewOption) ? _targetTypePreviewOption.Selected : 0);
			string text = (GodotObject.IsInstanceValid(_targetNamePreviewEdit) ? _targetNamePreviewEdit.Text.StripEdges() : "");
			bool flag = GodotObject.IsInstanceValid(_targetHypnosesPreviewCheck) && _targetHypnosesPreviewCheck.ButtonPressed;
			bool flag2 = GodotObject.IsInstanceValid(_targetHologramPreviewCheck) && _targetHologramPreviewCheck.ButtonPressed;
			bool flag3;
			string text2;
			switch (num)
			{
			case 1:
				flag3 = !flag && !flag2;
				text2 = (flag3 ? "道具护盾默认可铲" : "魅惑或全息护盾不可铲");
				break;
			case 2:
				flag3 = false;
				text2 = "普通道具在正式关卡中不可铲";
				break;
			case 3:
				flag3 = !string.IsNullOrWhiteSpace(text) && _editingShovel.shovelableNames != null && _editingShovel.shovelableNames.Contains(text);
				text2 = (flag3 ? "名称命中 shovelableNames" : "墓碑或弹坑名称未命中 shovelableNames");
				break;
			default:
				flag3 = !flag && !flag2;
				text2 = (flag3 ? "普通角色默认可铲" : "魅惑或全息角色不可铲");
				break;
			}
			_targetPreviewResultLabel.Text = (flag3 ? ("✓ 可铲 · " + text2) : ("✗ 不可铲 · " + text2));
			_targetPreviewResultLabel.Modulate = (flag3 ? new Color(0.48f, 0.92f, 0.55f) : new Color(1f, 0.52f, 0.44f));
			_shovelPreviewAllowed = flag3;
		}
	}

	private void RebuildShovelGameTarget()
	{
		if (!GodotObject.IsInstanceValid(_shovelTargetRoot))
		{
			return;
		}
		foreach (Node child in _shovelTargetRoot.GetChildren())
		{
			child.QueueFree();
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>((!GodotObject.IsInstanceValid(_targetTypePreviewOption) || _targetTypePreviewOption.Selected == 0) ? "res://Asset/Anime/Character/Plant/Chapter0/Blover/Scene/TowerDefensePlantBlover.tscn" : "res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn", null, ResourceLoader.CacheMode.Reuse);
		if (GodotObject.IsInstanceValid(packedScene))
		{
			Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node is TowerDefenseCharacter towerDefenseCharacter)
			{
				towerDefenseCharacter.inGame = false;
				towerDefenseCharacter.editorPreviewMode = true;
				towerDefenseCharacter.Modulate = (_shovelPreviewAllowed ? Colors.White : new Color(1f, 0.55f, 0.55f));
			}
			_shovelTargetRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void UseShovelOnPreviewTarget()
	{
		if (GodotObject.IsInstanceValid(_shovelTargetRoot) && _shovelTargetRoot.GetChildCount() != 0 && _shovelTargetRoot.GetChild(0) is CanvasItem canvasItem)
		{
			Tween tween = canvasItem.CreateTween();
			if (_shovelPreviewAllowed)
			{
				tween.SetParallel();
				tween.TweenProperty(canvasItem, "position:y", -120f, 0.35);
				tween.TweenProperty(canvasItem, "rotation", -1.4f, 0.35);
				tween.TweenProperty(canvasItem, "modulate:a", 0f, 0.35);
				tween.SetParallel(parallel: false);
				tween.TweenCallback(Callable.From(RebuildShovelGameTarget));
			}
			else
			{
				tween.TweenProperty(canvasItem, "position:x", -12f, 0.06);
				tween.TweenProperty(canvasItem, "position:x", 12f, 0.06);
				tween.TweenProperty(canvasItem, "position:x", 0f, 0.06);
			}
		}
	}

	private void UpdateShovelPreview()
	{
		if (_editingShovel != null)
		{
			if (GodotObject.IsInstanceValid(_texturePreview))
			{
				_texturePreview.Texture = _editingShovel.texture;
			}
			if (GodotObject.IsInstanceValid(_titleLabel))
			{
				_titleLabel.Text = "铲子：" + EmptyToPlaceholder(_editingShovel.saveKey);
			}
			if (GodotObject.IsInstanceValid(_textSummaryLabel))
			{
				_textSummaryLabel.Text = "文本：" + EmptyToPlaceholder(_editingShovel.name) + " / " + EmptyToPlaceholder(_editingShovel.describe);
			}
			if (GodotObject.IsInstanceValid(_listSummaryLabel))
			{
				_listSummaryLabel.Text = "可铲 / 事件：" + BuildListSummary(_editingShovel);
			}
			if (GodotObject.IsInstanceValid(_textureResourceLabel))
			{
				_textureResourceLabel.Text = "texture：" + FormatResource(_editingShovel.texture);
			}
			RefreshShovelTargetPreview();
		}
	}

	private void RefreshShovelLists(ShovelConfig shovel)
	{
		PopulateStringList(_shovelableList, shovel.shovelableNames);
		PopulateResourceList(_unlockList, shovel.unlockCheckList);
		PopulateResourceList(_eventList, shovel.eventList);
		RestoreSelection(_shovelableList, _selectedShovelableIndex, shovel.shovelableNames?.Count ?? 0);
		RestoreSelection(_unlockList, _selectedUnlockIndex, shovel.unlockCheckList?.Count ?? 0);
		RestoreSelection(_eventList, _selectedEventIndex, shovel.eventList?.Count ?? 0);
	}

	private void AddSummaryRows(ShovelConfig shovel)
	{
		AddItemIfMissing(base.PreviewList, "铲子 saveKey → " + EmptyToPlaceholder(shovel.saveKey));
		AddItemIfMissing(base.PreviewList, "texture → " + FormatResource(shovel.texture));
		AddItemIfMissing(base.PreviewList, $"shovelableNames → {shovel.shovelableNames?.Count ?? 0}");
		AddItemIfMissing(base.TimelineList, $"unlockCheckList → {shovel.unlockCheckList?.Count ?? 0}");
		AddItemIfMissing(base.TimelineList, $"eventList → {shovel.eventList?.Count ?? 0}");
		AddItemIfMissing(base.GraphList, "铲子 → texture → shovelableNames → unlockCheckList → eventList");
		AddItemIfMissing(base.ReferenceList, "texture → " + FormatResource(shovel.texture));
	}

	private static Array<string> CloneStringArray(Array<string> source)
	{
		Array<string> array = new Array<string>();
		if (source != null)
		{
			foreach (string item in source)
			{
				array.Add(item);
			}
		}
		return array;
	}

	private static Array<UnlockConditionBaseConfig> CloneUnlockArray(Array<UnlockConditionBaseConfig> source)
	{
		Array<UnlockConditionBaseConfig> array = new Array<UnlockConditionBaseConfig>();
		if (source != null)
		{
			foreach (UnlockConditionBaseConfig item in source)
			{
				array.Add(item);
			}
		}
		return array;
	}

	private static Array<ShovelEventConfig> CloneEventArray(Array<ShovelEventConfig> source)
	{
		Array<ShovelEventConfig> array = new Array<ShovelEventConfig>();
		if (source != null)
		{
			foreach (ShovelEventConfig item in source)
			{
				array.Add(item);
			}
		}
		return array;
	}

	private static void RestoreSelection(ItemList list, int index, int count)
	{
		if (GodotObject.IsInstanceValid(list) && index >= 0 && index < count)
		{
			list.Select(index);
		}
	}

	private static void PopulateStringList(ItemList list, Array<string> array)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.Clear();
		if (array == null || array.Count == 0)
		{
			list.AddItem("尚未配置");
			return;
		}
		for (int i = 0; i < array.Count; i++)
		{
			list.AddItem($"{i}: {array[i]}");
		}
	}

	private static void PopulateResourceList<[MustBeVariant] T>(ItemList list, Array<T> array)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.Clear();
		if (array == null || array.Count == 0)
		{
			list.AddItem("尚未配置");
			return;
		}
		for (int i = 0; i < array.Count; i++)
		{
			list.AddItem($"{i}: {FormatObject(array[i])}");
		}
	}

	private static string BuildListSummary(ShovelConfig shovel)
	{
		return $"shovelableNames={shovel.shovelableNames?.Count ?? 0}, unlockCheckList={shovel.unlockCheckList?.Count ?? 0}, eventList={shovel.eventList?.Count ?? 0}";
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

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未配置";
	}
}
