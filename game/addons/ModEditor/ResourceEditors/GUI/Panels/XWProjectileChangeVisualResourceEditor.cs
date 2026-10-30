using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWProjectileChangeVisualResourceEditor : XWGenericVisualResourceEditor
{
	private enum ProjectileChangeEditMode
	{
		None,
		Chain,
		Single
	}

	private enum ProjectileSelectorTarget
	{
		SourceProjectile,
		TargetProjectile
	}

	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileChangeEditorLayout.tscn";

	private static PackedScene _editorLayoutScene;

	private static readonly HashSet<string> InspectorEditableProperties = new HashSet<string> { "resource_name", "resource_local_to_scene", "changeList" };

	private static readonly HashSet<string> SingleInspectorEditableProperties = new HashSet<string> { "resource_name", "resource_local_to_scene", "projectileData", "projectileConfig", "changeAudio" };

	private ProjectileChangeEditMode _editMode;

	private ChangeProjectileConfig _editingChange;

	private ChangeProjectileSingleConfig _editingSingle;

	private XWVisualPropertyBinding _propertyBinding;

	private int _selectedIndex = -1;

	private Control _chainPanel;

	private ItemList _chainList;

	private Control _chainCanvas;

	private VBoxContainer _itemEditorHost;

	private Label _modeBadge;

	private Label _summaryLabel;

	private Label _selectedLabel;

	private Control _emptyState;

	private Label _emptyStateLabel;

	private Control _sourceField;

	private Control _targetField;

	private Control _audioField;

	private Label _entryPreviewLabel;

	private SubViewport _runtimePreviewViewport;

	private Node2D _runtimePreviewRoot;

	private Label _runtimePreviewStatus;

	private Button _runtimePreviewRunButton;

	private HSlider _runtimePreviewTimeline;

	private Control _runtimePreviewInput;

	private Label _runtimeZoomLabel;

	private Node _runtimeSourceProjectile;

	private Node _runtimeTargetProjectile;

	private double _runtimePreviewProgress;

	private bool _runtimePreviewRunning;

	private bool _runtimePreviewActive;

	private int _runtimePreviewRequestVersion;

	private Vector2 _runtimePreviewBasePosition;

	private Vector2 _runtimePreviewPan;

	private float _runtimePreviewZoom = 1f;

	private bool _runtimePreviewDragging;

	private XWResourcePicker _projectileDataPicker;

	private XWResourcePicker _projectileConfigPicker;

	private LineEdit _sourceProjectileKeyEdit;

	private LineEdit _changeAudioEdit;

	private Button _removeButton;

	private Button _moveUpButton;

	private Button _moveDownButton;

	private Button _runtimePreviousButton;

	private Button _runtimeNextButton;

	private bool _updatingControls;

	private ProjectileSelectorTarget _projectileSelectorTarget;

	private XWGameplayResourcePickerWindow _projectileSelectorWindow;

	private XWGameplayResourcePickerWindow _audioSelectorWindow;

	private bool IsChainMode => _editMode == ProjectileChangeEditMode.Chain;

	public override void _Ready()
	{
		SetProcess(enable: false);
		base.VisibilityChanged += OnProjectileChangeVisibilityChanged;
		base._Ready();
		OnProjectileChangeVisibilityChanged();
	}

	public override void _ExitTree()
	{
		base.VisibilityChanged -= OnProjectileChangeVisibilityChanged;
		DisposeProjectileChangeEditor();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeProjectileChangeEditor();
		if (base.CanvasGrid != null && TryBindEditingResource(base.CurrentResource))
		{
			_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), delegate
			{
				NotifyCurrentResourceEdited();
				RefreshSelectedEntryVisuals();
			});
			RenderProjectileChangeEditor();
		}
	}

	protected override HashSet<string> GetEmbeddedInspectorAllowedProperties(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!(resource is ChangeProjectileConfig))
		{
			if (resource is ChangeProjectileSingleConfig)
			{
				return SingleInspectorEditableProperties;
			}
			return base.GetEmbeddedInspectorAllowedProperties(resource, path, descriptor);
		}
		return InspectorEditableProperties;
	}

	protected override void OnEmbeddedInspectorPropertyChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if (obj is Resource resource && TryBindEditingResource(resource))
		{
			ClampSelectedIndex();
			RefreshChangeChain();
			RenderSelectedEntryEditor();
			QueueChangeCanvasRedraw();
		}
	}

	private void RenderProjectileChangeEditor()
	{
		if (base.CanvasGrid != null)
		{
			ClampSelectedIndex();
			base.CanvasGrid.Columns = 1;
			if (_editorLayoutScene == null)
			{
				_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileChangeEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindEditorLayout(vBoxContainer);
				ApplyEditModeLayout();
				RefreshChangeChain();
				RenderSelectedEntryEditor();
			}
		}
	}

	private bool TryBindEditingResource(Resource resource)
	{
		if (!(resource is ChangeProjectileConfig changeProjectileConfig))
		{
			if (resource is ChangeProjectileSingleConfig editingSingle)
			{
				_editMode = ProjectileChangeEditMode.Single;
				_editingChange = null;
				_editingSingle = editingSingle;
				_selectedIndex = 0;
				return true;
			}
			_editMode = ProjectileChangeEditMode.None;
			_editingChange = null;
			_editingSingle = null;
			_selectedIndex = -1;
			return false;
		}
		_editMode = ProjectileChangeEditMode.Chain;
		_editingChange = changeProjectileConfig;
		_editingSingle = null;
		EnsureChangeList(changeProjectileConfig);
		_selectedIndex = ((changeProjectileConfig.changeList.Count > 0) ? Mathf.Clamp(_selectedIndex, 0, changeProjectileConfig.changeList.Count - 1) : (-1));
		return true;
	}

	private int GetEntryCount()
	{
		if (!IsChainMode)
		{
			return GodotObject.IsInstanceValid(_editingSingle) ? 1 : 0;
		}
		return (_editingChange?.changeList?.Count).GetValueOrDefault();
	}

	private Resource GetEditingTarget()
	{
		if (_editMode == ProjectileChangeEditMode.Single)
		{
			return _editingSingle;
		}
		return GetSelectedEntry();
	}

	private void BindRuntimeChangePreview(Control panel)
	{
		_runtimePreviewViewport = panel.GetNode<SubViewport>("Layout/PreviewStack/ViewportContainer/Viewport");
		_runtimePreviewRoot = panel.GetNode<Node2D>("Layout/PreviewStack/ViewportContainer/Viewport/PreviewRoot");
		_runtimePreviewStatus = panel.GetNode<Label>("Layout/Status");
		_runtimePreviewRunButton = panel.GetNode<Button>("Layout/Toolbar/RunButton");
		_runtimePreviewTimeline = panel.GetNode<HSlider>("Layout/Toolbar/TimelineSlider");
		_runtimePreviewInput = panel.GetNode<Control>("Layout/PreviewStack/PreviewInput");
		_runtimeZoomLabel = panel.GetNode<Label>("Layout/Toolbar/ZoomLabel");
		_runtimePreviousButton = panel.GetNode<Button>("Layout/Toolbar/PreviousButton");
		_runtimeNextButton = panel.GetNode<Button>("Layout/Toolbar/NextButton");
		_runtimePreviousButton.Pressed += delegate
		{
			AdvanceRuntimeChangePreview(-1);
		};
		_runtimeNextButton.Pressed += delegate
		{
			AdvanceRuntimeChangePreview(1);
		};
		_runtimePreviewRunButton.Pressed += delegate
		{
			SetRuntimeChangePreviewRunning(!_runtimePreviewRunning);
		};
		_runtimePreviewTimeline.ValueChanged += delegate(double value)
		{
			_runtimePreviewProgress = value;
			_runtimePreviewRunning = false;
			UpdateRuntimeChangePlayback();
		};
		_runtimePreviewInput.GuiInput += HandleRuntimePreviewInput;
		panel.GetNode<Button>("Layout/Toolbar/ResetViewButton").Pressed += ResetRuntimePreviewView;
		_runtimePreviewBasePosition = _runtimePreviewRoot.Position;
		ResetRuntimePreviewView();
		RebuildRuntimeChangePreview();
	}

	private void HandleRuntimePreviewInput(InputEvent inputEvent)
	{
		bool flag = false;
		if (inputEvent is InputEventMouseButton inputEventMouseButton)
		{
			if (inputEventMouseButton.ButtonIndex == MouseButton.Middle || inputEventMouseButton.ButtonIndex == MouseButton.Left)
			{
				_runtimePreviewDragging = inputEventMouseButton.Pressed;
				flag = true;
			}
			else if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				SetRuntimePreviewZoom(_runtimePreviewZoom * 1.12f, inputEventMouseButton.Position);
				flag = true;
			}
			else if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				SetRuntimePreviewZoom(_runtimePreviewZoom / 1.12f, inputEventMouseButton.Position);
				flag = true;
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _runtimePreviewDragging)
		{
			_runtimePreviewPan += inputEventMouseMotion.Relative;
			ApplyRuntimePreviewView();
			flag = true;
		}
		if (flag && GodotObject.IsInstanceValid(_runtimePreviewInput))
		{
			_runtimePreviewInput.AcceptEvent();
		}
	}

	private void SetRuntimePreviewZoom(float zoom, Vector2 pointer)
	{
		float runtimePreviewZoom = _runtimePreviewZoom;
		Vector2 vector = _runtimePreviewBasePosition + _runtimePreviewPan;
		Vector2 vector2 = ((runtimePreviewZoom > 0f) ? ((pointer - vector) / runtimePreviewZoom) : Vector2.Zero);
		_runtimePreviewZoom = zoom;
		_runtimePreviewZoom = Mathf.Clamp(_runtimePreviewZoom, 0.35f, 3f);
		if (!Mathf.IsEqualApprox(runtimePreviewZoom, _runtimePreviewZoom))
		{
			_runtimePreviewPan = pointer - vector2 * _runtimePreviewZoom - _runtimePreviewBasePosition;
		}
		ApplyRuntimePreviewView();
	}

	private void ResetRuntimePreviewView()
	{
		_runtimePreviewPan = Vector2.Zero;
		_runtimePreviewZoom = 1f;
		_runtimePreviewDragging = false;
		ApplyRuntimePreviewView();
	}

	private void ApplyRuntimePreviewView()
	{
		if (GodotObject.IsInstanceValid(_runtimePreviewRoot))
		{
			_runtimePreviewRoot.Position = _runtimePreviewBasePosition + _runtimePreviewPan;
			_runtimePreviewRoot.Scale = Vector2.One * _runtimePreviewZoom;
		}
		if (GodotObject.IsInstanceValid(_runtimeZoomLabel))
		{
			_runtimeZoomLabel.Text = $"{Mathf.RoundToInt(_runtimePreviewZoom * 100f)}%";
		}
	}

	private void AdvanceRuntimeChangePreview(int delta)
	{
		if (IsChainMode)
		{
			int entryCount = GetEntryCount();
			if (entryCount > 0)
			{
				_selectedIndex = Mathf.Clamp(_selectedIndex + delta, 0, entryCount - 1);
				RefreshChangeChain();
				RenderSelectedEntryEditor();
				QueueChangeCanvasRedraw();
			}
		}
	}

	private void RebuildRuntimeChangePreview()
	{
		int requestVersion = ++_runtimePreviewRequestVersion;
		Resource currentResource = base.CurrentResource;
		Node2D runtimePreviewRoot = _runtimePreviewRoot;
		if (!GodotObject.IsInstanceValid(runtimePreviewRoot))
		{
			return;
		}
		foreach (Node child in runtimePreviewRoot.GetChildren())
		{
			child.QueueFree();
		}
		_runtimeSourceProjectile = null;
		_runtimeTargetProjectile = null;
		ChangeProjectileSingleConfig selectedEntry = GetSelectedEntry();
		if (!GodotObject.IsInstanceValid(selectedEntry))
		{
			if (GodotObject.IsInstanceValid(_runtimePreviewStatus))
			{
				_runtimePreviewStatus.Text = "未选择子弹变化项";
			}
			return;
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = selectedEntry.projectileData?.BuildConfig();
		string text = ReadEntryProjectileName(selectedEntry);
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileConfig) && ResourceManager.Instance != null && !string.IsNullOrWhiteSpace(text))
		{
			towerDefenseProjectileConfig = TowerDefenseManager.GetProjectileConfig(text);
		}
		if (!IsRuntimePreviewRequestCurrent(requestVersion, currentResource, runtimePreviewRoot))
		{
			return;
		}
		_runtimeSourceProjectile = AddRuntimeProjectileScene(towerDefenseProjectileConfig?.projectileScene, Vector2.Zero, "SourceProjectilePreview", new Color(0.35f, 0.68f, 1f), runtimePreviewRoot);
		if (!IsRuntimePreviewRequestCurrent(requestVersion, currentResource, runtimePreviewRoot))
		{
			QueueNodeForDeletion(_runtimeSourceProjectile);
			_runtimeSourceProjectile = null;
			return;
		}
		_runtimeTargetProjectile = AddRuntimeProjectileScene(selectedEntry.projectileConfig?.projectileScene, Vector2.Zero, "TargetProjectilePreview", new Color(1f, 0.6f, 0.3f), runtimePreviewRoot);
		_runtimePreviewProgress = 0.0;
		_runtimePreviewTimeline?.SetValueNoSignal(0.0);
		SetRuntimeChangePreviewRunning(_runtimePreviewRunning);
		if (GodotObject.IsInstanceValid(_runtimePreviewStatus))
		{
			_runtimePreviewStatus.Text = (IsChainMode ? $"变化项 {_selectedIndex + 1}/{GetEntryCount()} · {FormatProjectileData(selectedEntry)} → {FormatProjectileConfig(selectedEntry)} · 音效 {EmptyToPlaceholder(selectedEntry.changeAudio)}" : $"单次变化 · {FormatProjectileData(selectedEntry)} → {FormatProjectileConfig(selectedEntry)} · 音效 {EmptyToPlaceholder(selectedEntry.changeAudio)}");
		}
		UpdateRuntimeChangePlayback();
	}

	private bool IsRuntimePreviewRequestCurrent(int requestVersion, Resource requestedResource, Node2D requestedRoot)
	{
		if (requestVersion != _runtimePreviewRequestVersion)
		{
			return false;
		}
		if (requestedResource == base.CurrentResource && requestedRoot == _runtimePreviewRoot && GodotObject.IsInstanceValid(requestedRoot))
		{
			return !requestedRoot.IsQueuedForDeletion();
		}
		return false;
	}

	private Node AddRuntimeProjectileScene(PackedScene scene, Vector2 position, string nodeName, Color fallbackColor, Node2D previewRoot)
	{
		if (!GodotObject.IsInstanceValid(previewRoot) || previewRoot.IsQueuedForDeletion())
		{
			return null;
		}
		Node node = null;
		if (GodotObject.IsInstanceValid(scene))
		{
			try
			{
				node = scene.Instantiate(PackedScene.GenEditState.Disabled);
				node.Name = nodeName;
				node.ProcessMode = (ProcessModeEnum)(_runtimePreviewActive ? 0 : 4);
				if (node is Node2D node2D)
				{
					node2D.Position = position;
				}
				else if (node is Control control)
				{
					control.Position = position;
				}
				previewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			}
			catch (Exception ex)
			{
				GD.PushWarning("Projectile change runtime preview failed: " + nodeName + " " + ex.Message);
			}
		}
		if (node == null)
		{
			ColorRect colorRect = new ColorRect
			{
				Name = nodeName + "Fallback",
				Color = fallbackColor,
				Position = position - new Vector2(22f, 22f),
				Size = new Vector2(44f, 44f),
				MouseFilter = MouseFilterEnum.Ignore
			};
			previewRoot.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
			node = colorRect;
		}
		return node;
	}

	private void SetRuntimeChangePreviewRunning(bool running)
	{
		_runtimePreviewRunning = running;
		if (running && _runtimePreviewProgress >= 1.0)
		{
			_runtimePreviewProgress = 0.0;
		}
		if (GodotObject.IsInstanceValid(_runtimePreviewRunButton))
		{
			_runtimePreviewRunButton.Text = (running ? "暂停变化" : "播放变化");
		}
		SetRuntimePreviewActive(IsVisibleInTree());
		UpdateRuntimeChangePlayback();
	}

	private void OnProjectileChangeVisibilityChanged()
	{
		SetRuntimePreviewActive(IsVisibleInTree());
	}

	private void SetRuntimePreviewActive(bool active)
	{
		_runtimePreviewActive = active;
		SetProcess(active && _runtimePreviewRunning);
		if (GodotObject.IsInstanceValid(_runtimeSourceProjectile))
		{
			_runtimeSourceProjectile.ProcessMode = (ProcessModeEnum)(active ? 0 : 4);
		}
		if (GodotObject.IsInstanceValid(_runtimeTargetProjectile))
		{
			_runtimeTargetProjectile.ProcessMode = (ProcessModeEnum)(active ? 0 : 4);
		}
		if (!active)
		{
			_runtimePreviewDragging = false;
			_projectileSelectorWindow?.Dismiss();
			_audioSelectorWindow?.Dismiss();
		}
		else
		{
			UpdateRuntimeChangePlayback();
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_runtimePreviewActive && _runtimePreviewRunning)
		{
			_runtimePreviewProgress = Mathf.Clamp(_runtimePreviewProgress + delta / 1.6, 0.0, 1.0);
			_runtimePreviewTimeline?.SetValueNoSignal(_runtimePreviewProgress);
			UpdateRuntimeChangePlayback();
			if (_runtimePreviewProgress >= 1.0)
			{
				SetRuntimeChangePreviewRunning(running: false);
			}
		}
	}

	private void UpdateRuntimeChangePlayback()
	{
		float num = (float)Mathf.Clamp(_runtimePreviewProgress, 0.0, 1.0);
		bool flag = num >= 0.5f;
		float weight = (flag ? ((num - 0.5f) * 2f) : (num * 2f));
		Vector2 position = (flag ? new Vector2(0f, 0f).Lerp(new Vector2(250f, -12f), weight) : new Vector2(-250f, 12f).Lerp(Vector2.Zero, weight));
		SetRuntimeProjectileVisual(_runtimeSourceProjectile, !flag, position);
		SetRuntimeProjectileVisual(_runtimeTargetProjectile, flag, position);
		if (GodotObject.IsInstanceValid(_runtimePreviewStatus) && GetEntryCount() > 0)
		{
			_runtimePreviewStatus.Text = (IsChainMode ? $"变化项 {_selectedIndex + 1}/{GetEntryCount()} · {(flag ? "目标子弹飞行" : "来源子弹飞行")} · {num:P0}" : $"单次变化 · {(flag ? "目标子弹飞行" : "来源子弹飞行")} · {num:P0}");
		}
	}

	private void SetRuntimeProjectileVisual(Node node, bool visible, Vector2 position)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			if (node is CanvasItem canvasItem)
			{
				canvasItem.Visible = visible;
			}
			if (node is Node2D node2D)
			{
				node2D.Position = position;
			}
			else if (node is Control control)
			{
				control.Position = position;
			}
			node.ProcessMode = (ProcessModeEnum)((_runtimePreviewActive && visible) ? 0 : 4);
		}
	}

	private void BindEditorLayout(VBoxContainer root)
	{
		_modeBadge = root.GetNode<Label>("%ModeBadge");
		_summaryLabel = root.GetNode<Label>("%SummaryLabel");
		_summaryLabel.Text = BuildSummary();
		BindRuntimeChangePreview(root.GetNode<Control>("%RuntimePreview"));
		root.GetNode<Button>("%AddButton").Pressed += AddChangeEntry;
		_removeButton = root.GetNode<Button>("%RemoveButton");
		_moveUpButton = root.GetNode<Button>("%MoveUpButton");
		_moveDownButton = root.GetNode<Button>("%MoveDownButton");
		_removeButton.Pressed += RemoveSelectedChangeEntry;
		_moveUpButton.Pressed += delegate
		{
			MoveSelectedChangeEntry(-1);
		};
		_moveDownButton.Pressed += delegate
		{
			MoveSelectedChangeEntry(1);
		};
		_chainList = root.GetNode<ItemList>("%ChainList");
		_chainList.ItemSelected += delegate(long index)
		{
			OnSelectedChangeEntryChanged((int)index);
		};
		Control chainCanvas = root.GetNode<Control>("%ChainCanvas");
		_chainCanvas = chainCanvas;
		chainCanvas.Draw += delegate
		{
			DrawChangeChain(chainCanvas);
		};
		_chainPanel = root.GetNode<Control>("%ChainPanel");
		_itemEditorHost = root.GetNode<VBoxContainer>("%ItemEditorHost");
		_selectedLabel = root.GetNode<Label>("%SelectedLabel");
		_emptyState = root.GetNode<Control>("%EmptyState");
		_emptyStateLabel = _emptyState.GetNode<Label>("EmptyStateLabel");
		_sourceField = root.GetNode<Control>("%SourceField");
		_targetField = root.GetNode<Control>("%TargetField");
		_audioField = root.GetNode<Control>("%AudioField");
		_entryPreviewLabel = root.GetNode<Label>("%EntryPreviewLabel");
		_sourceProjectileKeyEdit = root.GetNode<LineEdit>("%SourceProjectileKeyEdit");
		_changeAudioEdit = root.GetNode<LineEdit>("%ChangeAudioLineEdit");
		_projectileDataPicker = root.GetNode<XWResourcePicker>("%ProjectileDataPicker");
		_projectileDataPicker.Setup("TowerDefenseProjectileCreateData");
		_projectileDataPicker.ResourceChanged += delegate(Resource resource)
		{
			SetEntryProperty("projectileData", Variant.From(in resource), "修改来源子弹数据");
		};
		_projectileConfigPicker = root.GetNode<XWResourcePicker>("%ProjectileConfigPicker");
		_projectileConfigPicker.Setup("TowerDefenseProjectileConfig");
		_projectileConfigPicker.ResourceChanged += delegate(Resource resource)
		{
			SetEntryProperty("projectileConfig", Variant.From(in resource), "修改目标子弹");
		};
		root.GetNode<Button>("%ChooseSourceButton").Pressed += delegate
		{
			ShowProjectileSelectorWindow(ProjectileSelectorTarget.SourceProjectile);
		};
		root.GetNode<Button>("%ChooseTargetButton").Pressed += delegate
		{
			ShowProjectileSelectorWindow(ProjectileSelectorTarget.TargetProjectile);
		};
		root.GetNode<Button>("%ChooseAudioButton").Pressed += ShowAudioSelectorWindow;
		root.GetNode<Button>("%ClearAudioButton").Pressed += ClearAudioSelection;
	}

	private void ApplyEditModeLayout()
	{
		if (GodotObject.IsInstanceValid(_chainPanel))
		{
			_chainPanel.Visible = IsChainMode;
		}
		if (GodotObject.IsInstanceValid(_runtimePreviousButton))
		{
			_runtimePreviousButton.Visible = IsChainMode;
		}
		if (GodotObject.IsInstanceValid(_runtimeNextButton))
		{
			_runtimeNextButton.Visible = IsChainMode;
		}
		if (GodotObject.IsInstanceValid(_modeBadge))
		{
			_modeBadge.Text = (IsChainMode ? "连续变化链" : "单次子弹变化");
		}
		if (GodotObject.IsInstanceValid(_summaryLabel))
		{
			_summaryLabel.Text = BuildSummary();
		}
	}

	private void RenderSelectedEntryEditor()
	{
		if (!GodotObject.IsInstanceValid(_itemEditorHost) || !GodotObject.IsInstanceValid(_emptyState))
		{
			return;
		}
		ChangeProjectileSingleConfig selectedEntry = GetSelectedEntry();
		bool flag = GodotObject.IsInstanceValid(selectedEntry);
		_emptyState.Visible = !flag;
		_sourceField.Visible = flag;
		_targetField.Visible = flag;
		_audioField.Visible = flag;
		_entryPreviewLabel.Visible = flag;
		_selectedLabel.Visible = flag;
		if (!flag)
		{
			_emptyStateLabel.Text = (IsChainMode ? "还没有子弹变化项。使用左侧的添加按钮建立一段变化。" : "单条子弹变化资源不可用。");
			_projectileDataPicker.SetEditedResource(null);
			_projectileConfigPicker.SetEditedResource(null);
			RebuildRuntimeChangePreview();
			return;
		}
		_updatingControls = true;
		try
		{
			_selectedLabel.Text = (IsChainMode ? $"变化项 {_selectedIndex + 1}: {FormatEntryTitle(selectedEntry)}" : ("单次变化: " + FormatEntryTitle(selectedEntry)));
			_sourceProjectileKeyEdit.Text = ReadEntryProjectileName(selectedEntry);
			_projectileDataPicker.SetEditedResource(selectedEntry.projectileData);
			_projectileConfigPicker.SetEditedResource(selectedEntry.projectileConfig);
			_changeAudioEdit.Text = selectedEntry.changeAudio ?? "";
			_entryPreviewLabel.Text = BuildSelectedEntryPreview(selectedEntry);
		}
		finally
		{
			_updatingControls = false;
		}
		RebuildRuntimeChangePreview();
	}

	private void AddChangeEntry()
	{
		if (IsChainMode && _editingChange != null && _propertyBinding != null)
		{
			EnsureChangeList(_editingChange);
			ChangeProjectileSingleConfig item = new ChangeProjectileSingleConfig
			{
				ResourceName = $"ProjectileChange_{_editingChange.changeList.Count + 1}"
			};
			Array<ChangeProjectileSingleConfig> array = new Array<ChangeProjectileSingleConfig>(_editingChange.changeList);
			array.Add(item);
			ReplaceChangeList(array, "添加子弹变化", array.Count - 1);
		}
	}

	private void RemoveSelectedChangeEntry()
	{
		if (IsChainMode && _editingChange?.changeList != null && _selectedIndex >= 0 && _selectedIndex < _editingChange.changeList.Count)
		{
			Array<ChangeProjectileSingleConfig> array = new Array<ChangeProjectileSingleConfig>(_editingChange.changeList);
			array.RemoveAt(_selectedIndex);
			ReplaceChangeList(array, "删除子弹变化", Mathf.Min(_selectedIndex, array.Count - 1));
		}
	}

	private void MoveSelectedChangeEntry(int direction)
	{
		if (IsChainMode && _editingChange?.changeList != null && _selectedIndex >= 0)
		{
			int num = _selectedIndex + Math.Sign(direction);
			if (num >= 0 && num < _editingChange.changeList.Count)
			{
				Array<ChangeProjectileSingleConfig> array = new Array<ChangeProjectileSingleConfig>(_editingChange.changeList);
				ChangeProjectileSingleConfig item = array[_selectedIndex];
				array.RemoveAt(_selectedIndex);
				array.Insert(num, item);
				ReplaceChangeList(array, (direction < 0) ? "上移子弹变化" : "下移子弹变化", num);
			}
		}
	}

	private void ReplaceChangeList(Array<ChangeProjectileSingleConfig> next, string actionName, int nextIndex)
	{
		if (IsChainMode && GodotObject.IsInstanceValid(_editingChange) && _propertyBinding != null && next != null)
		{
			_selectedIndex = ((next.Count == 0) ? (-1) : Mathf.Clamp(nextIndex, 0, next.Count - 1));
			_propertyBinding.SetValue(_editingChange, "changeList", next, actionName);
			RefreshSelectedEntryVisuals();
		}
	}

	private void OnSelectedChangeEntryChanged(int index)
	{
		if (IsChainMode)
		{
			_selectedIndex = index;
			ClampSelectedIndex();
			RefreshChangeChain();
			RenderSelectedEntryEditor();
			QueueChangeCanvasRedraw();
		}
	}

	private void RefreshSelectedEntryVisuals()
	{
		RefreshChangeChain();
		RenderSelectedEntryEditor();
		QueueChangeCanvasRedraw();
	}

	private void SetEntryProperty(StringName property, Variant value, string actionName)
	{
		if (!_updatingControls && _propertyBinding != null)
		{
			ChangeProjectileSingleConfig selectedEntry = GetSelectedEntry();
			if (GodotObject.IsInstanceValid(selectedEntry) && !property.IsEmpty)
			{
				_propertyBinding.SetValue(selectedEntry, property, value, actionName);
			}
		}
	}

	private void ShowProjectileSelectorWindow(ProjectileSelectorTarget target)
	{
		_projectileSelectorTarget = target;
		EnsureProjectileSelectorWindow();
		if (GodotObject.IsInstanceValid(_projectileSelectorWindow))
		{
			ChangeProjectileSingleConfig selectedEntry = GetSelectedEntry();
			bool flag = target == ProjectileSelectorTarget.SourceProjectile;
			string currentPath = ((!flag) ? (selectedEntry?.projectileConfig?.ResourcePath ?? "") : (selectedEntry?.projectileData?.ResourcePath ?? ""));
			string projectPath = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			string[] resourceClassNames = ((!flag) ? new string[1] { "TowerDefenseProjectileConfig" } : new string[2] { "TowerDefenseProjectileConfig", "TowerDefenseProjectileCreateData" });
			_projectileSelectorWindow.OpenResourceLibrary("Projectile", flag ? "来源子弹" : "目标子弹", currentPath, projectPath, resourceClassNames, new string[2] { "Asset/Config/Projectile", "Registry/Projectile/Config" }, "res://addons/ModEditor/Icons/ResourceProjectile.svg", SelectProjectileChoice);
		}
	}

	private void EnsureProjectileSelectorWindow()
	{
		if (!GodotObject.IsInstanceValid(_projectileSelectorWindow))
		{
			_projectileSelectorWindow = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_projectileSelectorWindow))
			{
				AddChild(_projectileSelectorWindow, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void SelectProjectileChoice(XWGameplayResourceChoice choice)
	{
		if (choice == null || string.IsNullOrWhiteSpace(choice.ResourcePath) || !GodotObject.IsInstanceValid(GetSelectedEntry()))
		{
			return;
		}
		Resource resource = (ResourceLoader.Exists(choice.ResourcePath) ? ResourceLoader.Load<Resource>(choice.ResourcePath, null, ResourceLoader.CacheMode.Reuse) : null);
		if (!GodotObject.IsInstanceValid(resource))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载子弹资源: " + choice.ResourcePath);
		}
		else if (_projectileSelectorTarget == ProjectileSelectorTarget.SourceProjectile)
		{
			TowerDefenseProjectileCreateData from = resource as TowerDefenseProjectileCreateData;
			string text = from?.projectileName.ToString() ?? "";
			if (resource is TowerDefenseProjectileConfig config)
			{
				text = ReadProjectileConfigKey(config);
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = Path.GetFileNameWithoutExtension(choice.ResourcePath);
			}
			if (!GodotObject.IsInstanceValid(from))
			{
				from = new TowerDefenseProjectileCreateData
				{
					projectileName = new StringName(text),
					ResourceName = text + "CreateData"
				};
			}
			SetEntryProperty("projectileData", Variant.From(in from), "选择来源子弹 " + text);
		}
		else
		{
			TowerDefenseProjectileConfig from2 = resource as TowerDefenseProjectileConfig;
			if (from2 == null)
			{
				XWEditorInterface.Instance?.ShowToast("目标子弹资源类型不匹配: " + choice.ResourcePath);
				return;
			}
			string text2 = ReadProjectileConfigKey(from2);
			SetEntryProperty("projectileConfig", Variant.From(in from2), "选择目标子弹 " + text2);
		}
	}

	private void ShowAudioSelectorWindow()
	{
		EnsureAudioSelectorWindow();
		if (GodotObject.IsInstanceValid(_audioSelectorWindow))
		{
			_audioSelectorWindow.Open(XWGameplayResourceKind.Audio, GetSelectedEntry()?.changeAudio ?? "", SelectProjectileAudioChoice, (XWGameplayResourceChoice choice) => choice.Kind == XWGameplayResourceKind.Audio, lockKind: true, "变化音效图鉴");
		}
	}

	private void EnsureAudioSelectorWindow()
	{
		if (!GodotObject.IsInstanceValid(_audioSelectorWindow))
		{
			_audioSelectorWindow = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_audioSelectorWindow))
			{
				AddChild(_audioSelectorWindow, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void SelectProjectileAudioChoice(XWGameplayResourceChoice choice)
	{
		if (!(choice == null) && choice.Kind == XWGameplayResourceKind.Audio)
		{
			SetEntryProperty("changeAudio", Variant.From<string>(choice.Key), "选择音效 " + choice.Key);
		}
	}

	private void ClearAudioSelection()
	{
		SetEntryProperty("changeAudio", Variant.From<string>(""), "清除变化音效");
	}

	private void RefreshChangeChain()
	{
		ClampSelectedIndex();
		if (GodotObject.IsInstanceValid(_summaryLabel))
		{
			_summaryLabel.Text = BuildSummary();
		}
		if (!IsChainMode || _editingChange == null)
		{
			UpdateToolbarState();
			return;
		}
		EnsureChangeList(_editingChange);
		if (!GodotObject.IsInstanceValid(_chainList))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			_chainList.Clear();
			for (int i = 0; i < _editingChange.changeList.Count; i++)
			{
				ChangeProjectileSingleConfig entry = _editingChange.changeList[i];
				_chainList.AddItem($"{i + 1}. {FormatEntryTitle(entry)}");
			}
			if (_editingChange.changeList.Count == 0)
			{
				_chainList.AddItem("未配置 changeList");
			}
			else if (_selectedIndex >= 0 && _selectedIndex < _chainList.ItemCount)
			{
				_chainList.Select(_selectedIndex);
			}
		}
		finally
		{
			_updatingControls = false;
		}
		UpdateToolbarState();
	}

	private void DrawChangeChain(Control canvas)
	{
		if (!IsChainMode || !GodotObject.IsInstanceValid(canvas) || _editingChange?.changeList == null)
		{
			return;
		}
		Rect2 rect = new Rect2(Vector2.Zero, canvas.Size);
		canvas.DrawRect(rect, new Color(0.055f, 0.06f, 0.07f));
		int count = _editingChange.changeList.Count;
		if (count == 0)
		{
			canvas.DrawString(GetThemeDefaultFont(), new Vector2(18f, 42f), "暂无子弹变化项", HorizontalAlignment.Left, -1f, 14, new Color(0.72f, 0.74f, 0.78f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			return;
		}
		float num = Mathf.Clamp((canvas.Size.X - 48f) / (float)Mathf.Max(count, 1) - 14f, 76f, 150f);
		float num2 = 52f;
		float num3 = Mathf.Max(24f, canvas.Size.Y * 0.5f - num2 * 0.5f);
		float num4 = 18f;
		for (int i = 0; i < count; i++)
		{
			ChangeProjectileSingleConfig entry = _editingChange.changeList[i];
			Rect2 rect2 = new Rect2(new Vector2(num4, num3), new Vector2(num, num2));
			bool num5 = i == _selectedIndex;
			Color color = (num5 ? new Color(0.18f, 0.34f, 0.58f) : new Color(0.16f, 0.17f, 0.18f));
			Color color2 = (num5 ? new Color(0.45f, 0.72f, 1f) : new Color(0.31f, 0.33f, 0.36f));
			canvas.DrawRect(rect2, color);
			canvas.DrawRect(rect2, color2, filled: false, 2f);
			canvas.DrawString(GetThemeDefaultFont(), rect2.Position + new Vector2(8f, 21f), $"{i + 1}. {FormatProjectileData(entry)}", HorizontalAlignment.Left, num - 12f, 12, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			canvas.DrawString(GetThemeDefaultFont(), rect2.Position + new Vector2(8f, 40f), FormatProjectileConfig(entry), HorizontalAlignment.Left, num - 12f, 12, new Color(0.78f, 0.82f, 0.88f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			float num6 = num4 + num + 14f;
			if (i < count - 1)
			{
				Vector2 vector = new Vector2(num4 + num, num3 + num2 * 0.5f);
				Vector2 vector2 = new Vector2(num6, num3 + num2 * 0.5f);
				canvas.DrawLine(vector, vector2, new Color(0.56f, 0.66f, 0.78f), 2f);
				canvas.DrawLine(vector2 - new Vector2(8f, 5f), vector2, new Color(0.56f, 0.66f, 0.78f), 2f);
				canvas.DrawLine(vector2 - new Vector2(8f, -5f), vector2, new Color(0.56f, 0.66f, 0.78f), 2f);
			}
			num4 = num6;
		}
	}

	private void EnsureChangeList(ChangeProjectileConfig projectileChange)
	{
		if (projectileChange.changeList == null)
		{
			projectileChange.changeList = new Array<ChangeProjectileSingleConfig>();
		}
	}

	private ChangeProjectileSingleConfig GetSelectedEntry()
	{
		if (_editMode == ProjectileChangeEditMode.Single)
		{
			return _editingSingle;
		}
		if (_editingChange?.changeList == null || _selectedIndex < 0 || _selectedIndex >= _editingChange.changeList.Count)
		{
			return null;
		}
		return _editingChange.changeList[_selectedIndex];
	}

	private void ClampSelectedIndex()
	{
		if (_editMode == ProjectileChangeEditMode.Single)
		{
			_selectedIndex = ((!GodotObject.IsInstanceValid(_editingSingle)) ? (-1) : 0);
		}
		else if (_editingChange?.changeList == null || _editingChange.changeList.Count == 0)
		{
			_selectedIndex = -1;
		}
		else
		{
			_selectedIndex = Mathf.Clamp(_selectedIndex, 0, _editingChange.changeList.Count - 1);
		}
	}

	private void UpdateToolbarState()
	{
		bool flag = IsChainMode && _editingChange?.changeList != null && _selectedIndex >= 0 && _selectedIndex < _editingChange.changeList.Count;
		if (GodotObject.IsInstanceValid(_removeButton))
		{
			_removeButton.Disabled = !flag || !IsChainMode;
		}
		if (GodotObject.IsInstanceValid(_moveUpButton))
		{
			_moveUpButton.Disabled = !flag || !IsChainMode || _selectedIndex <= 0;
		}
		if (GodotObject.IsInstanceValid(_moveDownButton))
		{
			_moveDownButton.Disabled = !flag || !IsChainMode || _editingChange == null || _selectedIndex >= _editingChange.changeList.Count - 1;
		}
	}

	private void UpdateSelectedEntryLabel()
	{
		if (GodotObject.IsInstanceValid(_selectedLabel))
		{
			_selectedLabel.Text = (IsChainMode ? $"变化项 {_selectedIndex + 1}: {FormatEntryTitle(GetSelectedEntry())}" : ("单次变化: " + FormatEntryTitle(GetSelectedEntry())));
		}
	}

	private void QueueChangeCanvasRedraw()
	{
		if (GodotObject.IsInstanceValid(_chainCanvas))
		{
			_chainCanvas.QueueRedraw();
		}
	}

	private string BuildSummary()
	{
		if (!IsChainMode)
		{
			return "单次子弹变化: 在游戏草坪中预览来源与目标";
		}
		return $"子弹变化链路: {GetEntryCount()} 条变化";
	}

	private void DisposeProjectileChangeEditor()
	{
		_runtimePreviewRequestVersion++;
		_runtimePreviewRunning = false;
		_runtimePreviewActive = false;
		SetProcess(enable: false);
		_projectileSelectorWindow?.Dismiss();
		_audioSelectorWindow?.Dismiss();
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		QueueNodeForDeletion(_runtimeSourceProjectile);
		QueueNodeForDeletion(_runtimeTargetProjectile);
		QueueNodeForDeletion(_projectileSelectorWindow);
		QueueNodeForDeletion(_audioSelectorWindow);
		_editMode = ProjectileChangeEditMode.None;
		_editingChange = null;
		_editingSingle = null;
		_selectedIndex = -1;
		_chainPanel = null;
		_chainList = null;
		_chainCanvas = null;
		_itemEditorHost = null;
		_modeBadge = null;
		_summaryLabel = null;
		_selectedLabel = null;
		_emptyState = null;
		_emptyStateLabel = null;
		_sourceField = null;
		_targetField = null;
		_audioField = null;
		_entryPreviewLabel = null;
		_runtimePreviewViewport = null;
		_runtimePreviewRoot = null;
		_runtimePreviewStatus = null;
		_runtimePreviewRunButton = null;
		_runtimePreviewTimeline = null;
		_runtimePreviewInput = null;
		_runtimeZoomLabel = null;
		_runtimePreviousButton = null;
		_runtimeNextButton = null;
		_runtimeSourceProjectile = null;
		_runtimeTargetProjectile = null;
		_runtimePreviewProgress = 0.0;
		_runtimePreviewBasePosition = Vector2.Zero;
		_runtimePreviewPan = Vector2.Zero;
		_runtimePreviewZoom = 1f;
		_runtimePreviewDragging = false;
		_projectileDataPicker = null;
		_projectileConfigPicker = null;
		_sourceProjectileKeyEdit = null;
		_changeAudioEdit = null;
		_projectileSelectorWindow = null;
		_projectileSelectorTarget = ProjectileSelectorTarget.SourceProjectile;
		_audioSelectorWindow = null;
		_removeButton = null;
		_moveUpButton = null;
		_moveDownButton = null;
		_updatingControls = false;
	}

	private static void QueueNodeForDeletion(Node node)
	{
		if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
		{
			node.ProcessMode = ProcessModeEnum.Disabled;
			if (node is Window window)
			{
				window.Hide();
			}
			node.QueueFree();
		}
	}

	private static string BuildSelectedEntryPreview(ChangeProjectileSingleConfig entry)
	{
		return $"来源: {FormatProjectileData(entry)}\n目标: {FormatProjectileConfig(entry)}\n音效: {EmptyToPlaceholder(entry?.changeAudio)}";
	}

	private static string FormatEntryTitle(ChangeProjectileSingleConfig entry)
	{
		if (!GodotObject.IsInstanceValid(entry))
		{
			return "未配置";
		}
		return FormatProjectileData(entry) + " -> " + FormatProjectileConfig(entry);
	}

	private static string FormatProjectileData(ChangeProjectileSingleConfig entry)
	{
		string text = ReadEntryProjectileName(entry);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "未配置来源";
	}

	private static string FormatProjectileConfig(ChangeProjectileSingleConfig entry)
	{
		if (!GodotObject.IsInstanceValid(entry?.projectileConfig))
		{
			return "未配置目标";
		}
		string text = ReadProjectileConfigKey(entry.projectileConfig);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return FormatResource(entry.projectileConfig);
	}

	private static string ReadEntryProjectileName(ChangeProjectileSingleConfig entry)
	{
		if (!GodotObject.IsInstanceValid(entry))
		{
			return "";
		}
		if (GodotObject.IsInstanceValid(entry.projectileData))
		{
			return entry.projectileData.projectileName.ToString();
		}
		Variant variant = entry.Get("projectileName");
		if (variant.VariantType == Variant.Type.String || variant.VariantType == Variant.Type.StringName)
		{
			return variant.AsString();
		}
		return "";
	}

	private static string ReadProjectileConfigKey(TowerDefenseProjectileConfig config)
	{
		if (!GodotObject.IsInstanceValid(config))
		{
			return "";
		}
		if (!string.IsNullOrWhiteSpace(config.name))
		{
			return config.name;
		}
		return config.ResourceName;
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
