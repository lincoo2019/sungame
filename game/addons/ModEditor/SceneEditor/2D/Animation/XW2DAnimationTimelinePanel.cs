using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.ResourceEditors.GUI;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DAnimationTimelinePanel : PanelContainer
{
	private readonly struct PropertyChoice(Node node, string property, string label, Variant value)
	{
		public readonly Node Node = node;

		public readonly string Property = property;

		public readonly string Label = label;

		public readonly Variant Value = value;
	}

	private XW2DSceneEditor _editor;

	private Node _sceneRoot;

	private AnimationPlayer _player;

	private AnimationLibrary _library;

	private Animation _animation;

	private XW2DAnimationTimelineCanvas _timeline;

	private XW2DAnimationSafePreview _preview;

	private OptionButton _playerOption;

	private OptionButton _libraryOption;

	private OptionButton _animationOption;

	private Label _treeLinkLabel;

	private Label _readonlyLabel;

	private Label _previewSafetyLabel;

	private Label _statusLabel;

	private Button _copyLocalButton;

	private Button _playPauseButton;

	private CheckButton _loopButton;

	private SpinBox _speedSpin;

	private SpinBox _lengthSpin;

	private SpinBox _stepSpin;

	private SpinBox _zoomSpin;

	private Label _timeLabel;

	private ItemList _propertyPalette;

	private ItemList _trackList;

	private Label _selectedTrackLabel;

	private CheckButton _trackEnabledCheck;

	private Button _deleteTrackButton;

	private Button _moveTrackUpButton;

	private Button _moveTrackDownButton;

	private Button _insertKeyButton;

	private Button _deleteKeyButton;

	private Button _duplicateKeyButton;

	private Button _moveKeyEarlierButton;

	private Button _moveKeyLaterButton;

	private Label _selectedKeyLabel;

	private VBoxContainer _valueEditorHost;

	private OptionButton _trackInterpolationOption;

	private OptionButton _valueUpdateModeOption;

	private CheckButton _interpolationLoopWrapCheck;

	private XWVisualSegmentedOption _trackInterpolationSegments;

	private XWVisualSegmentedOption _valueUpdateModeSegments;

	private HFlowContainer _trackInterpolationVisualHost;

	private HFlowContainer _valueUpdateModeVisualHost;

	private HFlowContainer _easePresetShelf;

	private XW2DAnimationEaseCurveCanvas _easeCurveCanvas;

	private SpinBox _keyTransitionSpin;

	private Label _motionCurveStatusLabel;

	private ScrollContainer _workbenchScroll;

	private VBoxContainer _workbenchRoot;

	private ConfirmationDialog _deleteAnimationDialog;

	private AcceptDialog _nameDialog;

	private LineEdit _nameEdit;

	private readonly List<AnimationPlayer> _players = new List<AnimationPlayer>();

	private readonly List<StringName> _libraries = new List<StringName>();

	private readonly List<StringName> _animations = new List<StringName>();

	private readonly List<PropertyChoice> _properties = new List<PropertyChoice>();

	private bool _updating;

	private bool _suspendedForSave;

	private bool _resumePlayingAfterSave;

	private double _resumePositionAfterSave;

	private bool _transitionSpinEditing;

	private bool _transitionSpinPending;

	private float _transitionSpinStart = 1f;

	private int _selectedTrack = -1;

	private double _selectedKeyTime = -1.0;

	private string _pendingNameOperation = "";

	private Dictionary _pendingBindState;

	public AnimationPlayer CurrentPlayer => _player;

	public AnimationLibrary CurrentLibrary => _library;

	public Animation CurrentAnimation => _animation;

	public string CurrentAnimationName => GetSelectedAnimationName();

	public string CurrentLibraryName => GetSelectedLibraryName();

	public int SelectedTrack => _selectedTrack;

	public double SelectedKeyTime => _selectedKeyTime;

	public int TrackCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_animation))
			{
				return 0;
			}
			return _animation.GetTrackCount();
		}
	}

	public int SelectedTrackKeyCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_animation) || _selectedTrack < 0 || _selectedTrack >= _animation.GetTrackCount())
			{
				return 0;
			}
			return _animation.TrackGetKeyCount(_selectedTrack);
		}
	}

	public double CurrentPlayhead => _timeline?.Playhead ?? 0.0;

	public int PreviewUnsafeTrackCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_preview))
			{
				return 0;
			}
			return _preview.UnsafeTrackCount;
		}
	}

	public int PreviewBaselineCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_preview))
			{
				return 0;
			}
			return _preview.BaselineCount;
		}
	}

	public bool IsPreviewPlaying
	{
		get
		{
			if (GodotObject.IsInstanceValid(_preview))
			{
				return _preview.IsPlaying;
			}
			return false;
		}
	}

	public double PreviewPosition
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_preview))
			{
				return 0.0;
			}
			return _preview.CurrentPosition;
		}
	}

	public bool IsReadOnly => IsCurrentAnimationReadOnly();

	public XW2DAnimationEaseCurveCanvas EaseCurveCanvas => _easeCurveCanvas;

	public Animation.InterpolationType SelectedTrackInterpolation
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_animation) || _selectedTrack < 0 || _selectedTrack >= _animation.GetTrackCount())
			{
				return Animation.InterpolationType.Linear;
			}
			return _animation.TrackGetInterpolationType(_selectedTrack);
		}
	}

	public Animation.UpdateMode SelectedValueUpdateMode
	{
		get
		{
			if (!IsEditableValueTrack(_selectedTrack))
			{
				return Animation.UpdateMode.Continuous;
			}
			return _animation.ValueTrackGetUpdateMode(_selectedTrack);
		}
	}

	public bool SelectedTrackInterpolationLoopWrap
	{
		get
		{
			if (GodotObject.IsInstanceValid(_animation) && _selectedTrack >= 0 && _selectedTrack < _animation.GetTrackCount())
			{
				return _animation.TrackGetInterpolationLoopWrap(_selectedTrack);
			}
			return false;
		}
	}

	public float SelectedKeyTransition
	{
		get
		{
			if (!TryGetSelectedKey(out var track, out var key))
			{
				return 1f;
			}
			return _animation.TrackGetKeyTransition(track, key);
		}
	}

	public override void _Ready()
	{
		BuildInterface();
		base.VisibilityChanged += UpdateProcessing;
		SetProcess(enable: false);
		CallDeferred("RefreshWorkbenchLayout");
	}

	public override void _ExitTree()
	{
		_trackInterpolationSegments?.Dispose();
		_valueUpdateModeSegments?.Dispose();
		if (GodotObject.IsInstanceValid(_preview))
		{
			_preview.Clear();
		}
	}

	public override void _Process(double delta)
	{
		if (!base.Visible || !IsVisibleInTree() || !GodotObject.IsInstanceValid(_preview))
		{
			SetProcess(enable: false);
		}
		else if (!_preview.IsPlaying)
		{
			SetProcess(enable: false);
			UpdatePlaybackButton();
		}
		else
		{
			double currentPosition = _preview.CurrentPosition;
			_timeline?.SetPlayhead(currentPosition);
			UpdateTimeLabel(currentPosition);
		}
	}

	public void BindEditor(XW2DSceneEditor editor)
	{
		_editor = editor;
	}

	public void BindScene(Node sceneRoot, Dictionary state)
	{
		if (_sceneRoot == sceneRoot && sceneRoot != null)
		{
			_pendingBindState = state?.Duplicate(deep: true) ?? new Dictionary();
			RefreshSources();
			return;
		}
		StopPreview();
		_sceneRoot = sceneRoot;
		_pendingBindState = state?.Duplicate(deep: true) ?? new Dictionary();
		RefreshSources();
		RefreshSelection();
	}

	public Dictionary CaptureState()
	{
		return new Dictionary
		{
			["player_path"] = (IsNodeInsideScene(_player) ? _sceneRoot.GetPathTo(_player).ToString() : ""),
			["library"] = GetSelectedLibraryName(),
			["animation"] = GetSelectedAnimationName(),
			["playhead"] = (GodotObject.IsInstanceValid(_preview) ? _preview.CurrentPosition : (_timeline?.Playhead ?? 0.0)),
			["speed"] = (GodotObject.IsInstanceValid(_speedSpin) ? _speedSpin.Value : 1.0),
			["loop"] = GodotObject.IsInstanceValid(_loopButton) && _loopButton.ButtonPressed,
			["zoom"] = (GodotObject.IsInstanceValid(_zoomSpin) ? _zoomSpin.Value : 120.0)
		};
	}

	public void RefreshSelection()
	{
		_properties.Clear();
		if (!GodotObject.IsInstanceValid(_propertyPalette))
		{
			return;
		}
		_propertyPalette.Clear();
		Node node = XWEditorInterface.Instance?.GetSceneTreeDock()?.GetSelectedNode();
		if (!IsNodeInsideScene(node))
		{
			node = _sceneRoot;
		}
		if (!GodotObject.IsInstanceValid(node))
		{
			_propertyPalette.AddItem("先在画布或场景树选择节点");
			_propertyPalette.SetItemDisabled(0, disabled: true);
			return;
		}
		AddPropertyChoices(node);
		if (_properties.Count == 0)
		{
			_propertyPalette.AddItem($"{node.Name} 没有可直接制作动画的常用属性");
			_propertyPalette.SetItemDisabled(0, disabled: true);
			return;
		}
		foreach (PropertyChoice property in _properties)
		{
			_propertyPalette.AddItem(property.Label);
		}
		_propertyPalette.Select(0);
	}

	public bool SuspendForSave()
	{
		if (_suspendedForSave)
		{
			return GodotObject.IsInstanceValid(_animation);
		}
		CommitPendingTransitionInput();
		_suspendedForSave = true;
		_resumePlayingAfterSave = GodotObject.IsInstanceValid(_preview) && _preview.IsPlaying;
		_resumePositionAfterSave = (GodotObject.IsInstanceValid(_preview) ? _preview.CurrentPosition : (_timeline?.Playhead ?? 0.0));
		_preview?.Stop();
		SetProcess(enable: false);
		SetStatus("已暂停安全预览，正在保存场景。");
		return GodotObject.IsInstanceValid(_animation);
	}

	public void ResumeAfterSave()
	{
		if (!_suspendedForSave)
		{
			return;
		}
		_suspendedForSave = false;
		if (GodotObject.IsInstanceValid(_animation))
		{
			BindSafePreview();
			double num = ClampTime(_resumePositionAfterSave);
			_preview.Seek(num);
			_timeline.SetPlayhead(num);
			if (_resumePlayingAfterSave)
			{
				_preview.Play();
				SetProcess(IsVisibleInTree());
			}
		}
		_resumePlayingAfterSave = false;
		_resumePositionAfterSave = 0.0;
		UpdatePlaybackButton();
		SetStatus("保存完成；动画工作台已恢复。");
	}

	public void RefreshFromSceneHistory()
	{
		string selectedAnimationName = GetSelectedAnimationName();
		double time = _timeline?.Playhead ?? 0.0;
		PauseForMutation();
		RefreshLibraries();
		if (!string.IsNullOrWhiteSpace(selectedAnimationName))
		{
			RefreshAnimations(selectedAnimationName);
		}
		time = ClampTime(time);
		_timeline?.SetPlayhead(time);
		_preview?.Seek(time);
		RefreshSelection();
		_editor?.QueueViewportRedraw();
	}

	public void ClearSurface()
	{
		StopPreview();
		_preview?.Clear();
		_sceneRoot = null;
		_player = null;
		_library = null;
		_animation = null;
		_players.Clear();
		_libraries.Clear();
		_animations.Clear();
		_properties.Clear();
		_playerOption?.Clear();
		_libraryOption?.Clear();
		_animationOption?.Clear();
		_trackList?.Clear();
		_propertyPalette?.Clear();
		_timeline?.BindAnimation(null);
		RefreshValueEditor();
		UpdateEditingState();
		SetStatus("打开 2D 场景后可编辑 AnimationPlayer 动画。");
	}

	public bool CreateAnimation(string requestedName = "")
	{
		if (!GodotObject.IsInstanceValid(_player))
		{
			return Fail("当前场景没有可用的动画播放器。");
		}
		if (!GodotObject.IsInstanceValid(_library))
		{
			return Fail("请先选择一个动画库。");
		}
		if (IsLibraryExternal(_library))
		{
			return Fail("外部动画库只读；请先复制片段到当前场景。");
		}
		string text = BuildUniqueAnimationName(string.IsNullOrWhiteSpace(requestedName) ? "新动画" : requestedName.Trim());
		Animation snapshot = new Animation
		{
			Length = 1.0,
			Step = 1f / 30f,
			ResourceLocalToScene = true
		};
		return CommitAnimationSlot("新建动画片段", _library, text, snapshot, add: true, text);
	}

	public bool SelectAnimation(string playerPath, string libraryName, string animationName)
	{
		int num = -1;
		for (int i = 0; i < _players.Count; i++)
		{
			if (string.Equals(IsNodeInsideScene(_players[i]) ? _sceneRoot.GetPathTo(_players[i]).ToString() : ((string?)_players[i].Name), playerPath, StringComparison.Ordinal) || string.Equals(_players[i].Name, playerPath, StringComparison.Ordinal))
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			return Fail("找不到动画播放器：" + playerPath);
		}
		_updating = true;
		_playerOption.Select(num);
		_player = _players[num];
		_pendingBindState = new Dictionary
		{
			["library"] = libraryName ?? "",
			["animation"] = animationName ?? ""
		};
		RefreshLibraries();
		_updating = false;
		ApplyPendingState();
		if (GodotObject.IsInstanceValid(_animation))
		{
			return string.Equals(GetSelectedAnimationName(), animationName, StringComparison.Ordinal);
		}
		return false;
	}

	public bool SelectKey(int trackIndex, double keyTime)
	{
		if (!GodotObject.IsInstanceValid(_animation) || trackIndex < 0 || trackIndex >= _animation.GetTrackCount() || FindKeyAtTime(_animation, trackIndex, keyTime) < 0)
		{
			return Fail("找不到指定关键帧。");
		}
		_selectedTrack = trackIndex;
		_selectedKeyTime = keyTime;
		_timeline.SelectKey(trackIndex, keyTime);
		if (_trackList.ItemCount > trackIndex)
		{
			_trackList.Select(trackIndex);
		}
		RefreshTrackSelection();
		return true;
	}

	public void SeekPreview(double position)
	{
		OnTimelinePlayheadChanged(position);
	}

	public void PlayPreview()
	{
		if (GodotObject.IsInstanceValid(_animation) && !_suspendedForSave)
		{
			if (!_preview.IsPlaying)
			{
				_preview.Play();
				SetProcess(IsVisibleInTree());
			}
			UpdatePlaybackButton();
		}
	}

	public void PausePreview()
	{
		_preview?.Pause();
		SetProcess(enable: false);
		UpdatePlaybackButton();
	}

	public bool AddValueTrack()
	{
		int[] array = _propertyPalette?.GetSelectedItems();
		int num = ((array != null && array.Length > 0) ? array[0] : (-1));
		if (num < 0 || num >= _properties.Count)
		{
			return Fail("请先从属性图库选择一个节点属性。");
		}
		PropertyChoice propertyChoice = _properties[num];
		return AddValueTrack(propertyChoice.Node, propertyChoice.Property);
	}

	public bool AddValueTrack(Node node, string propertyName)
	{
		if (!IsNodeInsideScene(node) || string.IsNullOrWhiteSpace(propertyName))
		{
			return Fail("属性目标不在当前场景中。");
		}
		Node node2 = ResolveAnimationRoot(_player);
		if (!GodotObject.IsInstanceValid(node2))
		{
			return Fail("动画播放器的根节点路径无效。");
		}
		string text = ((node2 == node) ? "." : node2.GetPathTo(node).ToString());
		return AddValueTrack(text + ":" + propertyName);
	}

	public bool AddValueTrack(string propertyPath)
	{
		if (!EnsureEditable("新增属性轨道"))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(propertyPath) || !propertyPath.Contains(':'))
		{
			return Fail("轨道路径应为“节点路径:属性”。");
		}
		return CommitAnimationEdit("新增属性轨道", delegate(Animation animation)
		{
			int num = animation.AddTrack(Animation.TrackType.Value);
			animation.TrackSetPath(num, new NodePath(propertyPath));
			animation.ValueTrackSetUpdateMode(num, Animation.UpdateMode.Continuous);
			_selectedTrack = num;
			_selectedKeyTime = -1.0;
			return true;
		});
	}

	public bool SetSelectedTrackInterpolation(Animation.InterpolationType interpolation)
	{
		if (!EnsureEditable("切换轨道插值") || !GodotObject.IsInstanceValid(_animation) || _selectedTrack < 0 || _selectedTrack >= _animation.GetTrackCount())
		{
			return Fail("请先选择可编辑的动画轨道。");
		}
		int track = _selectedTrack;
		if (IsAngleInterpolation(interpolation) && !IsRotationTrack(track))
		{
			return Fail("角度插值只适用于 rotation 旋转属性轨道。");
		}
		if (_animation.TrackGetInterpolationType(track) == interpolation)
		{
			return true;
		}
		return CommitAnimationEdit("切换轨道插值为" + GetInterpolationLabel(interpolation), delegate(Animation animation)
		{
			animation.TrackSetInterpolationType(track, interpolation);
			_selectedTrack = track;
			return true;
		});
	}

	public bool SetSelectedValueUpdateMode(Animation.UpdateMode updateMode)
	{
		if (!EnsureEditable("切换属性轨更新方式") || !IsEditableValueTrack(_selectedTrack))
		{
			return Fail("更新方式只适用于可编辑的 Value 属性轨道。");
		}
		int track = _selectedTrack;
		if (_animation.ValueTrackGetUpdateMode(track) == updateMode)
		{
			return true;
		}
		return CommitAnimationEdit("切换属性轨为" + GetUpdateModeLabel(updateMode) + "更新", delegate(Animation animation)
		{
			animation.ValueTrackSetUpdateMode(track, updateMode);
			_selectedTrack = track;
			return true;
		});
	}

	public bool SetSelectedTrackInterpolationLoopWrap(bool enabled)
	{
		if (!EnsureEditable("切换循环插值衔接") || !GodotObject.IsInstanceValid(_animation) || _selectedTrack < 0 || _selectedTrack >= _animation.GetTrackCount())
		{
			return Fail("请先选择可编辑的动画轨道。");
		}
		int track = _selectedTrack;
		if (_animation.TrackGetInterpolationLoopWrap(track) == enabled)
		{
			return true;
		}
		return CommitAnimationEdit(enabled ? "开启循环插值衔接" : "关闭循环插值衔接", delegate(Animation animation)
		{
			animation.TrackSetInterpolationLoopWrap(track, enabled);
			_selectedTrack = track;
			return true;
		});
	}

	public bool SetSelectedKeyTransition(float transition)
	{
		if (!TryGetSelectedKey(out var track, out var key))
		{
			return Fail("请先选择要编辑缓动的关键帧。");
		}
		float normalized = NormalizeTransition(transition);
		if (Mathf.IsEqualApprox(_animation.TrackGetKeyTransition(track, key), normalized))
		{
			return true;
		}
		double keyTime = _animation.TrackGetKeyTime(track, key);
		return CommitAnimationEdit("调整关键帧运动曲线", delegate(Animation animation)
		{
			int num = FindKeyAtTime(animation, track, keyTime);
			if (num < 0)
			{
				return false;
			}
			animation.TrackSetKeyTransition(track, num, normalized);
			_selectedTrack = track;
			_selectedKeyTime = keyTime;
			return true;
		});
	}

	public bool InsertCurrentKey()
	{
		if (!TryReadCurrentTrackValue(out var value))
		{
			return false;
		}
		return InsertCurrentKey(value);
	}

	public bool InsertCurrentKey(Variant value)
	{
		if (!EnsureEditable("插入关键帧") || !IsEditableValueTrack(_selectedTrack))
		{
			return Fail("只能给属性值轨道插入关键帧。");
		}
		double time = ClampTime(_timeline?.Playhead ?? 0.0);
		int track = _selectedTrack;
		return CommitAnimationEdit("在播放头写入当前值", delegate(Animation animation)
		{
			animation.TrackInsertKey(track, time, value);
			_selectedTrack = track;
			_selectedKeyTime = time;
			return true;
		});
	}

	public bool MoveSelectedKey(double newTime)
	{
		if (!TryGetSelectedKey(out var track, out var key))
		{
			return false;
		}
		double sourceTime = _animation.TrackGetKeyTime(track, key);
		double targetTime = ClampTime(newTime);
		if (Mathf.IsEqualApprox(sourceTime, targetTime))
		{
			return true;
		}
		return CommitAnimationEdit("移动关键帧", delegate(Animation animation)
		{
			int num = FindKeyAtTime(animation, track, sourceTime);
			if (num < 0)
			{
				return false;
			}
			Variant key2 = animation.TrackGetKeyValue(track, num);
			float transition = animation.TrackGetKeyTransition(track, num);
			animation.TrackRemoveKey(track, num);
			int keyIdx = animation.TrackInsertKey(track, targetTime, key2, transition);
			_selectedTrack = track;
			_selectedKeyTime = animation.TrackGetKeyTime(track, keyIdx);
			return true;
		});
	}

	public bool DeleteSelectedKey()
	{
		if (!TryGetSelectedKey(out var track, out var key))
		{
			return false;
		}
		double time = _animation.TrackGetKeyTime(track, key);
		return CommitAnimationEdit("删除关键帧", delegate(Animation animation)
		{
			int num = FindKeyAtTime(animation, track, time);
			if (num < 0)
			{
				return false;
			}
			animation.TrackRemoveKey(track, num);
			_selectedTrack = track;
			_selectedKeyTime = -1.0;
			return true;
		});
	}

	private void BuildInterface()
	{
		base.CustomMinimumSize = new Vector2(420f, 260f);
		AddThemeStyleboxOverride("panel", MakePanelStyle(new Color("#101923"), new Color("#36506a"), 10, 1));
		_workbenchScroll = new ScrollContainer
		{
			Name = "AnimationWorkbenchScroll",
			CustomMinimumSize = new Vector2(0f, 220f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.Auto
		};
		AddChild(_workbenchScroll, forceReadableName: false, InternalMode.Disabled);
		_workbenchRoot = new VBoxContainer
		{
			Name = "AnimationWorkbench",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_workbenchRoot.AddThemeConstantOverride("separation", 8);
		_workbenchScroll.AddChild(_workbenchRoot, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer workbenchRoot = _workbenchRoot;
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(MakeIconBadge("◆", new Color("#67d9ff")), forceReadableName: false, InternalMode.Disabled);
		Label label = new Label
		{
			Text = "2D 动画工作台",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", 18);
		label.AddThemeColorOverride("font_color", new Color("#f5dc91"));
		hBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		Button button = MakeButton("刷新节点", "重新扫描 AnimationPlayer 与所选节点");
		button.Pressed += delegate
		{
			RefreshSources();
			RefreshSelection();
		};
		hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		workbenchRoot.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer = MakeSection(workbenchRoot, "播放器 · 动画库 · 片段", "场景内资源直接编辑，外部与共享资源保持只读");
		HBoxContainer hBoxContainer2 = new HBoxContainer();
		hBoxContainer2.AddThemeConstantOverride("separation", 6);
		_playerOption = MakeOption("选择 AnimationPlayer");
		_libraryOption = MakeOption("选择动画库");
		_animationOption = MakeOption("选择动画片段");
		_playerOption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_libraryOption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_animationOption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer2.AddChild(Labeled("播放器", _playerOption), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer2.AddChild(Labeled("动画库", _libraryOption), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer2.AddChild(Labeled("片段", _animationOption), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		_playerOption.ItemSelected += OnPlayerSelected;
		_libraryOption.ItemSelected += OnLibrarySelected;
		_animationOption.ItemSelected += OnAnimationSelected;
		HBoxContainer hBoxContainer3 = new HBoxContainer();
		Button button2 = MakeButton("＋ 新建", "在当前动画库新建片段");
		Button button3 = MakeButton("▣ 复制", "复制当前片段为本地可编辑资源");
		Button button4 = MakeButton("✎ 改名", "重命名当前片段");
		Button button5 = MakeButton("× 删除", "删除当前片段");
		_copyLocalButton = MakeButton("⇩ 复制到当前场景", "外部或共享动画先复制为场景本地片段");
		button2.Pressed += delegate
		{
			OpenNameDialog("create", "新建动画片段", "新动画");
		};
		button3.Pressed += DuplicateCurrentAnimation;
		button4.Pressed += delegate
		{
			OpenNameDialog("rename", "重命名动画片段", GetSelectedAnimationName());
		};
		button5.Pressed += ConfirmDeleteAnimation;
		_copyLocalButton.Pressed += CopyCurrentAnimationToScene;
		Button[] array = new Button[5] { button2, button3, button4, button5, _copyLocalButton };
		foreach (Button node in array)
		{
			hBoxContainer3.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
		vBoxContainer.AddChild(hBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		_treeLinkLabel = MakeHintLabel("");
		_readonlyLabel = MakeHintLabel("");
		vBoxContainer.AddChild(_treeLinkLabel, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(_readonlyLabel, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer2 = MakeSection(workbenchRoot, "安全片段预览", "只复制安全属性轨道；方法、音频与嵌套动画轨不会执行");
		HBoxContainer hBoxContainer4 = new HBoxContainer();
		Button button6 = MakeButton("◀◆", "跳到前一个关键帧");
		_playPauseButton = MakeButton("▶ 播放", "在隔离播放器中播放安全轨道");
		Button button7 = MakeButton("■ 停止", "停止并回到片段起点");
		Button button8 = MakeButton("◆▶", "跳到后一个关键帧");
		_loopButton = new CheckButton
		{
			Text = "循环",
			ButtonPressed = false
		};
		_speedSpin = MakeSpin(0.1, 4.0, 0.1, 1.0, "0.0×");
		_lengthSpin = MakeSpin(0.01, 3600.0, 0.01, 1.0, "0.00 秒");
		_stepSpin = MakeSpin(0.001, 1.0, 0.001, 1.0 / 30.0, "0.000 秒");
		_zoomSpin = MakeSpin(24.0, 480.0, 8.0, 120.0, "0 像素/秒");
		_timeLabel = new Label
		{
			Text = "0.00 / 0.00 秒",
			CustomMinimumSize = new Vector2(130f, 0f)
		};
		hBoxContainer4.AddChild(button6, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(_playPauseButton, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(button7, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(button8, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(_loopButton, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(Labeled("速度", _speedSpin), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(Labeled("长度", _lengthSpin), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(Labeled("步长", _stepSpin), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(Labeled("缩放", _zoomSpin), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer4.AddChild(_timeLabel, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(hBoxContainer4, forceReadableName: false, InternalMode.Disabled);
		_timeline = new XW2DAnimationTimelineCanvas
		{
			Name = "TimelineCanvas",
			CustomMinimumSize = new Vector2(0f, 190f),
			SizeFlagsVertical = SizeFlags.Fill,
			MouseFilter = MouseFilterEnum.Stop
		};
		vBoxContainer2.AddChild(_timeline, forceReadableName: false, InternalMode.Disabled);
		_preview = new XW2DAnimationSafePreview
		{
			Name = "SafePreview"
		};
		AddChild(_preview, forceReadableName: false, InternalMode.Disabled);
		_previewSafetyLabel = MakeHintLabel("预览尚未绑定。源 AnimationPlayer 不会被播放。");
		vBoxContainer2.AddChild(_previewSafetyLabel, forceReadableName: false, InternalMode.Disabled);
		_playPauseButton.Pressed += TogglePlayback;
		button7.Pressed += StopPreview;
		button6.Pressed += delegate
		{
			JumpToKey(-1);
		};
		button8.Pressed += delegate
		{
			JumpToKey(1);
		};
		_loopButton.Toggled += OnLoopToggled;
		_speedSpin.ValueChanged += OnSpeedChanged;
		_lengthSpin.ValueChanged += OnLengthChanged;
		_stepSpin.ValueChanged += OnStepChanged;
		_zoomSpin.ValueChanged += delegate(double value)
		{
			_timeline.SetTimeScale(value);
		};
		_timeline.PlayheadChanged += OnTimelinePlayheadChanged;
		_timeline.KeySelected += OnTimelineKeySelected;
		_timeline.KeyMoveRequested += OnTimelineKeyMoveRequested;
		HSplitContainer hSplitContainer = new HSplitContainer();
		hSplitContainer.CustomMinimumSize = new Vector2(0f, 500f);
		hSplitContainer.SizeFlagsVertical = SizeFlags.Fill;
		hSplitContainer.SplitOffsets = new int[1];
		HSplitContainer hSplitContainer2 = hSplitContainer;
		workbenchRoot.AddChild(hSplitContainer2, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer3 = MakeStandaloneSection("所选节点属性图库", "选择可视属性后，一键生成属性轨道");
		vBoxContainer3.SizeFlagsHorizontal = SizeFlags.Fill;
		hSplitContainer2.AddChild(vBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		_propertyPalette = new ItemList
		{
			Name = "PropertyPalette",
			CustomMinimumSize = new Vector2(280f, 160f),
			SizeFlagsVertical = SizeFlags.ExpandFill,
			AllowReselect = true
		};
		vBoxContainer3.AddChild(_propertyPalette, forceReadableName: false, InternalMode.Disabled);
		Button button9 = MakeButton("＋ 当前属性生成轨道", "不打开检查器，直接将所选属性加入时间轴");
		button9.Pressed += delegate
		{
			AddValueTrack();
		};
		vBoxContainer3.AddChild(button9, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer4 = MakeStandaloneSection("轨道与关键帧", "轨道按执行顺序排列；菱形可拖动换时刻");
		vBoxContainer4.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hSplitContainer2.AddChild(vBoxContainer4, forceReadableName: false, InternalMode.Disabled);
		hSplitContainer = new HSplitContainer();
		hSplitContainer.CustomMinimumSize = new Vector2(0f, 180f);
		hSplitContainer.SizeFlagsVertical = SizeFlags.Fill;
		hSplitContainer.SplitOffsets = new int[1];
		HSplitContainer hSplitContainer3 = hSplitContainer;
		vBoxContainer4.AddChild(hSplitContainer3, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer5 = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(300f, 0f),
			SizeFlagsHorizontal = SizeFlags.Fill
		};
		hSplitContainer3.AddChild(vBoxContainer5, forceReadableName: false, InternalMode.Disabled);
		_trackList = new ItemList
		{
			Name = "TrackList",
			CustomMinimumSize = new Vector2(300f, 150f),
			SizeFlagsVertical = SizeFlags.ExpandFill,
			AllowReselect = true
		};
		_trackList.ItemSelected += OnTrackListSelected;
		vBoxContainer5.AddChild(_trackList, forceReadableName: false, InternalMode.Disabled);
		_selectedTrackLabel = MakeHintLabel("未选择轨道");
		vBoxContainer5.AddChild(_selectedTrackLabel, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer5 = new HBoxContainer();
		_trackEnabledCheck = new CheckButton
		{
			Text = "启用轨道"
		};
		_deleteTrackButton = MakeButton("删除", "删除整条轨道");
		_moveTrackUpButton = MakeButton("↑", "轨道上移");
		_moveTrackDownButton = MakeButton("↓", "轨道下移");
		_trackEnabledCheck.Toggled += ToggleSelectedTrack;
		_deleteTrackButton.Pressed += DeleteSelectedTrack;
		_moveTrackUpButton.Pressed += delegate
		{
			MoveSelectedTrack(-1);
		};
		_moveTrackDownButton.Pressed += delegate
		{
			MoveSelectedTrack(1);
		};
		hBoxContainer5.AddChild(_trackEnabledCheck, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer5.AddChild(_moveTrackUpButton, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer5.AddChild(_moveTrackDownButton, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer5.AddChild(_deleteTrackButton, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer5.AddChild(hBoxContainer5, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer6 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hSplitContainer3.AddChild(vBoxContainer6, forceReadableName: false, InternalMode.Disabled);
		_selectedKeyLabel = MakeHintLabel("选择时间轴上的菱形关键帧");
		vBoxContainer6.AddChild(_selectedKeyLabel, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer6 = new HBoxContainer();
		_insertKeyButton = MakeButton("◆ 写入当前值", "读取场景节点当前属性并写入播放头");
		_duplicateKeyButton = MakeButton("▣ 复制帧", "复制到下一步长");
		_deleteKeyButton = MakeButton("× 删除帧", "删除选中关键帧");
		_moveKeyEarlierButton = MakeButton("← 前移", "前移一个动画步长");
		_moveKeyLaterButton = MakeButton("后移 →", "后移一个动画步长");
		_insertKeyButton.Pressed += delegate
		{
			InsertCurrentKey();
		};
		_duplicateKeyButton.Pressed += DuplicateSelectedKey;
		_deleteKeyButton.Pressed += delegate
		{
			DeleteSelectedKey();
		};
		_moveKeyEarlierButton.Pressed += delegate
		{
			MoveSelectedKey(((_selectedKeyTime < 0.0) ? 0.0 : _selectedKeyTime) - GetStep());
		};
		_moveKeyLaterButton.Pressed += delegate
		{
			MoveSelectedKey(((_selectedKeyTime < 0.0) ? 0.0 : _selectedKeyTime) + GetStep());
		};
		array = new Button[5] { _insertKeyButton, _duplicateKeyButton, _deleteKeyButton, _moveKeyEarlierButton, _moveKeyLaterButton };
		foreach (Button node2 in array)
		{
			hBoxContainer6.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
		}
		vBoxContainer6.AddChild(hBoxContainer6, forceReadableName: false, InternalMode.Disabled);
		_valueEditorHost = new VBoxContainer
		{
			Name = "DirectKeyValueEditor",
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer6.AddChild(_valueEditorHost, forceReadableName: false, InternalMode.Disabled);
		workbenchRoot.AddChild(BuildMotionCurveCard(), forceReadableName: false, InternalMode.Disabled);
		_statusLabel = MakeHintLabel("打开包含 AnimationPlayer 的 2D 场景即可开始。");
		_statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		workbenchRoot.AddChild(_statusLabel, forceReadableName: false, InternalMode.Disabled);
		BuildDialogs();
		UpdateEditingState();
	}

	private void RefreshWorkbenchLayout()
	{
		if (GodotObject.IsInstanceValid(_workbenchScroll) && GodotObject.IsInstanceValid(_workbenchRoot))
		{
			_workbenchRoot.UpdateMinimumSize();
			_workbenchRoot.QueueSort();
			_workbenchScroll.UpdateMinimumSize();
			_workbenchScroll.QueueSort();
		}
	}

	private Control BuildMotionCurveCard()
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = "MotionCurveCard";
		panelContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		panelContainer.AddThemeStyleboxOverride("panel", MakePanelStyle(ModEditorTheme.GrassColor.Darkened(0.42f), ModEditorTheme.LeafColor.Darkened(0.18f), 10, 1));
		XWUiMotion.BindPanel(panelContainer);
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 7);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		hBoxContainer.AddChild(new TextureRect
		{
			Texture = LoadClassIcon("CurveEdit"),
			CustomMinimumSize = new Vector2(28f, 28f),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		}, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Label label = new Label
		{
			Text = "运动曲线"
		};
		label.AddThemeFontSizeOverride("font_size", 15);
		label.AddThemeColorOverride("font_color", ModEditorTheme.SunGoldColor.Lightened(0.16f));
		vBoxContainer2.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		_motionCurveStatusLabel = MakeHintLabel("选择 Value 轨和关键帧后，可直接配置插值、更新方式与缓动。");
		_motionCurveStatusLabel.Name = "MotionCurveStatus";
		_motionCurveStatusLabel.Modulate = ModEditorTheme.DimTextColor.Lightened(0.16f);
		vBoxContainer2.AddChild(_motionCurveStatusLabel, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(MakeSmallHeading("轨道插值"), forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "TrackInterpolationSegments",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 5);
		hFlowContainer.AddThemeConstantOverride("v_separation", 5);
		vBoxContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		_trackInterpolationOption = new OptionButton
		{
			Name = "TrackInterpolationOption",
			TooltipText = "选择当前轨道在关键帧之间的插值方式"
		};
		AddOptionItem(_trackInterpolationOption, "保持", Animation.InterpolationType.Nearest);
		AddOptionItem(_trackInterpolationOption, "线性", Animation.InterpolationType.Linear);
		AddOptionItem(_trackInterpolationOption, "三次平滑", Animation.InterpolationType.Cubic);
		AddOptionItem(_trackInterpolationOption, "角度线性", Animation.InterpolationType.LinearAngle);
		AddOptionItem(_trackInterpolationOption, "角度平滑", Animation.InterpolationType.CubicAngle);
		_trackInterpolationOption.ItemSelected += OnTrackInterpolationSelected;
		hFlowContainer.AddChild(_trackInterpolationOption, forceReadableName: false, InternalMode.Disabled);
		_trackInterpolationVisualHost = new HFlowContainer
		{
			Name = "TrackInterpolationVisualOptions",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddChild(_trackInterpolationVisualHost, forceReadableName: false, InternalMode.Disabled);
		_trackInterpolationSegments = new XWVisualSegmentedOption(_trackInterpolationOption, _trackInterpolationVisualHost, (int index) => LoadClassIcon(index switch
		{
			0 => "InterpRaw", 
			1 => "InterpLinear", 
			2 => "InterpCubic", 
			3 => "InterpLinearAngle", 
			_ => "InterpCubicAngle", 
		}));
		_trackInterpolationSegments.Rebuild();
		StyleMotionSegments(_trackInterpolationVisualHost);
		_interpolationLoopWrapCheck = new CheckButton
		{
			Name = "InterpolationLoopWrapCheck",
			Text = "↻ 循环衔接",
			TooltipText = "开启后，循环动画会让末帧与首帧参与连续插值"
		};
		_interpolationLoopWrapCheck.Toggled += delegate(bool enabled)
		{
			if (!_updating)
			{
				SetSelectedTrackInterpolationLoopWrap(enabled);
			}
		};
		XWUiMotion.BindButton(_interpolationLoopWrapCheck, XWUiMotion.MotionRole.Card);
		vBoxContainer.AddChild(_interpolationLoopWrapCheck, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(MakeSmallHeading("Value 轨更新方式"), forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer2 = new HFlowContainer
		{
			Name = "ValueUpdateModeSegments",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer2.AddThemeConstantOverride("h_separation", 5);
		hFlowContainer2.AddThemeConstantOverride("v_separation", 5);
		vBoxContainer.AddChild(hFlowContainer2, forceReadableName: false, InternalMode.Disabled);
		_valueUpdateModeOption = new OptionButton
		{
			Name = "ValueUpdateModeOption",
			TooltipText = "选择属性值连续变化、到帧跳变或从当前对象捕获"
		};
		AddOptionItem(_valueUpdateModeOption, "连续", Animation.UpdateMode.Continuous);
		AddOptionItem(_valueUpdateModeOption, "离散", Animation.UpdateMode.Discrete);
		AddOptionItem(_valueUpdateModeOption, "捕获", Animation.UpdateMode.Capture);
		_valueUpdateModeOption.ItemSelected += OnValueUpdateModeSelected;
		hFlowContainer2.AddChild(_valueUpdateModeOption, forceReadableName: false, InternalMode.Disabled);
		_valueUpdateModeVisualHost = new HFlowContainer
		{
			Name = "ValueUpdateModeVisualOptions",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer2.AddChild(_valueUpdateModeVisualHost, forceReadableName: false, InternalMode.Disabled);
		_valueUpdateModeSegments = new XWVisualSegmentedOption(_valueUpdateModeOption, _valueUpdateModeVisualHost, (int index) => LoadClassIcon(index switch
		{
			0 => "TrackContinuous", 
			1 => "TrackDiscrete", 
			_ => "TrackCapture", 
		}));
		_valueUpdateModeSegments.Rebuild();
		StyleMotionSegments(_valueUpdateModeVisualHost);
		vBoxContainer.AddChild(MakeSmallHeading("关键帧缓动"), forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer3 = new VBoxContainer
		{
			Name = "KeyTransitionCurveBody",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer3.AddThemeConstantOverride("separation", 8);
		vBoxContainer.AddChild(vBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		_easeCurveCanvas = new XW2DAnimationEaseCurveCanvas
		{
			Name = "KeyTransitionCurve",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_easeCurveCanvas.TransitionPreviewChanged += OnEaseTransitionPreviewChanged;
		_easeCurveCanvas.TransitionCommitRequested += OnEaseTransitionCommitRequested;
		vBoxContainer3.AddChild(_easeCurveCanvas, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer4 = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(250f, 0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer3.AddChild(vBoxContainer4, forceReadableName: false, InternalMode.Disabled);
		_keyTransitionSpin = MakeSpin(-1000000.0, 1000000.0, 0.01, 1.0, "");
		_keyTransitionSpin.Name = "KeyTransitionSpin";
		_keyTransitionSpin.TooltipText = "输入 Godot transition 精确曲率；失去焦点时只提交一次";
		_keyTransitionSpin.ValueChanged += OnTransitionSpinValueChanged;
		LineEdit lineEdit = _keyTransitionSpin.GetLineEdit();
		lineEdit.FocusEntered += BeginTransitionSpinEdit;
		lineEdit.FocusExited += CommitTransitionSpinEdit;
		lineEdit.TextSubmitted += delegate
		{
			CommitTransitionSpinEdit();
		};
		vBoxContainer4.AddChild(Labeled("精确曲率", _keyTransitionSpin), forceReadableName: false, InternalMode.Disabled);
		_easePresetShelf = new HFlowContainer
		{
			Name = "EasePresetShelf",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_easePresetShelf.AddThemeConstantOverride("h_separation", 5);
		_easePresetShelf.AddThemeConstantOverride("v_separation", 5);
		vBoxContainer4.AddChild(_easePresetShelf, forceReadableName: false, InternalMode.Disabled);
		AddEasePreset("EaseLinearPreset", "匀速", "CurveLinear", 1f);
		AddEasePreset("EaseInPreset", "缓入", "CurveIn", 2f);
		AddEasePreset("EaseOutPreset", "缓出", "CurveOut", 0.5f);
		AddEasePreset("EaseInOutPreset", "缓入缓出", "CurveInOut", -2f);
		return panelContainer;
	}

	private void AddEasePreset(string nodeName, string label, string iconName, float transition)
	{
		Button button = new Button
		{
			Name = nodeName,
			Text = label,
			TooltipText = $"{label} · transition {transition:0.###}",
			Icon = LoadClassIcon(iconName),
			ExpandIcon = false,
			ThemeTypeVariation = "GameNavCard",
			CustomMinimumSize = new Vector2(112f, 42f)
		};
		button.AddThemeConstantOverride("icon_max_width", 24);
		button.Pressed += delegate
		{
			SetSelectedKeyTransition(transition);
		};
		_easePresetShelf.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		XWUiMotion.BindButton(button, XWUiMotion.MotionRole.Card);
	}

	private void OnTrackInterpolationSelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_trackInterpolationOption) && index >= 0 && index < _trackInterpolationOption.ItemCount)
		{
			int itemId = _trackInterpolationOption.GetItemId((int)index);
			SetSelectedTrackInterpolation((Animation.InterpolationType)itemId);
		}
	}

	private void OnValueUpdateModeSelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_valueUpdateModeOption) && index >= 0 && index < _valueUpdateModeOption.ItemCount)
		{
			int itemId = _valueUpdateModeOption.GetItemId((int)index);
			SetSelectedValueUpdateMode((Animation.UpdateMode)itemId);
		}
	}

	private void OnEaseTransitionPreviewChanged(float transition)
	{
		if (GodotObject.IsInstanceValid(_keyTransitionSpin))
		{
			_updating = true;
			_keyTransitionSpin.Value = transition;
			_updating = false;
		}
		if (GodotObject.IsInstanceValid(_motionCurveStatusLabel))
		{
			_motionCurveStatusLabel.Text = $"正在预览曲率 {transition:0.###}；松开后只写入一次撤销历史。";
		}
	}

	private void OnEaseTransitionCommitRequested(float _, float transition)
	{
		if (!SetSelectedKeyTransition(transition) && GodotObject.IsInstanceValid(_easeCurveCanvas))
		{
			_easeCurveCanvas.SetTransition(SelectedKeyTransition);
		}
	}

	private void BeginTransitionSpinEdit()
	{
		if (!_updating && TryGetSelectedKey(out var _, out var _))
		{
			_transitionSpinEditing = true;
			_transitionSpinPending = false;
			_transitionSpinStart = SelectedKeyTransition;
		}
	}

	private void OnTransitionSpinValueChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_easeCurveCanvas))
		{
			float num = NormalizeTransition((float)value);
			_easeCurveCanvas.SetTransition(num);
			_transitionSpinPending = true;
			if (!_transitionSpinEditing && GodotObject.IsInstanceValid(_keyTransitionSpin) && !_keyTransitionSpin.GetLineEdit().HasFocus())
			{
				_transitionSpinPending = false;
				SetSelectedKeyTransition(num);
			}
		}
	}

	private void CommitTransitionSpinEdit()
	{
		if (!_transitionSpinPending)
		{
			_transitionSpinEditing = false;
			return;
		}
		float num = (GodotObject.IsInstanceValid(_keyTransitionSpin) ? NormalizeTransition((float)_keyTransitionSpin.Value) : _transitionSpinStart);
		_transitionSpinEditing = false;
		_transitionSpinPending = false;
		if (!Mathf.IsEqualApprox(_transitionSpinStart, num))
		{
			SetSelectedKeyTransition(num);
		}
	}

	private void CommitPendingTransitionInput()
	{
		if (GodotObject.IsInstanceValid(_keyTransitionSpin))
		{
			LineEdit lineEdit = _keyTransitionSpin.GetLineEdit();
			if (GodotObject.IsInstanceValid(lineEdit) && lineEdit.HasFocus())
			{
				_keyTransitionSpin.Apply();
			}
			CommitTransitionSpinEdit();
		}
	}

	private void RefreshMotionCurveEditor()
	{
		if (!GodotObject.IsInstanceValid(_trackInterpolationOption) || !GodotObject.IsInstanceValid(_valueUpdateModeOption) || !GodotObject.IsInstanceValid(_easeCurveCanvas))
		{
			return;
		}
		bool flag = GodotObject.IsInstanceValid(_animation) && _selectedTrack >= 0 && _selectedTrack < _animation.GetTrackCount();
		bool flag2 = flag && IsEditableValueTrack(_selectedTrack);
		bool flag3 = flag2 && !IsCurrentAnimationReadOnly();
		int track = -1;
		int key = -1;
		bool flag4 = flag2 && TryGetSelectedKey(out track, out key);
		bool flag5 = flag2 && IsRotationTrack(_selectedTrack);
		Animation.InterpolationType interpolationType = (flag ? _animation.TrackGetInterpolationType(_selectedTrack) : Animation.InterpolationType.Linear);
		Animation.UpdateMode updateMode = (flag2 ? _animation.ValueTrackGetUpdateMode(_selectedTrack) : Animation.UpdateMode.Continuous);
		_updating = true;
		_trackInterpolationOption.Disabled = !flag3;
		_valueUpdateModeOption.Disabled = !flag3;
		SelectOptionById(_trackInterpolationOption, (int)interpolationType);
		SelectOptionById(_valueUpdateModeOption, (int)updateMode);
		_trackInterpolationSegments?.RefreshSelection();
		_valueUpdateModeSegments?.RefreshSelection();
		if (GodotObject.IsInstanceValid(_interpolationLoopWrapCheck))
		{
			_interpolationLoopWrapCheck.Disabled = !flag3;
			_interpolationLoopWrapCheck.SetPressedNoSignal(flag && _animation.TrackGetInterpolationLoopWrap(_selectedTrack));
		}
		_updating = false;
		if (GodotObject.IsInstanceValid(_trackInterpolationVisualHost))
		{
			int num = 0;
			foreach (Node child in _trackInterpolationVisualHost.GetChildren())
			{
				if (child is Button button)
				{
					bool flag6 = num >= 3;
					button.Visible = !flag6 || flag5 || IsAngleInterpolation(interpolationType);
					button.Disabled = !flag3 || (flag6 && !flag5);
				}
				num++;
			}
		}
		if (GodotObject.IsInstanceValid(_valueUpdateModeVisualHost))
		{
			foreach (Node child2 in _valueUpdateModeVisualHost.GetChildren())
			{
				if (child2 is Button button2)
				{
					button2.Disabled = !flag3;
				}
			}
		}
		float num2 = (flag4 ? _animation.TrackGetKeyTransition(track, key) : 1f);
		_updating = true;
		if (GodotObject.IsInstanceValid(_keyTransitionSpin))
		{
			_keyTransitionSpin.Editable = flag3 && flag4;
			_keyTransitionSpin.Value = num2;
		}
		_updating = false;
		_easeCurveCanvas.SetTransition(num2);
		_easeCurveCanvas.SetEditable(flag3 && flag4);
		if (GodotObject.IsInstanceValid(_easePresetShelf))
		{
			foreach (Node child3 in _easePresetShelf.GetChildren())
			{
				if (child3 is Button button3)
				{
					button3.Disabled = !flag3 || !flag4;
				}
			}
		}
		if (!flag)
		{
			_motionCurveStatusLabel.Text = "选择轨道后，这里会显示可视插值与循环衔接。";
			return;
		}
		if (!flag2)
		{
			_motionCurveStatusLabel.Text = "当前轨道仅显示；运动曲线直接编辑适用于 Value 属性轨。";
			return;
		}
		if (!flag4)
		{
			_motionCurveStatusLabel.Text = "轨道模式可编辑；选择一个菱形关键帧即可调整缓动曲线。";
			return;
		}
		if (updateMode == Animation.UpdateMode.Capture)
		{
			_motionCurveStatusLabel.Text = "捕获模式已开启；运行时会从对象当前值衔接，安全预览显示已保存关键帧部分。";
			return;
		}
		_motionCurveStatusLabel.Text = $"{GetInterpolationLabel(interpolationType)} · {GetUpdateModeLabel(updateMode)} · 曲率 {num2:0.###}";
	}

	private static void StyleMotionSegments(HFlowContainer host)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		foreach (Node child in host.GetChildren())
		{
			if (child is Button button)
			{
				button.ThemeTypeVariation = "GameNavCard";
				button.CustomMinimumSize = new Vector2(110f, 40f);
				button.ExpandIcon = false;
				button.AddThemeConstantOverride("icon_max_width", 24);
				XWUiMotion.BindButton(button, XWUiMotion.MotionRole.Card);
			}
		}
	}

	private static Label MakeSmallHeading(string text)
	{
		Label label = new Label();
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", 11);
		label.AddThemeColorOverride("font_color", ModEditorTheme.LeafColor.Lightened(0.42f));
		return label;
	}

	private static void AddOptionItem<TEnum>(OptionButton option, string label, TEnum value) where TEnum : struct, Enum
	{
		option.AddItem(label, Convert.ToInt32(value));
	}

	private static void SelectOptionById(OptionButton option, int id)
	{
		if (!GodotObject.IsInstanceValid(option))
		{
			return;
		}
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemId(i) == id)
			{
				option.Select(i);
				break;
			}
		}
	}

	private static Texture2D LoadClassIcon(string iconName)
	{
		return ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/" + iconName + ".svg", null, ResourceLoader.CacheMode.Reuse);
	}

	private void BuildDialogs()
	{
		_nameDialog = new AcceptDialog
		{
			Title = "动画片段名称",
			DialogText = "输入中文或英文片段名称：",
			OkButtonText = "确定",
			MinSize = new Vector2I(420, 150)
		};
		_nameEdit = new LineEdit
		{
			PlaceholderText = "例如：待机、攻击、胜利"
		};
		_nameDialog.AddChild(_nameEdit, forceReadableName: false, InternalMode.Disabled);
		_nameDialog.Confirmed += OnNameDialogConfirmed;
		AddChild(_nameDialog, forceReadableName: false, InternalMode.Disabled);
		_deleteAnimationDialog = new ConfirmationDialog
		{
			Title = "删除动画片段",
			DialogText = "确定删除当前动画片段？此操作可以撤销。",
			OkButtonText = "删除"
		};
		_deleteAnimationDialog.Confirmed += DeleteCurrentAnimation;
		AddChild(_deleteAnimationDialog, forceReadableName: false, InternalMode.Disabled);
	}

	private void RefreshSources()
	{
		_updating = true;
		string b = ReadStateString(_pendingBindState, "player_path");
		_players.Clear();
		_playerOption.Clear();
		CollectAnimationPlayers(_sceneRoot, _players);
		for (int i = 0; i < _players.Count; i++)
		{
			AnimationPlayer animationPlayer = _players[i];
			string text = (IsNodeInsideScene(animationPlayer) ? _sceneRoot.GetPathTo(animationPlayer).ToString() : ((string?)animationPlayer.Name));
			_playerOption.AddItem("▶ " + text);
			if (string.Equals(text, b, StringComparison.Ordinal))
			{
				_playerOption.Select(i);
			}
		}
		if (_players.Count == 0)
		{
			_playerOption.AddItem("场景中没有 AnimationPlayer");
			_playerOption.SetItemDisabled(0, disabled: true);
			_player = null;
			RefreshLibraries();
			_updating = false;
			UpdateEditingState();
			SetStatus("没有找到动画播放器；可在场景树创建 AnimationPlayer 后刷新。", error: true);
		}
		else
		{
			int num = Math.Clamp(_playerOption.Selected, 0, _players.Count - 1);
			_playerOption.Select(num);
			_player = _players[num];
			RefreshLibraries();
			_updating = false;
			ApplyPendingState();
		}
	}

	private void RefreshLibraries()
	{
		string text = ReadStateString(_pendingBindState, "library");
		if (string.IsNullOrEmpty(text))
		{
			text = GetSelectedLibraryName();
		}
		_libraries.Clear();
		_libraryOption.Clear();
		if (GodotObject.IsInstanceValid(_player))
		{
			foreach (StringName animationLibrary in _player.GetAnimationLibraryList())
			{
				int itemCount = _libraryOption.ItemCount;
				_libraries.Add(animationLibrary);
				string text2 = (string.IsNullOrEmpty(animationLibrary.ToString()) ? "默认库" : animationLibrary.ToString());
				_libraryOption.AddItem("▤ " + text2);
				if (string.Equals(animationLibrary.ToString(), text, StringComparison.Ordinal))
				{
					_libraryOption.Select(itemCount);
				}
			}
		}
		if (_libraries.Count == 0)
		{
			_libraryOption.AddItem("没有动画库");
			_libraryOption.SetItemDisabled(0, disabled: true);
			_library = null;
		}
		else
		{
			int num = Math.Clamp(_libraryOption.Selected, 0, _libraries.Count - 1);
			_libraryOption.Select(num);
			_library = _player.GetAnimationLibrary(_libraries[num]);
		}
		RefreshAnimations();
	}

	private void RefreshAnimations(string requestedName = "")
	{
		string text = (string.IsNullOrWhiteSpace(requestedName) ? ReadStateString(_pendingBindState, "animation") : requestedName);
		if (string.IsNullOrEmpty(text))
		{
			text = GetSelectedAnimationName();
		}
		_animations.Clear();
		_animationOption.Clear();
		if (GodotObject.IsInstanceValid(_library))
		{
			foreach (StringName animation in _library.GetAnimationList())
			{
				int itemCount = _animationOption.ItemCount;
				_animations.Add(animation);
				_animationOption.AddItem($"◆ {animation}");
				if (string.Equals(animation.ToString(), text, StringComparison.Ordinal))
				{
					_animationOption.Select(itemCount);
				}
			}
		}
		if (_animations.Count == 0)
		{
			_animationOption.AddItem("动画库为空");
			_animationOption.SetItemDisabled(0, disabled: true);
			BindAnimation(null);
		}
		else
		{
			int num = Math.Clamp(_animationOption.Selected, 0, _animations.Count - 1);
			_animationOption.Select(num);
			BindAnimation(_library.GetAnimation(_animations[num]));
		}
	}

	private void BindAnimation(Animation animation)
	{
		StopPreview();
		_animation = animation;
		_selectedTrack = -1;
		_selectedKeyTime = -1.0;
		_timeline.BindAnimation(animation);
		RefreshTrackList();
		_updating = true;
		if (GodotObject.IsInstanceValid(animation))
		{
			_lengthSpin.Value = Math.Max(0.01, animation.Length);
			_stepSpin.Value = Math.Max(0.001, animation.Step);
			_loopButton.ButtonPressed = animation.LoopMode != Animation.LoopModeEnum.None;
			BindSafePreview();
		}
		else
		{
			_lengthSpin.Value = 1.0;
			_stepSpin.Value = 1.0 / 30.0;
			_loopButton.ButtonPressed = false;
			_preview.Clear();
		}
		_updating = false;
		UpdateTimeLabel(0.0);
		UpdateSourceInformation();
		UpdateEditingState();
	}

	private void BindSafePreview()
	{
		if (!GodotObject.IsInstanceValid(_player) || !GodotObject.IsInstanceValid(_animation))
		{
			_preview.Clear();
			return;
		}
		bool flag = _preview.Bind(_player, _animation, new StringName("__编辑预览_" + GetSelectedAnimationName()));
		_preview.SpeedScale = _speedSpin?.Value ?? 1.0;
		_previewSafetyLabel.Text = (flag ? $"隔离预览已建立：{_preview.BaselineCount} 个安全初始值，跳过 {_preview.UnsafeTrackCount} 条方法/音频/嵌套或不安全轨道。" : "无法建立安全预览；源播放器仍保持停止，不会执行动画事件。");
		_previewSafetyLabel.Modulate = (flag ? new Color("#9ee6a7") : new Color("#ff8d75"));
	}

	private void RefreshTrackList()
	{
		_updating = true;
		_trackList.Clear();
		if (!GodotObject.IsInstanceValid(_animation))
		{
			_updating = false;
			RefreshTrackSelection();
			return;
		}
		for (int i = 0; i < _animation.GetTrackCount(); i++)
		{
			Animation.TrackType type = _animation.TrackGetType(i);
			bool num = IsPreviewUnsafeTrack(type);
			string value = (_animation.TrackIsEnabled(i) ? "●" : "○");
			string value2 = (num ? "  ⛔ 仅显示" : "");
			_trackList.AddItem($"{value} {GetTrackTypeLabel(type)}  {_animation.TrackGetPath(i)}{value2}");
			_trackList.SetItemMetadata(i, i);
		}
		if (_selectedTrack >= 0 && _selectedTrack < _animation.GetTrackCount())
		{
			_trackList.Select(_selectedTrack);
		}
		else if (_animation.GetTrackCount() > 0)
		{
			_selectedTrack = 0;
			_trackList.Select(0);
		}
		_updating = false;
		RefreshTrackSelection();
	}

	private void RefreshTrackSelection()
	{
		int num;
		int num2;
		if (GodotObject.IsInstanceValid(_animation) && _selectedTrack >= 0)
		{
			num = ((_selectedTrack < _animation.GetTrackCount()) ? 1 : 0);
			if (num != 0)
			{
				num2 = (IsPreviewUnsafeTrack(_animation.TrackGetType(_selectedTrack)) ? 1 : 0);
				goto IL_0048;
			}
		}
		else
		{
			num = 0;
		}
		num2 = 0;
		goto IL_0048;
		IL_0048:
		bool flag = (byte)num2 != 0;
		_updating = true;
		if (num != 0)
		{
			Animation.TrackType type = _animation.TrackGetType(_selectedTrack);
			_selectedTrackLabel.Text = $"轨道 {_selectedTrack + 1} · {GetTrackTypeLabel(type)} · {_animation.TrackGetPath(_selectedTrack)}";
			_trackEnabledCheck.ButtonPressed = _animation.TrackIsEnabled(_selectedTrack);
		}
		else
		{
			_selectedTrackLabel.Text = "未选择轨道";
			_trackEnabledCheck.ButtonPressed = false;
		}
		_updating = false;
		RefreshValueEditor();
		RefreshMotionCurveEditor();
		UpdateEditingState();
		if (flag)
		{
			_selectedTrackLabel.Text += " · 仅显示（安全预览不执行）";
		}
	}

	private void RefreshValueEditor()
	{
		if (!GodotObject.IsInstanceValid(_valueEditorHost))
		{
			return;
		}
		foreach (Node child in _valueEditorHost.GetChildren())
		{
			child.QueueFree();
		}
		if (!TryGetSelectedKey(out var track, out var key))
		{
			_valueEditorHost.AddChild(MakeHintLabel("选择属性值轨道上的关键帧，可直接编辑布尔、整数、小数、文本、坐标与颜色。"), forceReadableName: false, InternalMode.Disabled);
			_selectedKeyLabel.Text = "选择时间轴上的菱形关键帧";
			return;
		}
		Variant value = _animation.TrackGetKeyValue(track, key);
		_selectedKeyLabel.Text = $"关键帧 {_selectedKeyTime:0.###} 秒 · {GetVariantTypeLabel(value.VariantType)}";
		Control control = CreateVariantEditor(value);
		if (control == null)
		{
			_valueEditorHost.AddChild(MakeHintLabel(GetVariantTypeLabel(value.VariantType) + " 当前仅显示。方法、音频、嵌套动画和复杂资源不会在编辑预览中执行。"), forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			_valueEditorHost.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private Control CreateVariantEditor(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 4uL)
		{
			switch ((int)num)
			{
			case 0:
			{
				CheckButton checkButton = new CheckButton
				{
					Text = "开启",
					ButtonPressed = value.AsBool()
				};
				checkButton.Toggled += delegate(bool next)
				{
					SetSelectedKeyValue(Variant.From(in next));
				};
				return Labeled("布尔开关", checkButton);
			}
			case 1:
			{
				SpinBox spinBox2 = MakeSpin(-1000000.0, 1000000.0, 1.0, value.AsInt64(), "0");
				spinBox2.ValueChanged += delegate(double next)
				{
					if (!_updating)
					{
						SetSelectedKeyValue(Variant.From<long>((long)Math.Round(next)));
					}
				};
				return Labeled("整数值", spinBox2);
			}
			case 2:
			{
				SpinBox spinBox = MakeSpin(-1000000.0, 1000000.0, 0.01, value.AsDouble(), "0.###");
				spinBox.ValueChanged += delegate(double next)
				{
					if (!_updating)
					{
						SetSelectedKeyValue(Variant.From(in next));
					}
				};
				return Labeled("小数值", spinBox);
			}
			case 3:
			{
				LineEdit text = new LineEdit
				{
					Text = value.AsString(),
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				};
				text.TextSubmitted += delegate(string next)
				{
					SetSelectedKeyValue(Variant.From(in next));
				};
				text.FocusExited += delegate
				{
					SetSelectedKeyValue(Variant.From<string>(text.Text));
				};
				return Labeled("文本内容", text);
			}
			case 4:
			{
				Vector2 vector = value.AsVector2();
				HBoxContainer hBoxContainer = new HBoxContainer();
				SpinBox x = MakeSpin(-1000000.0, 1000000.0, 0.1, vector.X, "X 0.##");
				SpinBox y = MakeSpin(-1000000.0, 1000000.0, 0.1, vector.Y, "Y 0.##");
				x.ValueChanged += delegate(double next)
				{
					if (!_updating)
					{
						SetSelectedKeyValue(Variant.From<Vector2>(new Vector2((float)next, (float)y.Value)));
					}
				};
				y.ValueChanged += delegate(double next)
				{
					if (!_updating)
					{
						SetSelectedKeyValue(Variant.From<Vector2>(new Vector2((float)x.Value, (float)next)));
					}
				};
				hBoxContainer.AddChild(x, forceReadableName: false, InternalMode.Disabled);
				hBoxContainer.AddChild(y, forceReadableName: false, InternalMode.Disabled);
				return Labeled("二维坐标", hBoxContainer);
			}
			}
		}
		if (variantType == Variant.Type.Color)
		{
			ColorPickerButton colorPickerButton = new ColorPickerButton
			{
				Color = value.AsColor(),
				CustomMinimumSize = new Vector2(160f, 36f),
				EditAlpha = true
			};
			colorPickerButton.ColorChanged += delegate(Color next)
			{
				if (!_updating)
				{
					SetSelectedKeyValue(Variant.From(in next));
				}
			};
			return Labeled("颜色", colorPickerButton);
		}
		return null;
	}

	private void SetSelectedKeyValue(Variant value)
	{
		if (_updating || !TryGetSelectedKey(out var track, out var key))
		{
			return;
		}
		double time = _animation.TrackGetKeyTime(track, key);
		CommitAnimationEdit("直接编辑关键帧值", delegate(Animation animation)
		{
			int num = FindKeyAtTime(animation, track, time);
			if (num < 0)
			{
				return false;
			}
			animation.TrackSetKeyValue(track, num, value);
			_selectedTrack = track;
			_selectedKeyTime = time;
			return true;
		});
	}

	private bool CommitAnimationEdit(string actionName, Func<Animation, bool> mutation)
	{
		if (!EnsureEditable(actionName) || mutation == null)
		{
			return false;
		}
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null)
		{
			return Fail("场景撤销历史不可用，已取消修改。");
		}
		PauseForMutation();
		Animation from = DuplicateAnimation(_animation);
		string from2 = GetSelectedAnimationName();
		int selectedTrack = _selectedTrack;
		double selectedKeyTime = _selectedKeyTime;
		if (from == null || !mutation(_animation))
		{
			if (from != null)
			{
				ReplaceAnimationDirect(_library, from2, from);
			}
			RefreshAnimations(from2);
			return Fail("动画修改没有完成。");
		}
		Animation from3 = DuplicateAnimation(_animation);
		if (from3 == null)
		{
			ReplaceAnimationDirect(_library, from2, from);
			RefreshAnimations(from2);
			return Fail("无法创建动画撤销快照。");
		}
		int selectedTrack2 = _selectedTrack;
		double selectedKeyTime2 = _selectedKeyTime;
		ReplaceAnimationDirect(_library, from2, from);
		xWUndoRedoManager.CreateAction(actionName, mergeMode: false, GetSceneHistoryId());
		xWUndoRedoManager.AddDoMethod(this, "ApplyAnimationSnapshot", Variant.From(in _library), Variant.From(in from2), Variant.From(in from3), selectedTrack2, selectedKeyTime2);
		xWUndoRedoManager.AddUndoMethod(this, "ApplyAnimationSnapshot", Variant.From(in _library), Variant.From(in from2), Variant.From(in from), selectedTrack, selectedKeyTime);
		xWUndoRedoManager.CommitAction();
		SetStatus(actionName + "；可使用场景撤销/重做。");
		return true;
	}

	public void ApplyAnimationSnapshot(AnimationLibrary library, string animationName, Animation snapshot, int selectedTrack, double selectedKeyTime)
	{
		if (!GodotObject.IsInstanceValid(library) || !GodotObject.IsInstanceValid(snapshot))
		{
			return;
		}
		ReplaceAnimationDirect(library, animationName, snapshot);
		if (_library == library)
		{
			double time = _timeline?.Playhead ?? 0.0;
			RefreshAnimations(animationName);
			_selectedTrack = (GodotObject.IsInstanceValid(_animation) ? Math.Clamp(selectedTrack, -1, _animation.GetTrackCount() - 1) : (-1));
			_selectedKeyTime = ((_selectedTrack >= 0 && selectedKeyTime >= 0.0 && FindKeyAtTime(_animation, _selectedTrack, selectedKeyTime) >= 0) ? selectedKeyTime : (-1.0));
			if (_selectedTrack >= 0 && _trackList.ItemCount > _selectedTrack)
			{
				_trackList.Select(_selectedTrack);
			}
			if (_selectedTrack >= 0 && _selectedKeyTime >= 0.0)
			{
				_timeline.SelectKey(_selectedTrack, _selectedKeyTime);
			}
			RefreshTrackSelection();
			OnTimelinePlayheadChanged(time);
			_editor?.QueueViewportRedraw();
		}
	}

	private bool CommitAnimationSlot(string actionName, AnimationLibrary library, string animationName, Animation snapshot, bool add, string selectionAfter)
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null || !GodotObject.IsInstanceValid(library) || !GodotObject.IsInstanceValid(snapshot))
		{
			return Fail("场景撤销历史或动画库不可用。");
		}
		PauseForMutation();
		xWUndoRedoManager.CreateAction(actionName, mergeMode: false, GetSceneHistoryId());
		if (add)
		{
			xWUndoRedoManager.AddDoMethod(this, "SetAnimationSlot", Variant.From(in library), Variant.From(in animationName), Variant.From(in snapshot), true, Variant.From(in selectionAfter));
			xWUndoRedoManager.AddUndoMethod(this, "SetAnimationSlot", Variant.From(in library), Variant.From(in animationName), Variant.From(in snapshot), false, Variant.From<string>(GetSelectedAnimationName()));
		}
		else
		{
			xWUndoRedoManager.AddDoMethod(this, "SetAnimationSlot", Variant.From(in library), Variant.From(in animationName), Variant.From(in snapshot), false, Variant.From(in selectionAfter));
			xWUndoRedoManager.AddUndoMethod(this, "SetAnimationSlot", Variant.From(in library), Variant.From(in animationName), Variant.From(in snapshot), true, Variant.From(in animationName));
		}
		xWUndoRedoManager.CommitAction();
		SetStatus(actionName + "；可使用场景撤销/重做。");
		return true;
	}

	public void SetAnimationSlot(AnimationLibrary library, string animationName, Animation snapshot, bool present, string requestedSelection)
	{
		if (GodotObject.IsInstanceValid(library))
		{
			if (library.HasAnimation(animationName))
			{
				library.RemoveAnimation(animationName);
			}
			if (present && GodotObject.IsInstanceValid(snapshot))
			{
				library.AddAnimation(animationName, DuplicateAnimation(snapshot));
			}
			if (_library == library)
			{
				RefreshAnimations(requestedSelection);
			}
			_editor?.QueueViewportRedraw();
		}
	}

	private void DuplicateCurrentAnimation()
	{
		if (GodotObject.IsInstanceValid(_animation) && GodotObject.IsInstanceValid(_library))
		{
			if (IsLibraryExternal(_library))
			{
				CopyCurrentAnimationToScene();
				return;
			}
			string text = BuildUniqueAnimationName(GetSelectedAnimationName() + "_副本");
			Animation animation = DuplicateAnimation(_animation);
			animation.ResourceLocalToScene = true;
			CommitAnimationSlot("复制动画片段", _library, text, animation, add: true, text);
		}
	}

	private void DeleteCurrentAnimation()
	{
		if (!EnsureEditable("删除动画片段") || !GodotObject.IsInstanceValid(_library))
		{
			return;
		}
		string selectedAnimationName = GetSelectedAnimationName();
		Animation snapshot = DuplicateAnimation(_animation);
		string selectionAfter = "";
		foreach (StringName animation in _library.GetAnimationList())
		{
			if (!string.Equals(animation.ToString(), selectedAnimationName, StringComparison.Ordinal))
			{
				selectionAfter = animation.ToString();
				break;
			}
		}
		CommitAnimationSlot("删除动画片段", _library, selectedAnimationName, snapshot, add: false, selectionAfter);
	}

	private void RenameCurrentAnimation(string requestedName)
	{
		if (!EnsureEditable("重命名动画片段") || !GodotObject.IsInstanceValid(_library))
		{
			return;
		}
		string from = GetSelectedAnimationName();
		string from2 = requestedName?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(from2))
		{
			Fail("动画名称不能为空。");
		}
		else
		{
			if (string.Equals(from, from2, StringComparison.Ordinal))
			{
				return;
			}
			if (_library.HasAnimation(from2))
			{
				Fail("动画片段“" + from2 + "”已存在。");
				return;
			}
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				Fail("场景撤销历史不可用。");
				return;
			}
			PauseForMutation();
			xWUndoRedoManager.CreateAction("重命名动画片段", mergeMode: false, GetSceneHistoryId());
			xWUndoRedoManager.AddDoMethod(this, "RenameAnimationSlot", Variant.From(in _library), Variant.From(in from), Variant.From(in from2));
			xWUndoRedoManager.AddUndoMethod(this, "RenameAnimationSlot", Variant.From(in _library), Variant.From(in from2), Variant.From(in from));
			xWUndoRedoManager.CommitAction();
		}
	}

	public void RenameAnimationSlot(AnimationLibrary library, string from, string to)
	{
		if (GodotObject.IsInstanceValid(library) && library.HasAnimation(from) && !library.HasAnimation(to))
		{
			library.RenameAnimation(from, to);
			if (_library == library)
			{
				RefreshAnimations(to);
			}
			_editor?.QueueViewportRedraw();
		}
	}

	private void CopyCurrentAnimationToScene()
	{
		if (GodotObject.IsInstanceValid(_player) && GodotObject.IsInstanceValid(_animation))
		{
			PauseForMutation();
			string from = FindWritableLibraryName();
			string from2 = BuildUniqueAnimationNameAcrossLibrary(from, GetSelectedAnimationName() + "_本地");
			Animation from3 = DuplicateAnimation(_animation);
			from3.ResourcePath = "";
			from3.ResourceLocalToScene = true;
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				Fail("场景撤销历史不可用。");
				return;
			}
			bool flag = !_player.HasAnimationLibrary(from);
			xWUndoRedoManager.CreateAction("复制动画到当前场景", mergeMode: false, GetSceneHistoryId());
			xWUndoRedoManager.AddDoMethod(this, "SetLocalAnimationCopy", Variant.From(in _player), Variant.From(in from), Variant.From(in from2), Variant.From(in from3), true, flag);
			xWUndoRedoManager.AddUndoMethod(this, "SetLocalAnimationCopy", Variant.From(in _player), Variant.From(in from), Variant.From(in from2), Variant.From(in from3), false, flag);
			xWUndoRedoManager.CommitAction();
		}
	}

	public void SetLocalAnimationCopy(AnimationPlayer player, string libraryName, string animationName, Animation snapshot, bool present, bool removeEmptyLibrary)
	{
		if (!GodotObject.IsInstanceValid(player))
		{
			return;
		}
		AnimationLibrary animationLibrary = (player.HasAnimationLibrary(libraryName) ? player.GetAnimationLibrary(libraryName) : null);
		if (present)
		{
			if (!GodotObject.IsInstanceValid(animationLibrary))
			{
				animationLibrary = new AnimationLibrary
				{
					ResourceLocalToScene = true
				};
				player.AddAnimationLibrary(libraryName, animationLibrary);
			}
			if (animationLibrary.HasAnimation(animationName))
			{
				animationLibrary.RemoveAnimation(animationName);
			}
			animationLibrary.AddAnimation(animationName, DuplicateAnimation(snapshot));
		}
		else if (GodotObject.IsInstanceValid(animationLibrary))
		{
			if (animationLibrary.HasAnimation(animationName))
			{
				animationLibrary.RemoveAnimation(animationName);
			}
			if (removeEmptyLibrary && animationLibrary.GetAnimationList().Count == 0)
			{
				player.RemoveAnimationLibrary(libraryName);
			}
		}
		if (_player == player)
		{
			_pendingBindState = new Dictionary
			{
				["library"] = libraryName,
				["animation"] = (present ? animationName : "")
			};
			RefreshLibraries();
			ApplyPendingState();
		}
		_editor?.QueueViewportRedraw();
	}

	private void DeleteSelectedTrack()
	{
		if (EnsureEditable("删除轨道") && _selectedTrack >= 0 && _selectedTrack < _animation.GetTrackCount())
		{
			int track = _selectedTrack;
			CommitAnimationEdit("删除动画轨道", delegate(Animation animation)
			{
				animation.RemoveTrack(track);
				_selectedTrack = Math.Min(track, animation.GetTrackCount() - 1);
				_selectedKeyTime = -1.0;
				return true;
			});
		}
	}

	private void MoveSelectedTrack(int direction)
	{
		if (!EnsureEditable("移动轨道") || _selectedTrack < 0 || _selectedTrack >= _animation.GetTrackCount())
		{
			return;
		}
		int source = _selectedTrack;
		int target = Math.Clamp(source + Math.Sign(direction), 0, _animation.GetTrackCount() - 1);
		if (source != target)
		{
			CommitAnimationEdit("调整动画轨道顺序", delegate(Animation animation)
			{
				animation.TrackMoveTo(source, target);
				_selectedTrack = target;
				return true;
			});
		}
	}

	private void ToggleSelectedTrack(bool enabled)
	{
		if (!_updating && EnsureEditable("启停轨道") && _selectedTrack >= 0 && _selectedTrack < _animation.GetTrackCount())
		{
			int track = _selectedTrack;
			CommitAnimationEdit(enabled ? "启用动画轨道" : "停用动画轨道", delegate(Animation animation)
			{
				animation.TrackSetEnabled(track, enabled);
				_selectedTrack = track;
				return true;
			});
		}
	}

	private void DuplicateSelectedKey()
	{
		if (TryGetSelectedKey(out var track, out var key))
		{
			double num = _animation.TrackGetKeyTime(track, key);
			double target = ClampTime(num + GetStep());
			if (Mathf.IsEqualApprox(num, target))
			{
				target = ClampTime(num - GetStep());
			}
			Variant value = _animation.TrackGetKeyValue(track, key);
			float transition = _animation.TrackGetKeyTransition(track, key);
			CommitAnimationEdit("复制关键帧", delegate(Animation animation)
			{
				int keyIdx = animation.TrackInsertKey(track, target, value, transition);
				_selectedTrack = track;
				_selectedKeyTime = animation.TrackGetKeyTime(track, keyIdx);
				return true;
			});
		}
	}

	private void OnTimelinePlayheadChanged(double time)
	{
		if (GodotObject.IsInstanceValid(_animation))
		{
			double num = ClampTime(time);
			_preview?.Seek(num);
			_timeline.SetPlayhead(num);
			UpdateTimeLabel(num);
		}
	}

	private void OnTimelineKeySelected(int track, double time)
	{
		_selectedTrack = track;
		_selectedKeyTime = time;
		if (_trackList.ItemCount > track)
		{
			_trackList.Select(track);
		}
		_preview?.Seek(ClampTime(time));
		RefreshTrackSelection();
	}

	private void OnTimelineKeyMoveRequested(int track, double oldTime, double newTime)
	{
		_selectedTrack = track;
		_selectedKeyTime = oldTime;
		MoveSelectedKey(newTime);
	}

	private void OnTrackListSelected(long index)
	{
		if (!_updating && index >= 0 && index < _trackList.ItemCount)
		{
			_selectedTrack = _trackList.GetItemMetadata((int)index).AsInt32();
			_selectedKeyTime = -1.0;
			RefreshTrackSelection();
		}
	}

	private void TogglePlayback()
	{
		if (!GodotObject.IsInstanceValid(_preview) || !GodotObject.IsInstanceValid(_animation) || _suspendedForSave)
		{
			return;
		}
		if (_preview.IsPlaying)
		{
			_preview.Pause();
			SetProcess(enable: false);
		}
		else
		{
			if (_preview.CurrentPosition >= _animation.Length - 0.0001)
			{
				_preview.Seek(0.0);
			}
			_preview.Play();
			SetProcess(IsVisibleInTree());
		}
		UpdatePlaybackButton();
	}

	public void StopPreview()
	{
		if (GodotObject.IsInstanceValid(_preview))
		{
			_preview.Stop();
		}
		SetProcess(enable: false);
		_timeline?.SetPlayhead(0.0);
		UpdateTimeLabel(0.0);
		UpdatePlaybackButton();
	}

	private void PauseForMutation()
	{
		if (GodotObject.IsInstanceValid(_preview) && _preview.IsPlaying)
		{
			_preview.Pause();
		}
		SetProcess(enable: false);
		UpdatePlaybackButton();
	}

	private void JumpToKey(int direction)
	{
		if (!GodotObject.IsInstanceValid(_animation))
		{
			return;
		}
		double playhead = _timeline.Playhead;
		double num = ((direction < 0) ? (-1.0 / 0.0) : (1.0 / 0.0));
		for (int i = 0; i < _animation.GetTrackCount(); i++)
		{
			for (int j = 0; j < _animation.TrackGetKeyCount(i); j++)
			{
				double num2 = _animation.TrackGetKeyTime(i, j);
				if (direction < 0 && num2 < playhead - 0.0001 && num2 > num)
				{
					num = num2;
				}
				if (direction > 0 && num2 > playhead + 0.0001 && num2 < num)
				{
					num = num2;
				}
			}
		}
		if (double.IsInfinity(num))
		{
			num = ((direction < 0) ? 0.0 : _animation.Length);
		}
		OnTimelinePlayheadChanged(num);
	}

	private void OnLoopToggled(bool enabled)
	{
		if (!_updating && EnsureEditable("设置动画循环"))
		{
			CommitAnimationEdit(enabled ? "开启动画循环" : "关闭动画循环", delegate(Animation animation)
			{
				animation.LoopMode = (Animation.LoopModeEnum)(enabled ? 1 : 0);
				return true;
			});
		}
	}

	private void OnSpeedChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_preview))
		{
			_preview.SpeedScale = value;
			SetStatus($"安全预览速度：{value:0.0}×。");
		}
	}

	private void OnLengthChanged(double value)
	{
		if (!_updating && EnsureEditable("调整动画长度"))
		{
			CommitAnimationEdit("调整动画长度", delegate(Animation animation)
			{
				animation.Length = Math.Max(0.01, value);
				return true;
			});
		}
	}

	private void OnStepChanged(double value)
	{
		if (!_updating && EnsureEditable("调整动画步长"))
		{
			CommitAnimationEdit("调整动画步长", delegate(Animation animation)
			{
				animation.Step = (float)Math.Max(0.001, value);
				return true;
			});
		}
	}

	private void OnPlayerSelected(long index)
	{
		if (!_updating && index >= 0 && index < _players.Count)
		{
			_player = _players[(int)index];
			_pendingBindState = new Dictionary();
			RefreshLibraries();
			UpdateAnimationTreeLink();
		}
	}

	private void OnLibrarySelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_player) && index >= 0 && index < _libraries.Count)
		{
			_library = _player.GetAnimationLibrary(_libraries[(int)index]);
			_pendingBindState = new Dictionary();
			RefreshAnimations();
		}
	}

	private void OnAnimationSelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_library) && index >= 0 && index < _animations.Count)
		{
			_pendingBindState = new Dictionary();
			BindAnimation(_library.GetAnimation(_animations[(int)index]));
		}
	}

	private void ApplyPendingState()
	{
		Dictionary pendingBindState = _pendingBindState;
		if (pendingBindState != null && pendingBindState.Count != 0)
		{
			_updating = true;
			SelectOptionByString(_libraryOption, _libraries, ReadStateString(pendingBindState, "library"));
			if (_libraryOption.Selected >= 0 && _libraryOption.Selected < _libraries.Count)
			{
				_library = _player.GetAnimationLibrary(_libraries[_libraryOption.Selected]);
				RefreshAnimations(ReadStateString(pendingBindState, "animation"));
			}
			double value = ReadStateDouble(pendingBindState, "speed", 1.0);
			double value2 = ReadStateDouble(pendingBindState, "zoom", 120.0);
			_speedSpin.Value = Math.Clamp(value, 0.1, 4.0);
			_zoomSpin.Value = Math.Clamp(value2, 24.0, 480.0);
			_timeline.SetTimeScale(_zoomSpin.Value);
			_preview.SpeedScale = _speedSpin.Value;
			bool buttonPressed = ReadStateBool(pendingBindState, "loop", GodotObject.IsInstanceValid(_animation) && _animation.LoopMode != Animation.LoopModeEnum.None);
			_loopButton.ButtonPressed = buttonPressed;
			double num = ClampTime(ReadStateDouble(pendingBindState, "playhead", 0.0));
			_timeline.SetPlayhead(num);
			_preview.Seek(num);
			_updating = false;
			_pendingBindState = null;
			UpdateTimeLabel(num);
		}
	}

	private void UpdateSourceInformation()
	{
		UpdateAnimationTreeLink();
		bool flag = IsCurrentAnimationReadOnly();
		if (!GodotObject.IsInstanceValid(_animation))
		{
			_readonlyLabel.Text = "动画库中还没有片段。";
		}
		else if (flag)
		{
			_readonlyLabel.Text = "\ud83d\udd12 外部/共享动画只读：不会直接改写原资源。点击“复制到当前场景”后可自由编辑。";
		}
		else
		{
			_readonlyLabel.Text = "✦ 当前场景本地动画：所有修改写入独立场景撤销历史。";
		}
		_readonlyLabel.Modulate = (flag ? new Color("#ffcf76") : new Color("#9ee6a7"));
		_copyLocalButton.Visible = GodotObject.IsInstanceValid(_animation) && flag;
	}

	private void UpdateAnimationTreeLink()
	{
		if (!GodotObject.IsInstanceValid(_sceneRoot) || !GodotObject.IsInstanceValid(_player))
		{
			_treeLinkLabel.Text = "未发现 AnimationTree 关联。";
			return;
		}
		List<string> list = new List<string>();
		CollectAnimationTrees(_sceneRoot, _player, list);
		_treeLinkLabel.Text = ((list.Count == 0) ? "未发现 AnimationTree 关联；当前直接编辑 AnimationPlayer 片段。" : ("检测到 AnimationTree：" + string.Join("、", list) + "；已解析其 AnimPlayer 关联，仅预览底层安全属性轨道。"));
	}

	private void UpdateEditingState()
	{
		bool flag = GodotObject.IsInstanceValid(_animation);
		bool flag2 = flag && !IsCurrentAnimationReadOnly();
		bool flag3 = flag && IsEditableValueTrack(_selectedTrack);
		bool flag4 = flag && _selectedTrack >= 0 && _selectedTrack < _animation.GetTrackCount() && IsPreviewUnsafeTrack(_animation.TrackGetType(_selectedTrack));
		int track;
		int key;
		bool flag5 = TryGetSelectedKey(out track, out key);
		Button[] array = new Button[3] { _deleteTrackButton, _moveTrackUpButton, _moveTrackDownButton };
		foreach (Button button in array)
		{
			if (GodotObject.IsInstanceValid(button))
			{
				button.Disabled = !flag2 || _selectedTrack < 0 || flag4;
			}
		}
		if (GodotObject.IsInstanceValid(_trackEnabledCheck))
		{
			_trackEnabledCheck.Disabled = !flag2 || _selectedTrack < 0 || flag4;
		}
		if (GodotObject.IsInstanceValid(_insertKeyButton))
		{
			_insertKeyButton.Disabled = !flag2 || !flag3;
		}
		array = new Button[4] { _deleteKeyButton, _duplicateKeyButton, _moveKeyEarlierButton, _moveKeyLaterButton };
		foreach (Button button2 in array)
		{
			if (GodotObject.IsInstanceValid(button2))
			{
				button2.Disabled = !flag2 || !flag5 || !flag3;
			}
		}
		if (GodotObject.IsInstanceValid(_lengthSpin))
		{
			_lengthSpin.Editable = flag2;
		}
		if (GodotObject.IsInstanceValid(_stepSpin))
		{
			_stepSpin.Editable = flag2;
		}
		if (GodotObject.IsInstanceValid(_playPauseButton))
		{
			_playPauseButton.Disabled = !flag;
		}
	}

	private void UpdatePlaybackButton()
	{
		if (GodotObject.IsInstanceValid(_playPauseButton))
		{
			_playPauseButton.Text = ((GodotObject.IsInstanceValid(_preview) && _preview.IsPlaying) ? "Ⅱ 暂停" : "▶ 播放");
		}
	}

	private void UpdateProcessing()
	{
		bool flag = IsVisibleInTree();
		if (!flag)
		{
			_easeCurveCanvas?.CancelPendingDrag();
			if (GodotObject.IsInstanceValid(_preview) && _preview.IsPlaying)
			{
				_preview.Pause();
			}
		}
		if (flag)
		{
			RefreshMotionCurveEditor();
		}
		SetProcess(flag && GodotObject.IsInstanceValid(_preview) && _preview.IsPlaying);
		UpdatePlaybackButton();
	}

	private void UpdateTimeLabel(double time)
	{
		if (GodotObject.IsInstanceValid(_timeLabel))
		{
			_timeLabel.Text = $"{time:0.00} / {(GodotObject.IsInstanceValid(_animation) ? _animation.Length : 0.0):0.00} 秒";
		}
	}

	private bool EnsureEditable(string operation)
	{
		if (!GodotObject.IsInstanceValid(_animation))
		{
			return Fail("无法" + operation + "：请先选择动画片段。");
		}
		if (IsCurrentAnimationReadOnly())
		{
			return Fail("无法" + operation + "：外部/共享动画只读，请先复制到当前场景。");
		}
		return true;
	}

	private bool IsCurrentAnimationReadOnly()
	{
		if (!GodotObject.IsInstanceValid(_animation) || !GodotObject.IsInstanceValid(_library))
		{
			return true;
		}
		if (!IsLibraryExternal(_library))
		{
			return IsResourceExternal(_animation);
		}
		return true;
	}

	private bool IsLibraryExternal(AnimationLibrary library)
	{
		return IsResourceExternal(library);
	}

	private bool IsResourceExternal(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource) || string.IsNullOrWhiteSpace(resource.ResourcePath))
		{
			return false;
		}
		string text = _sceneRoot?.SceneFilePath ?? "";
		if (!string.IsNullOrWhiteSpace(text) && resource.ResourcePath.StartsWith(text + "::", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return !resource.ResourcePath.Contains("::", StringComparison.Ordinal);
	}

	private bool TryReadCurrentTrackValue(out Variant value)
	{
		value = default(Variant);
		if (!GodotObject.IsInstanceValid(_animation) || !GodotObject.IsInstanceValid(_player) || !IsEditableValueTrack(_selectedTrack))
		{
			return Fail("请选择一条属性值轨道。");
		}
		Node node = ResolveAnimationRoot(_player);
		string text = _animation.TrackGetPath(_selectedTrack).ToString();
		int num = text.LastIndexOf(':');
		if (num < 0)
		{
			return Fail("轨道没有可读取的属性路径。");
		}
		string text2 = text.Substring(0, num);
		string text3 = text;
		int num2 = num + 1;
		string text4 = text3.Substring(num2, text3.Length - num2);
		bool flag = (((text2 != null && text2.Length == 0) || text2 == ".") ? true : false);
		Node node2 = (flag ? node : node?.GetNodeOrNull(new NodePath(text2)));
		if (!GodotObject.IsInstanceValid(node2))
		{
			return Fail("找不到轨道目标节点：" + text2);
		}
		value = node2.Get(text4);
		if (!IsSupportedEditableVariant(value.VariantType))
		{
			return Fail("当前属性类型 " + GetVariantTypeLabel(value.VariantType) + " 仅显示，不能直接写入。");
		}
		return true;
	}

	private bool TryGetSelectedKey(out int track, out int key)
	{
		track = _selectedTrack;
		key = -1;
		if (!GodotObject.IsInstanceValid(_animation) || track < 0 || track >= _animation.GetTrackCount() || _selectedKeyTime < -0.0001)
		{
			return false;
		}
		key = FindKeyAtTime(_animation, track, _selectedKeyTime);
		return key >= 0;
	}

	private bool IsEditableValueTrack(int track)
	{
		if (GodotObject.IsInstanceValid(_animation) && track >= 0 && track < _animation.GetTrackCount())
		{
			return _animation.TrackGetType(track) == Animation.TrackType.Value;
		}
		return false;
	}

	private double ClampTime(double time)
	{
		return Math.Clamp(time, 0.0, GodotObject.IsInstanceValid(_animation) ? Math.Max(0.0, _animation.Length) : 0.0);
	}

	private double GetStep()
	{
		if (!GodotObject.IsInstanceValid(_animation))
		{
			return 1.0 / 30.0;
		}
		return Math.Max(0.001, _animation.Step);
	}

	private int GetSceneHistoryId()
	{
		if (!GodotObject.IsInstanceValid(_editor))
		{
			return XWEditorInterface.Instance?.GetCurrentSceneHistoryId() ?? 1;
		}
		return _editor.GetCurrentSceneHistoryId();
	}

	private string GetSelectedLibraryName()
	{
		int num = _libraryOption?.Selected ?? (-1);
		if (num < 0 || num >= _libraries.Count)
		{
			return "";
		}
		return _libraries[num].ToString();
	}

	private string GetSelectedAnimationName()
	{
		int num = _animationOption?.Selected ?? (-1);
		if (num < 0 || num >= _animations.Count)
		{
			return "";
		}
		return _animations[num].ToString();
	}

	private string BuildUniqueAnimationName(string baseName)
	{
		if (!GodotObject.IsInstanceValid(_library))
		{
			return baseName;
		}
		string text = baseName;
		int num = 2;
		while (_library.HasAnimation(text))
		{
			text = $"{baseName}_{num++}";
		}
		return text;
	}

	private string BuildUniqueAnimationNameAcrossLibrary(string libraryName, string baseName)
	{
		if (!GodotObject.IsInstanceValid(_player) || !_player.HasAnimationLibrary(libraryName))
		{
			return baseName;
		}
		AnimationLibrary animationLibrary = _player.GetAnimationLibrary(libraryName);
		string text = baseName;
		int num = 2;
		while (animationLibrary.HasAnimation(text))
		{
			text = $"{baseName}_{num++}";
		}
		return text;
	}

	private string FindWritableLibraryName()
	{
		if (GodotObject.IsInstanceValid(_player))
		{
			foreach (StringName animationLibrary2 in _player.GetAnimationLibraryList())
			{
				AnimationLibrary animationLibrary = _player.GetAnimationLibrary(animationLibrary2);
				if (!IsLibraryExternal(animationLibrary))
				{
					return animationLibrary2.ToString();
				}
			}
		}
		return "场景动画";
	}

	private void OpenNameDialog(string operation, string title, string initial)
	{
		_pendingNameOperation = operation;
		_nameDialog.Title = title;
		_nameEdit.Text = initial ?? "";
		_nameDialog.PopupCentered();
		_nameEdit.GrabFocus();
		_nameEdit.SelectAll();
	}

	private void OnNameDialogConfirmed()
	{
		string text = _nameEdit.Text;
		string pendingNameOperation = _pendingNameOperation;
		if (!(pendingNameOperation == "create"))
		{
			if (pendingNameOperation == "rename")
			{
				RenameCurrentAnimation(text);
			}
		}
		else
		{
			CreateAnimation(text);
		}
		_pendingNameOperation = "";
	}

	private void ConfirmDeleteAnimation()
	{
		if (GodotObject.IsInstanceValid(_animation))
		{
			_deleteAnimationDialog.DialogText = "确定删除动画片段“" + GetSelectedAnimationName() + "”？此操作可以撤销。";
			_deleteAnimationDialog.PopupCentered();
		}
	}

	private bool Fail(string message)
	{
		SetStatus(message, error: true);
		return false;
	}

	private void SetStatus(string message, bool error = false)
	{
		if (GodotObject.IsInstanceValid(_statusLabel))
		{
			_statusLabel.Text = message ?? "";
			_statusLabel.Modulate = (error ? new Color("#ff8876") : new Color("#a8d8f0"));
		}
	}

	private void AddPropertyChoices(Node node)
	{
		HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
		if (node is Node2D)
		{
			AddPropertyChoice(node, "position", "◇ 位置", names);
			AddPropertyChoice(node, "rotation", "↻ 旋转", names);
			AddPropertyChoice(node, "scale", "↔ 缩放", names);
			AddPropertyChoice(node, "skew", "⟋ 倾斜", names);
		}
		if (node is Control)
		{
			AddPropertyChoice(node, "position", "◇ UI 位置", names);
			AddPropertyChoice(node, "size", "▭ UI 尺寸", names);
			AddPropertyChoice(node, "pivot_offset", "⊹ 旋转中心", names);
			AddPropertyChoice(node, "rotation", "↻ UI 旋转", names);
			AddPropertyChoice(node, "scale", "↔ UI 缩放", names);
		}
		if (node is CanvasItem)
		{
			AddPropertyChoice(node, "visible", "◉ 可见性", names);
			AddPropertyChoice(node, "modulate", "▧ 整体颜色", names);
			AddPropertyChoice(node, "self_modulate", "▧ 自身颜色", names);
		}
		if (node is Sprite2D)
		{
			AddPropertyChoice(node, "frame", "▣ 图集帧", names);
			AddPropertyChoice(node, "flip_h", "⇋ 水平翻转", names);
			AddPropertyChoice(node, "flip_v", "⇅ 垂直翻转", names);
		}
		if ((node is Label || node is RichTextLabel || node is LineEdit || node is TextEdit) ? true : false)
		{
			AddPropertyChoice(node, "text", "文 文本内容", names);
		}
		if (node is Godot.Range)
		{
			AddPropertyChoice(node, "value", "◫ 数值", names);
		}
		if (node is AnimatedSprite2D)
		{
			AddPropertyChoice(node, "frame", "▣ 动画帧编号", names);
		}
	}

	private void AddPropertyChoice(Node node, string property, string label, HashSet<string> names)
	{
		if (names.Add(property))
		{
			Variant value = node.Get(property);
			if (IsSupportedEditableVariant(value.VariantType))
			{
				_properties.Add(new PropertyChoice(node, property, $"{label}  ·  {node.Name}  ·  {FormatVariant(value)}", value));
			}
		}
	}

	private bool IsNodeInsideScene(Node node)
	{
		if (GodotObject.IsInstanceValid(node) && GodotObject.IsInstanceValid(_sceneRoot))
		{
			if (node != _sceneRoot)
			{
				return _sceneRoot.IsAncestorOf(node);
			}
			return true;
		}
		return false;
	}

	private static Node ResolveAnimationRoot(AnimationPlayer player)
	{
		if (!GodotObject.IsInstanceValid(player))
		{
			return null;
		}
		return player.GetNodeOrNull(player.RootNode) ?? player;
	}

	private static Animation DuplicateAnimation(Animation source)
	{
		if (!GodotObject.IsInstanceValid(source))
		{
			return null;
		}
		return source.Duplicate(deep: true) as Animation;
	}

	private static void ReplaceAnimationDirect(AnimationLibrary library, string name, Animation source)
	{
		if (GodotObject.IsInstanceValid(library) && GodotObject.IsInstanceValid(source))
		{
			if (library.HasAnimation(name))
			{
				library.RemoveAnimation(name);
			}
			library.AddAnimation(name, DuplicateAnimation(source));
		}
	}

	private static int FindKeyAtTime(Animation animation, int track, double time)
	{
		if (!GodotObject.IsInstanceValid(animation) || track < 0 || track >= animation.GetTrackCount())
		{
			return -1;
		}
		for (int i = 0; i < animation.TrackGetKeyCount(track); i++)
		{
			if (Math.Abs(animation.TrackGetKeyTime(track, i) - time) <= 0.0001)
			{
				return i;
			}
		}
		return -1;
	}

	private static bool IsPreviewUnsafeTrack(Animation.TrackType type)
	{
		if (type == Animation.TrackType.Method || (ulong)(type - 7) <= 1uL)
		{
			return true;
		}
		return false;
	}

	private static bool IsSupportedEditableVariant(Variant.Type type)
	{
		if ((ulong)(type - 1) <= 4uL || type == Variant.Type.Color)
		{
			return true;
		}
		return false;
	}

	private static string GetTrackTypeLabel(Animation.TrackType type)
	{
		Animation.TrackType num = type;
		if ((ulong)num <= 8uL)
		{
			switch ((int)num)
			{
			case 0:
				return "属性";
			case 5:
				return "方法";
			case 7:
				return "音频";
			case 8:
				return "嵌套动画";
			case 6:
				return "曲线";
			case 1:
				return "3D 位置";
			case 2:
				return "3D 旋转";
			case 3:
				return "3D 缩放";
			case 4:
				return "混合形状";
			}
		}
		return type.ToString();
	}

	private bool IsRotationTrack(int track)
	{
		if (!GodotObject.IsInstanceValid(_animation) || track < 0 || track >= _animation.GetTrackCount())
		{
			return false;
		}
		return _animation.TrackGetPath(track).ToString().EndsWith(":rotation", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsAngleInterpolation(Animation.InterpolationType interpolation)
	{
		if ((ulong)(interpolation - 3) <= 1uL)
		{
			return true;
		}
		return false;
	}

	private static string GetInterpolationLabel(Animation.InterpolationType interpolation)
	{
		Animation.InterpolationType num = interpolation;
		if ((ulong)num <= 4uL)
		{
			switch ((int)num)
			{
			case 0:
				return "保持";
			case 1:
				return "线性";
			case 2:
				return "三次平滑";
			case 3:
				return "角度线性";
			case 4:
				return "角度平滑";
			}
		}
		return interpolation.ToString();
	}

	private static string GetUpdateModeLabel(Animation.UpdateMode updateMode)
	{
		Animation.UpdateMode num = updateMode;
		if ((ulong)num <= 2uL)
		{
			switch ((int)num)
			{
			case 0:
				return "连续";
			case 1:
				return "离散";
			case 2:
				return "捕获";
			}
		}
		return updateMode.ToString();
	}

	private static float NormalizeTransition(float transition)
	{
		if (!float.IsFinite(transition))
		{
			return 1f;
		}
		return Math.Clamp(transition, -1000000f, 1000000f);
	}

	private static string GetVariantTypeLabel(Variant.Type type)
	{
		Variant.Type num = type - 1;
		if ((ulong)num <= 4uL)
		{
			switch ((int)num)
			{
			case 0:
				return "开关";
			case 1:
				return "整数";
			case 2:
				return "小数";
			case 3:
				return "文本";
			case 4:
				return "二维坐标";
			}
		}
		if (type == Variant.Type.Color)
		{
			return "颜色";
		}
		return type.ToString();
	}

	private static string FormatVariant(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 4uL)
		{
			switch ((int)num)
			{
			case 0:
				return value.AsBool() ? "开启" : "关闭";
			case 1:
				return value.AsInt64().ToString();
			case 2:
				return value.AsDouble().ToString("0.###");
			case 3:
				return value.AsString();
			case 4:
				return $"({value.AsVector2().X:0.#}, {value.AsVector2().Y:0.#})";
			}
		}
		if (variantType == Variant.Type.Color)
		{
			return "#" + value.AsColor().ToHtml();
		}
		return value.ToString();
	}

	private static void CollectAnimationPlayers(Node node, List<AnimationPlayer> output)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AnimationPlayer item)
		{
			output.Add(item);
		}
		foreach (Node child in node.GetChildren())
		{
			CollectAnimationPlayers(child, output);
		}
	}

	private static void CollectAnimationTrees(Node node, AnimationPlayer player, List<string> output)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AnimationTree animationTree && animationTree.GetNodeOrNull(animationTree.AnimPlayer) == player)
		{
			output.Add(animationTree.Name);
		}
		foreach (Node child in node.GetChildren())
		{
			CollectAnimationTrees(child, player, output);
		}
	}

	private static void SelectOptionByString(OptionButton option, List<StringName> values, string requested)
	{
		if (option == null || string.IsNullOrEmpty(requested))
		{
			return;
		}
		for (int i = 0; i < values.Count; i++)
		{
			if (string.Equals(values[i].ToString(), requested, StringComparison.Ordinal))
			{
				option.Select(i);
				break;
			}
		}
	}

	private static string ReadStateString(Dictionary state, string key)
	{
		if (state == null || !state.ContainsKey(key))
		{
			return "";
		}
		return state[key].AsString();
	}

	private static double ReadStateDouble(Dictionary state, string key, double fallback)
	{
		if (state == null || !state.ContainsKey(key))
		{
			return fallback;
		}
		return state[key].AsDouble();
	}

	private static bool ReadStateBool(Dictionary state, string key, bool fallback)
	{
		if (state == null || !state.ContainsKey(key))
		{
			return fallback;
		}
		return state[key].AsBool();
	}

	private static VBoxContainer MakeSection(VBoxContainer parent, string title, string subtitle)
	{
		VBoxContainer vBoxContainer = MakeStandaloneSection(title, subtitle);
		parent.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private static VBoxContainer MakeStandaloneSection(string title, string subtitle)
	{
		PanelContainer panelContainer = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		panelContainer.AddThemeStyleboxOverride("panel", MakePanelStyle(new Color("#172534"), new Color("#2c4962"), 8, 1));
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 5);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		Label label = new Label
		{
			Text = title
		};
		label.AddThemeFontSizeOverride("font_size", 15);
		label.AddThemeColorOverride("font_color", new Color("#f1d487"));
		vBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		if (!string.IsNullOrWhiteSpace(subtitle))
		{
			Label label2 = MakeHintLabel(subtitle);
			label2.Modulate = new Color("#8eacc1");
			vBoxContainer.AddChild(label2, forceReadableName: false, InternalMode.Disabled);
		}
		VBoxContainer vBoxContainer2 = new VBoxContainer();
		vBoxContainer2.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer2;
	}

	private static Control Labeled(string text, Control control)
	{
		VBoxContainer obj = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Label label = new Label
		{
			Text = text
		};
		label.AddThemeFontSizeOverride("font_size", 11);
		label.AddThemeColorOverride("font_color", new Color("#8fabc0"));
		obj.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		obj.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		return obj;
	}

	private static OptionButton MakeOption(string tooltip)
	{
		return new OptionButton
		{
			TooltipText = tooltip,
			FitToLongestItem = false,
			CustomMinimumSize = new Vector2(150f, 34f)
		};
	}

	private static Button MakeButton(string text, string tooltip)
	{
		Button button = new Button();
		button.Text = text;
		button.TooltipText = tooltip;
		button.CustomMinimumSize = new Vector2(38f, 34f);
		button.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color("#20364a"), new Color("#426985"), 6, 1));
		button.AddThemeStyleboxOverride("hover", MakePanelStyle(new Color("#2c4d65"), new Color("#68b9d8"), 6, 1));
		button.AddThemeStyleboxOverride("pressed", MakePanelStyle(new Color("#132534"), new Color("#f0c86f"), 6, 1));
		return button;
	}

	private static Label MakeIconBadge(string text, Color color)
	{
		Label label = new Label();
		label.Text = text;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.CustomMinimumSize = new Vector2(34f, 34f);
		label.AddThemeFontSizeOverride("font_size", 20);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeStyleboxOverride("normal", MakePanelStyle(new Color("#172c3b"), color.Darkened(0.35f), 17, 1));
		return label;
	}

	private static Label MakeHintLabel(string text)
	{
		return new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private static SpinBox MakeSpin(double min, double max, double step, double value, string suffix)
	{
		return new SpinBox
		{
			MinValue = min,
			MaxValue = max,
			Step = step,
			Value = value,
			Suffix = suffix,
			AllowGreater = false,
			AllowLesser = false,
			CustomMinimumSize = new Vector2(105f, 34f)
		};
	}

	private static StyleBoxFlat MakePanelStyle(Color background, Color border, int radius, int width)
	{
		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,
			BorderWidthLeft = width,
			BorderWidthTop = width,
			BorderWidthRight = width,
			BorderWidthBottom = width,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius,
			ContentMarginLeft = 8f,
			ContentMarginTop = 6f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 6f
		};
	}
}
