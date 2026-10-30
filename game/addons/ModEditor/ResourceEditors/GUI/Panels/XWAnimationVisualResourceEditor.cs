using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.AdobeAnimateEditor.Inspector;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWAnimationVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string AnimationRuntimePreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAnimationRuntimePreview.tscn";

	private const string AnimationReplacementCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAnimationReplacementCard.tscn";

	private static PackedScene _animationRuntimePreviewScene;

	private static PackedScene _animationReplacementCardScene;

	private static Texture2D _animationVisualFallbackIcon;

	private Label _animationPreviewStatusLabel;

	private Label _animationDirectSummaryLabel;

	private Label _animationResourcePathLabel;

	private Label _animationRuntimeFrameLabel;

	private Label _animationPlaybackStateLabel;

	private ItemList _animationClipList;

	private LineEdit _animationClipNameEdit;

	private SpinBox _animationClipStartSpinBox;

	private SpinBox _animationClipEndSpinBox;

	private XWPacketSpawnEntryRangeControl _animationClipRangeControl;

	private Button _animationAddClipButton;

	private Button _animationDeleteClipButton;

	private Button _animationShiftClipEarlierButton;

	private Button _animationShiftClipLaterButton;

	private Label _animationClipEditStatusLabel;

	private XWAnimationFrameTimeline _animationFrameTimeline;

	private Button _animationAddKeyframeButton;

	private Button _animationDuplicateKeyframeButton;

	private Button _animationDeleteKeyframeButton;

	private Button _animationMoveKeyframeEarlierButton;

	private Button _animationMoveKeyframeLaterButton;

	private Label _animationSelectedKeyframeLabel;

	private OptionButton _animationMediaOption;

	private OptionButton _animationLayerOption;

	private TextureRect _animationMediaThumbnail;

	private SpinBox _animationPositionXSpinBox;

	private SpinBox _animationPositionYSpinBox;

	private SpinBox _animationRotationSpinBox;

	private SpinBox _animationSkewSpinBox;

	private SpinBox _animationScaleXSpinBox;

	private SpinBox _animationScaleYSpinBox;

	private HSlider _animationAlphaSlider;

	private Label _animationAlphaPercentLabel;

	private Label _animationKeyframeEditStatusLabel;

	private bool _updatingAnimationFrameControls;

	private Dictionary _animationFrameEditSnapshot;

	private AdobeAnimateData _animationFrameEditTarget;

	private int _animationFrameEditFrame = -1;

	private int _animationFrameEditSliceKey = -1;

	private bool _animationFrameEditMutated;

	private int _animationFrameMutationCount;

	private Button _animationDatFileButton;

	private FileDialog _animationDatFileDialog;

	private ConfirmationDialog _animationReimportConfirmDialog;

	private SpinBox _animationFrameScaleSpinBox;

	private bool _updatingAnimationFrameScaleControl;

	private AdobeAnimateData _pendingAnimationReimportTarget;

	private string _pendingAnimationReimportPath = "";

	private int _pendingAnimationFrameScale = -1;

	private Dictionary _animationExplicitReimportOldSnapshot;

	private int _animationExplicitReimportFrame = -1;

	private int _animationExplicitReimportSliceKey = -1;

	private string _animationExplicitReimportActionName = "重新导入动画 DAT";

	private string _animationExplicitReimportSuccessMessage = "DAT 已在后台重新导入，可使用全局 Undo 无损恢复手工帧。";

	private bool _animationExplicitReimportRequiresRestore;

	private XWVisualPropertyBinding _animationPropertyBinding;

	private AdobeAnimateInspectorPreview _runtimePreview;

	private AdobeAnimateData _animationDatSelectionTarget;

	private string _animationHydrationPath = "";

	private AdobeAnimateData _animationHydrationTarget;

	private CancellationTokenSource _animationHydrationCancellation;

	private Task<AdobeAnimateData.EditorDatHydrationResult> _animationHydrationTask;

	private int _animationHydrationRequestedFrameScale = -1;

	private int _animationHydrationExpectedFrameScale = -1;

	private long _animationHydrationExpectedAuthoringRevision = -1L;

	private int _animationStateUpdateCount;

	private string _selectedAnimationClipName = "";

	private bool _updatingAnimationClipControls;

	private Dictionary _animationClipEditSnapshot;

	private string _animationClipEditName = "";

	private AdobeAnimateData _animationClipEditTarget;

	private int _animationClipMutationCount;

	private AdobeAnimateData _editingAnimation;

	private HFlowContainer _animationReplacementSlotCards;

	private HFlowContainer _animationExtraTextureCards;

	private Label _animationReplacementSummaryLabel;

	private Label _animationReplacementStatusLabel;

	private Button _animationAddReplacementSlotButton;

	private Button _animationAddExtraTextureButton;

	private Button _animationReplacementSaveButton;

	private Button _animationReplacementReloadButton;

	private ConfirmationDialog _animationReplacementReloadConfirmDialog;

	private XWGameplayResourcePickerWindow _animationReplacementPicker;

	private int _animationReplacementMutationCount;

	private int _animationReplacementSlotCardCount;

	private int _animationExtraTextureCardCount;

	private Control _animationWorkbench;

	private TabBar _animationWorkbenchTabs;

	private VBoxContainer _animationChoiceSurface;

	private VBoxContainer _animationSourceHost;

	private VBoxContainer _animationSourceContainer;

	private XWAnimationVisualChoiceGrid _animationMediaChoiceGrid;

	private XWAnimationVisualChoiceGrid _animationLayerChoiceGrid;

	private bool _animationWorkbenchSignalsConnected;

	public int AnimationStateUpdateCount => _animationStateUpdateCount;

	public int AnimationClipMutationCount => _animationClipMutationCount;

	public int AnimationFrameMutationCount => _animationFrameMutationCount;

	public int AnimationReplacementMutationCount => _animationReplacementMutationCount;

	public int AnimationReplacementSlotCardCount => _animationReplacementSlotCardCount;

	public int AnimationExtraTextureCardCount => _animationExtraTextureCardCount;

	public string SelectedAnimationClipName => _selectedAnimationClipName;

	public bool PackedFrameEditingSupported => true;

	public int SelectedAnimationFrame
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_animationFrameTimeline))
			{
				return -1;
			}
			return _animationFrameTimeline.SelectedFrame;
		}
	}

	public int SelectedAnimationSliceKey
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_animationFrameTimeline))
			{
				return -1;
			}
			return _animationFrameTimeline.SelectedSliceKey;
		}
	}

	public int AnimationWorkbenchCurrentTab
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_animationWorkbenchTabs))
			{
				return -1;
			}
			return _animationWorkbenchTabs.CurrentTab;
		}
	}

	public int AnimationMediaVisualChoiceCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_animationMediaChoiceGrid))
			{
				return 0;
			}
			return _animationMediaChoiceGrid.ChoiceCount;
		}
	}

	public int AnimationLayerVisualChoiceCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_animationLayerChoiceGrid))
			{
				return 0;
			}
			return _animationLayerChoiceGrid.ChoiceCount;
		}
	}

	public bool AnimationHighFrequencyActive
	{
		get
		{
			if (IsVisibleInTree() && GodotObject.IsInstanceValid(_runtimePreview))
			{
				return _runtimePreview.IsVisibleInTree();
			}
			return false;
		}
	}

	public bool AnimationHydrationPending
	{
		get
		{
			if (_animationHydrationTask != null)
			{
				return !_animationHydrationTask.IsCompleted;
			}
			return false;
		}
	}

	public int PendingAnimationFrameScale => _pendingAnimationFrameScale;

	public int RequestedAnimationHydrationFrameScale => _animationHydrationRequestedFrameScale;

	public ulong LastAnimationSavePersistedTicks { get; private set; }

	public override void _Ready()
	{
		SetProcess(enable: false);
		base._Ready();
		BindAnimationWorkbench();
	}

	public override void _Process(double delta)
	{
		if (IsVisibleInTree())
		{
			base._Process(delta);
			PollAnimationResourceHydration();
		}
	}

	public override void _ExitTree()
	{
		ReleaseClosedAnimationResource();
		UnbindAnimationWorkbench();
		base._ExitTree();
	}

	protected override void OnCurrentResourceSaved()
	{
		LastAnimationSavePersistedTicks = Time.GetTicksMsec();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		if (!(base.CurrentResource is AdobeAnimateData animation))
		{
			ReleaseClosedAnimationResource();
			return;
		}
		RenderAnimationPreview(animation);
		RenderAnimationTimeline(animation);
		RenderAnimationGraph(animation);
		RenderAnimationReferences(animation);
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!(resource is AdobeAnimateData))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	protected override bool ShouldBindDirectPropertySurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!(resource is AdobeAnimateData))
		{
			return base.ShouldBindDirectPropertySurface(resource, path, descriptor);
		}
		return false;
	}

	private void BindAnimationWorkbench()
	{
		_animationWorkbench = GetNodeOrNull<Control>("%AnimationWorkbench");
		_animationWorkbenchTabs = GetNodeOrNull<TabBar>("%AnimationWorkbenchTabs");
		_animationChoiceSurface = GetNodeOrNull<VBoxContainer>("%AnimationChoiceSurface");
		_animationSourceHost = GetNodeOrNull<VBoxContainer>("%AnimationSourceHost");
		_animationMediaChoiceGrid = GetNodeOrNull<XWAnimationVisualChoiceGrid>("%MediaChoiceGrid");
		_animationLayerChoiceGrid = GetNodeOrNull<XWAnimationVisualChoiceGrid>("%LayerChoiceGrid");
		if (GodotObject.IsInstanceValid(_animationWorkbenchTabs))
		{
			_animationWorkbenchTabs.TabChanged += OnAnimationWorkbenchTabChanged;
		}
		if (GodotObject.IsInstanceValid(_animationMediaChoiceGrid))
		{
			_animationMediaChoiceGrid.ChoiceSelected += OnAnimationVisualMediaSelected;
		}
		if (GodotObject.IsInstanceValid(_animationLayerChoiceGrid))
		{
			_animationLayerChoiceGrid.ChoiceSelected += OnAnimationVisualLayerSelected;
		}
		base.VisibilityChanged += OnAnimationEditorVisibilityChanged;
		_animationWorkbenchSignalsConnected = true;
	}

	private void UnbindAnimationWorkbench()
	{
		if (_animationWorkbenchSignalsConnected)
		{
			if (GodotObject.IsInstanceValid(_animationWorkbenchTabs))
			{
				_animationWorkbenchTabs.TabChanged -= OnAnimationWorkbenchTabChanged;
			}
			if (GodotObject.IsInstanceValid(_animationMediaChoiceGrid))
			{
				_animationMediaChoiceGrid.ChoiceSelected -= OnAnimationVisualMediaSelected;
			}
			if (GodotObject.IsInstanceValid(_animationLayerChoiceGrid))
			{
				_animationLayerChoiceGrid.ChoiceSelected -= OnAnimationVisualLayerSelected;
			}
			base.VisibilityChanged -= OnAnimationEditorVisibilityChanged;
			_animationWorkbenchSignalsConnected = false;
		}
	}

	private void OnAnimationWorkbenchTabChanged(long tab)
	{
		ConfigureAnimationWorkbenchSurface((int)tab);
		if (tab == 0L && GodotObject.IsInstanceValid(_runtimePreview))
		{
			FitRuntimePreviewAfterLayout(_runtimePreview);
		}
		UpdateAnimationProcessEligibility();
	}

	private void OnAnimationEditorVisibilityChanged()
	{
		if (!IsVisibleInTree())
		{
			if (GodotObject.IsInstanceValid(_pendingAnimationReimportTarget))
			{
				_animationReimportConfirmDialog?.Hide();
				CancelPendingAnimationReimportConfirmation();
			}
			CancelExplicitAnimationReimport(restoreOldSnapshot: true);
			CancelAnimationResourceHydration();
			UpdateAnimationProcessEligibility();
		}
		else
		{
			if (base.CurrentResource is AdobeAnimateData adobeAnimateData && !adobeAnimateData.HasPackedRuntimeData() && !AnimationHydrationPending)
			{
				StartAnimationResourceHydration();
			}
			UpdateAnimationProcessEligibility();
			if (GodotObject.IsInstanceValid(_animationWorkbenchTabs) && _animationWorkbenchTabs.CurrentTab == 0 && GodotObject.IsInstanceValid(_runtimePreview))
			{
				FitRuntimePreviewAfterLayout(_runtimePreview);
			}
		}
	}

	private void UpdateAnimationProcessEligibility()
	{
		bool flag = _animationHydrationTask != null;
		SetProcess(IsVisibleInTree() && flag);
	}

	private void MountAnimationResponsiveWorkbench(VBoxContainer source)
	{
		if (!GodotObject.IsInstanceValid(source) || !GodotObject.IsInstanceValid(_animationWorkbench) || !GodotObject.IsInstanceValid(_animationSourceHost))
		{
			return;
		}
		_animationWorkbench.Visible = true;
		if (GodotObject.IsInstanceValid(_animationWorkbenchTabs))
		{
			_animationWorkbenchTabs.CurrentTab = 0;
		}
		Control nodeOrNull = source.GetNodeOrNull<Control>("StageLayout/DirectSettingsPanel");
		Control nodeOrNull2 = source.GetNodeOrNull<Control>("FrameTimelinePanel/FrameTimelineMargin/FrameTimelineStack/FrameTimelineBody/KeyframePropertyPanel");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.Owner = null;
			nodeOrNull.Reparent(source, keepGlobalTransform: false);
			nodeOrNull.Owner = source;
		}
		if (GodotObject.IsInstanceValid(nodeOrNull2))
		{
			nodeOrNull2.Owner = null;
			nodeOrNull2.Reparent(source, keepGlobalTransform: false);
			nodeOrNull2.Owner = source;
		}
		_animationSourceHost.AddChild(source, forceReadableName: false, InternalMode.Disabled);
		_animationSourceContainer = source;
		source.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SubViewportContainer nodeOrNull3 = source.GetNodeOrNull<SubViewportContainer>("%PreviewViewportContainer");
		if (GodotObject.IsInstanceValid(nodeOrNull3))
		{
			nodeOrNull3.Stretch = true;
			nodeOrNull3.StretchShrink = 2;
			nodeOrNull3.CustomMinimumSize = new Vector2(240f, 190f);
			SubViewport nodeOrNull4 = source.GetNodeOrNull<SubViewport>("%PreviewViewport");
			if (GodotObject.IsInstanceValid(nodeOrNull4))
			{
				nodeOrNull4.Size = new Vector2I(640, 360);
			}
		}
		Control nodeOrNull5 = source.GetNodeOrNull<Control>("FrameTimelinePanel/FrameTimelineMargin/FrameTimelineStack/FrameTimelineBody/FrameTimelineScroll");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.CustomMinimumSize = Vector2.Zero;
		}
		if (GodotObject.IsInstanceValid(nodeOrNull5))
		{
			nodeOrNull5.CustomMinimumSize = new Vector2(0f, 330f);
		}
		if (GodotObject.IsInstanceValid(nodeOrNull2))
		{
			nodeOrNull2.CustomMinimumSize = Vector2.Zero;
		}
		HSplitContainer nodeOrNull6 = source.GetNodeOrNull<HSplitContainer>("ClipEditorPanel/ClipEditorMargin/ClipEditor/Body");
		Control nodeOrNull7 = source.GetNodeOrNull<Control>("ClipEditorPanel/ClipEditorMargin/ClipEditor/Body/ClipList");
		Control nodeOrNull8 = source.GetNodeOrNull<Control>("ClipEditorPanel/ClipEditorMargin/ClipEditor/Body/Edit/ClipRangeControl");
		Control nodeOrNull9 = source.GetNodeOrNull<Control>("ClipEditorPanel/ClipEditorMargin/ClipEditor/Body/Edit/NameRow/ClipNameEdit");
		Control nodeOrNull10 = source.GetNodeOrNull<Control>("ClipEditorPanel/ClipEditorMargin/ClipEditor/Body/Edit/NameRow/ShiftEarlierButton");
		Control nodeOrNull11 = source.GetNodeOrNull<Control>("ClipEditorPanel/ClipEditorMargin/ClipEditor/Body/Edit/NameRow/ShiftLaterButton");
		if (GodotObject.IsInstanceValid(nodeOrNull6))
		{
			nodeOrNull6.SplitOffsets = new int[1] { 210 };
		}
		if (GodotObject.IsInstanceValid(nodeOrNull7))
		{
			nodeOrNull7.CustomMinimumSize = new Vector2(180f, 190f);
		}
		if (GodotObject.IsInstanceValid(nodeOrNull8))
		{
			nodeOrNull8.CustomMinimumSize = new Vector2(260f, 92f);
		}
		if (GodotObject.IsInstanceValid(nodeOrNull9))
		{
			nodeOrNull9.CustomMinimumSize = new Vector2(110f, 0f);
		}
		if (GodotObject.IsInstanceValid(nodeOrNull10))
		{
			nodeOrNull10.CustomMinimumSize = new Vector2(64f, 34f);
		}
		if (GodotObject.IsInstanceValid(nodeOrNull11))
		{
			nodeOrNull11.CustomMinimumSize = new Vector2(64f, 34f);
		}
		if (GodotObject.IsInstanceValid(_animationMediaOption))
		{
			_animationMediaOption.Visible = false;
		}
		if (GodotObject.IsInstanceValid(_animationLayerOption))
		{
			_animationLayerOption.Visible = false;
		}
		ConfigureAnimationWorkbenchSurface(0);
		UpdateAnimationProcessEligibility();
	}

	private void ConfigureAnimationWorkbenchSurface(int tab)
	{
		if (GodotObject.IsInstanceValid(_animationSourceContainer))
		{
			bool flag = tab == 0;
			bool flag2 = tab == 1;
			bool visible = tab == 2;
			bool visible2 = tab == 3;
			SetAnimationSurfaceVisible("TitleRow", visible: true);
			SetAnimationSurfaceVisible("StageLayout", flag || flag2);
			SetAnimationSurfaceVisible("StageLayout/PreviewHost", flag);
			SetAnimationSurfaceVisible("DirectSettingsPanel", flag2);
			SetAnimationSurfaceVisible("AtlasLabel", flag);
			SetAnimationSurfaceVisible("ReplacementLibraryPanel", tab == 5);
			SetAnimationSurfaceVisible("ClipEditorPanel", tab == 4);
			SetAnimationSurfaceVisible("FrameTimelinePanel", visible);
			SetAnimationSurfaceVisible("FrameTimelinePanel/FrameTimelineMargin/FrameTimelineStack/FrameTimelineHeader", visible);
			SetAnimationSurfaceVisible("FrameTimelinePanel/FrameTimelineMargin/FrameTimelineStack/FrameTimelineBody/FrameTimelineScroll", visible);
			SetAnimationSurfaceVisible("KeyframePropertyPanel", visible2);
			if (GodotObject.IsInstanceValid(_animationChoiceSurface))
			{
				_animationChoiceSurface.Visible = visible2;
			}
		}
	}

	private void SetAnimationSurfaceVisible(string path, bool visible)
	{
		Control control = _animationSourceContainer?.GetNodeOrNull<Control>(path);
		if (GodotObject.IsInstanceValid(control))
		{
			control.Visible = visible;
		}
	}

	private void ClearAnimationResponsiveWorkbench()
	{
		if (GodotObject.IsInstanceValid(_animationSourceContainer))
		{
			Node parent = _animationSourceContainer.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(_animationSourceContainer);
			}
			_animationSourceContainer.QueueFree();
		}
		_animationSourceContainer = null;
		_animationMediaChoiceGrid?.ClearChoices();
		_animationLayerChoiceGrid?.ClearChoices();
		if (GodotObject.IsInstanceValid(_animationChoiceSurface))
		{
			_animationChoiceSurface.Visible = false;
		}
		if (GodotObject.IsInstanceValid(_animationWorkbench))
		{
			_animationWorkbench.Visible = false;
		}
	}

	private void DisposeAnimationPropertyBinding()
	{
		CancelExplicitAnimationReimport(restoreOldSnapshot: true);
		UnbindAnimationFrameScaleControl();
		if (GodotObject.IsInstanceValid(_animationFrameTimeline))
		{
			_animationFrameTimeline.BindAnimation(null);
		}
		if (GodotObject.IsInstanceValid(_runtimePreview))
		{
			_runtimePreview.ClearPreview();
		}
		DisposeAnimationFrameSurface();
		DisposeAnimationClipSurface();
		DisposeAnimationReplacementSurface();
		if (GodotObject.IsInstanceValid(_runtimePreview))
		{
			_runtimePreview.PreviewStateChanged -= OnAnimationPreviewStateChanged;
		}
		_animationPropertyBinding?.Dispose();
		_animationPropertyBinding = null;
		ClearAnimationResponsiveWorkbench();
		_animationDatSelectionTarget = null;
		_runtimePreview = null;
		_pendingAnimationReimportTarget = null;
		_pendingAnimationReimportPath = "";
		_pendingAnimationFrameScale = -1;
		_animationReimportConfirmDialog = null;
	}

	private void RenderAnimationPreview(AdobeAnimateData animation)
	{
		AdobeAnimateData editingAnimation = _editingAnimation;
		DisposeAnimationPropertyBinding();
		if (GodotObject.IsInstanceValid(editingAnimation) && editingAnimation != animation)
		{
			AdobeAnimateDefinitionCache.Invalidate(editingAnimation);
		}
		_editingAnimation = animation;
		AddPreviewRow("动画文件: " + animation.animeFile);
		AddPreviewRow($"帧率: {animation.frameRate:0.##} fps");
		AddPreviewRow($"总帧数: {animation.frameMax}");
		AddPreviewRow($"图层数: {animation.layerDictionary?.Count ?? 0}");
		AddPreviewRow($"Clip 数: {animation.clips?.Count ?? 0}");
		AddPreviewRow($"事件帧数: {CountEventFrames(animation)}");
		bool flag = animation.HasPackedRuntimeData();
		bool flag2 = flag || !string.IsNullOrWhiteSpace(ResolveCompanionDatPath(animation));
		AddPreviewRow("完整数据: " + (flag ? "已读取" : (flag2 ? "后台加载中" : "等待导入 DAT")));
		AddPreviewRow($"时间线帧数据: {0}");
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 1);
		defaultInterpolatedStringHandler.AppendLiteral("Packed 帧数据: ");
		int[] frameOffsets = animation.frameOffsets;
		defaultInterpolatedStringHandler.AppendFormatted((frameOffsets != null) ? frameOffsets.Length : 0);
		AddPreviewRow(defaultInterpolatedStringHandler.ToStringAndClear());
		defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 1);
		defaultInterpolatedStringHandler.AppendLiteral("Packed 切片数据: ");
		int[] sliceKeys = animation.sliceKeys;
		defaultInterpolatedStringHandler.AppendFormatted((sliceKeys != null) ? sliceKeys.Length : 0);
		AddPreviewRow(defaultInterpolatedStringHandler.ToStringAndClear());
		if (base.CanvasGrid == null)
		{
			return;
		}
		base.CanvasGrid.Columns = 1;
		if (_animationRuntimePreviewScene == null)
		{
			_animationRuntimePreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAnimationRuntimePreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _animationRuntimePreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			return;
		}
		MountAnimationResponsiveWorkbench(vBoxContainer);
		BindAnimationDirectEditSurface(vBoxContainer, animation);
		AdobeAnimateInspectorPreview adobeAnimateInspectorPreview = (_runtimePreview = vBoxContainer.GetNode<AdobeAnimateInspectorPreview>("%RuntimeInspectorPreview"));
		_animationRuntimeFrameLabel = vBoxContainer.GetNode<Label>("TitleRow/FrameLabel");
		_animationPlaybackStateLabel = vBoxContainer.GetNode<Label>("%PlaybackStateLabel");
		_animationStateUpdateCount = 0;
		adobeAnimateInspectorPreview.PreviewStateChanged += OnAnimationPreviewStateChanged;
		BindAnimationClipSurface(vBoxContainer, animation);
		BindAnimationFrameSurface(vBoxContainer, animation);
		PanelContainer node = vBoxContainer.GetNode<PanelContainer>("%LoadingPanel");
		_animationPreviewStatusLabel = vBoxContainer.GetNode<Label>("%LoadingLabel");
		vBoxContainer.GetNode<Label>("TitleRow/FrameLabel").Text = $"{GetAnimationFrameCount(animation)} 帧";
		if (!flag)
		{
			adobeAnimateInspectorPreview.Visible = false;
			node.Visible = true;
			if (flag2)
			{
				StartAnimationResourceHydration();
			}
			else if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
			{
				_animationPreviewStatusLabel.Text = "尚未选择动画 DAT；可直接使用上方按钮导入。";
			}
		}
		else
		{
			node.Visible = false;
			adobeAnimateInspectorPreview.Visible = true;
			adobeAnimateInspectorPreview.EditAnimation(animation);
			FitRuntimePreviewAfterLayout(adobeAnimateInspectorPreview);
		}
		Vector2 vector = (flag ? TryGetSharedAnimationAtlasSize(animation) : Vector2.Zero);
		vBoxContainer.GetNode<Label>("AtlasLabel").Text = ((!(vector == Vector2.Zero)) ? $"内嵌图集: {vector.X:0} x {vector.Y:0}" : (flag ? "未读取到内嵌图集" : (flag2 ? "正在后台读取动画资源..." : "等待导入动画 DAT")));
		if (GodotObject.IsInstanceValid(_animationMediaOption))
		{
			_animationMediaOption.Visible = false;
		}
		if (GodotObject.IsInstanceValid(_animationLayerOption))
		{
			_animationLayerOption.Visible = false;
		}
		ConfigureAnimationWorkbenchSurface(0);
	}

	private void ReleaseClosedAnimationResource()
	{
		AdobeAnimateData editingAnimation = _editingAnimation;
		DisposeAnimationPropertyBinding();
		CancelAnimationResourceHydration();
		_editingAnimation = null;
		if (GodotObject.IsInstanceValid(editingAnimation))
		{
			AdobeAnimateDefinitionCache.Invalidate(editingAnimation);
		}
	}

	private void OnAnimationPreviewStateChanged(int frame, int startFrame, int endFrame, string clipName, bool playing)
	{
		if (AnimationHighFrequencyActive)
		{
			_animationStateUpdateCount++;
			if (GodotObject.IsInstanceValid(_animationFrameTimeline))
			{
				_animationFrameTimeline.SetPlayhead(frame);
			}
			if (GodotObject.IsInstanceValid(_animationRuntimeFrameLabel))
			{
				_animationRuntimeFrameLabel.Text = $"帧 {frame} / {endFrame}";
			}
			if (GodotObject.IsInstanceValid(_animationPlaybackStateLabel))
			{
				string value = (string.IsNullOrWhiteSpace(clipName) ? "全部动画" : clipName);
				_animationPlaybackStateLabel.Text = $"{(playing ? "播放中" : "已暂停")} · {value} · 范围 {startFrame}-{endFrame}";
				_animationPlaybackStateLabel.Modulate = (playing ? new Color(0.66f, 1f, 0.48f) : new Color(1f, 0.78f, 0.34f));
			}
		}
	}

	private void BindAnimationClipSurface(VBoxContainer container, AdobeAnimateData animation)
	{
		_animationClipList = container.GetNode<ItemList>("%ClipList");
		_animationClipNameEdit = container.GetNode<LineEdit>("%ClipNameEdit");
		_animationClipStartSpinBox = container.GetNode<SpinBox>("%ClipStartSpinBox");
		_animationClipEndSpinBox = container.GetNode<SpinBox>("%ClipEndSpinBox");
		_animationClipRangeControl = container.GetNode<XWPacketSpawnEntryRangeControl>("%ClipRangeControl");
		_animationAddClipButton = container.GetNode<Button>("%AddClipButton");
		_animationDeleteClipButton = container.GetNode<Button>("%DeleteClipButton");
		_animationShiftClipEarlierButton = container.GetNode<Button>("%ShiftEarlierButton");
		_animationShiftClipLaterButton = container.GetNode<Button>("%ShiftLaterButton");
		_animationClipEditStatusLabel = container.GetNode<Label>("%ClipEditStatusLabel");
		_animationClipMutationCount = 0;
		_animationClipList.ItemSelected += OnAnimationClipSelected;
		_animationClipNameEdit.TextSubmitted += OnAnimationClipNameSubmitted;
		_animationClipNameEdit.FocusExited += OnAnimationClipNameFocusExited;
		_animationClipStartSpinBox.FocusEntered += BeginAnimationClipExactRangeEdit;
		_animationClipStartSpinBox.ValueChanged += PreviewAnimationClipExactRange;
		_animationClipStartSpinBox.FocusExited += CommitAnimationClipExactRangeEdit;
		_animationClipEndSpinBox.FocusEntered += BeginAnimationClipExactRangeEdit;
		_animationClipEndSpinBox.ValueChanged += PreviewAnimationClipExactRange;
		_animationClipEndSpinBox.FocusExited += CommitAnimationClipExactRangeEdit;
		_animationClipRangeControl.DragStarted += BeginAnimationClipRangeEdit;
		_animationClipRangeControl.ValuesPreviewed += PreviewAnimationClipRange;
		_animationClipRangeControl.ValuesCommitted += CommitAnimationClipRange;
		_animationAddClipButton.Pressed += AddAnimationClip;
		_animationDeleteClipButton.Pressed += DeleteSelectedAnimationClip;
		_animationShiftClipEarlierButton.Pressed += ShiftSelectedAnimationClipEarlier;
		_animationShiftClipLaterButton.Pressed += ShiftSelectedAnimationClipLater;
		RefreshAnimationClipSurface(animation, "", refreshPreviewOptions: true);
	}

	private void DisposeAnimationClipSurface()
	{
		CancelAnimationClipRangeEdit();
		if (GodotObject.IsInstanceValid(_animationClipList))
		{
			_animationClipList.ItemSelected -= OnAnimationClipSelected;
		}
		if (GodotObject.IsInstanceValid(_animationClipNameEdit))
		{
			_animationClipNameEdit.TextSubmitted -= OnAnimationClipNameSubmitted;
			_animationClipNameEdit.FocusExited -= OnAnimationClipNameFocusExited;
		}
		if (GodotObject.IsInstanceValid(_animationClipStartSpinBox))
		{
			_animationClipStartSpinBox.FocusEntered -= BeginAnimationClipExactRangeEdit;
			_animationClipStartSpinBox.ValueChanged -= PreviewAnimationClipExactRange;
			_animationClipStartSpinBox.FocusExited -= CommitAnimationClipExactRangeEdit;
		}
		if (GodotObject.IsInstanceValid(_animationClipEndSpinBox))
		{
			_animationClipEndSpinBox.FocusEntered -= BeginAnimationClipExactRangeEdit;
			_animationClipEndSpinBox.ValueChanged -= PreviewAnimationClipExactRange;
			_animationClipEndSpinBox.FocusExited -= CommitAnimationClipExactRangeEdit;
		}
		if (GodotObject.IsInstanceValid(_animationClipRangeControl))
		{
			_animationClipRangeControl.DragStarted -= BeginAnimationClipRangeEdit;
			_animationClipRangeControl.ValuesPreviewed -= PreviewAnimationClipRange;
			_animationClipRangeControl.ValuesCommitted -= CommitAnimationClipRange;
		}
		if (GodotObject.IsInstanceValid(_animationAddClipButton))
		{
			_animationAddClipButton.Pressed -= AddAnimationClip;
		}
		if (GodotObject.IsInstanceValid(_animationDeleteClipButton))
		{
			_animationDeleteClipButton.Pressed -= DeleteSelectedAnimationClip;
		}
		if (GodotObject.IsInstanceValid(_animationShiftClipEarlierButton))
		{
			_animationShiftClipEarlierButton.Pressed -= ShiftSelectedAnimationClipEarlier;
		}
		if (GodotObject.IsInstanceValid(_animationShiftClipLaterButton))
		{
			_animationShiftClipLaterButton.Pressed -= ShiftSelectedAnimationClipLater;
		}
		_animationClipList = null;
		_animationClipNameEdit = null;
		_animationClipStartSpinBox = null;
		_animationClipEndSpinBox = null;
		_animationClipRangeControl = null;
		_animationAddClipButton = null;
		_animationDeleteClipButton = null;
		_animationShiftClipEarlierButton = null;
		_animationShiftClipLaterButton = null;
		_animationClipEditStatusLabel = null;
		_selectedAnimationClipName = "";
	}

	private void RefreshAnimationClipSurface(AdobeAnimateData animation, string requestedSelection, bool refreshPreviewOptions)
	{
		if (!GodotObject.IsInstanceValid(animation) || !GodotObject.IsInstanceValid(_animationClipList))
		{
			return;
		}
		int num = Math.Max(0, GetAnimationFrameCount(animation) - 1);
		_updatingAnimationClipControls = true;
		_animationClipStartSpinBox.MinValue = 0.0;
		_animationClipStartSpinBox.MaxValue = num;
		_animationClipEndSpinBox.MinValue = 0.0;
		_animationClipEndSpinBox.MaxValue = num;
		_animationClipRangeControl.SetTitle("Clip 可播放区间（结束帧为可见帧）");
		_animationClipRangeControl.Configure(0.0, num, 1.0, allowUnlimited: false);
		_animationClipList.Clear();
		string text = (HasAnimationClip(animation, requestedSelection) ? requestedSelection : (HasAnimationClip(animation, _selectedAnimationClipName) ? _selectedAnimationClipName : ""));
		int num2 = -1;
		if (animation.clips != null)
		{
			foreach (Variant key in animation.clips.Keys)
			{
				string text2 = key.AsString();
				Vector2I range = ClampAnimationClipRange(animation, animation.GetClip(text2));
				int num3 = _animationClipList.AddItem(FormatAnimationClipListItem(text2, range));
				_animationClipList.SetItemMetadata(num3, text2);
				if (string.Equals(text, text2, StringComparison.Ordinal))
				{
					num2 = num3;
				}
			}
		}
		_updatingAnimationClipControls = false;
		if (refreshPreviewOptions && GodotObject.IsInstanceValid(_runtimePreview))
		{
			_runtimePreview.RefreshClipOptions(text);
		}
		if (num2 >= 0)
		{
			_animationClipList.Select(num2);
			SelectAnimationClip(text, !refreshPreviewOptions);
		}
		else
		{
			SelectAnimationClip("", !refreshPreviewOptions);
		}
	}

	private void OnAnimationClipSelected(long index)
	{
		if (!_updatingAnimationClipControls && GodotObject.IsInstanceValid(_animationClipList))
		{
			CommitAnimationClipRangeEdit();
			string clipName = ((index >= 0 && index < _animationClipList.ItemCount) ? _animationClipList.GetItemMetadata((int)index).AsString() : "");
			SelectAnimationClip(clipName, syncPreview: true);
		}
	}

	private void SelectAnimationClip(string clipName, bool syncPreview)
	{
		_selectedAnimationClipName = clipName ?? "";
		AdobeAnimateData adobeAnimateData = base.CurrentResource as AdobeAnimateData;
		bool flag = GodotObject.IsInstanceValid(adobeAnimateData) && HasAnimationClip(adobeAnimateData, _selectedAnimationClipName);
		_updatingAnimationClipControls = true;
		if (GodotObject.IsInstanceValid(_animationClipNameEdit))
		{
			_animationClipNameEdit.Editable = flag;
			_animationClipNameEdit.Text = (flag ? _selectedAnimationClipName : "");
		}
		if (flag)
		{
			Vector2I animationClipRangeControls = ClampAnimationClipRange(adobeAnimateData, adobeAnimateData.GetClip(_selectedAnimationClipName));
			SetAnimationClipRangeControls(animationClipRangeControls);
		}
		else
		{
			SetAnimationClipRangeControls(new Vector2I(0, 1));
		}
		_updatingAnimationClipControls = false;
		if (GodotObject.IsInstanceValid(_animationDeleteClipButton))
		{
			_animationDeleteClipButton.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_animationShiftClipEarlierButton))
		{
			_animationShiftClipEarlierButton.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_animationShiftClipLaterButton))
		{
			_animationShiftClipLaterButton.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_animationClipStartSpinBox))
		{
			_animationClipStartSpinBox.Editable = flag;
		}
		if (GodotObject.IsInstanceValid(_animationClipEndSpinBox))
		{
			_animationClipEndSpinBox.Editable = flag;
		}
		if (GodotObject.IsInstanceValid(_animationClipRangeControl))
		{
			_animationClipRangeControl.MouseFilter = (MouseFilterEnum)(flag ? 0 : 2);
			_animationClipRangeControl.Modulate = (flag ? Colors.White : new Color(0.48f, 0.52f, 0.48f, 0.7f));
		}
		if (syncPreview && GodotObject.IsInstanceValid(_runtimePreview))
		{
			_runtimePreview.SelectClipByName(flag ? _selectedAnimationClipName : "");
		}
		SetAnimationClipStatus(flag ? ("正在编辑 " + _selectedAnimationClipName + "：拖动两个彩色端点调整可播放范围。") : "当前没有 Clip；点击“创建 Clip”从当前预览帧建立。");
	}

	private void SetAnimationClipRangeControls(Vector2I storedRange)
	{
		int num = Math.Max(storedRange.X, storedRange.Y - 1);
		if (GodotObject.IsInstanceValid(_animationClipStartSpinBox))
		{
			_animationClipStartSpinBox.Value = storedRange.X;
		}
		if (GodotObject.IsInstanceValid(_animationClipEndSpinBox))
		{
			_animationClipEndSpinBox.Value = num;
		}
		if (GodotObject.IsInstanceValid(_animationClipRangeControl))
		{
			_animationClipRangeControl.SetValues(storedRange.X, num);
		}
	}

	private void BeginAnimationClipRangeEdit(bool minimumHandle)
	{
		BeginAnimationClipRangeEdit();
	}

	private void BeginAnimationClipExactRangeEdit()
	{
		BeginAnimationClipRangeEdit();
	}

	private void BeginAnimationClipRangeEdit()
	{
		if (_animationClipEditSnapshot == null && base.CurrentResource is AdobeAnimateData adobeAnimateData && HasAnimationClip(adobeAnimateData, _selectedAnimationClipName))
		{
			_animationClipEditSnapshot = CloneAnimationClips(adobeAnimateData.clips);
			_animationClipEditName = _selectedAnimationClipName;
			_animationClipEditTarget = adobeAnimateData;
		}
	}

	private void PreviewAnimationClipExactRange(double value)
	{
		if (!_updatingAnimationClipControls)
		{
			PreviewAnimationClipRange(_animationClipStartSpinBox.Value, _animationClipEndSpinBox.Value);
		}
	}

	private void PreviewAnimationClipRange(double minimum, double maximum)
	{
		if (!_updatingAnimationClipControls && base.CurrentResource is AdobeAnimateData adobeAnimateData && HasAnimationClip(adobeAnimateData, _selectedAnimationClipName))
		{
			BeginAnimationClipRangeEdit();
			Vector2I vector2I = ClampAnimationClipRange(adobeAnimateData, new Vector2I((int)Math.Round(minimum), (int)Math.Round(maximum) + 1));
			Dictionary dictionary = CloneAnimationClips(adobeAnimateData.clips);
			dictionary[_selectedAnimationClipName] = vector2I;
			adobeAnimateData.clips = dictionary;
			adobeAnimateData.EmitChanged();
			_updatingAnimationClipControls = true;
			SetAnimationClipRangeControls(vector2I);
			UpdateAnimationClipListItem(_selectedAnimationClipName, vector2I);
			_updatingAnimationClipControls = false;
			if (GodotObject.IsInstanceValid(_runtimePreview))
			{
				_runtimePreview.SelectClipByName(_selectedAnimationClipName);
			}
			SetAnimationClipStatus($"预览 {_selectedAnimationClipName}：{vector2I.X}-{vector2I.Y - 1}（松开后写入撤销历史）");
		}
	}

	private void CommitAnimationClipExactRangeEdit()
	{
		CommitAnimationClipRangeEdit();
	}

	private void CommitAnimationClipRange(double minimum, double maximum)
	{
		PreviewAnimationClipRange(minimum, maximum);
		CommitAnimationClipRangeEdit();
	}

	private void CommitAnimationClipRangeEdit()
	{
		AdobeAnimateData animationClipEditTarget = _animationClipEditTarget;
		if (_animationClipEditSnapshot == null || !GodotObject.IsInstanceValid(animationClipEditTarget) || base.CurrentResource != animationClipEditTarget)
		{
			CancelAnimationClipRangeEdit();
			return;
		}
		Dictionary animationClipEditSnapshot = _animationClipEditSnapshot;
		string animationClipEditName = _animationClipEditName;
		Dictionary nextClips = CloneAnimationClips(animationClipEditTarget.clips);
		_animationClipEditSnapshot = null;
		_animationClipEditName = "";
		_animationClipEditTarget = null;
		CommitAnimationClipDictionaryChange(animationClipEditSnapshot, nextClips, animationClipEditName, animationClipEditName, "调整动画 Clip 范围");
	}

	private void CancelAnimationClipRangeEdit()
	{
		if (_animationClipEditSnapshot != null && GodotObject.IsInstanceValid(_animationClipEditTarget))
		{
			_animationClipEditTarget.clips = CloneAnimationClips(_animationClipEditSnapshot);
			_animationClipEditTarget.EmitChanged();
		}
		_animationClipEditSnapshot = null;
		_animationClipEditName = "";
		_animationClipEditTarget = null;
	}

	private void AddAnimationClip()
	{
		CommitAnimationClipRangeEdit();
		if (base.CurrentResource is AdobeAnimateData adobeAnimateData)
		{
			Dictionary oldClips = CloneAnimationClips(adobeAnimateData.clips);
			Dictionary dictionary = CloneAnimationClips(adobeAnimateData.clips);
			string text = BuildUniqueAnimationClipName(dictionary, "clip");
			int num = Math.Max(1, GetAnimationFrameCount(adobeAnimateData));
			int num2 = Math.Clamp(GodotObject.IsInstanceValid(_runtimePreview) ? _runtimePreview.CurrentFrame : 0, 0, num - 1);
			dictionary[text] = new Vector2I(num2, Math.Min(num, num2 + 1));
			CommitAnimationClipDictionaryChange(oldClips, dictionary, text, _selectedAnimationClipName, "创建动画 Clip");
		}
	}

	private void DeleteSelectedAnimationClip()
	{
		CommitAnimationClipRangeEdit();
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData) || !HasAnimationClip(adobeAnimateData, _selectedAnimationClipName))
		{
			return;
		}
		string selectedAnimationClipName = _selectedAnimationClipName;
		Dictionary oldClips = CloneAnimationClips(adobeAnimateData.clips);
		Dictionary dictionary = new Dictionary();
		string text = "";
		foreach (Variant key in adobeAnimateData.clips.Keys)
		{
			string text2 = key.AsString();
			if (!string.Equals(text2, selectedAnimationClipName, StringComparison.Ordinal))
			{
				dictionary[key] = adobeAnimateData.clips[key];
				if (string.IsNullOrEmpty(text))
				{
					text = text2;
				}
			}
		}
		CommitAnimationClipDictionaryChange(oldClips, dictionary, text, selectedAnimationClipName, "删除动画 Clip");
	}

	private void ShiftSelectedAnimationClipEarlier()
	{
		ShiftSelectedAnimationClip(-1);
	}

	private void ShiftSelectedAnimationClipLater()
	{
		ShiftSelectedAnimationClip(1);
	}

	private void ShiftSelectedAnimationClip(int direction)
	{
		CommitAnimationClipRangeEdit();
		if (base.CurrentResource is AdobeAnimateData adobeAnimateData && HasAnimationClip(adobeAnimateData, _selectedAnimationClipName))
		{
			Vector2I vector2I = ClampAnimationClipRange(adobeAnimateData, adobeAnimateData.GetClip(_selectedAnimationClipName));
			int num = Math.Max(1, GetAnimationFrameCount(adobeAnimateData));
			int num2 = Math.Max(1, vector2I.Y - vector2I.X);
			int num3 = Math.Clamp(vector2I.X + Math.Sign(direction), 0, Math.Max(0, num - num2));
			if (num3 != vector2I.X)
			{
				Dictionary oldClips = CloneAnimationClips(adobeAnimateData.clips);
				Dictionary dictionary = CloneAnimationClips(adobeAnimateData.clips);
				dictionary[_selectedAnimationClipName] = new Vector2I(num3, num3 + num2);
				CommitAnimationClipDictionaryChange(oldClips, dictionary, _selectedAnimationClipName, _selectedAnimationClipName, (direction < 0) ? "动画 Clip 前移 1 帧" : "动画 Clip 后移 1 帧");
			}
		}
	}

	private void OnAnimationClipNameSubmitted(string text)
	{
		RenameSelectedAnimationClip(text);
	}

	private void OnAnimationClipNameFocusExited()
	{
		if (!_updatingAnimationClipControls && GodotObject.IsInstanceValid(_animationClipNameEdit))
		{
			RenameSelectedAnimationClip(_animationClipNameEdit.Text);
		}
	}

	private void RenameSelectedAnimationClip(string requestedName)
	{
		CommitAnimationClipRangeEdit();
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData) || !HasAnimationClip(adobeAnimateData, _selectedAnimationClipName))
		{
			return;
		}
		string selectedAnimationClipName = _selectedAnimationClipName;
		string text = (requestedName ?? "").Trim();
		if (string.Equals(selectedAnimationClipName, text, StringComparison.Ordinal))
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			SetAnimationClipStatus("Clip 名称不能为空。", error: true);
			return;
		}
		if (HasAnimationClip(adobeAnimateData, text))
		{
			SetAnimationClipStatus("Clip 名称“" + text + "”已存在。", error: true);
			return;
		}
		Dictionary oldClips = CloneAnimationClips(adobeAnimateData.clips);
		Dictionary dictionary = new Dictionary();
		foreach (Variant key in adobeAnimateData.clips.Keys)
		{
			if (string.Equals(key.AsString(), selectedAnimationClipName, StringComparison.Ordinal))
			{
				dictionary[text] = adobeAnimateData.clips[key];
			}
			else
			{
				dictionary[key] = adobeAnimateData.clips[key];
			}
		}
		CommitAnimationClipDictionaryChange(oldClips, dictionary, text, selectedAnimationClipName, "重命名动画 Clip");
	}

	private void CommitAnimationClipDictionaryChange(Dictionary oldClips, Dictionary nextClips, string doSelection, string undoSelection, string actionName)
	{
		AdobeAnimateData from = base.CurrentResource as AdobeAnimateData;
		if (from != null && !AnimationClipDictionariesEqual(oldClips, nextClips))
		{
			Dictionary from2 = CloneAnimationClips(nextClips);
			Dictionary from3 = CloneAnimationClips(oldClips);
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				from.clips = from2;
				RefreshAnimationClipEditorFromHistory(from, doSelection);
				return;
			}
			xWUndoRedoManager.CreateAction(actionName);
			xWUndoRedoManager.AddDoProperty(from, "clips", Variant.From(in from2));
			xWUndoRedoManager.AddUndoProperty(from, "clips", Variant.From(in from3));
			Variant[] obj = new Variant[2]
			{
				Variant.From(in from),
				default(Variant)
			};
			string from4 = doSelection ?? "";
			obj[1] = Variant.From(in from4);
			xWUndoRedoManager.AddDoMethod(this, "RefreshAnimationClipEditorFromHistory", obj);
			Variant[] obj2 = new Variant[2]
			{
				Variant.From(in from),
				default(Variant)
			};
			obj2[1] = Variant.From<string>(undoSelection ?? "");
			xWUndoRedoManager.AddUndoMethod(this, "RefreshAnimationClipEditorFromHistory", obj2);
			xWUndoRedoManager.CommitAction();
		}
	}

	public void RefreshAnimationClipEditorFromHistory(AdobeAnimateData animation, string requestedSelection)
	{
		if (GodotObject.IsInstanceValid(animation))
		{
			AdobeAnimateDefinitionCache.Invalidate(animation);
			animation.EmitChanged();
			if (base.CurrentResource == animation)
			{
				_animationClipMutationCount++;
				RefreshAnimationClipSurface(animation, requestedSelection ?? "", refreshPreviewOptions: true);
				RefreshAnimationTimelineClipRows(animation);
				UpdateAnimationDirectSummary();
				NotifyCurrentResourceEdited();
			}
		}
	}

	private void RefreshAnimationTimelineClipRows(AdobeAnimateData animation)
	{
		if (!GodotObject.IsInstanceValid(base.TimelineList))
		{
			return;
		}
		for (int num = base.TimelineList.ItemCount - 1; num >= 0; num--)
		{
			string itemText = base.TimelineList.GetItemText(num);
			if (itemText.StartsWith("Clip ", StringComparison.Ordinal) || itemText.StartsWith("Clip:", StringComparison.Ordinal))
			{
				base.TimelineList.RemoveItem(num);
			}
		}
		if (animation.clips == null || animation.clips.Count == 0)
		{
			AddTimelineRow("Clip: 未定义");
			return;
		}
		foreach (Variant key in animation.clips.Keys)
		{
			AddTimelineRow("Clip " + key.AsString() + ": " + FormatVariant(animation.clips[key]));
		}
	}

	private void UpdateAnimationClipListItem(string clipName, Vector2I range)
	{
		if (!GodotObject.IsInstanceValid(_animationClipList))
		{
			return;
		}
		for (int i = 0; i < _animationClipList.ItemCount; i++)
		{
			if (string.Equals(_animationClipList.GetItemMetadata(i).AsString(), clipName, StringComparison.Ordinal))
			{
				_animationClipList.SetItemText(i, FormatAnimationClipListItem(clipName, range));
				break;
			}
		}
	}

	private void SetAnimationClipStatus(string message, bool error = false)
	{
		if (GodotObject.IsInstanceValid(_animationClipEditStatusLabel))
		{
			_animationClipEditStatusLabel.Text = message ?? "";
			_animationClipEditStatusLabel.Modulate = (error ? new Color(1f, 0.42f, 0.34f) : new Color(0.68f, 0.88f, 0.52f));
		}
	}

	private static string FormatAnimationClipListItem(string clipName, Vector2I range)
	{
		return $"{clipName}    {range.X} ━ {Math.Max(range.X, range.Y - 1)}    ({Math.Max(1, range.Y - range.X)} 帧)";
	}

	private static Vector2I ClampAnimationClipRange(AdobeAnimateData animation, Vector2I range)
	{
		int num = Math.Max(1, GetAnimationFrameCount(animation));
		int num2 = Math.Clamp(range.X, 0, num - 1);
		int num3 = Math.Clamp(Math.Max(num2, range.Y - 1), num2, num - 1);
		return new Vector2I(num2, num3 + 1);
	}

	private static bool HasAnimationClip(AdobeAnimateData animation, string clipName)
	{
		if (animation?.clips == null || string.IsNullOrEmpty(clipName))
		{
			return false;
		}
		foreach (Variant key in animation.clips.Keys)
		{
			if (string.Equals(key.AsString(), clipName, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static string BuildUniqueAnimationClipName(Dictionary clips, string baseName)
	{
		baseName = (string.IsNullOrWhiteSpace(baseName) ? "clip" : baseName.Trim());
		string text = baseName;
		int num = 2;
		while (AnimationClipDictionaryContains(clips, text))
		{
			text = $"{baseName}_{num++}";
		}
		return text;
	}

	private static bool AnimationClipDictionaryContains(Dictionary clips, string clipName)
	{
		if (clips == null)
		{
			return false;
		}
		foreach (Variant key in clips.Keys)
		{
			if (string.Equals(key.AsString(), clipName, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static Dictionary CloneAnimationClips(Dictionary source)
	{
		Dictionary dictionary = new Dictionary();
		if (source == null)
		{
			return dictionary;
		}
		foreach (Variant key in source.Keys)
		{
			dictionary[key] = source[key];
		}
		return dictionary;
	}

	private static bool AnimationClipDictionariesEqual(Dictionary left, Dictionary right)
	{
		if (left == right)
		{
			return true;
		}
		if (left == null || right == null || left.Count != right.Count)
		{
			return false;
		}
		foreach (Variant key in left.Keys)
		{
			string text = key.AsString();
			if (!AnimationClipDictionaryContains(right, text))
			{
				return false;
			}
			Variant variant = default(Variant);
			foreach (Variant key2 in right.Keys)
			{
				if (string.Equals(key2.AsString(), text, StringComparison.Ordinal))
				{
					variant = right[key2];
					break;
				}
			}
			if (!left[key].Equals(variant))
			{
				return false;
			}
		}
		return true;
	}

	private void BindAnimationFrameSurface(VBoxContainer container, AdobeAnimateData animation)
	{
		_animationFrameTimeline = container.GetNode<XWAnimationFrameTimeline>("%FrameTimeline");
		_animationAddKeyframeButton = container.GetNode<Button>("%AddKeyframeButton");
		_animationDuplicateKeyframeButton = container.GetNode<Button>("%DuplicateKeyframeButton");
		_animationDeleteKeyframeButton = container.GetNode<Button>("%DeleteKeyframeButton");
		_animationMoveKeyframeEarlierButton = container.GetNode<Button>("%MoveKeyframeEarlierButton");
		_animationMoveKeyframeLaterButton = container.GetNode<Button>("%MoveKeyframeLaterButton");
		_animationSelectedKeyframeLabel = container.GetNode<Label>("%SelectedKeyframeLabel");
		_animationMediaOption = container.GetNode<OptionButton>("%MediaOption");
		_animationLayerOption = container.GetNode<OptionButton>("%LayerOption");
		_animationMediaThumbnail = container.GetNode<TextureRect>("%MediaThumbnail");
		_animationPositionXSpinBox = container.GetNode<SpinBox>("%PositionXSpinBox");
		_animationPositionYSpinBox = container.GetNode<SpinBox>("%PositionYSpinBox");
		_animationRotationSpinBox = container.GetNode<SpinBox>("%RotationSpinBox");
		_animationSkewSpinBox = container.GetNode<SpinBox>("%SkewSpinBox");
		_animationScaleXSpinBox = container.GetNode<SpinBox>("%ScaleXSpinBox");
		_animationScaleYSpinBox = container.GetNode<SpinBox>("%ScaleYSpinBox");
		_animationAlphaSlider = container.GetNode<HSlider>("%AlphaSlider");
		_animationAlphaPercentLabel = container.GetNode<Label>("%AlphaPercentLabel");
		_animationKeyframeEditStatusLabel = container.GetNode<Label>("%KeyframeEditStatusLabel");
		_animationFrameMutationCount = 0;
		_animationFrameTimeline.KeyframeSelected += OnAnimationKeyframeSelected;
		_animationFrameTimeline.KeyframeMoveRequested += OnAnimationKeyframeMoveRequested;
		_animationAddKeyframeButton.Pressed += AddAnimationKeyframeAtPlayhead;
		_animationDuplicateKeyframeButton.Pressed += DuplicateSelectedAnimationKeyframe;
		_animationDeleteKeyframeButton.Pressed += DeleteSelectedAnimationKeyframe;
		_animationMoveKeyframeEarlierButton.Pressed += MoveSelectedAnimationKeyframeEarlier;
		_animationMoveKeyframeLaterButton.Pressed += MoveSelectedAnimationKeyframeLater;
		_animationMediaOption.ItemSelected += OnAnimationFrameMediaSelected;
		_animationLayerOption.ItemSelected += OnAnimationFrameLayerSelected;
		BindAnimationFrameNumberControl(_animationPositionXSpinBox);
		BindAnimationFrameNumberControl(_animationPositionYSpinBox);
		BindAnimationFrameNumberControl(_animationRotationSpinBox);
		BindAnimationFrameNumberControl(_animationSkewSpinBox);
		BindAnimationFrameNumberControl(_animationScaleXSpinBox);
		BindAnimationFrameNumberControl(_animationScaleYSpinBox);
		_animationAlphaSlider.DragStarted += BeginAnimationFramePropertyEdit;
		_animationAlphaSlider.ValueChanged += PreviewSelectedAnimationFrameSlice;
		_animationAlphaSlider.DragEnded += CommitAnimationFrameAlphaEdit;
		PopulateAnimationFrameOptions(animation);
		_animationFrameTimeline.BindAnimation(animation);
		if (!_animationFrameTimeline.SelectFirstOccupiedCell())
		{
			SetAnimationFrameControlsEnabled(enabled: false);
		}
	}

	private void BindAnimationFrameNumberControl(SpinBox spinBox)
	{
		spinBox.FocusEntered += BeginAnimationFramePropertyEdit;
		spinBox.ValueChanged += PreviewSelectedAnimationFrameSlice;
		spinBox.FocusExited += CommitAnimationFramePropertyEdit;
	}

	private void DisposeAnimationFrameSurface()
	{
		CancelAnimationFramePropertyEdit();
		if (GodotObject.IsInstanceValid(_animationFrameTimeline))
		{
			_animationFrameTimeline.KeyframeSelected -= OnAnimationKeyframeSelected;
			_animationFrameTimeline.KeyframeMoveRequested -= OnAnimationKeyframeMoveRequested;
		}
		if (GodotObject.IsInstanceValid(_animationAddKeyframeButton))
		{
			_animationAddKeyframeButton.Pressed -= AddAnimationKeyframeAtPlayhead;
		}
		if (GodotObject.IsInstanceValid(_animationDuplicateKeyframeButton))
		{
			_animationDuplicateKeyframeButton.Pressed -= DuplicateSelectedAnimationKeyframe;
		}
		if (GodotObject.IsInstanceValid(_animationDeleteKeyframeButton))
		{
			_animationDeleteKeyframeButton.Pressed -= DeleteSelectedAnimationKeyframe;
		}
		if (GodotObject.IsInstanceValid(_animationMoveKeyframeEarlierButton))
		{
			_animationMoveKeyframeEarlierButton.Pressed -= MoveSelectedAnimationKeyframeEarlier;
		}
		if (GodotObject.IsInstanceValid(_animationMoveKeyframeLaterButton))
		{
			_animationMoveKeyframeLaterButton.Pressed -= MoveSelectedAnimationKeyframeLater;
		}
		if (GodotObject.IsInstanceValid(_animationMediaOption))
		{
			_animationMediaOption.ItemSelected -= OnAnimationFrameMediaSelected;
		}
		if (GodotObject.IsInstanceValid(_animationLayerOption))
		{
			_animationLayerOption.ItemSelected -= OnAnimationFrameLayerSelected;
		}
		UnbindAnimationFrameNumberControl(_animationPositionXSpinBox);
		UnbindAnimationFrameNumberControl(_animationPositionYSpinBox);
		UnbindAnimationFrameNumberControl(_animationRotationSpinBox);
		UnbindAnimationFrameNumberControl(_animationSkewSpinBox);
		UnbindAnimationFrameNumberControl(_animationScaleXSpinBox);
		UnbindAnimationFrameNumberControl(_animationScaleYSpinBox);
		if (GodotObject.IsInstanceValid(_animationAlphaSlider))
		{
			_animationAlphaSlider.DragStarted -= BeginAnimationFramePropertyEdit;
			_animationAlphaSlider.ValueChanged -= PreviewSelectedAnimationFrameSlice;
			_animationAlphaSlider.DragEnded -= CommitAnimationFrameAlphaEdit;
		}
		_animationFrameTimeline = null;
		_animationAddKeyframeButton = null;
		_animationDuplicateKeyframeButton = null;
		_animationDeleteKeyframeButton = null;
		_animationMoveKeyframeEarlierButton = null;
		_animationMoveKeyframeLaterButton = null;
		_animationSelectedKeyframeLabel = null;
		_animationMediaOption = null;
		_animationLayerOption = null;
		_animationMediaThumbnail = null;
		_animationPositionXSpinBox = null;
		_animationPositionYSpinBox = null;
		_animationRotationSpinBox = null;
		_animationSkewSpinBox = null;
		_animationScaleXSpinBox = null;
		_animationScaleYSpinBox = null;
		_animationAlphaSlider = null;
		_animationAlphaPercentLabel = null;
		_animationKeyframeEditStatusLabel = null;
	}

	private void UnbindAnimationFrameNumberControl(SpinBox spinBox)
	{
		if (GodotObject.IsInstanceValid(spinBox))
		{
			spinBox.FocusEntered -= BeginAnimationFramePropertyEdit;
			spinBox.ValueChanged -= PreviewSelectedAnimationFrameSlice;
			spinBox.FocusExited -= CommitAnimationFramePropertyEdit;
		}
	}

	private void PopulateAnimationFrameOptions(AdobeAnimateData animation)
	{
		_updatingAnimationFrameControls = true;
		_animationMediaOption.Clear();
		string[] animationMediaNames = GetAnimationMediaNames(animation);
		List<XWAnimationVisualChoice> list = new List<XWAnimationVisualChoice>(animationMediaNames.Length);
		if (_animationVisualFallbackIcon == null)
		{
			_animationVisualFallbackIcon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/2D.svg", null, ResourceLoader.CacheMode.Reuse);
		}
		for (int i = 0; i < animationMediaNames.Length; i++)
		{
			int itemCount = _animationMediaOption.ItemCount;
			_animationMediaOption.AddItem(animationMediaNames[i]);
			_animationMediaOption.SetItemMetadata(itemCount, i);
			Texture2D icon = CreateAnimationMediaThumbnail(animation, i) ?? _animationVisualFallbackIcon;
			Color accent = Color.FromHsv(Mathf.PosMod((float)i * 0.113f + 0.08f, 1f), 0.5f, 0.94f);
			list.Add(new XWAnimationVisualChoice(i, animationMediaNames[i], $"媒体 #{i}", icon, accent));
		}
		_animationLayerOption.Clear();
		string[] animationLayerNames = GetAnimationLayerNames(animation);
		List<XWAnimationVisualChoice> list2 = new List<XWAnimationVisualChoice>(animationLayerNames.Length);
		for (int j = 0; j < animationLayerNames.Length; j++)
		{
			int itemCount2 = _animationLayerOption.ItemCount;
			_animationLayerOption.AddItem(animationLayerNames[j]);
			_animationLayerOption.SetItemMetadata(itemCount2, j);
			Color accent2 = Color.FromHsv(Mathf.PosMod((float)j * 0.137f + 0.23f, 1f), 0.52f, 0.94f);
			list2.Add(new XWAnimationVisualChoice(j, animationLayerNames[j], $"图层 #{j + 1}", _animationVisualFallbackIcon, accent2));
		}
		if (GodotObject.IsInstanceValid(_animationMediaChoiceGrid))
		{
			_animationMediaChoiceGrid.Configure("媒体缩略图", list, -1, enabled: false);
		}
		if (GodotObject.IsInstanceValid(_animationLayerChoiceGrid))
		{
			_animationLayerChoiceGrid.Configure("图层状态色", list2, -1, enabled: false);
		}
		_updatingAnimationFrameControls = false;
	}

	private void OnAnimationKeyframeSelected(int frame, int layer, int sliceKey)
	{
		CommitAnimationFramePropertyEdit();
		if (GodotObject.IsInstanceValid(_runtimePreview))
		{
			_runtimePreview.SetFrameFromSlider(frame);
		}
		RefreshSelectedAnimationFrameControls(frame, sliceKey);
	}

	private void RefreshSelectedAnimationFrameControls(int frame, int sliceKey)
	{
		string error = "";
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData) || !adobeAnimateData.TryGetFrameSlice(frame, sliceKey, out var slice, out error))
		{
			SetAnimationFrameControlsEnabled(enabled: false);
			SetAnimationFrameStatus(string.IsNullOrWhiteSpace(error) ? "选中帧状态已不存在。" : error, error: true);
			return;
		}
		_updatingAnimationFrameControls = true;
		SetAnimationFrameControlsEnabled(enabled: true);
		string animationNameById = GetAnimationNameById(adobeAnimateData.layerDictionary, slice.LayerId, $"图层 {slice.LayerId + 1}");
		string animationNameById2 = GetAnimationNameById(adobeAnimateData.mediaDictionary, slice.MediaId, $"媒体 {slice.MediaId + 1}");
		_animationSelectedKeyframeLabel.Text = $"帧 {frame}  ·  {animationNameById}  ·  Slice #{slice.SliceKey}";
		SelectOptionByMetadata(_animationMediaOption, slice.MediaId);
		SelectOptionByMetadata(_animationLayerOption, slice.LayerId);
		_animationMediaChoiceGrid?.SelectChoice(slice.MediaId, emitSignal: false);
		_animationLayerChoiceGrid?.SelectChoice(slice.LayerId, emitSignal: false);
		Transform2D transform = slice.Transform;
		_animationPositionXSpinBox.Value = transform.Origin.X;
		_animationPositionYSpinBox.Value = transform.Origin.Y;
		_animationRotationSpinBox.Value = Mathf.RadToDeg(transform.Rotation);
		_animationSkewSpinBox.Value = Mathf.RadToDeg(transform.Skew);
		_animationScaleXSpinBox.Value = transform.Scale.X;
		_animationScaleYSpinBox.Value = transform.Scale.Y;
		_animationAlphaSlider.Value = slice.Alpha;
		_animationAlphaPercentLabel.Text = $"{Mathf.RoundToInt(slice.Alpha * 100f)}%";
		UpdateAnimationMediaThumbnail(adobeAnimateData, slice.MediaId);
		_updatingAnimationFrameControls = false;
		SetAnimationFrameStatus("正在直接编辑 " + animationNameById2 + "；拖动时间轴菱形可移帧。");
	}

	private void SetAnimationFrameControlsEnabled(bool enabled)
	{
		if (GodotObject.IsInstanceValid(_animationMediaOption))
		{
			_animationMediaOption.Disabled = !enabled;
		}
		if (GodotObject.IsInstanceValid(_animationLayerOption))
		{
			_animationLayerOption.Disabled = !enabled;
		}
		_animationMediaChoiceGrid?.SetChoicesEnabled(enabled);
		_animationLayerChoiceGrid?.SetChoicesEnabled(enabled);
		SpinBox[] array = new SpinBox[6] { _animationPositionXSpinBox, _animationPositionYSpinBox, _animationRotationSpinBox, _animationSkewSpinBox, _animationScaleXSpinBox, _animationScaleYSpinBox };
		foreach (SpinBox spinBox in array)
		{
			if (GodotObject.IsInstanceValid(spinBox))
			{
				spinBox.Editable = enabled;
			}
		}
		if (GodotObject.IsInstanceValid(_animationAlphaSlider))
		{
			_animationAlphaSlider.Editable = enabled;
		}
		if (GodotObject.IsInstanceValid(_animationDeleteKeyframeButton))
		{
			_animationDeleteKeyframeButton.Disabled = !enabled;
		}
		if (GodotObject.IsInstanceValid(_animationDuplicateKeyframeButton))
		{
			_animationDuplicateKeyframeButton.Disabled = !enabled;
		}
		if (GodotObject.IsInstanceValid(_animationMoveKeyframeEarlierButton))
		{
			_animationMoveKeyframeEarlierButton.Disabled = !enabled;
		}
		if (GodotObject.IsInstanceValid(_animationMoveKeyframeLaterButton))
		{
			_animationMoveKeyframeLaterButton.Disabled = !enabled;
		}
		if (!enabled && GodotObject.IsInstanceValid(_animationSelectedKeyframeLabel))
		{
			_animationSelectedKeyframeLabel.Text = "在时间轴中选择一个菱形帧状态";
		}
	}

	private void BeginAnimationFramePropertyEdit()
	{
		if (!_updatingAnimationFrameControls && _animationFrameEditSnapshot == null && base.CurrentResource is AdobeAnimateData adobeAnimateData && GodotObject.IsInstanceValid(_animationFrameTimeline) && _animationFrameTimeline.SelectedFrame >= 0 && _animationFrameTimeline.SelectedSliceKey >= 0)
		{
			_animationFrameEditSnapshot = adobeAnimateData.CaptureFrameEditSnapshot();
			_animationFrameEditTarget = adobeAnimateData;
			_animationFrameEditFrame = _animationFrameTimeline.SelectedFrame;
			_animationFrameEditSliceKey = _animationFrameTimeline.SelectedSliceKey;
			_animationFrameEditMutated = false;
		}
	}

	private void PreviewSelectedAnimationFrameSlice(double ignoredValue = 0.0)
	{
		if (_updatingAnimationFrameControls || !(base.CurrentResource is AdobeAnimateData adobeAnimateData) || !GodotObject.IsInstanceValid(_animationFrameTimeline) || _animationFrameTimeline.SelectedFrame < 0 || _animationFrameTimeline.SelectedSliceKey < 0)
		{
			return;
		}
		BeginAnimationFramePropertyEdit();
		int selectedFrame = _animationFrameTimeline.SelectedFrame;
		int selectedSliceKey = _animationFrameTimeline.SelectedSliceKey;
		if (!adobeAnimateData.TryGetFrameSlice(selectedFrame, selectedSliceKey, out var slice, out var error))
		{
			SetAnimationFrameStatus(error, error: true);
			return;
		}
		int selectedOptionMetadata = GetSelectedOptionMetadata(_animationMediaOption, slice.MediaId);
		int selectedOptionMetadata2 = GetSelectedOptionMetadata(_animationLayerOption, slice.LayerId);
		Transform2D transform = new Transform2D(Mathf.DegToRad((float)_animationRotationSpinBox.Value), new Vector2((float)_animationScaleXSpinBox.Value, (float)_animationScaleYSpinBox.Value), Mathf.DegToRad((float)_animationSkewSpinBox.Value), new Vector2((float)_animationPositionXSpinBox.Value, (float)_animationPositionYSpinBox.Value));
		float num = Math.Clamp((float)_animationAlphaSlider.Value, 0f, 1f);
		if (!adobeAnimateData.TryUpdateFrameSlice(selectedFrame, selectedSliceKey, selectedOptionMetadata, selectedOptionMetadata2, transform, num, out var error2))
		{
			SetAnimationFrameStatus(error2, error: true);
			return;
		}
		_animationFrameEditMutated = true;
		_animationAlphaPercentLabel.Text = $"{Mathf.RoundToInt(num * 100f)}%";
		UpdateAnimationMediaThumbnail(adobeAnimateData, selectedOptionMetadata);
		_animationFrameTimeline.RefreshFromAnimation();
		_animationFrameTimeline.SelectKeyframe(selectedFrame, selectedOptionMetadata2, selectedSliceKey, notify: false);
		RefreshAnimationRuntimeAtFrame(selectedFrame);
		SetAnimationFrameStatus($"正在预览帧 {selectedFrame} 的修改；离开控件后写入全局撤销历史。");
	}

	private void CommitAnimationFrameAlphaEdit(bool valueChanged)
	{
		CommitAnimationFramePropertyEdit();
	}

	private void CommitAnimationFramePropertyEdit()
	{
		AdobeAnimateData animationFrameEditTarget = _animationFrameEditTarget;
		if (_animationFrameEditSnapshot == null || !GodotObject.IsInstanceValid(animationFrameEditTarget))
		{
			CancelAnimationFramePropertyEdit();
			return;
		}
		Dictionary animationFrameEditSnapshot = _animationFrameEditSnapshot;
		int animationFrameEditFrame = _animationFrameEditFrame;
		int animationFrameEditSliceKey = _animationFrameEditSliceKey;
		bool animationFrameEditMutated = _animationFrameEditMutated;
		Dictionary nextSnapshot = (animationFrameEditMutated ? animationFrameEditTarget.CaptureFrameEditSnapshot() : null);
		_animationFrameEditSnapshot = null;
		_animationFrameEditTarget = null;
		_animationFrameEditFrame = -1;
		_animationFrameEditSliceKey = -1;
		_animationFrameEditMutated = false;
		if (animationFrameEditMutated)
		{
			if (!animationFrameEditTarget.TryRestoreFrameEditSnapshot(animationFrameEditSnapshot, out var error))
			{
				SetAnimationFrameStatus("无法建立撤销历史：" + error, error: true);
			}
			else
			{
				CommitAnimationFrameSnapshotChange(animationFrameEditTarget, animationFrameEditSnapshot, nextSnapshot, animationFrameEditFrame, animationFrameEditSliceKey, animationFrameEditFrame, animationFrameEditSliceKey, "编辑动画帧状态");
			}
		}
	}

	private void CancelAnimationFramePropertyEdit()
	{
		if (_animationFrameEditSnapshot != null && GodotObject.IsInstanceValid(_animationFrameEditTarget))
		{
			_animationFrameEditTarget.TryRestoreFrameEditSnapshot(_animationFrameEditSnapshot, out var _);
		}
		_animationFrameEditSnapshot = null;
		_animationFrameEditTarget = null;
		_animationFrameEditFrame = -1;
		_animationFrameEditSliceKey = -1;
		_animationFrameEditMutated = false;
	}

	private void OnAnimationFrameMediaSelected(long index)
	{
		if (!_updatingAnimationFrameControls)
		{
			BeginAnimationFramePropertyEdit();
			PreviewSelectedAnimationFrameSlice();
			CommitAnimationFramePropertyEdit();
		}
	}

	private void OnAnimationFrameLayerSelected(long index)
	{
		if (!_updatingAnimationFrameControls)
		{
			BeginAnimationFramePropertyEdit();
			PreviewSelectedAnimationFrameSlice();
			CommitAnimationFramePropertyEdit();
		}
	}

	private void OnAnimationVisualMediaSelected(int mediaId)
	{
		if (!_updatingAnimationFrameControls && GodotObject.IsInstanceValid(_animationMediaOption))
		{
			SelectOptionByMetadata(_animationMediaOption, mediaId);
			OnAnimationFrameMediaSelected(_animationMediaOption.Selected);
		}
	}

	private void OnAnimationVisualLayerSelected(int layerId)
	{
		if (!_updatingAnimationFrameControls && GodotObject.IsInstanceValid(_animationLayerOption))
		{
			SelectOptionByMetadata(_animationLayerOption, layerId);
			OnAnimationFrameLayerSelected(_animationLayerOption.Selected);
		}
	}

	private void AddAnimationKeyframeAtPlayhead()
	{
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData))
		{
			return;
		}
		int targetFrame = Math.Clamp(GodotObject.IsInstanceValid(_runtimePreview) ? _runtimePreview.CurrentFrame : 0, 0, Math.Max(0, adobeAnimateData.EditableFrameCount - 1));
		int preferredSliceKey = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedSliceKey : (-1));
		if (!TryFindNearestAnimationFrameSlice(adobeAnimateData, targetFrame, preferredSliceKey, out var source))
		{
			SetAnimationFrameStatus("附近没有可复制的 Slice 状态。", error: true);
			return;
		}
		CommitAnimationFrameMutation(adobeAnimateData, "添加动画帧状态", targetFrame, source.SliceKey, (AdobeAnimateData resource) => (!resource.TryAddFrameSlice(targetFrame, source, out var error)) ? (Success: false, Error: error) : (Success: true, Error: ""));
	}

	private void DuplicateSelectedAnimationKeyframe()
	{
		if (!TryGetSelectedAnimationFrameSlice(out var animation, out var frame, out var slice))
		{
			return;
		}
		int targetFrame = Math.Min(animation.EditableFrameCount - 1, frame + 1);
		if (targetFrame == frame)
		{
			SetAnimationFrameStatus("已经是最后一帧。", error: true);
			return;
		}
		CommitAnimationFrameMutation(animation, "复制动画帧状态", targetFrame, slice.SliceKey, (AdobeAnimateData resource) => (!resource.TryCopyFrameSlice(frame, targetFrame, slice.SliceKey, out var error)) ? (Success: false, Error: error) : (Success: true, Error: ""));
	}

	private void DeleteSelectedAnimationKeyframe()
	{
		if (TryGetSelectedAnimationFrameSlice(out var animation, out var frame, out var slice))
		{
			CommitAnimationFrameMutation(animation, "删除动画帧状态", frame, -1, (AdobeAnimateData resource) => (!resource.TryRemoveFrameSlice(frame, slice.SliceKey, out var error)) ? (Success: false, Error: error) : (Success: true, Error: ""));
		}
	}

	private void MoveSelectedAnimationKeyframeEarlier()
	{
		MoveSelectedAnimationKeyframeBy(-1);
	}

	private void MoveSelectedAnimationKeyframeLater()
	{
		MoveSelectedAnimationKeyframeBy(1);
	}

	private void MoveSelectedAnimationKeyframeBy(int direction)
	{
		if (TryGetSelectedAnimationFrameSlice(out var animation, out var frame, out var slice))
		{
			int num = Math.Clamp(frame + Math.Sign(direction), 0, Math.Max(0, animation.EditableFrameCount - 1));
			if (num != frame)
			{
				MoveAnimationKeyframe(animation, frame, num, slice);
			}
		}
	}

	private void OnAnimationKeyframeMoveRequested(int sourceFrame, int destinationFrame, int layer, int sliceKey)
	{
		string error = "";
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData) || !adobeAnimateData.TryGetFrameSlice(sourceFrame, sliceKey, out var slice, out error))
		{
			SetAnimationFrameStatus(error, error: true);
		}
		else
		{
			MoveAnimationKeyframe(adobeAnimateData, sourceFrame, destinationFrame, slice);
		}
	}

	private void MoveAnimationKeyframe(AdobeAnimateData animation, int sourceFrame, int destinationFrame, AdobeAnimateData.FrameSlice slice)
	{
		CommitAnimationFrameMutation(animation, "移动动画帧状态", destinationFrame, slice.SliceKey, (AdobeAnimateData resource) => (!resource.TryMoveFrameSlice(sourceFrame, destinationFrame, slice.SliceKey, out var error)) ? (Success: false, Error: error) : (Success: true, Error: ""));
	}

	private void CommitAnimationFrameMutation(AdobeAnimateData animation, string actionName, int doFrame, int doSliceKey, Func<AdobeAnimateData, (bool Success, string Error)> mutation)
	{
		CommitAnimationFramePropertyEdit();
		if (!GodotObject.IsInstanceValid(animation) || mutation == null)
		{
			return;
		}
		Dictionary dictionary = animation.CaptureFrameEditSnapshot();
		var (flag, text) = mutation(animation);
		if (!flag)
		{
			SetAnimationFrameStatus(string.IsNullOrWhiteSpace(text) ? "无法修改选中帧状态。" : text, error: true);
			return;
		}
		Dictionary nextSnapshot = animation.CaptureFrameEditSnapshot();
		if (!animation.TryRestoreFrameEditSnapshot(dictionary, out var error))
		{
			SetAnimationFrameStatus("无法建立撤销历史：" + error, error: true);
			return;
		}
		int undoFrame = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedFrame : doFrame);
		int undoSliceKey = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedSliceKey : doSliceKey);
		CommitAnimationFrameSnapshotChange(animation, dictionary, nextSnapshot, doFrame, doSliceKey, undoFrame, undoSliceKey, actionName);
	}

	private void CommitAnimationFrameSnapshotChange(AdobeAnimateData animation, Dictionary oldSnapshot, Dictionary nextSnapshot, int doFrame, int doSliceKey, int undoFrame, int undoSliceKey, string actionName)
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null)
		{
			animation.ApplyFrameEditSnapshot(nextSnapshot);
			RefreshAnimationFrameEditorFromHistory(animation, doFrame, doSliceKey);
			return;
		}
		xWUndoRedoManager.CreateAction(actionName);
		xWUndoRedoManager.AddDoMethod(animation, "ApplyFrameEditSnapshot", Variant.From(in nextSnapshot));
		xWUndoRedoManager.AddDoMethod(this, "RefreshAnimationFrameEditorFromHistory", Variant.From(in animation), doFrame, doSliceKey);
		xWUndoRedoManager.AddUndoMethod(this, "RefreshAnimationFrameEditorFromHistory", Variant.From(in animation), undoFrame, undoSliceKey);
		xWUndoRedoManager.AddUndoMethod(animation, "ApplyFrameEditSnapshot", Variant.From(in oldSnapshot));
		xWUndoRedoManager.CommitAction();
	}

	public void RefreshAnimationFrameEditorFromHistory(AdobeAnimateData animation, int frame, int sliceKey)
	{
		if (GodotObject.IsInstanceValid(animation) && base.CurrentResource == animation)
		{
			_animationFrameMutationCount++;
			SyncAnimationFrameScaleControl(animation);
			PopulateAnimationFrameOptions(animation);
			_animationFrameTimeline.RefreshFromAnimation();
			bool flag = frame >= 0 && sliceKey >= 0 && _animationFrameTimeline.SelectSliceKey(frame, sliceKey, notify: false);
			if (!flag)
			{
				flag = TrySelectAnimationSliceByKey(frame, sliceKey);
			}
			if (!flag)
			{
				flag = _animationFrameTimeline.SelectFirstOccupiedCell(notify: false);
			}
			if (flag)
			{
				RefreshSelectedAnimationFrameControls(_animationFrameTimeline.SelectedFrame, _animationFrameTimeline.SelectedSliceKey);
				RefreshAnimationRuntimeAtFrame(_animationFrameTimeline.SelectedFrame);
			}
			else
			{
				SetAnimationFrameControlsEnabled(enabled: false);
			}
			UpdateAnimationDirectSummary();
			NotifyCurrentResourceEdited();
		}
	}

	private bool TrySelectAnimationSliceByKey(int preferredFrame, int sliceKey)
	{
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData) || sliceKey < 0)
		{
			return false;
		}
		for (int i = 0; i < adobeAnimateData.EditableFrameCount; i++)
		{
			int num = preferredFrame - i;
			if (num >= 0 && adobeAnimateData.TryGetFrameSlice(num, sliceKey, out var slice, out var error) && _animationFrameTimeline.SelectKeyframe(num, slice.LayerId, sliceKey, notify: false))
			{
				return true;
			}
			int num2 = preferredFrame + i;
			if (i > 0 && num2 < adobeAnimateData.EditableFrameCount && adobeAnimateData.TryGetFrameSlice(num2, sliceKey, out var slice2, out error) && _animationFrameTimeline.SelectKeyframe(num2, slice2.LayerId, sliceKey, notify: false))
			{
				return true;
			}
		}
		return false;
	}

	private bool TryGetSelectedAnimationFrameSlice(out AdobeAnimateData animation, out int frame, out AdobeAnimateData.FrameSlice slice)
	{
		animation = base.CurrentResource as AdobeAnimateData;
		frame = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedFrame : (-1));
		int num = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedSliceKey : (-1));
		slice = default(AdobeAnimateData.FrameSlice);
		string error = "";
		if (!GodotObject.IsInstanceValid(animation) || frame < 0 || num < 0 || !animation.TryGetFrameSlice(frame, num, out slice, out error))
		{
			SetAnimationFrameStatus(string.IsNullOrWhiteSpace(error) ? "请先选择一个菱形帧状态。" : error, error: true);
			return false;
		}
		return true;
	}

	private static bool TryFindNearestAnimationFrameSlice(AdobeAnimateData animation, int targetFrame, int preferredSliceKey, out AdobeAnimateData.FrameSlice slice)
	{
		slice = default(AdobeAnimateData.FrameSlice);
		if (!GodotObject.IsInstanceValid(animation) || animation.EditableFrameCount <= 0)
		{
			return false;
		}
		for (int i = 1; i < animation.EditableFrameCount; i++)
		{
			int[] array = new int[2]
			{
				targetFrame - i,
				targetFrame + i
			};
			foreach (int num in array)
			{
				if (num >= 0 && num < animation.EditableFrameCount)
				{
					if (preferredSliceKey >= 0 && animation.TryGetFrameSlice(num, preferredSliceKey, out slice, out var error))
					{
						return true;
					}
					if (preferredSliceKey < 0 && animation.TryGetFrameSlices(num, out var slices, out error) && slices.Length != 0)
					{
						slice = slices[0];
						return true;
					}
				}
			}
		}
		return false;
	}

	private void RefreshAnimationRuntimeAtFrame(int frame)
	{
		if (GodotObject.IsInstanceValid(_runtimePreview))
		{
			_runtimePreview.SetFrameFromSlider(frame);
		}
	}

	private void UpdateAnimationMediaThumbnail(AdobeAnimateData animation, int mediaId)
	{
		if (GodotObject.IsInstanceValid(_animationMediaThumbnail))
		{
			_animationMediaThumbnail.Texture = CreateAnimationMediaThumbnail(animation, mediaId);
		}
	}

	private static Texture2D CreateAnimationMediaThumbnail(AdobeAnimateData animation, int mediaId)
	{
		if (!TryGetAnimationDefinition(animation, out var definition) || !GodotObject.IsInstanceValid(definition.BaseAtlas) || definition.MediaRects == null || mediaId < 0 || mediaId >= definition.MediaRects.Length)
		{
			return null;
		}
		Rect2 region = definition.MediaRects[mediaId];
		if (region.Size.X <= 0f || region.Size.Y <= 0f)
		{
			return null;
		}
		return new AtlasTexture
		{
			Atlas = definition.BaseAtlas,
			Region = region
		};
	}

	private void SetAnimationFrameStatus(string message, bool error = false)
	{
		if (GodotObject.IsInstanceValid(_animationKeyframeEditStatusLabel))
		{
			_animationKeyframeEditStatusLabel.Text = message ?? "";
			_animationKeyframeEditStatusLabel.Modulate = (error ? new Color(1f, 0.42f, 0.34f) : new Color(0.68f, 0.88f, 0.52f));
		}
	}

	private static int GetSelectedOptionMetadata(OptionButton option, int fallback)
	{
		if (!GodotObject.IsInstanceValid(option) || option.Selected < 0 || option.Selected >= option.ItemCount)
		{
			return fallback;
		}
		return option.GetItemMetadata(option.Selected).AsInt32();
	}

	private static void SelectOptionByMetadata(OptionButton option, int requestedId)
	{
		if (!GodotObject.IsInstanceValid(option))
		{
			return;
		}
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemMetadata(i).AsInt32() == requestedId)
			{
				option.Select(i);
				break;
			}
		}
	}

	private static string GetAnimationNameById(Dictionary dictionary, int requestedId, string fallback)
	{
		if (dictionary == null)
		{
			return fallback;
		}
		foreach (Variant key in dictionary.Keys)
		{
			if (dictionary[key].AsInt32() == requestedId)
			{
				return key.AsString();
			}
		}
		return fallback;
	}

	private void BindAnimationDirectEditSurface(VBoxContainer container, AdobeAnimateData animation)
	{
		LineEdit node = container.GetNode<LineEdit>("%ResourceNameEdit");
		SpinBox node2 = container.GetNode<SpinBox>("%FrameRateSpinBox");
		_animationFrameScaleSpinBox = container.GetNode<SpinBox>("%FrameScaleSpinBox");
		SpinBox node3 = container.GetNode<SpinBox>("%FrameMaxSpinBox");
		CheckButton node4 = container.GetNode<CheckButton>("%LocalToSceneCheck");
		Button node5 = container.GetNode<Button>("%ReloadButton");
		_animationResourcePathLabel = container.GetNode<Label>("%ResourcePathLabel");
		_animationDatFileButton = container.GetNode<Button>("%DatFileButton");
		_animationDirectSummaryLabel = container.GetNode<Label>("%DirectSummaryLabel");
		_animationDatFileDialog = container.GetNode<FileDialog>("%DatFileDialog");
		_animationReimportConfirmDialog = container.GetNode<ConfirmationDialog>("%ReimportConfirmDialog");
		_animationPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnAnimationVisualPropertyEdited);
		_animationPropertyBinding.BindText(node, animation, "resource_name", UpdateAnimationDirectSummary);
		_animationPropertyBinding.BindNumber(node2, animation, "frameRate", RefreshRuntimeAnimationPreview);
		BindAnimationFrameScaleControl(animation);
		_animationPropertyBinding.BindNumber(node3, animation, "frameMax", RefreshRuntimeAnimationPreview);
		_animationPropertyBinding.BindToggle(node4, animation, "resource_local_to_scene", UpdateAnimationDirectSummary);
		_animationDatFileButton.Pressed += delegate
		{
			OpenAnimationDatFileDialog(animation);
		};
		node5.Pressed += ReloadAnimationFromDirectSettings;
		_animationDatFileDialog.FileSelected += SelectAnimationDatFile;
		_animationReimportConfirmDialog.Confirmed += ConfirmExplicitAnimationReimport;
		_animationReimportConfirmDialog.Canceled += CancelPendingAnimationReimportConfirmation;
		BindAnimationReplacementSurface(container, animation);
		UpdateAnimationDirectSummary();
	}

	private void BindAnimationFrameScaleControl(AdobeAnimateData animation)
	{
		if (GodotObject.IsInstanceValid(_animationFrameScaleSpinBox) && GodotObject.IsInstanceValid(animation))
		{
			_updatingAnimationFrameScaleControl = true;
			_animationFrameScaleSpinBox.SetValueNoSignal(Math.Max(1, animation.frameScale));
			_updatingAnimationFrameScaleControl = false;
			_animationFrameScaleSpinBox.FocusEntered += BeginAnimationFrameScaleEdit;
			_animationFrameScaleSpinBox.ValueChanged += PreviewAnimationFrameScaleEdit;
			_animationFrameScaleSpinBox.FocusExited += CommitAnimationFrameScaleEdit;
		}
	}

	private void UnbindAnimationFrameScaleControl()
	{
		if (GodotObject.IsInstanceValid(_animationFrameScaleSpinBox))
		{
			_animationFrameScaleSpinBox.FocusEntered -= BeginAnimationFrameScaleEdit;
			_animationFrameScaleSpinBox.ValueChanged -= PreviewAnimationFrameScaleEdit;
			_animationFrameScaleSpinBox.FocusExited -= CommitAnimationFrameScaleEdit;
		}
		_animationFrameScaleSpinBox = null;
		_updatingAnimationFrameScaleControl = false;
	}

	private void BeginAnimationFrameScaleEdit()
	{
		if (!_updatingAnimationFrameScaleControl && base.CurrentResource is AdobeAnimateData adobeAnimateData)
		{
			SetAnimationFrameStatus($"当前帧缩放为 {Math.Max(1, adobeAnimateData.frameScale)}；修改后将在后台重建 DAT。");
		}
	}

	private void PreviewAnimationFrameScaleEdit(double value)
	{
		if (!_updatingAnimationFrameScaleControl && base.CurrentResource is AdobeAnimateData adobeAnimateData)
		{
			int num = Math.Max(1, (int)Math.Round(value));
			if (GodotObject.IsInstanceValid(_animationDirectSummaryLabel))
			{
				_animationDirectSummaryLabel.Text = ((num == adobeAnimateData.frameScale) ? $"{GetAnimationFrameCount(adobeAnimateData)} 帧 · {adobeAnimateData.layerDictionary?.Count ?? 0} 图层 · {adobeAnimateData.clips?.Count ?? 0} Clip · {adobeAnimateData.frameRate:0.##} FPS" : $"准备应用 {num}x 帧缩放 · 松开/失焦后在后台重建");
			}
		}
	}

	private void CommitAnimationFrameScaleEdit()
	{
		if (_updatingAnimationFrameScaleControl || !GodotObject.IsInstanceValid(_animationFrameScaleSpinBox) || !(base.CurrentResource is AdobeAnimateData adobeAnimateData))
		{
			return;
		}
		int num = Math.Max(1, (int)Math.Round(_animationFrameScaleSpinBox.Value));
		_updatingAnimationFrameScaleControl = true;
		_animationFrameScaleSpinBox.SetValueNoSignal(num);
		_updatingAnimationFrameScaleControl = false;
		if (num == Math.Max(1, adobeAnimateData.frameScale))
		{
			UpdateAnimationDirectSummary();
			return;
		}
		string text = ResolveCompanionDatPath(adobeAnimateData);
		if (string.IsNullOrWhiteSpace(text) || !Godot.FileAccess.FileExists(text))
		{
			SyncAnimationFrameScaleControl(adobeAnimateData);
			SetAnimationFrameStatus("无法修改帧缩放：未找到可用于重建的 DAT。", error: true);
		}
		else if (adobeAnimateData.HasAuthoredPackedFrames)
		{
			RequestAnimationFrameScaleConfirmation(adobeAnimateData, num);
		}
		else
		{
			StartAnimationFrameScaleHydration(adobeAnimateData, num);
		}
	}

	private void RequestAnimationFrameScaleConfirmation(AdobeAnimateData animation, int requestedScale)
	{
		if (!GodotObject.IsInstanceValid(animation) || animation != base.CurrentResource || !GodotObject.IsInstanceValid(_animationReimportConfirmDialog))
		{
			SyncAnimationFrameScaleControl(animation);
			return;
		}
		_pendingAnimationReimportTarget = animation;
		_pendingAnimationReimportPath = animation.animeFile ?? "";
		_pendingAnimationFrameScale = requestedScale;
		_animationReimportConfirmDialog.DialogText = $"当前动画含有手工帧编辑。\n\n帧缩放：{animation.frameScale}x → {requestedScale}x\n\n" + "继续会在后台从 DAT 重建帧、Clip 与事件；现有手工帧会先保存为完整快照，可由全局 Undo/Redo 无损恢复。";
		_animationReimportConfirmDialog.PopupCentered(new Vector2I(620, 280));
		SetAnimationFrameStatus("等待确认帧缩放；当前手工帧尚未改变。");
	}

	private void StartAnimationFrameScaleHydration(AdobeAnimateData animation, int requestedScale)
	{
		if (GodotObject.IsInstanceValid(animation) && animation == base.CurrentResource)
		{
			CancelExplicitAnimationReimport(restoreOldSnapshot: true);
			CancelAnimationResourceHydration();
			_animationExplicitReimportOldSnapshot = animation.CaptureFullFrameEditSnapshot();
			_animationExplicitReimportFrame = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedFrame : 0);
			_animationExplicitReimportSliceKey = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedSliceKey : (-1));
			_animationExplicitReimportActionName = "修改动画帧缩放";
			_animationExplicitReimportSuccessMessage = $"帧缩放已在后台重建为 {requestedScale}x，可使用全局 Undo 恢复。";
			_animationExplicitReimportRequiresRestore = false;
			StartAnimationResourceHydration(restart: true, requestedScale);
			SetAnimationFrameStatus($"正在后台重建 {requestedScale}x 帧缩放；主界面可继续操作。");
		}
	}

	private void SyncAnimationFrameScaleControl(AdobeAnimateData animation)
	{
		if (GodotObject.IsInstanceValid(animation) && GodotObject.IsInstanceValid(_animationFrameScaleSpinBox))
		{
			_updatingAnimationFrameScaleControl = true;
			_animationFrameScaleSpinBox.SetValueNoSignal(Math.Max(1, animation.frameScale));
			_updatingAnimationFrameScaleControl = false;
			UpdateAnimationDirectSummary();
		}
	}

	private void BindAnimationReplacementSurface(VBoxContainer container, AdobeAnimateData animation)
	{
		_animationReplacementSlotCards = container.GetNode<HFlowContainer>("%ReplacementSlotCards");
		_animationExtraTextureCards = container.GetNode<HFlowContainer>("%ExtraTextureCards");
		_animationReplacementSummaryLabel = container.GetNode<Label>("%ReplacementSummaryLabel");
		_animationReplacementStatusLabel = container.GetNode<Label>("%ReplacementStatusLabel");
		_animationAddReplacementSlotButton = container.GetNode<Button>("%AddReplacementSlotButton");
		_animationAddExtraTextureButton = container.GetNode<Button>("%AddExtraTextureButton");
		_animationReplacementSaveButton = container.GetNode<Button>("%ReplacementSaveButton");
		_animationReplacementReloadButton = container.GetNode<Button>("%ReplacementReloadButton");
		_animationReplacementReloadConfirmDialog = container.GetNode<ConfirmationDialog>("%ReplacementReloadConfirmDialog");
		_animationReplacementMutationCount = 0;
		_animationAddReplacementSlotButton.Pressed += OpenAnimationReplacementSlotPicker;
		_animationAddExtraTextureButton.Pressed += OpenAnimationExtraTexturePicker;
		_animationReplacementSaveButton.Pressed += base.SaveCurrentResource;
		_animationReplacementReloadButton.Pressed += RequestAnimationReplacementReload;
		_animationReplacementReloadConfirmDialog.Confirmed += ReloadAnimationReplacementConfiguration;
		RebuildAnimationReplacementWorkbench(animation);
	}

	private void DisposeAnimationReplacementSurface()
	{
		if (GodotObject.IsInstanceValid(_animationAddReplacementSlotButton))
		{
			_animationAddReplacementSlotButton.Pressed -= OpenAnimationReplacementSlotPicker;
		}
		if (GodotObject.IsInstanceValid(_animationAddExtraTextureButton))
		{
			_animationAddExtraTextureButton.Pressed -= OpenAnimationExtraTexturePicker;
		}
		if (GodotObject.IsInstanceValid(_animationReplacementSaveButton))
		{
			_animationReplacementSaveButton.Pressed -= base.SaveCurrentResource;
		}
		if (GodotObject.IsInstanceValid(_animationReplacementReloadButton))
		{
			_animationReplacementReloadButton.Pressed -= RequestAnimationReplacementReload;
		}
		if (GodotObject.IsInstanceValid(_animationReplacementReloadConfirmDialog))
		{
			_animationReplacementReloadConfirmDialog.Confirmed -= ReloadAnimationReplacementConfiguration;
		}
		if (GodotObject.IsInstanceValid(_animationReplacementPicker))
		{
			_animationReplacementPicker.Dismiss();
			_animationReplacementPicker.QueueFree();
		}
		_animationReplacementSlotCards = null;
		_animationExtraTextureCards = null;
		_animationReplacementSummaryLabel = null;
		_animationReplacementStatusLabel = null;
		_animationAddReplacementSlotButton = null;
		_animationAddExtraTextureButton = null;
		_animationReplacementSaveButton = null;
		_animationReplacementReloadButton = null;
		_animationReplacementReloadConfirmDialog = null;
		_animationReplacementPicker = null;
		_animationReplacementSlotCardCount = 0;
		_animationExtraTextureCardCount = 0;
	}

	private void EnsureAnimationReplacementPicker()
	{
		if (!GodotObject.IsInstanceValid(_animationReplacementPicker))
		{
			_animationReplacementPicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_animationReplacementPicker))
			{
				AddChild(_animationReplacementPicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void OpenAnimationReplacementSlotPicker()
	{
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData))
		{
			return;
		}
		EnsureAnimationReplacementPicker();
		if (!GodotObject.IsInstanceValid(_animationReplacementPicker))
		{
			return;
		}
		List<XWGameplayResourceChoice> list = new List<XWGameplayResourceChoice>();
		if (adobeAnimateData.mediaDictionary != null)
		{
			List<(int, string)> list2 = new List<(int, string)>();
			foreach (Variant key in adobeAnimateData.mediaDictionary.Keys)
			{
				string text = key.AsString();
				if (!ContainsAnimationReplacementSlot(adobeAnimateData.replaceSlotDictionary, text))
				{
					list2.Add((adobeAnimateData.mediaDictionary[key].AsInt32(), text));
				}
			}
			list2.Sort(((int Id, string Name) left, (int Id, string Name) right) => left.Id.CompareTo(right.Id));
			foreach (var (num, text2) in list2)
			{
				list.Add(new XWGameplayResourceChoice(text2, text2, $"media://{num}", CreateAnimationMediaThumbnail(adobeAnimateData, num), XWGameplayResourceKind.Resource, IsModResource: false));
			}
		}
		if (list.Count == 0)
		{
			SetAnimationReplacementStatus("所有可用动画媒体都已建立替换槽位。", error: false);
			return;
		}
		_animationReplacementPicker.OpenChoices("动画媒体缩略图库", "按原始媒体画面选择需要开放给 Mod 的替换槽位", "", list, delegate(XWGameplayResourceChoice choice)
		{
			AddAnimationReplacementSlot(choice.Key);
		}, "创建替换槽位");
	}

	private void OpenAnimationExtraTexturePicker()
	{
		OpenAnimationExtraTexturePicker(-1);
	}

	private void OpenAnimationExtraTexturePicker(int replacementIndex)
	{
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData))
		{
			return;
		}
		EnsureAnimationReplacementPicker();
		if (GodotObject.IsInstanceValid(_animationReplacementPicker))
		{
			string currentPath = ((replacementIndex >= 0 && replacementIndex < (adobeAnimateData.extraMediaReplaceTexturePaths?.Count ?? 0)) ? adobeAnimateData.extraMediaReplaceTexturePaths[replacementIndex] : "");
			string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			if (string.IsNullOrWhiteSpace(text) && XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
			{
				text = modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
			}
			_animationReplacementPicker.OpenResourceLibrary("Texture", "动画替换纹理", currentPath, text, new string[1] { "Texture2D" }, new string[2] { "Asset/Anime", "Asset/Texture" }, "res://addons/ModEditor/Icons/ResourceAnimation.svg", delegate(XWGameplayResourceChoice choice)
			{
				SelectAnimationExtraTexture(choice.ResourcePath, replacementIndex);
			});
		}
	}

	private void SelectAnimationExtraTexture(string path, int replacementIndex)
	{
		if (!GodotObject.IsInstanceValid(LoadAnimationReplacementTexture(path)))
		{
			SetAnimationReplacementStatus("无法加载纹理：" + path, error: true);
		}
		else if (replacementIndex >= 0)
		{
			ReplaceAnimationExtraTexturePath(replacementIndex, path);
		}
		else
		{
			AddAnimationExtraTexturePath(path);
		}
	}

	private static Texture2D LoadAnimationReplacementTexture(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		if (ResourceLoader.Exists(path))
		{
			try
			{
				Texture2D texture2D = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
				if (GodotObject.IsInstanceValid(texture2D))
				{
					return texture2D;
				}
			}
			catch (Exception ex)
			{
				GD.PushWarning($"Animation replacement texture load failed: {path} ({ex.Message})");
			}
		}
		string text = path.GetExtension().ToLowerInvariant();
		if (text != null)
		{
			int length = text.Length;
			if (length != 3)
			{
				if (length == 4)
				{
					char c = text[0];
					if (c != 'j')
					{
						if (c == 'w' && text == "webp")
						{
							goto IL_0166;
						}
					}
					else if (text == "jpeg")
					{
						goto IL_0166;
					}
				}
			}
			else
			{
				switch (text[0])
				{
				case 'p':
					break;
				case 'j':
					goto IL_010e;
				case 's':
					goto IL_011d;
				case 'b':
					goto IL_012c;
				case 't':
					goto IL_013b;
				default:
					goto IL_016b;
				}
				if (text == "png")
				{
					goto IL_0166;
				}
			}
		}
		goto IL_016b;
		IL_016e:
		bool flag;
		if (!flag)
		{
			return null;
		}
		string path2 = ((path.StartsWith("res://") || path.StartsWith("user://")) ? ProjectSettings.GlobalizePath(path) : path.Replace('\\', '/'));
		if (!Godot.FileAccess.FileExists(path2))
		{
			return null;
		}
		Image image = new Image();
		if (((text == "svg") ? image.LoadSvgFromBuffer(Godot.FileAccess.GetFileAsBytes(path2)) : image.Load(path2)) != Error.Ok || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return null;
		}
		ImageTexture imageTexture = ImageTexture.CreateFromImage(image);
		imageTexture.ResourcePath = path;
		return imageTexture;
		IL_013b:
		if (text == "tga")
		{
			goto IL_0166;
		}
		goto IL_016b;
		IL_010e:
		if (text == "jpg")
		{
			goto IL_0166;
		}
		goto IL_016b;
		IL_011d:
		if (text == "svg")
		{
			goto IL_0166;
		}
		goto IL_016b;
		IL_016b:
		flag = false;
		goto IL_016e;
		IL_0166:
		flag = true;
		goto IL_016e;
		IL_012c:
		if (text == "bmp")
		{
			goto IL_0166;
		}
		goto IL_016b;
	}

	public bool AddAnimationReplacementSlot(string mediaName)
	{
		if (_animationPropertyBinding == null || !(base.CurrentResource is AdobeAnimateData adobeAnimateData) || string.IsNullOrWhiteSpace(mediaName) || adobeAnimateData.mediaDictionary == null || !TryGetAnimationDictionaryValue(adobeAnimateData.mediaDictionary, mediaName, out var value) || ContainsAnimationReplacementSlot(adobeAnimateData.replaceSlotDictionary, mediaName))
		{
			return false;
		}
		Dictionary from = adobeAnimateData.replaceSlotDictionary?.Duplicate(deep: true) ?? new Dictionary();
		from[mediaName] = value;
		_animationPropertyBinding.SetValue(adobeAnimateData, "replaceSlotDictionary", Variant.From(in from), "添加动画媒体替换槽位", this, "RefreshAnimationReplacementWorkbenchFromHistory");
		SetAnimationReplacementStatus("已开放媒体槽位：" + mediaName, error: false);
		return true;
	}

	public bool RemoveAnimationReplacementSlot(string mediaName)
	{
		if (_animationPropertyBinding == null || !(base.CurrentResource is AdobeAnimateData adobeAnimateData) || !TryFindAnimationDictionaryKey(adobeAnimateData.replaceSlotDictionary, mediaName, out var matchedKey))
		{
			return false;
		}
		Dictionary from = adobeAnimateData.replaceSlotDictionary.Duplicate(deep: true);
		from.Remove(matchedKey);
		_animationPropertyBinding.SetValue(adobeAnimateData, "replaceSlotDictionary", Variant.From(in from), "删除动画媒体替换槽位", this, "RefreshAnimationReplacementWorkbenchFromHistory");
		SetAnimationReplacementStatus("已移除媒体槽位：" + mediaName, error: false);
		return true;
	}

	public bool AddAnimationExtraTexture(Texture2D texture)
	{
		if (GodotObject.IsInstanceValid(texture))
		{
			return AddAnimationExtraTexturePath(texture.ResourcePath);
		}
		return false;
	}

	public bool ReplaceAnimationExtraTexture(int index, Texture2D texture)
	{
		if (GodotObject.IsInstanceValid(texture))
		{
			return ReplaceAnimationExtraTexturePath(index, texture.ResourcePath);
		}
		return false;
	}

	private bool AddAnimationExtraTexturePath(string texturePath)
	{
		if (_animationPropertyBinding == null || !(base.CurrentResource is AdobeAnimateData adobeAnimateData) || string.IsNullOrWhiteSpace(texturePath))
		{
			return false;
		}
		Array<string> array = DuplicateAnimationExtraTexturePaths(adobeAnimateData.extraMediaReplaceTexturePaths);
		array.Add(texturePath);
		CommitAnimationExtraTexturePaths(adobeAnimateData, array, "添加动画附加替换纹理");
		SetAnimationReplacementStatus("已加入替换纹理：" + texturePath.GetFile(), error: false);
		return true;
	}

	private bool ReplaceAnimationExtraTexturePath(int index, string texturePath)
	{
		if (_animationPropertyBinding == null || !(base.CurrentResource is AdobeAnimateData adobeAnimateData) || string.IsNullOrWhiteSpace(texturePath) || index < 0 || index >= (adobeAnimateData.extraMediaReplaceTexturePaths?.Count ?? 0))
		{
			return false;
		}
		Array<string> array = DuplicateAnimationExtraTexturePaths(adobeAnimateData.extraMediaReplaceTexturePaths);
		array[index] = texturePath;
		CommitAnimationExtraTexturePaths(adobeAnimateData, array, "更换动画附加替换纹理");
		SetAnimationReplacementStatus($"已更换第 {index + 1} 张替换纹理。", error: false);
		return true;
	}

	public bool RemoveAnimationExtraTexture(int index)
	{
		if (_animationPropertyBinding == null || !(base.CurrentResource is AdobeAnimateData adobeAnimateData) || index < 0 || index >= (adobeAnimateData.extraMediaReplaceTexturePaths?.Count ?? 0))
		{
			return false;
		}
		Array<string> array = DuplicateAnimationExtraTexturePaths(adobeAnimateData.extraMediaReplaceTexturePaths);
		array.RemoveAt(index);
		CommitAnimationExtraTexturePaths(adobeAnimateData, array, "删除动画附加替换纹理");
		SetAnimationReplacementStatus($"已移除第 {index + 1} 张替换纹理。", error: false);
		return true;
	}

	private void CommitAnimationExtraTexturePaths(AdobeAnimateData animation, Array<string> next, string actionName)
	{
		_animationPropertyBinding.SetValue(animation, "extraMediaReplaceTexturePaths", Variant.From(in next), actionName, this, "RefreshAnimationReplacementWorkbenchFromHistory");
	}

	private static Array<string> DuplicateAnimationExtraTexturePaths(Array<string> source)
	{
		if (source != null)
		{
			return source.Duplicate();
		}
		return new Array<string>();
	}

	public void RefreshAnimationReplacementWorkbenchFromHistory()
	{
		if (base.CurrentResource is AdobeAnimateData adobeAnimateData)
		{
			_animationReplacementMutationCount++;
			AdobeAnimateDefinitionCache.Invalidate(adobeAnimateData);
			adobeAnimateData.EmitChanged();
			RebuildAnimationReplacementWorkbench(adobeAnimateData);
			if (GodotObject.IsInstanceValid(_runtimePreview))
			{
				RefreshAnimationRuntimeAtFrame(_runtimePreview.CurrentFrame);
			}
		}
	}

	private void RebuildAnimationReplacementWorkbench(AdobeAnimateData animation)
	{
		if (!GodotObject.IsInstanceValid(animation) || !GodotObject.IsInstanceValid(_animationReplacementSlotCards) || !GodotObject.IsInstanceValid(_animationExtraTextureCards))
		{
			return;
		}
		ClearDynamicAnimationCards(_animationReplacementSlotCards);
		ClearDynamicAnimationCards(_animationExtraTextureCards);
		_animationReplacementSlotCardCount = animation.replaceSlotDictionary?.Count ?? 0;
		_animationExtraTextureCardCount = animation.extraMediaReplaceTexturePaths?.Count ?? 0;
		if (GodotObject.IsInstanceValid(_animationReplacementSummaryLabel))
		{
			_animationReplacementSummaryLabel.Text = $"{_animationReplacementSlotCardCount} 个媒体槽位 · {_animationExtraTextureCardCount} 张附加纹理 · {animation.mediaDictionary?.Count ?? 0} 个原始媒体";
		}
		if (_animationReplacementSlotCardCount == 0)
		{
			AddAnimationReplacementEmptyCard(_animationReplacementSlotCards, "＋", "选择原始媒体缩略图", "为图层开放可替换入口");
		}
		else
		{
			foreach (Variant key in animation.replaceSlotDictionary.Keys)
			{
				AddAnimationReplacementSlotCard(_animationReplacementSlotCards, animation, key.AsString());
			}
		}
		if (_animationExtraTextureCardCount == 0)
		{
			AddAnimationReplacementEmptyCard(_animationExtraTextureCards, "▧", "附加替换纹理库为空", "可选；支持内置资源与 Mod 图片");
			return;
		}
		for (int i = 0; i < animation.extraMediaReplaceTexturePaths.Count; i++)
		{
			AddAnimationExtraTextureCard(_animationExtraTextureCards, animation.extraMediaReplaceTexturePaths[i], i);
		}
	}

	private void AddAnimationReplacementSlotCard(Control host, AdobeAnimateData animation, string mediaName)
	{
		Variant value;
		bool flag = TryGetAnimationDictionaryValue(animation.mediaDictionary, mediaName, out value);
		int num = (flag ? value.AsInt32() : (-1));
		XWAnimationReplacementCard xWAnimationReplacementCard = InstantiateAnimationReplacementCard(host);
		if (GodotObject.IsInstanceValid(xWAnimationReplacementCard))
		{
			xWAnimationReplacementCard.Name = $"ReplacementSlotCard{Math.Max(0, num)}";
			xWAnimationReplacementCard.BindSlot(mediaName, num, CreateAnimationMediaThumbnail(animation, num), flag ? BuildAnimationMediaLayerSummary(animation, num) : "", flag, delegate(string name)
			{
				RemoveAnimationReplacementSlot(name);
			});
		}
	}

	private void AddAnimationExtraTextureCard(Control host, string texturePath, int index)
	{
		XWAnimationReplacementCard xWAnimationReplacementCard = InstantiateAnimationReplacementCard(host);
		if (GodotObject.IsInstanceValid(xWAnimationReplacementCard))
		{
			xWAnimationReplacementCard.Name = $"ExtraTextureCard{index}";
			Texture2D texture = LoadAnimationReplacementTexture(texturePath);
			xWAnimationReplacementCard.BindTexture(texture, index, FormatAnimationTextureName(texture, texturePath, index), texturePath, OpenAnimationExtraTexturePicker, delegate(int textureIndex)
			{
				RemoveAnimationExtraTexture(textureIndex);
			});
		}
	}

	private static void AddAnimationReplacementEmptyCard(Control host, string glyph, string title, string subtitle)
	{
		XWAnimationReplacementCard xWAnimationReplacementCard = InstantiateAnimationReplacementCard(host);
		if (GodotObject.IsInstanceValid(xWAnimationReplacementCard))
		{
			xWAnimationReplacementCard.BindEmpty(glyph, title, subtitle);
		}
	}

	private static XWAnimationReplacementCard InstantiateAnimationReplacementCard(Control host)
	{
		if (_animationReplacementCardScene == null)
		{
			_animationReplacementCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAnimationReplacementCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		XWAnimationReplacementCard xWAnimationReplacementCard = _animationReplacementCardScene?.Instantiate<XWAnimationReplacementCard>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(xWAnimationReplacementCard) || !GodotObject.IsInstanceValid(host))
		{
			return null;
		}
		host.AddChild(xWAnimationReplacementCard, forceReadableName: false, InternalMode.Disabled);
		return xWAnimationReplacementCard;
	}

	private static void ClearDynamicAnimationCards(Control host)
	{
		foreach (Node child in host.GetChildren())
		{
			host.RemoveChild(child);
			child.QueueFree();
		}
	}

	private static string FormatAnimationTextureName(Texture2D texture, string texturePath, int index)
	{
		if (GodotObject.IsInstanceValid(texture) && !string.IsNullOrWhiteSpace(texture.ResourceName))
		{
			return texture.ResourceName;
		}
		if (!string.IsNullOrWhiteSpace(texturePath))
		{
			return texturePath.GetFile();
		}
		return $"无效纹理 {index + 1}";
	}

	private static bool ContainsAnimationReplacementSlot(Dictionary dictionary, string mediaName)
	{
		Variant matchedKey;
		return TryFindAnimationDictionaryKey(dictionary, mediaName, out matchedKey);
	}

	private static bool TryFindAnimationDictionaryKey(Dictionary dictionary, string requestedName, out Variant matchedKey)
	{
		matchedKey = default(Variant);
		if (dictionary == null)
		{
			return false;
		}
		foreach (Variant key in dictionary.Keys)
		{
			if (string.Equals(key.AsString(), requestedName, StringComparison.Ordinal))
			{
				matchedKey = key;
				return true;
			}
		}
		return false;
	}

	private static bool TryGetAnimationDictionaryValue(Dictionary dictionary, string requestedName, out Variant value)
	{
		value = default(Variant);
		if (!TryFindAnimationDictionaryKey(dictionary, requestedName, out var matchedKey))
		{
			return false;
		}
		value = dictionary[matchedKey];
		return true;
	}

	private static string BuildAnimationMediaLayerSummary(AdobeAnimateData animation, int mediaId)
	{
		if (animation?.sliceMediaIds == null || animation.sliceLayerIds == null)
		{
			return "尚未被关键帧使用";
		}
		int num = Math.Min(animation.sliceMediaIds.Length, animation.sliceLayerIds.Length);
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < num; i++)
		{
			if (animation.sliceMediaIds[i] == mediaId)
			{
				hashSet.Add(animation.sliceLayerIds[i]);
			}
		}
		if (hashSet.Count == 0)
		{
			return "尚未被关键帧使用";
		}
		List<string> list = new List<string>();
		foreach (int item in hashSet)
		{
			list.Add(GetAnimationNameById(animation.layerDictionary, item, $"图层 {item + 1}"));
			if (list.Count >= 3)
			{
				break;
			}
		}
		string text = ((hashSet.Count > list.Count) ? $" +{hashSet.Count - list.Count}" : "");
		return "图层：" + string.Join(" / ", list) + text;
	}

	private void RequestAnimationReplacementReload()
	{
		if (GodotObject.IsInstanceValid(_animationReplacementReloadConfirmDialog))
		{
			_animationReplacementReloadConfirmDialog.PopupCentered(new Vector2I(560, 220));
		}
	}

	public void ReloadAnimationReplacementConfiguration()
	{
		string currentResourcePath = base.CurrentResourcePath;
		if (string.IsNullOrWhiteSpace(currentResourcePath) || !ResourceLoader.Exists(currentResourcePath))
		{
			SetAnimationReplacementStatus("当前动画尚未保存，无法从磁盘重载。", error: true);
			return;
		}
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>(currentResourcePath, "", ResourceLoader.CacheMode.Replace);
		if (!GodotObject.IsInstanceValid(adobeAnimateData))
		{
			SetAnimationReplacementStatus("动画资源重载失败：" + currentResourcePath, error: true);
			return;
		}
		LoadResource(adobeAnimateData, currentResourcePath, base.CurrentDescriptor);
		SetAnimationReplacementStatus("已从磁盘重载动画与替换配置。", error: false);
	}

	private void SetAnimationReplacementStatus(string message, bool error)
	{
		if (GodotObject.IsInstanceValid(_animationReplacementStatusLabel))
		{
			_animationReplacementStatusLabel.Text = message ?? "";
			_animationReplacementStatusLabel.Modulate = (error ? new Color(1f, 0.42f, 0.34f) : new Color(0.66f, 0.9f, 0.53f));
		}
	}

	private void OnAnimationVisualPropertyEdited(bool committed)
	{
		if (GodotObject.IsInstanceValid(base.CurrentResource))
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
				return;
			}
			MarkCurrentResourceDirty();
			base.CurrentResource.EmitChanged();
		}
	}

	private void OpenAnimationDatFileDialog(AdobeAnimateData animation)
	{
		if (GodotObject.IsInstanceValid(_animationDatFileDialog) && animation == base.CurrentResource)
		{
			_animationDatSelectionTarget = animation;
			string text = ResolveCompanionDatPath(animation);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string instance = (text.StartsWith("res://") ? NormalizePath(ProjectSettings.GlobalizePath(text)) : NormalizePath(text));
				_animationDatFileDialog.CurrentDir = instance.GetBaseDir();
				_animationDatFileDialog.CurrentFile = instance.GetFile();
			}
			_animationDatFileDialog.PopupCenteredClamped(new Vector2I(920, 620), 0.9f);
		}
	}

	private void SelectAnimationDatFile(string selectedPath)
	{
		if (_animationPropertyBinding == null)
		{
			return;
		}
		AdobeAnimateData animationDatSelectionTarget = _animationDatSelectionTarget;
		if (animationDatSelectionTarget != null && animationDatSelectionTarget == base.CurrentResource && !string.IsNullOrWhiteSpace(selectedPath))
		{
			string text = ToStoredAnimationDatPath(selectedPath);
			if (animationDatSelectionTarget.HasAuthoredPackedFrames)
			{
				RequestExplicitAnimationReimport(animationDatSelectionTarget, text);
				_animationDatSelectionTarget = null;
			}
			else
			{
				_animationPropertyBinding.SetValue(animationDatSelectionTarget, "animeFile", text, "选择动画 DAT", this, "ReloadAnimationFromDirectSettings");
				_animationDatSelectionTarget = null;
				UpdateAnimationDirectSummary();
			}
		}
	}

	private string ToStoredAnimationDatPath(string selectedPath)
	{
		string text = NormalizePath(selectedPath);
		string text2 = NormalizePath(base.CurrentResourcePath);
		if (text2.StartsWith("res://"))
		{
			text2 = NormalizePath(ProjectSettings.GlobalizePath(text2));
		}
		string baseDir = text2.GetBaseDir();
		if (!string.IsNullOrWhiteSpace(baseDir) && string.Equals(text.GetBaseDir(), baseDir, StringComparison.OrdinalIgnoreCase))
		{
			return text.GetFile();
		}
		string text3 = NormalizePath(ProjectSettings.GlobalizePath("res://")).TrimEnd('/');
		if (!string.IsNullOrWhiteSpace(text3) && text.StartsWith(text3 + "/", StringComparison.OrdinalIgnoreCase))
		{
			string text4 = text;
			int num = text3.Length + 1;
			return "res://" + text4.Substring(num, text4.Length - num);
		}
		return text;
	}

	public void ReloadAnimationFromDirectSettings()
	{
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData))
		{
			return;
		}
		string text = ResolveCompanionDatPath(adobeAnimateData);
		if (string.IsNullOrWhiteSpace(text) || !Godot.FileAccess.FileExists(text))
		{
			if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
			{
				_animationPreviewStatusLabel.Text = "找不到动画 DAT，请重新选择数据文件。";
			}
			UpdateAnimationDirectSummary();
		}
		else if (adobeAnimateData.HasAuthoredPackedFrames)
		{
			RequestExplicitAnimationReimport(adobeAnimateData, adobeAnimateData.animeFile);
		}
		else
		{
			StartAnimationResourceHydration(restart: true);
			UpdateAnimationDirectSummary();
		}
	}

	private void RequestExplicitAnimationReimport(AdobeAnimateData animation, string storedPath)
	{
		if (GodotObject.IsInstanceValid(animation) && animation == base.CurrentResource && GodotObject.IsInstanceValid(_animationReimportConfirmDialog))
		{
			_pendingAnimationReimportTarget = animation;
			_pendingAnimationReimportPath = storedPath ?? animation.animeFile ?? "";
			_pendingAnimationFrameScale = -1;
			_animationReimportConfirmDialog.DialogText = "当前动画含有手工帧编辑。\n\n来源：" + _pendingAnimationReimportPath + "\n\n重新导入会覆盖手工帧，但会先建立包含媒体、图层、Clip、事件与全部 Packed 数组的完整快照，可由全局 Undo/Redo 无损恢复。";
			_animationReimportConfirmDialog.PopupCentered(new Vector2I(620, 280));
		}
	}

	private void CancelPendingAnimationReimportConfirmation()
	{
		AdobeAnimateData pendingAnimationReimportTarget = _pendingAnimationReimportTarget;
		_pendingAnimationReimportTarget = null;
		_pendingAnimationReimportPath = "";
		_pendingAnimationFrameScale = -1;
		SyncAnimationFrameScaleControl(pendingAnimationReimportTarget);
		SetAnimationFrameStatus("已取消重新导入，手工帧编辑保持不变。");
	}

	private void ConfirmExplicitAnimationReimport()
	{
		AdobeAnimateData pendingAnimationReimportTarget = _pendingAnimationReimportTarget;
		string pendingAnimationReimportPath = _pendingAnimationReimportPath;
		int pendingAnimationFrameScale = _pendingAnimationFrameScale;
		_pendingAnimationReimportTarget = null;
		_pendingAnimationReimportPath = "";
		_pendingAnimationFrameScale = -1;
		if (GodotObject.IsInstanceValid(pendingAnimationReimportTarget) && pendingAnimationReimportTarget == base.CurrentResource)
		{
			if (pendingAnimationFrameScale > 0)
			{
				StartAnimationFrameScaleHydration(pendingAnimationReimportTarget, pendingAnimationFrameScale);
				return;
			}
			_animationExplicitReimportOldSnapshot = pendingAnimationReimportTarget.CaptureFullFrameEditSnapshot();
			_animationExplicitReimportFrame = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedFrame : 0);
			_animationExplicitReimportSliceKey = (GodotObject.IsInstanceValid(_animationFrameTimeline) ? _animationFrameTimeline.SelectedSliceKey : (-1));
			_animationExplicitReimportActionName = "重新导入动画 DAT";
			_animationExplicitReimportSuccessMessage = "DAT 已在后台重新导入，可使用全局 Undo 无损恢复手工帧。";
			_animationExplicitReimportRequiresRestore = true;
			pendingAnimationReimportTarget.animeFile = pendingAnimationReimportPath;
			StartAnimationResourceHydration(restart: true);
			SetAnimationFrameStatus("正在后台解析 DAT；当前帧编辑在导入成功并建立 Undo 前不会丢失。");
		}
	}

	private void CancelExplicitAnimationReimport(bool restoreOldSnapshot)
	{
		AdobeAnimateData adobeAnimateData = _animationHydrationTarget ?? _pendingAnimationReimportTarget;
		if (_animationExplicitReimportOldSnapshot != null && restoreOldSnapshot && _animationExplicitReimportRequiresRestore && GodotObject.IsInstanceValid(adobeAnimateData))
		{
			adobeAnimateData.TryRestoreFrameEditSnapshot(_animationExplicitReimportOldSnapshot, out var _);
		}
		_animationExplicitReimportOldSnapshot = null;
		_animationExplicitReimportFrame = -1;
		_animationExplicitReimportSliceKey = -1;
		_animationExplicitReimportActionName = "重新导入动画 DAT";
		_animationExplicitReimportSuccessMessage = "DAT 已在后台重新导入，可使用全局 Undo 无损恢复手工帧。";
		_animationExplicitReimportRequiresRestore = false;
		if (restoreOldSnapshot)
		{
			SyncAnimationFrameScaleControl(adobeAnimateData);
		}
	}

	private void RefreshRuntimeAnimationPreview()
	{
		if (GodotObject.IsInstanceValid(_runtimePreview))
		{
			_runtimePreview.SetFrameFromSlider(_runtimePreview.CurrentFrame);
		}
		UpdateAnimationDirectSummary();
	}

	private void UpdateAnimationDirectSummary()
	{
		if (base.CurrentResource is AdobeAnimateData adobeAnimateData)
		{
			if (GodotObject.IsInstanceValid(_animationResourcePathLabel))
			{
				_animationResourcePathLabel.Text = (string.IsNullOrWhiteSpace(base.CurrentResourcePath) ? "未保存的动画资源" : NormalizePath(base.CurrentResourcePath));
				_animationResourcePathLabel.TooltipText = _animationResourcePathLabel.Text;
			}
			if (GodotObject.IsInstanceValid(_animationDatFileButton))
			{
				_animationDatFileButton.Text = (string.IsNullOrWhiteSpace(adobeAnimateData.animeFile) ? "选择 Adobe Animate DAT" : adobeAnimateData.animeFile.GetFile());
				_animationDatFileButton.TooltipText = ResolveCompanionDatPath(adobeAnimateData);
			}
			if (GodotObject.IsInstanceValid(_animationDirectSummaryLabel))
			{
				_animationDirectSummaryLabel.Text = $"{GetAnimationFrameCount(adobeAnimateData)} 帧 · {adobeAnimateData.layerDictionary?.Count ?? 0} 图层 · {adobeAnimateData.clips?.Count ?? 0} Clip · {adobeAnimateData.frameRate:0.##} FPS";
			}
		}
	}

	private async void FitRuntimePreviewAfterLayout(AdobeAnimateInspectorPreview runtimePreview)
	{
		for (int layoutPass = 0; layoutPass < 6; layoutPass++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (!GodotObject.IsInstanceValid(runtimePreview) || !runtimePreview.IsVisibleInTree())
			{
				break;
			}
			ClampRuntimePreviewViewportMinimum(runtimePreview);
			if (base.MainPanel is ScrollContainer scrollContainer)
			{
				scrollContainer.ScrollHorizontal = 0;
				scrollContainer.ScrollVertical = 0;
			}
			runtimePreview.FitToPanel();
		}
	}

	private static void ClampRuntimePreviewViewportMinimum(AdobeAnimateInspectorPreview runtimePreview)
	{
		SubViewport subViewport = runtimePreview?.GetNodeOrNull<SubViewport>("%PreviewViewport");
		if (GodotObject.IsInstanceValid(subViewport) && subViewport.Size.X > 640)
		{
			subViewport.Size = new Vector2I(640, Math.Clamp(subViewport.Size.Y, 190, 420));
		}
	}

	private void StartAnimationResourceHydration(bool restart = false, int requestedFrameScale = -1)
	{
		string text = NormalizePath(base.CurrentResourcePath);
		if (base.CurrentResource is AdobeAnimateData adobeAnimateData && !string.IsNullOrWhiteSpace(text))
		{
			switch (text.GetExtension().ToLowerInvariant())
			{
			case "tres":
			case "res":
			case "dat":
				if (string.IsNullOrWhiteSpace(ResolveCompanionDatPath(adobeAnimateData)))
				{
					break;
				}
				if ((restart || !adobeAnimateData.HasAuthoredPackedFrames) && (restart || _animationHydrationTask == null || _animationHydrationTask.IsCompleted || !(_animationHydrationPath == text) || _animationHydrationTarget != adobeAnimateData))
				{
					CancelAnimationResourceHydration();
					_animationHydrationPath = text;
					_animationHydrationTarget = adobeAnimateData;
					_animationHydrationRequestedFrameScale = ((requestedFrameScale > 0) ? requestedFrameScale : Math.Max(1, adobeAnimateData.frameScale));
					_animationHydrationExpectedFrameScale = Math.Max(1, adobeAnimateData.frameScale);
					_animationHydrationExpectedAuthoringRevision = adobeAnimateData.AuthoringRevision;
					_animationHydrationCancellation = new CancellationTokenSource();
					string sourceDatPath = ResolveCompanionDatPath(adobeAnimateData);
					_animationHydrationTask = adobeAnimateData.BuildEditorDatHydrationAsync(sourceDatPath, _animationHydrationRequestedFrameScale, _animationHydrationCancellation.Token);
					UpdateAnimationProcessEligibility();
					if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
					{
						_animationPreviewStatusLabel.Text = "正在后台读取完整动画，编辑器仍可继续操作...";
					}
				}
				return;
			}
		}
		if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
		{
			_animationPreviewStatusLabel.Text = "当前只有 DAT 元数据；请先导入为动画资源再预览。";
		}
	}

	private void PollAnimationResourceHydration()
	{
		if (_animationHydrationTask == null || !_animationHydrationTask.IsCompleted)
		{
			return;
		}
		string animationHydrationPath = _animationHydrationPath;
		AdobeAnimateData animationHydrationTarget = _animationHydrationTarget;
		Task<AdobeAnimateData.EditorDatHydrationResult> animationHydrationTask = _animationHydrationTask;
		int animationHydrationRequestedFrameScale = _animationHydrationRequestedFrameScale;
		int animationHydrationExpectedFrameScale = _animationHydrationExpectedFrameScale;
		long animationHydrationExpectedAuthoringRevision = _animationHydrationExpectedAuthoringRevision;
		bool flag = _animationExplicitReimportOldSnapshot != null && GodotObject.IsInstanceValid(animationHydrationTarget);
		_animationHydrationPath = "";
		_animationHydrationTarget = null;
		_animationHydrationTask = null;
		_animationHydrationRequestedFrameScale = -1;
		_animationHydrationExpectedFrameScale = -1;
		_animationHydrationExpectedAuthoringRevision = -1L;
		_animationHydrationCancellation?.Dispose();
		_animationHydrationCancellation = null;
		SetProcess(enable: false);
		if (animationHydrationTask.IsCanceled)
		{
			AbortExplicitAnimationReimport(animationHydrationTarget, "DAT 重新导入已取消。");
			return;
		}
		if (animationHydrationTask.IsFaulted)
		{
			AbortExplicitAnimationReimport(animationHydrationTarget, "DAT 重新导入失败：" + animationHydrationTask.Exception?.GetBaseException().Message);
			if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
			{
				_animationPreviewStatusLabel.Text = "完整动画载入失败：" + animationHydrationTask.Exception?.GetBaseException().Message;
			}
			return;
		}
		if (!string.Equals(NormalizePath(base.CurrentResourcePath), animationHydrationPath, StringComparison.OrdinalIgnoreCase))
		{
			AbortExplicitAnimationReimport(animationHydrationTarget, "动画资源已切换，已恢复重新导入前的帧快照。");
			return;
		}
		if (!(base.CurrentResource is AdobeAnimateData adobeAnimateData) || adobeAnimateData != animationHydrationTarget)
		{
			AbortExplicitAnimationReimport(animationHydrationTarget, "动画资源已切换，已恢复重新导入前的帧快照。");
			if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
			{
				_animationPreviewStatusLabel.Text = "动画资源已切换，已忽略过期的后台读取结果。";
			}
			return;
		}
		if (animationHydrationTarget.frameScale != animationHydrationExpectedFrameScale || animationHydrationTarget.AuthoringRevision != animationHydrationExpectedAuthoringRevision)
		{
			AbortExplicitAnimationReimport(animationHydrationTarget, "动画在后台重建期间又被修改，已忽略过期结果并保留当前内容。");
			return;
		}
		if (!flag && animationHydrationTarget.HasAuthoredPackedFrames)
		{
			if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
			{
				_animationPreviewStatusLabel.Text = "后台 DAT 结果已忽略，手工帧编辑保持不变。";
			}
			return;
		}
		AdobeAnimateData.EditorDatHydrationResult result = animationHydrationTask.GetAwaiter().GetResult();
		if (flag && animationHydrationRequestedFrameScale > 0 && result.IsSuccess)
		{
			animationHydrationTarget.SetFrameScaleForDeferredHydration(animationHydrationRequestedFrameScale);
			_animationExplicitReimportRequiresRestore = true;
		}
		if (!result.IsSuccess || !animationHydrationTarget.ApplyEditorDatHydration(result))
		{
			AbortExplicitAnimationReimport(animationHydrationTarget, string.IsNullOrWhiteSpace(result.ErrorMessage) ? "完整动画载入失败，已恢复原帧快照。" : ("完整动画载入失败：" + result.ErrorMessage));
			if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
			{
				_animationPreviewStatusLabel.Text = (string.IsNullOrWhiteSpace(result.ErrorMessage) ? "完整动画载入失败，请检查同名 DAT 文件。" : ("完整动画载入失败：" + result.ErrorMessage));
			}
		}
		else if (!animationHydrationTarget.HasPackedRuntimeData())
		{
			AbortExplicitAnimationReimport(animationHydrationTarget, "DAT 未生成可用的 Packed 动画数据，已恢复原帧快照。");
			if (GodotObject.IsInstanceValid(_animationPreviewStatusLabel))
			{
				_animationPreviewStatusLabel.Text = "DAT 未生成可用的 Packed 动画数据。";
			}
		}
		else if (flag)
		{
			animationHydrationTarget.ClearFrameAuthoringFlag();
			Dictionary animationExplicitReimportOldSnapshot = _animationExplicitReimportOldSnapshot;
			Dictionary nextSnapshot = animationHydrationTarget.CaptureFrameEditSnapshot();
			int animationExplicitReimportFrame = _animationExplicitReimportFrame;
			int animationExplicitReimportSliceKey = _animationExplicitReimportSliceKey;
			string animationExplicitReimportActionName = _animationExplicitReimportActionName;
			string animationExplicitReimportSuccessMessage = _animationExplicitReimportSuccessMessage;
			_animationExplicitReimportOldSnapshot = null;
			_animationExplicitReimportFrame = -1;
			_animationExplicitReimportSliceKey = -1;
			_animationExplicitReimportActionName = "重新导入动画 DAT";
			_animationExplicitReimportSuccessMessage = "DAT 已在后台重新导入，可使用全局 Undo 无损恢复手工帧。";
			_animationExplicitReimportRequiresRestore = false;
			if (!animationHydrationTarget.TryRestoreFrameEditSnapshot(animationExplicitReimportOldSnapshot, out var error))
			{
				SetAnimationFrameStatus("无法建立 DAT 重新导入撤销历史：" + error, error: true);
				return;
			}
			CommitAnimationFrameSnapshotChange(animationHydrationTarget, animationExplicitReimportOldSnapshot, nextSnapshot, Math.Clamp(animationExplicitReimportFrame, 0, Math.Max(0, animationHydrationTarget.EditableFrameCount - 1)), animationExplicitReimportSliceKey, animationExplicitReimportFrame, animationExplicitReimportSliceKey, animationExplicitReimportActionName);
			SyncAnimationFrameScaleControl(animationHydrationTarget);
			SetAnimationFrameStatus(animationExplicitReimportSuccessMessage);
		}
		else
		{
			LoadResource(animationHydrationTarget, animationHydrationPath, base.CurrentDescriptor);
		}
	}

	private void AbortExplicitAnimationReimport(AdobeAnimateData animation, string message)
	{
		if (_animationExplicitReimportOldSnapshot != null)
		{
			if (_animationExplicitReimportRequiresRestore && GodotObject.IsInstanceValid(animation))
			{
				animation.TryRestoreFrameEditSnapshot(_animationExplicitReimportOldSnapshot, out var _);
			}
			_animationExplicitReimportOldSnapshot = null;
			_animationExplicitReimportFrame = -1;
			_animationExplicitReimportSliceKey = -1;
			_animationExplicitReimportActionName = "重新导入动画 DAT";
			_animationExplicitReimportSuccessMessage = "DAT 已在后台重新导入，可使用全局 Undo 无损恢复手工帧。";
			_animationExplicitReimportRequiresRestore = false;
			SyncAnimationFrameScaleControl(animation);
			SetAnimationFrameStatus(message, error: true);
		}
	}

	private void CancelAnimationResourceHydration()
	{
		Task<AdobeAnimateData.EditorDatHydrationResult> animationHydrationTask = _animationHydrationTask;
		if (_animationHydrationCancellation != null)
		{
			_animationHydrationCancellation.Cancel();
			_animationHydrationCancellation.Dispose();
		}
		_animationHydrationCancellation = null;
		_animationHydrationTask = null;
		_animationHydrationTarget = null;
		_animationHydrationPath = "";
		_animationHydrationRequestedFrameScale = -1;
		_animationHydrationExpectedFrameScale = -1;
		_animationHydrationExpectedAuthoringRevision = -1L;
		animationHydrationTask?.ContinueWith((Task<AdobeAnimateData.EditorDatHydrationResult> task) => task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		SetProcess(enable: false);
	}

	private static int GetAnimationFrameCount(AdobeAnimateData animation)
	{
		if (animation == null)
		{
			return 0;
		}
		if (animation.frameMax > 0)
		{
			return animation.frameMax;
		}
		int[] frameOffsets = animation.frameOffsets;
		if (frameOffsets == null)
		{
			return 0;
		}
		return frameOffsets.Length;
	}

	private void RenderAnimationTimeline(AdobeAnimateData animation)
	{
		AddTimelineRow($"总帧数: {animation.frameMax}");
		AddTimelineRow($"帧率: {animation.frameRate:0.##} fps");
		AddTimelineRow($"帧缩放: {animation.frameScale}");
		if (animation.clips != null && animation.clips.Count > 0)
		{
			foreach (Variant key in animation.clips.Keys)
			{
				AddTimelineRow("Clip " + key.AsString() + ": " + FormatVariant(animation.clips[key]));
			}
		}
		else
		{
			AddTimelineRow("Clip: 未定义");
		}
		int num = 0;
		if (animation.events != null)
		{
			for (int i = 0; i < animation.events.Count; i++)
			{
				Godot.Collections.Array array = animation.events[i];
				if (array != null && array.Count != 0)
				{
					num++;
					AddTimelineRow($"事件帧 {i}: {array.Count} 个事件");
					if (num >= 32)
					{
						AddTimelineRow("还有更多事件帧...");
						break;
					}
				}
			}
		}
		if (num == 0)
		{
			AddTimelineRow("事件: 未定义");
		}
	}

	private static string[] GetAnimationLayerNames(AdobeAnimateData animation)
	{
		if (animation?.layerDictionary == null || animation.layerDictionary.Count == 0)
		{
			return System.Array.Empty<string>();
		}
		string[] array = new string[animation.layerDictionary.Count];
		foreach (Variant key in animation.layerDictionary.Keys)
		{
			int num = animation.layerDictionary[key].AsInt32();
			if (num >= 0 && num < array.Length)
			{
				array[num] = key.AsString();
			}
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (string.IsNullOrEmpty(array[i]))
			{
				array[i] = i.ToString();
			}
		}
		return array;
	}

	private static string[] GetAnimationMediaNames(AdobeAnimateData animation)
	{
		if (animation?.mediaDictionary == null || animation.mediaDictionary.Count == 0)
		{
			return System.Array.Empty<string>();
		}
		int num = -1;
		foreach (Variant key in animation.mediaDictionary.Keys)
		{
			num = Math.Max(num, animation.mediaDictionary[key].AsInt32());
		}
		if (num < 0)
		{
			return System.Array.Empty<string>();
		}
		string[] array = new string[num + 1];
		foreach (Variant key2 in animation.mediaDictionary.Keys)
		{
			int num2 = animation.mediaDictionary[key2].AsInt32();
			if ((uint)num2 < (uint)array.Length)
			{
				array[num2] = key2.AsString();
			}
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (string.IsNullOrWhiteSpace(array[i]))
			{
				array[i] = $"媒体 {i + 1}";
			}
		}
		return array;
	}

	private void RenderAnimationGraph(AdobeAnimateData animation)
	{
		string[] animationLayerNames = GetAnimationLayerNames(animation);
		if (animationLayerNames.Length != 0)
		{
			for (int i = 0; i < animationLayerNames.Length; i++)
			{
				AddGraphRow($"图层 {i + 1}: {animationLayerNames[i]}");
			}
		}
		else
		{
			AddGraphRow("图层: 未定义");
		}
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 1);
		defaultInterpolatedStringHandler.AppendLiteral("媒体切片数: ");
		Vector4[] mediaRects = animation.mediaRects;
		defaultInterpolatedStringHandler.AppendFormatted((mediaRects != null) ? mediaRects.Length : 0);
		AddGraphRow(defaultInterpolatedStringHandler.ToStringAndClear());
		defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 1);
		defaultInterpolatedStringHandler.AppendLiteral("Packed 帧数据: ");
		int[] frameOffsets = animation.frameOffsets;
		defaultInterpolatedStringHandler.AppendFormatted((frameOffsets != null) ? frameOffsets.Length : 0);
		AddGraphRow(defaultInterpolatedStringHandler.ToStringAndClear());
		defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 1);
		defaultInterpolatedStringHandler.AppendLiteral("Packed 切片数据: ");
		int[] sliceKeys = animation.sliceKeys;
		defaultInterpolatedStringHandler.AppendFormatted((sliceKeys != null) ? sliceKeys.Length : 0);
		AddGraphRow(defaultInterpolatedStringHandler.ToStringAndClear());
		if (animation.mediaDictionary == null || animation.mediaDictionary.Count <= 0)
		{
			return;
		}
		int num = 0;
		foreach (Variant key in animation.mediaDictionary.Keys)
		{
			AddGraphRow("媒体 " + key.AsString() + " -> " + FormatVariant(animation.mediaDictionary[key]));
			num++;
			if (num >= 48)
			{
				AddGraphRow("还有更多媒体切片...");
				break;
			}
		}
	}

	private void RenderAnimationReferences(AdobeAnimateData animation)
	{
		string text = ResolveCompanionDatPath(animation);
		if (!string.IsNullOrWhiteSpace(text))
		{
			AddReferenceRow("DAT 子资源 -> " + text);
		}
		AdobeAnimateRuntimeDefinition definition;
		if (TryGetSharedAnimationAtlas(animation, out var texture, out var _))
		{
			Vector2 vector = TryGetTextureSize(texture);
			AddReferenceRow((vector == Vector2.Zero) ? "共享图集 -> runtime atlas" : $"共享图集 -> runtime atlas ({vector.X:0} x {vector.Y:0})");
		}
		else if (TryGetAnimationDefinition(animation, out definition) && GodotObject.IsInstanceValid(definition.AtlasTextureArray))
		{
			Vector2 atlasTextureArraySize = definition.AtlasTextureArraySize;
			AddReferenceRow($"共享图集数组 -> runtime atlas ({atlasTextureArraySize.X:0} x {atlasTextureArraySize.Y:0})");
		}
		AddReferenceRow("动画资源 -> " + base.CurrentResourcePath);
	}

	private string ResolveCompanionDatPath(AdobeAnimateData animation)
	{
		if (animation == null || string.IsNullOrWhiteSpace(animation.animeFile))
		{
			return "";
		}
		string text = NormalizePath(animation.animeFile);
		if (text.StartsWith("res://") || text.StartsWith("uid://") || Path.IsPathRooted(text))
		{
			return text;
		}
		string text2 = NormalizePath(base.CurrentResourcePath);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		if (text2.StartsWith("res://"))
		{
			return text2.GetBaseDir().PathJoin(text);
		}
		try
		{
			return NormalizePath(Path.Combine(Path.GetDirectoryName(text2) ?? "", text));
		}
		catch
		{
			return text;
		}
	}

	private void AddTimelineRow(string text)
	{
		AddItemIfMissing(base.TimelineList, text);
	}

	private void AddPreviewRow(string text)
	{
		AddItemIfMissing(base.PreviewList, text);
	}

	private void AddGraphRow(string text)
	{
		AddItemIfMissing(base.GraphList, text);
	}

	private void AddReferenceRow(string text)
	{
		AddItemIfMissing(base.ReferenceList, text);
	}

	private static void AddItemIfMissing(ItemList list, string text)
	{
		if (list == null || string.IsNullOrWhiteSpace(text))
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

	private static int CountEventFrames(AdobeAnimateData animation)
	{
		if (animation?.events == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < animation.events.Count; i++)
		{
			if (animation.events[i] != null && animation.events[i].Count > 0)
			{
				num++;
			}
		}
		return num;
	}

	private static bool TryGetSharedAnimationAtlas(AdobeAnimateData animation, out Texture2D texture, out Rect2[] mediaRects)
	{
		texture = null;
		mediaRects = System.Array.Empty<Rect2>();
		if (!TryGetAnimationDefinition(animation, out var definition) || !GodotObject.IsInstanceValid(definition.BaseAtlas))
		{
			return false;
		}
		texture = definition.BaseAtlas;
		mediaRects = definition.MediaRects ?? System.Array.Empty<Rect2>();
		return true;
	}

	private static bool TryGetAnimationDefinition(AdobeAnimateData animation, out AdobeAnimateRuntimeDefinition definition)
	{
		definition = null;
		if (animation == null || animation.frameMax <= 0)
		{
			return false;
		}
		definition = AdobeAnimateDefinitionCache.GetOrBuild(animation);
		if (definition != null)
		{
			if (!GodotObject.IsInstanceValid(definition.BaseAtlas) && (definition.AtlasPages == null || definition.AtlasPages.Length == 0))
			{
				return GodotObject.IsInstanceValid(definition.AtlasTextureArray);
			}
			return true;
		}
		return false;
	}

	private static Vector2 TryGetSharedAnimationAtlasSize(AdobeAnimateData animation)
	{
		if (!TryGetAnimationDefinition(animation, out var definition))
		{
			return Vector2.Zero;
		}
		if (definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f)
		{
			return definition.AtlasTextureArraySize;
		}
		return TryGetTextureSize(definition.BaseAtlas);
	}

	private static Vector2 TryGetTextureSize(Texture2D texture)
	{
		if (!GodotObject.IsInstanceValid(texture))
		{
			return Vector2.Zero;
		}
		try
		{
			return texture.GetSize();
		}
		catch
		{
			return Vector2.Zero;
		}
	}

	private static string FormatVariant(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if (variantType <= Variant.Type.StringName)
		{
			if ((ulong)variantType <= 6uL)
			{
				switch ((int)variantType)
				{
				case 0:
					return "空";
				case 6:
					return $"{value.AsVector2I().X} - {value.AsVector2I().Y}";
				case 5:
					return $"{value.AsVector2().X:0.###}, {value.AsVector2().Y:0.###}";
				case 2:
					return value.AsInt64().ToString();
				case 3:
					return value.AsDouble().ToString("0.###");
				case 4:
					goto IL_0133;
				case 1:
					goto IL_01bd;
				}
			}
			if (variantType == Variant.Type.StringName)
			{
				goto IL_0133;
			}
		}
		else
		{
			switch (variantType)
			{
			case Variant.Type.Array:
				return $"Array({value.AsGodotArray().Count})";
			case Variant.Type.Dictionary:
				return $"Dictionary({value.AsGodotDictionary().Count})";
			}
		}
		goto IL_01bd;
		IL_0133:
		return value.AsString();
		IL_01bd:
		return value.ToString();
	}

	private static string NormalizePath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.Trim().Replace('\\', '/');
		}
		return "";
	}
}
