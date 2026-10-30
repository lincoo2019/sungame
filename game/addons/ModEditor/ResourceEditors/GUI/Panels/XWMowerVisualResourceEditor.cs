using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWMowerVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string VisualEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWMowerVisualEditorLayout.tscn";

	private static PackedScene _visualEditorLayoutScene;

	private MowerConfig _editingMower;

	private XWVisualPropertyBinding _mowerPropertyBinding;

	private TextureRect _texturePreview;

	private SubViewport _mowerPreviewViewport;

	private Node2D _mowerPreviewRoot;

	private Node2D _mowerTargetRoot;

	private readonly List<Node2D> _mowerTargets = new List<Node2D>();

	private double _mowerRunProgress;

	private Label _mowerSceneStatusLabel;

	private Button _mowerSceneRunButton;

	private float _mowerScenePreviewScale = 1f;

	private bool _mowerScenePreviewRunning;

	private Label _titleLabel;

	private Label _spriteLabel;

	private Label _textSummaryLabel;

	private Label _unlockSummaryLabel;

	private ItemList _unlockList;

	private ItemList _eventList;

	private XWResourcePicker _unlockPicker;

	private XWResourcePicker _eventPicker;

	private int _selectedUnlockIndex = -1;

	private int _selectedEventIndex = -1;

	private Timer _mowerSaveTimer;

	public override void _Ready()
	{
		base._Ready();
		_mowerSaveTimer = new Timer
		{
			WaitTime = 0.35,
			OneShot = true
		};
		_mowerSaveTimer.Timeout += SavePendingMowerResource;
		AddChild(_mowerSaveTimer, forceReadableName: false, InternalMode.Disabled);
		EnableVisibilityGatedProcessing();
	}

	public override void _ExitTree()
	{
		DisposeMowerEditorBindings();
		if (GodotObject.IsInstanceValid(_mowerSaveTimer))
		{
			_mowerSaveTimer.Stop();
		}
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		SetMowerScenePreviewRunning(running: false);
		DisposeMowerEditorBindings();
		if (base.CurrentResource is MowerConfig mowerConfig)
		{
			_editingMower = mowerConfig;
			RenderMowerEditor(mowerConfig);
		}
	}

	private void DisposeMowerEditorBindings()
	{
		_mowerPropertyBinding?.Dispose();
		_mowerPropertyBinding = null;
		_editingMower = null;
		_unlockPicker = null;
		_eventPicker = null;
		_mowerPreviewViewport = null;
		_mowerPreviewRoot = null;
		_mowerTargetRoot = null;
		_mowerTargets.Clear();
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (resource?.GetType() != typeof(MowerConfig))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private void RenderMowerEditor(MowerConfig mower)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = 1;
			if (_visualEditorLayoutScene == null)
			{
				_visualEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWMowerVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _visualEditorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindMowerPreviewPanel(vBoxContainer, mower);
				BindMowerDirectEditControls(vBoxContainer, mower);
				BindMowerCollectionEditors(vBoxContainer, mower);
				AddSummaryRows(mower);
			}
		}
	}

	private void BindMowerPreviewPanel(VBoxContainer root, MowerConfig mower)
	{
		_texturePreview = root.GetNode<TextureRect>("%TexturePreview");
		_titleLabel = root.GetNode<Label>("%TitleLabel");
		_spriteLabel = root.GetNode<Label>("%SpriteLabel");
		_textSummaryLabel = root.GetNode<Label>("%TextSummaryLabel");
		_unlockSummaryLabel = root.GetNode<Label>("%UnlockSummaryLabel");
		BindMowerScenePreview(root.GetNode<Control>("%ScenePreview"), mower);
		UpdateMowerPreview();
	}

	private void BindMowerScenePreview(Control frame, MowerConfig mower)
	{
		_mowerSceneStatusLabel = frame.GetNode<Label>("Layout/Toolbar/Status");
		_mowerSceneRunButton = frame.GetNode<Button>("Layout/Toolbar/RunButton");
		_mowerPreviewViewport = frame.GetNode<SubViewport>("Layout/ViewportContainer/Viewport");
		_mowerPreviewRoot = frame.GetNode<Node2D>("Layout/ViewportContainer/Viewport/PreviewRoot");
		_mowerTargetRoot = frame.GetNode<Node2D>("%TargetRoot");
		frame.GetNode<Button>("Layout/Toolbar/ReloadButton").Pressed += RebuildMowerScenePreview;
		_mowerSceneRunButton.Pressed += StartMowerGamePreview;
		HSlider node = frame.GetNode<HSlider>("Layout/ZoomSlider");
		node.Value = _mowerScenePreviewScale;
		node.ValueChanged += delegate(double value)
		{
			_mowerScenePreviewScale = (float)value;
			ApplyMowerScenePreviewTransform();
		};
		RebuildMowerTargets();
		RebuildMowerScenePreview();
	}

	private void RebuildMowerScenePreview()
	{
		if (!GodotObject.IsInstanceValid(_mowerPreviewRoot) || _editingMower == null)
		{
			return;
		}
		foreach (Node child in _mowerPreviewRoot.GetChildren())
		{
			child.QueueFree();
		}
		Node node = null;
		if (GodotObject.IsInstanceValid(_editingMower.sprite))
		{
			try
			{
				node = _editingMower.sprite.Instantiate(PackedScene.GenEditState.Disabled);
				node.ProcessMode = (ProcessModeEnum)(_mowerScenePreviewRunning ? 0 : 4);
			}
			catch (Exception ex)
			{
				GD.PushWarning("Mower editor scene preview failed: " + ex.Message);
				if (GodotObject.IsInstanceValid(_mowerSceneStatusLabel))
				{
					_mowerSceneStatusLabel.Text = "场景实例化失败: " + ex.Message;
				}
			}
		}
		if (node != null)
		{
			node.Name = "ConfiguredMowerScenePreview";
			_mowerPreviewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(_mowerSceneStatusLabel))
			{
				_mowerSceneStatusLabel.Text = "场景实例: " + FormatResource(_editingMower.sprite);
			}
		}
		else if (GodotObject.IsInstanceValid(_editingMower.texture))
		{
			_mowerPreviewRoot.AddChild(new Sprite2D
			{
				Name = "MowerTextureFallback",
				Texture = _editingMower.texture
			}, forceReadableName: false, InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(_mowerSceneStatusLabel))
			{
				_mowerSceneStatusLabel.Text = "未配置 sprite，使用 texture 预览";
			}
		}
		else if (GodotObject.IsInstanceValid(_mowerSceneStatusLabel))
		{
			_mowerSceneStatusLabel.Text = "未配置可预览的 sprite 或 texture";
		}
		ApplyMowerScenePreviewTransform();
		SetMowerScenePreviewRunning(_mowerScenePreviewRunning);
	}

	private void ApplyMowerScenePreviewTransform()
	{
		if (GodotObject.IsInstanceValid(_mowerPreviewRoot))
		{
			_mowerPreviewRoot.Position = new Vector2(65f + 570f * (float)_mowerRunProgress, 205f);
			_mowerPreviewRoot.Scale = Vector2.One * _mowerScenePreviewScale;
		}
	}

	private void SetMowerScenePreviewRunning(bool running)
	{
		_mowerScenePreviewRunning = running;
		RequestVisibilityGatedProcessing(running);
		ApplyMowerRuntimeProcessMode(IsVisibleInTree());
		if (GodotObject.IsInstanceValid(_mowerSceneRunButton))
		{
			_mowerSceneRunButton.Text = (running ? "运行中…" : "重新启动");
		}
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		ApplyMowerRuntimeProcessMode(visible);
	}

	private void ApplyMowerRuntimeProcessMode(bool visible)
	{
		ProcessModeEnum processMode = (ProcessModeEnum)((visible && _mowerScenePreviewRunning) ? 0 : 4);
		if (GodotObject.IsInstanceValid(_mowerPreviewRoot))
		{
			_mowerPreviewRoot.ProcessMode = processMode;
		}
		if (GodotObject.IsInstanceValid(_mowerTargetRoot))
		{
			_mowerTargetRoot.ProcessMode = processMode;
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_mowerScenePreviewRunning)
		{
			_mowerRunProgress = Mathf.Clamp(_mowerRunProgress + delta / 2.25, 0.0, 1.0);
			ApplyMowerScenePreviewTransform();
			UpdateMowerTargetHits();
			if (_mowerRunProgress >= 1.0)
			{
				SetMowerScenePreviewRunning(running: false);
			}
		}
	}

	private void StartMowerGamePreview()
	{
		_mowerRunProgress = 0.0;
		RebuildMowerTargets();
		ApplyMowerScenePreviewTransform();
		SetMowerScenePreviewRunning(running: true);
	}

	private void RebuildMowerTargets()
	{
		if (!GodotObject.IsInstanceValid(_mowerTargetRoot))
		{
			return;
		}
		foreach (Node child in _mowerTargetRoot.GetChildren())
		{
			child.QueueFree();
		}
		_mowerTargets.Clear();
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn", null, ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return;
		}
		float[] array = new float[3] { 260f, 410f, 550f };
		foreach (float x in array)
		{
			if (packedScene.Instantiate(PackedScene.GenEditState.Disabled) is TowerDefenseCharacter towerDefenseCharacter)
			{
				towerDefenseCharacter.inGame = false;
				towerDefenseCharacter.editorPreviewMode = true;
				towerDefenseCharacter.Position = new Vector2(x, 205f);
				_mowerTargetRoot.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
				_mowerTargets.Add(towerDefenseCharacter);
			}
		}
	}

	private void UpdateMowerTargetHits()
	{
		float num = 65f + 570f * (float)_mowerRunProgress;
		foreach (Node2D mowerTarget in _mowerTargets)
		{
			if (GodotObject.IsInstanceValid(mowerTarget) && !(num < mowerTarget.Position.X - 28f))
			{
				float num2 = Mathf.Clamp((num - mowerTarget.Position.X) / 100f, 0f, 1f);
				mowerTarget.Rotation = (0f - num2) * 1.2f;
				mowerTarget.Position = new Vector2(mowerTarget.Position.X + num2 * 3f, 205f - num2 * 145f);
				mowerTarget.Modulate = new Color(1f, 1f, 1f, 1f - num2);
			}
		}
	}

	private void BindMowerDirectEditControls(VBoxContainer root, MowerConfig mower)
	{
		_mowerPropertyBinding?.Dispose();
		_mowerPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnMowerPropertyEdited);
		_mowerPropertyBinding.BindText(root.GetNode<LineEdit>("%ResourceNameEdit"), mower, "resource_name", UpdateMowerPreview, this, "RefreshMowerEditorFromHistory");
		_mowerPropertyBinding.BindToggle(root.GetNode<CheckButton>("%LocalToSceneCheck"), mower, "resource_local_to_scene", UpdateMowerPreview, this, "RefreshMowerEditorFromHistory");
		_mowerPropertyBinding.BindText(root.GetNode<LineEdit>("%SaveKeyLineEdit"), mower, "saveKey", UpdateMowerPreview, this, "RefreshMowerEditorFromHistory");
		_mowerPropertyBinding.BindText(root.GetNode<LineEdit>("%NameLineEdit"), mower, "name", UpdateMowerPreview, this, "RefreshMowerEditorFromHistory");
		_mowerPropertyBinding.BindText(root.GetNode<TextEdit>("%DescribeTextEdit"), mower, "describe", UpdateMowerPreview, this, "RefreshMowerEditorFromHistory");
		_mowerPropertyBinding.BindText(root.GetNode<TextEdit>("%HandbookDescribeTextEdit"), mower, "handbookDescribe", UpdateMowerPreview, this, "RefreshMowerEditorFromHistory");
		_mowerPropertyBinding.BindText(root.GetNode<TextEdit>("%HandbookStoryTextEdit"), mower, "handbookStory", UpdateMowerPreview, this, "RefreshMowerEditorFromHistory");
		MountMowerResourcePicker(root.GetNode<HBoxContainer>("%TexturePickerHost"), "TexturePicker", "Texture2D", mower.texture, delegate(Resource resource)
		{
			SetMowerResource("texture", resource, "更换小推车贴图");
		});
		MountMowerResourcePicker(root.GetNode<HBoxContainer>("%SpritePickerHost"), "SpritePicker", "PackedScene", mower.sprite, delegate(Resource resource)
		{
			SetMowerResource("sprite", resource, "更换小推车场景");
		});
	}

	private void BindMowerCollectionEditors(VBoxContainer root, MowerConfig mower)
	{
		_unlockList = root.GetNode<ItemList>("%UnlockList");
		_unlockList.ItemSelected += delegate(long index)
		{
			SelectMowerUnlockCondition((int)index);
		};
		_unlockList.ItemActivated += delegate(long index)
		{
			OpenMowerNestedResource(mower.unlockCheckList, "unlockCheckList", (int)index);
		};
		_unlockPicker = MountMowerResourcePicker(root.GetNode<HBoxContainer>("%UnlockPickerHost"), "UnlockConditionPicker", "UnlockConditionBaseConfig", null, null);
		root.GetNode<Button>("%UnlockAddButton").Pressed += AddMowerUnlockCondition;
		root.GetNode<Button>("%UnlockReplaceButton").Pressed += ReplaceSelectedMowerUnlockCondition;
		root.GetNode<Button>("%UnlockRemoveButton").Pressed += RemoveSelectedMowerUnlockCondition;
		root.GetNode<Button>("%UnlockOpenButton").Pressed += OpenSelectedMowerUnlockCondition;
		root.GetNode<Button>("%UnlockMoveUpButton").Pressed += delegate
		{
			MoveSelectedMowerUnlockCondition(-1);
		};
		root.GetNode<Button>("%UnlockMoveDownButton").Pressed += delegate
		{
			MoveSelectedMowerUnlockCondition(1);
		};
		_eventList = root.GetNode<ItemList>("%EventList");
		_eventList.ItemSelected += delegate(long index)
		{
			SelectMowerEvent((int)index);
		};
		_eventList.ItemActivated += delegate(long index)
		{
			OpenMowerNestedResource(mower.eventList, "eventList", (int)index);
		};
		_eventPicker = MountMowerResourcePicker(root.GetNode<HBoxContainer>("%EventPickerHost"), "MowerEventPicker", "MowerEventConfig", null, null);
		root.GetNode<Button>("%EventAddButton").Pressed += AddMowerEvent;
		root.GetNode<Button>("%EventReplaceButton").Pressed += ReplaceSelectedMowerEvent;
		root.GetNode<Button>("%EventRemoveButton").Pressed += RemoveSelectedMowerEvent;
		root.GetNode<Button>("%EventOpenButton").Pressed += OpenSelectedMowerEvent;
		root.GetNode<Button>("%EventMoveUpButton").Pressed += delegate
		{
			MoveSelectedMowerEvent(-1);
		};
		root.GetNode<Button>("%EventMoveDownButton").Pressed += delegate
		{
			MoveSelectedMowerEvent(1);
		};
		RefreshMowerLists(mower);
	}

	private static XWResourcePicker MountMowerResourcePicker(HBoxContainer host, string nodeName, string baseType, Resource value, Action<Resource> changed)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return null;
		}
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Name = nodeName;
		xWResourcePicker.Setup(baseType);
		xWResourcePicker.SetEditedResource(value);
		xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		if (changed != null)
		{
			xWResourcePicker.ResourceChanged += delegate(Resource resource)
			{
				changed(resource);
			};
		}
		host.AddChild(xWResourcePicker, forceReadableName: false, InternalMode.Disabled);
		return xWResourcePicker;
	}

	private void SetMowerResource(string propertyName, Resource resource, string actionName)
	{
		if (_mowerPropertyBinding != null && base.CurrentResource is MowerConfig mowerConfig && mowerConfig == _editingMower)
		{
			Variant value = ((propertyName == "texture") ? Variant.From<Texture2D>(resource as Texture2D) : Variant.From<PackedScene>(resource as PackedScene));
			_mowerPropertyBinding.SetValue(mowerConfig, propertyName, value, actionName, this, "RefreshMowerEditorFromHistory");
			mowerConfig.NotifyPropertyListChanged();
			UpdateMowerPreview();
			RebuildMowerScenePreview();
		}
	}

	private void OpenMowerNestedResource<[MustBeVariant] T>(Array<T> array, string propertyName, int index) where T : Resource
	{
		if (array != null && index >= 0 && index < array.Count)
		{
			Resource resource = array[index];
			if (resource != null && GodotObject.IsInstanceValid(resource))
			{
				XWResourceEditContext context = XWResourceEditContext.ForProperty(resource, base.CurrentResource, resource.ResourcePath, base.CurrentResourcePath, propertyName, index, "mower_editor", base.CurrentEditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(base.CurrentResourcePath));
				XWEditorInterface.Instance?.EditResource(resource, context);
			}
		}
	}

	private void SelectMowerUnlockCondition(int index)
	{
		if (_editingMower?.unlockCheckList != null && index >= 0 && index < _editingMower.unlockCheckList.Count)
		{
			_selectedUnlockIndex = index;
			_unlockPicker?.SetEditedResource(_editingMower.unlockCheckList[index]);
		}
	}

	private void SelectMowerEvent(int index)
	{
		if (_editingMower?.eventList != null && index >= 0 && index < _editingMower.eventList.Count)
		{
			_selectedEventIndex = index;
			_eventPicker?.SetEditedResource(_editingMower.eventList[index]);
		}
	}

	private void AddMowerUnlockCondition()
	{
		if (base.CurrentResource is MowerConfig mowerConfig && _unlockPicker?.EditedResource is UnlockConditionBaseConfig unlockConditionBaseConfig && GodotObject.IsInstanceValid(unlockConditionBaseConfig))
		{
			Array<UnlockConditionBaseConfig> array = ((mowerConfig.unlockCheckList == null) ? new Array<UnlockConditionBaseConfig>() : new Array<UnlockConditionBaseConfig>(mowerConfig.unlockCheckList));
			array.Add(unlockConditionBaseConfig);
			_selectedUnlockIndex = array.Count - 1;
			ReplaceMowerUnlockConditions(array, "添加小推车解锁条件");
		}
	}

	private void ReplaceSelectedMowerUnlockCondition()
	{
		if (base.CurrentResource is MowerConfig mowerConfig && _unlockPicker?.EditedResource is UnlockConditionBaseConfig unlockConditionBaseConfig && GodotObject.IsInstanceValid(unlockConditionBaseConfig) && mowerConfig.unlockCheckList != null && _selectedUnlockIndex >= 0 && _selectedUnlockIndex < mowerConfig.unlockCheckList.Count)
		{
			Array<UnlockConditionBaseConfig> array = new Array<UnlockConditionBaseConfig>(mowerConfig.unlockCheckList);
			array[_selectedUnlockIndex] = unlockConditionBaseConfig;
			ReplaceMowerUnlockConditions(array, "替换小推车解锁条件");
		}
	}

	private void RemoveSelectedMowerUnlockCondition()
	{
		if (_editingMower?.unlockCheckList != null && _selectedUnlockIndex >= 0 && _selectedUnlockIndex < _editingMower.unlockCheckList.Count)
		{
			Array<UnlockConditionBaseConfig> array = new Array<UnlockConditionBaseConfig>(_editingMower.unlockCheckList);
			array.RemoveAt(_selectedUnlockIndex);
			_selectedUnlockIndex = ((array.Count == 0) ? (-1) : Math.Min(_selectedUnlockIndex, array.Count - 1));
			ReplaceMowerUnlockConditions(array, "移除小推车解锁条件");
		}
	}

	private void OpenSelectedMowerUnlockCondition()
	{
		OpenMowerNestedResource(_editingMower?.unlockCheckList, "unlockCheckList", _selectedUnlockIndex);
	}

	private void MoveSelectedMowerUnlockCondition(int direction)
	{
		if (_editingMower?.unlockCheckList != null && _selectedUnlockIndex >= 0 && _selectedUnlockIndex < _editingMower.unlockCheckList.Count)
		{
			int num = _selectedUnlockIndex + Math.Sign(direction);
			if (num >= 0 && num < _editingMower.unlockCheckList.Count)
			{
				Array<UnlockConditionBaseConfig> array = new Array<UnlockConditionBaseConfig>(_editingMower.unlockCheckList);
				UnlockConditionBaseConfig value = array[_selectedUnlockIndex];
				array[_selectedUnlockIndex] = array[num];
				array[num] = value;
				_selectedUnlockIndex = num;
				ReplaceMowerUnlockConditions(array, (direction < 0) ? "上移小推车解锁条件" : "下移小推车解锁条件");
			}
		}
	}

	private void AddMowerEvent()
	{
		if (base.CurrentResource is MowerConfig mowerConfig && _eventPicker?.EditedResource is MowerEventConfig mowerEventConfig && GodotObject.IsInstanceValid(mowerEventConfig))
		{
			Array<MowerEventConfig> array = ((mowerConfig.eventList == null) ? new Array<MowerEventConfig>() : new Array<MowerEventConfig>(mowerConfig.eventList));
			array.Add(mowerEventConfig);
			_selectedEventIndex = array.Count - 1;
			ReplaceMowerEvents(array, "添加小推车事件");
		}
	}

	private void ReplaceSelectedMowerEvent()
	{
		if (base.CurrentResource is MowerConfig mowerConfig && _eventPicker?.EditedResource is MowerEventConfig mowerEventConfig && GodotObject.IsInstanceValid(mowerEventConfig) && mowerConfig.eventList != null && _selectedEventIndex >= 0 && _selectedEventIndex < mowerConfig.eventList.Count)
		{
			Array<MowerEventConfig> array = new Array<MowerEventConfig>(mowerConfig.eventList);
			array[_selectedEventIndex] = mowerEventConfig;
			ReplaceMowerEvents(array, "替换小推车事件");
		}
	}

	private void RemoveSelectedMowerEvent()
	{
		if (_editingMower?.eventList != null && _selectedEventIndex >= 0 && _selectedEventIndex < _editingMower.eventList.Count)
		{
			Array<MowerEventConfig> array = new Array<MowerEventConfig>(_editingMower.eventList);
			array.RemoveAt(_selectedEventIndex);
			_selectedEventIndex = ((array.Count == 0) ? (-1) : Math.Min(_selectedEventIndex, array.Count - 1));
			ReplaceMowerEvents(array, "移除小推车事件");
		}
	}

	private void OpenSelectedMowerEvent()
	{
		OpenMowerNestedResource(_editingMower?.eventList, "eventList", _selectedEventIndex);
	}

	private void MoveSelectedMowerEvent(int direction)
	{
		if (_editingMower?.eventList != null && _selectedEventIndex >= 0 && _selectedEventIndex < _editingMower.eventList.Count)
		{
			int num = _selectedEventIndex + Math.Sign(direction);
			if (num >= 0 && num < _editingMower.eventList.Count)
			{
				Array<MowerEventConfig> array = new Array<MowerEventConfig>(_editingMower.eventList);
				MowerEventConfig value = array[_selectedEventIndex];
				array[_selectedEventIndex] = array[num];
				array[num] = value;
				_selectedEventIndex = num;
				ReplaceMowerEvents(array, (direction < 0) ? "上移小推车事件" : "下移小推车事件");
			}
		}
	}

	private void ReplaceMowerUnlockConditions(Array<UnlockConditionBaseConfig> next, string actionName)
	{
		if (_mowerPropertyBinding != null && base.CurrentResource is MowerConfig mowerConfig)
		{
			_mowerPropertyBinding.SetValue(mowerConfig, "unlockCheckList", next, actionName, this, "RefreshMowerEditorFromHistory");
			mowerConfig.NotifyPropertyListChanged();
			RefreshMowerLists(mowerConfig);
			UpdateMowerPreview();
		}
	}

	private void ReplaceMowerEvents(Array<MowerEventConfig> next, string actionName)
	{
		if (_mowerPropertyBinding != null && base.CurrentResource is MowerConfig mowerConfig)
		{
			_mowerPropertyBinding.SetValue(mowerConfig, "eventList", next, actionName, this, "RefreshMowerEditorFromHistory");
			mowerConfig.NotifyPropertyListChanged();
			RefreshMowerLists(mowerConfig);
			UpdateMowerPreview();
		}
	}

	private void OnMowerPropertyEdited(bool committed)
	{
		if (base.CurrentResource is MowerConfig mowerConfig && mowerConfig == _editingMower)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
			}
			else
			{
				MarkCurrentResourceDirty();
				mowerConfig.EmitChanged();
			}
			ScheduleMowerSave(mowerConfig);
		}
	}

	private void ScheduleMowerSave(MowerConfig mower)
	{
		if (GodotObject.IsInstanceValid(mower))
		{
			_editingMower = mower;
			if (GodotObject.IsInstanceValid(_mowerSaveTimer))
			{
				_mowerSaveTimer.Start();
			}
		}
	}

	public void RefreshMowerEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && base.CurrentResource is MowerConfig mowerConfig)
		{
			_editingMower = mowerConfig;
			ScheduleMowerSave(mowerConfig);
			RebuildMowerEditor();
		}
	}

	private void RebuildMowerEditor()
	{
		if (base.CanvasGrid == null || !GodotObject.IsInstanceValid(_editingMower))
		{
			return;
		}
		SetMowerScenePreviewRunning(running: false);
		_mowerPropertyBinding?.Dispose();
		_mowerPropertyBinding = null;
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		RenderMowerEditor(_editingMower);
	}

	private void SavePendingMowerResource()
	{
		if (GodotObject.IsInstanceValid(_editingMower))
		{
			SaveMowerResource(_editingMower);
		}
	}

	private void SaveMowerResource(MowerConfig mower)
	{
		string currentResourcePath = base.CurrentResourcePath;
		if (string.IsNullOrWhiteSpace(currentResourcePath) || !GodotObject.IsInstanceValid(mower))
		{
			return;
		}
		Error error = ResourceSaver.Save(mower, currentResourcePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			GD.PushWarning($"Mower editor save failed: {error} {currentResourcePath}");
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

	private void UpdateMowerPreview()
	{
		if (_editingMower != null)
		{
			if (GodotObject.IsInstanceValid(_texturePreview))
			{
				_texturePreview.Texture = _editingMower.texture;
			}
			if (GodotObject.IsInstanceValid(_titleLabel))
			{
				_titleLabel.Text = "小推车: " + EmptyToPlaceholder(_editingMower.saveKey);
			}
			if (GodotObject.IsInstanceValid(_spriteLabel))
			{
				_spriteLabel.Text = "sprite: " + FormatResource(_editingMower.sprite);
			}
			if (GodotObject.IsInstanceValid(_textSummaryLabel))
			{
				_textSummaryLabel.Text = "文本: " + EmptyToPlaceholder(_editingMower.name) + " / " + EmptyToPlaceholder(_editingMower.describe);
			}
			if (GodotObject.IsInstanceValid(_unlockSummaryLabel))
			{
				_unlockSummaryLabel.Text = "解锁/事件: " + BuildListSummary(_editingMower);
			}
		}
	}

	private void RefreshMowerLists(MowerConfig mower)
	{
		PopulateResourceList(_unlockList, mower.unlockCheckList);
		PopulateResourceList(_eventList, mower.eventList);
		SyncMowerCollectionSelections(mower);
	}

	private void SyncMowerCollectionSelections(MowerConfig mower)
	{
		int num = mower.unlockCheckList?.Count ?? 0;
		_selectedUnlockIndex = ((num == 0) ? (-1) : Math.Clamp((_selectedUnlockIndex >= 0) ? _selectedUnlockIndex : 0, 0, num - 1));
		if (_selectedUnlockIndex >= 0)
		{
			_unlockList?.Select(_selectedUnlockIndex);
			_unlockPicker?.SetEditedResource(mower.unlockCheckList[_selectedUnlockIndex]);
		}
		else
		{
			_unlockPicker?.SetEditedResource(null);
		}
		int num2 = mower.eventList?.Count ?? 0;
		_selectedEventIndex = ((num2 == 0) ? (-1) : Math.Clamp((_selectedEventIndex >= 0) ? _selectedEventIndex : 0, 0, num2 - 1));
		if (_selectedEventIndex >= 0)
		{
			_eventList?.Select(_selectedEventIndex);
			_eventPicker?.SetEditedResource(mower.eventList[_selectedEventIndex]);
		}
		else
		{
			_eventPicker?.SetEditedResource(null);
		}
	}

	private void AddSummaryRows(MowerConfig mower)
	{
		AddItemIfMissing(base.PreviewList, "小推车 saveKey -> " + EmptyToPlaceholder(mower.saveKey));
		AddItemIfMissing(base.PreviewList, "texture -> " + FormatResource(mower.texture));
		AddItemIfMissing(base.PreviewList, "sprite -> " + FormatResource(mower.sprite));
		AddItemIfMissing(base.TimelineList, $"unlockCheckList -> {mower.unlockCheckList?.Count ?? 0}");
		AddItemIfMissing(base.TimelineList, $"eventList -> {mower.eventList?.Count ?? 0}");
		AddItemIfMissing(base.GraphList, "小推车 -> texture / sprite -> unlockCheckList -> eventList");
		AddItemIfMissing(base.ReferenceList, "texture -> " + FormatResource(mower.texture));
		AddItemIfMissing(base.ReferenceList, "sprite -> " + FormatResource(mower.sprite));
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
			list.AddItem("未配置");
			return;
		}
		for (int i = 0; i < array.Count; i++)
		{
			list.AddItem($"{i}: {FormatObject(array[i])}");
		}
	}

	private static string BuildListSummary(MowerConfig mower)
	{
		return $"unlockCheckList={mower.unlockCheckList?.Count ?? 0}, eventList={mower.eventList?.Count ?? 0}";
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
