using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace PVZHE.AdobeAnimateEditor.Inspector;

[Tool]
public partial class AdobeAnimateInspectorPreview : PanelContainer
{
	private readonly List<string> _clipNames = new List<string>();

	private Label _statusLabel;

	private OptionButton _clipOption;

	private Button _playButton;

	private HSlider _frameSlider;

	private Label _frameLabel;

	private OptionButton _zoomOption;

	private SubViewportContainer _viewportContainer;

	private SubViewport _previewViewport;

	private ColorRect _background;

	private ColorRect _horizontalAxis;

	private ColorRect _verticalAxis;

	private Node2D _previewRoot;

	private AdobeAnimateCpuPreviewCanvas _cpuPreviewCanvas;

	private AdobeAnimateData _animation;

	private AdobeAnimateSprite _sprite;

	private AdobeAnimateData _hydrationTarget;

	private CancellationTokenSource _hydrationCancellation;

	private Task<AdobeAnimateData.EditorDatHydrationResult> _hydrationTask;

	private bool _updatingControls;

	private bool _draggingPreview;

	private bool _resumePlaybackWhenVisible;

	private bool _pausedForVisibility;

	private Vector2 _previewPan;

	private float _previewZoom = 1f;

	private int _lastControlFrame = -2147483648;

	private bool _lastControlPaused = true;

	private int _lastPublishedFrame = -2147483648;

	private int _lastPublishedStart = -2147483648;

	private int _lastPublishedEnd = -2147483648;

	private string _lastPublishedClip = "";

	private bool _lastPublishedPlaying;

	private int _controlRefreshCount;

	public int ControlRefreshCount => _controlRefreshCount;

	public int CurrentFrame
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_sprite))
			{
				return 0;
			}
			return _sprite.frameIndex;
		}
	}

	public bool IsPlaying
	{
		get
		{
			if (GodotObject.IsInstanceValid(_sprite) && !_sprite.pause)
			{
				return IsVisibleInTree();
			}
			return false;
		}
	}

	public string SelectedClipName
	{
		get
		{
			if (_clipOption == null || _clipOption.Selected < 0 || _clipOption.Selected >= _clipNames.Count)
			{
				return "";
			}
			return _clipNames[_clipOption.Selected];
		}
	}

	public event Action<int, int, int, string, bool> PreviewStateChanged;

	public override void _Ready()
	{
		_statusLabel = GetNode<Label>("%StatusLabel");
		_clipOption = GetNode<OptionButton>("%ClipOption");
		_playButton = GetNode<Button>("%PlayButton");
		_frameSlider = GetNode<HSlider>("%FrameSlider");
		_frameLabel = GetNode<Label>("%FrameLabel");
		_zoomOption = GetNode<OptionButton>("%ZoomOption");
		_viewportContainer = GetNode<SubViewportContainer>("%PreviewViewportContainer");
		_previewViewport = GetNode<SubViewport>("%PreviewViewport");
		_background = GetNode<ColorRect>("%Background");
		_horizontalAxis = GetNode<ColorRect>("%HorizontalAxis");
		_verticalAxis = GetNode<ColorRect>("%VerticalAxis");
		_previewRoot = GetNode<Node2D>("%PreviewRoot");
		_cpuPreviewCanvas = GetNode<AdobeAnimateCpuPreviewCanvas>("%CpuPreviewCanvas");
		GetNode<Button>("%PreviousButton").Connect(BaseButton.SignalName.Pressed, new Callable(this, "PreviousFrame"));
		GetNode<Button>("%NextButton").Connect(BaseButton.SignalName.Pressed, new Callable(this, "NextFrame"));
		GetNode<Button>("%FitButton").Connect(BaseButton.SignalName.Pressed, new Callable(this, "FitToPanel"));
		_playButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, "TogglePlayback"));
		_clipOption.Connect(OptionButton.SignalName.ItemSelected, new Callable(this, "SelectClip"));
		_frameSlider.Connect(Godot.Range.SignalName.ValueChanged, new Callable(this, "SetFrameFromSlider"));
		_zoomOption.Connect(OptionButton.SignalName.ItemSelected, new Callable(this, "SelectZoom"));
		_viewportContainer.Connect(Control.SignalName.GuiInput, new Callable(this, "OnPreviewGuiInput"));
		_viewportContainer.Connect(Control.SignalName.Resized, new Callable(this, "OnPreviewResized"));
		UpdatePreviewViewportLayout();
		SetProcess(enable: false);
		if (GodotObject.IsInstanceValid(_animation))
		{
			EditAnimation(_animation);
		}
		else
		{
			RefreshProcessEligibility();
		}
	}

	public override void _Process(double delta)
	{
		PollAnimationHydration();
		UpdateVisibilityPlaybackState();
		if (GodotObject.IsInstanceValid(_sprite) && IsVisibleInTree())
		{
			UpdateFrameControls(_sprite.frameIndex);
		}
		RefreshProcessEligibility();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if ((long)what == 31 && IsNodeReady())
		{
			UpdateVisibilityPlaybackState();
			RefreshProcessEligibility();
		}
	}

	public override void _ExitTree()
	{
		CancelAnimationHydration();
		base._ExitTree();
	}

	public void EditAnimation(AdobeAnimateData animation)
	{
		_animation = animation;
		if (IsNodeReady())
		{
			if (!GodotObject.IsInstanceValid(_animation))
			{
				ClearPreview();
			}
			else if (!_animation.HasPackedRuntimeData())
			{
				ClearRenderedPreview();
				StartAnimationHydration();
			}
			else
			{
				ShowHydratedAnimation();
			}
		}
	}

	public void RefreshAnimation()
	{
		if (GodotObject.IsInstanceValid(_animation))
		{
			EditAnimation(_animation);
		}
	}

	public bool RefreshClipOptions(string preferredClipName = "")
	{
		if (!GodotObject.IsInstanceValid(_animation) || !GodotObject.IsInstanceValid(_sprite))
		{
			return false;
		}
		BuildClipOptions(preferredClipName);
		if (!string.IsNullOrEmpty(preferredClipName))
		{
			return string.Equals(SelectedClipName, preferredClipName, StringComparison.Ordinal);
		}
		return true;
	}

	public bool SelectClipByName(string clipName)
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			return false;
		}
		if (clipName == null)
		{
			clipName = "";
		}
		for (int i = 0; i < _clipNames.Count; i++)
		{
			if (string.Equals(_clipNames[i], clipName, StringComparison.Ordinal))
			{
				_clipOption.Select(i);
				SelectClip(i);
				return true;
			}
		}
		return false;
	}

	public void ClearPreview()
	{
		CancelAnimationHydration();
		ClearRenderedPreview();
		_animation = null;
		_clipNames.Clear();
		if (GodotObject.IsInstanceValid(_clipOption))
		{
			_clipOption.Clear();
		}
		if (GodotObject.IsInstanceValid(_statusLabel))
		{
			_statusLabel.Text = "0 帧";
		}
	}

	private void StartAnimationHydration()
	{
		if (GodotObject.IsInstanceValid(_animation) && (_hydrationTask == null || _hydrationTask.IsCompleted || _hydrationTarget != _animation))
		{
			CancelAnimationHydration();
			_hydrationTarget = _animation;
			_hydrationCancellation = new CancellationTokenSource();
			_hydrationTask = _animation.BuildEditorDatHydrationAsync(_animation.GetResolvedAnimeFilePath(), _hydrationCancellation.Token);
			_statusLabel.Text = "正在后台读取完整动画...";
			RefreshProcessEligibility();
		}
	}

	private void PollAnimationHydration()
	{
		if (_hydrationTask == null || !_hydrationTask.IsCompleted)
		{
			return;
		}
		AdobeAnimateData hydrationTarget = _hydrationTarget;
		Task<AdobeAnimateData.EditorDatHydrationResult> hydrationTask = _hydrationTask;
		_hydrationTarget = null;
		_hydrationTask = null;
		_hydrationCancellation?.Dispose();
		_hydrationCancellation = null;
		if (hydrationTask.IsCanceled)
		{
			RefreshProcessEligibility();
			return;
		}
		if (hydrationTask.IsFaulted)
		{
			_statusLabel.Text = "DAT 读取失败：" + hydrationTask.Exception?.GetBaseException().Message;
			RefreshProcessEligibility();
			return;
		}
		if (!GodotObject.IsInstanceValid(_animation) || hydrationTarget != _animation)
		{
			RefreshProcessEligibility();
			return;
		}
		AdobeAnimateData.EditorDatHydrationResult result = hydrationTask.GetAwaiter().GetResult();
		if (!result.IsSuccess || !_animation.ApplyEditorDatHydration(result))
		{
			_statusLabel.Text = (string.IsNullOrWhiteSpace(result.ErrorMessage) ? "DAT 数据不可用" : ("DAT 读取失败：" + result.ErrorMessage));
			RefreshProcessEligibility();
		}
		else
		{
			ShowHydratedAnimation();
		}
	}

	private void CancelAnimationHydration()
	{
		if (_hydrationCancellation != null)
		{
			_hydrationCancellation.Cancel();
			_hydrationCancellation.Dispose();
		}
		_hydrationCancellation = null;
		_hydrationTask = null;
		_hydrationTarget = null;
		RefreshProcessEligibility();
	}

	private void ClearRenderedPreview()
	{
		if (GodotObject.IsInstanceValid(_cpuPreviewCanvas))
		{
			_cpuPreviewCanvas.BindSource(null);
		}
		if (GodotObject.IsInstanceValid(_sprite))
		{
			_sprite.QueueFree();
		}
		_sprite = null;
		_resumePlaybackWhenVisible = false;
		_pausedForVisibility = false;
		_lastControlFrame = -2147483648;
		_lastPublishedFrame = -2147483648;
		RefreshProcessEligibility();
	}

	private void ShowHydratedAnimation()
	{
		CreateSprite();
		BuildClipOptions();
		ConfigureFrameRange();
		CallDeferred("FitToPanel");
		_statusLabel.Text = $"{_animation.frameMax} 帧 · {_animation.frameRate:0.##} fps";
		RefreshProcessEligibility();
	}

	private void CreateSprite()
	{
		if (GodotObject.IsInstanceValid(_sprite))
		{
			_sprite.QueueFree();
		}
		_sprite = new AdobeAnimateSprite
		{
			Name = "InspectorAnimationSprite",
			flashAnimeData = _animation,
			preview = true,
			forceLocalRender = true,
			suppressEditorRenderSubmission = true,
			loop = true,
			skipLastFrame = false,
			pause = false,
			keepRenderSubmittedWhenPaused = true,
			refreshEveryFrame = true
		};
		_previewRoot.AddChild(_sprite, forceReadableName: false, InternalMode.Disabled);
		_cpuPreviewCanvas.BindSource(_sprite);
		_resumePlaybackWhenVisible = false;
		_pausedForVisibility = false;
		UpdateVisibilityPlaybackState();
		_previewPan = Vector2.Zero;
		_previewZoom = 1f;
		UpdatePreviewTransform();
	}

	private void BuildClipOptions(string preferredClipName = "")
	{
		_clipNames.Clear();
		_clipOption.Clear();
		_clipOption.AddItem("全部帧");
		_clipNames.Add("");
		if (_animation.clips != null)
		{
			foreach (Variant key in _animation.clips.Keys)
			{
				string text = key.AsString();
				Vector2I clip = _animation.GetClip(text);
				_clipOption.AddItem($"{text} ({clip.X}-{GetPlayableEndFrame(clip)})");
				_clipNames.Add(text);
			}
		}
		int num = 0;
		if (!string.IsNullOrEmpty(preferredClipName))
		{
			for (int i = 1; i < _clipNames.Count; i++)
			{
				if (string.Equals(_clipNames[i], preferredClipName, StringComparison.Ordinal))
				{
					num = i;
					break;
				}
			}
		}
		_clipOption.Select(num);
		SelectClip(num);
	}

	public void SelectClip(long index)
	{
		if (GodotObject.IsInstanceValid(_sprite) && index >= 0 && index < _clipNames.Count)
		{
			bool pause = _sprite.pause;
			string text = _clipNames[(int)index];
			if (string.IsNullOrEmpty(text))
			{
				_sprite.clipRange = new Vector2I(0, Math.Max(0, _animation.frameMax));
				_sprite.frameIndex = 0;
				_sprite.elapsedTimer = 0.0;
				_sprite.QueueRedraw();
			}
			else
			{
				_sprite.SetClip(text);
			}
			_sprite.pause = pause;
			ConfigureFrameRange();
		}
	}

	public void TogglePlayback()
	{
		if (GodotObject.IsInstanceValid(_sprite))
		{
			_sprite.pause = !_sprite.pause;
			UpdateFrameControls(_sprite.frameIndex);
			RefreshProcessEligibility();
		}
	}

	public void PreviousFrame()
	{
		StepFrame(-1);
	}

	private void UpdateVisibilityPlaybackState()
	{
		if (GodotObject.IsInstanceValid(_cpuPreviewCanvas))
		{
			_cpuPreviewCanvas.SetPreviewActive(IsVisibleInTree());
		}
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			RefreshProcessEligibility();
		}
		else if (!IsVisibleInTree())
		{
			if (_pausedForVisibility)
			{
				RefreshProcessEligibility();
				return;
			}
			_resumePlaybackWhenVisible = !_sprite.pause;
			_pausedForVisibility = true;
			if (_resumePlaybackWhenVisible)
			{
				_sprite.pause = true;
			}
			RefreshProcessEligibility();
		}
		else if (!_pausedForVisibility)
		{
			RefreshProcessEligibility();
		}
		else
		{
			_pausedForVisibility = false;
			if (_resumePlaybackWhenVisible)
			{
				_sprite.pause = false;
			}
			_resumePlaybackWhenVisible = false;
			UpdateFrameControls(_sprite.frameIndex);
			RefreshProcessEligibility();
		}
	}

	private void RefreshProcessEligibility()
	{
		if (IsInsideTree())
		{
			bool flag = _hydrationTask != null && !_hydrationTask.IsCompleted;
			bool flag2 = IsVisibleInTree() && GodotObject.IsInstanceValid(_sprite) && !_sprite.pause;
			SetProcess(flag || flag2);
		}
	}

	public void NextFrame()
	{
		StepFrame(1);
	}

	private void StepFrame(int direction)
	{
		if (GodotObject.IsInstanceValid(_sprite))
		{
			_sprite.pause = true;
			SetFrame(Mathf.Clamp(_sprite.frameIndex + direction, GetStartFrame(), GetEndFrame()));
			RefreshProcessEligibility();
		}
	}

	public void SetFrameFromSlider(double value)
	{
		if (!_updatingControls && GodotObject.IsInstanceValid(_sprite))
		{
			int num = Mathf.Clamp(Mathf.RoundToInt(value), GetStartFrame(), GetEndFrame());
			_sprite.pause = true;
			if (_sprite.frameIndex != num)
			{
				SetFrame(num);
			}
			else
			{
				UpdateFrameControls(num);
			}
			RefreshProcessEligibility();
		}
	}

	private void SetFrame(int frame)
	{
		_sprite.frameIndex = Mathf.Clamp(frame, GetStartFrame(), GetEndFrame());
		_sprite.elapsedTimer = 0.0;
		_sprite.QueueRedraw();
		UpdateFrameControls(_sprite.frameIndex);
	}

	private void ConfigureFrameRange()
	{
		if (GodotObject.IsInstanceValid(_sprite))
		{
			_updatingControls = true;
			_frameSlider.MinValue = GetStartFrame();
			_frameSlider.MaxValue = GetEndFrame();
			_updatingControls = false;
			UpdateFrameControls(_sprite.frameIndex);
		}
	}

	private void UpdateFrameControls(int frame)
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			return;
		}
		bool pause = _sprite.pause;
		if (_lastControlFrame != frame || _lastControlPaused != pause)
		{
			_updatingControls = true;
			if (_lastControlFrame != frame)
			{
				_frameSlider.Value = frame;
				_frameLabel.Text = $"{frame} / {GetEndFrame()}";
			}
			if (_lastControlPaused != pause)
			{
				_playButton.Text = (pause ? "播放" : "暂停");
			}
			_updatingControls = false;
			_lastControlFrame = frame;
			_lastControlPaused = pause;
			_controlRefreshCount++;
		}
		PublishPreviewState(frame);
	}

	private void PublishPreviewState(int frame)
	{
		int startFrame = GetStartFrame();
		int endFrame = GetEndFrame();
		string selectedClipName = SelectedClipName;
		bool isPlaying = IsPlaying;
		if (_lastPublishedFrame != frame || _lastPublishedStart != startFrame || _lastPublishedEnd != endFrame || !string.Equals(_lastPublishedClip, selectedClipName, StringComparison.Ordinal) || _lastPublishedPlaying != isPlaying)
		{
			_lastPublishedFrame = frame;
			_lastPublishedStart = startFrame;
			_lastPublishedEnd = endFrame;
			_lastPublishedClip = selectedClipName;
			_lastPublishedPlaying = isPlaying;
			this.PreviewStateChanged?.Invoke(frame, startFrame, endFrame, selectedClipName, isPlaying);
		}
	}

	private int GetStartFrame()
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			return 0;
		}
		return Math.Max(0, _sprite.clipRange.X);
	}

	private int GetEndFrame()
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			return 0;
		}
		return Math.Max(GetStartFrame(), Math.Min(Math.Max(0, _animation.frameMax - 1), GetPlayableEndFrame(_sprite.clipRange)));
	}

	private static int GetPlayableEndFrame(Vector2I range)
	{
		return Math.Max(range.X, range.Y - 1);
	}

	public void SelectZoom(long index)
	{
		if (GodotObject.IsInstanceValid(_sprite) && index >= 0 && index < _zoomOption.ItemCount)
		{
			int itemId = _zoomOption.GetItemId((int)index);
			if (itemId <= 0)
			{
				FitToPanel();
			}
			else
			{
				SetPreviewZoom((float)itemId / 100f, GetViewportCenter());
			}
		}
	}

	public void FitToPanel()
	{
		if (!GodotObject.IsInstanceValid(_sprite) || !GodotObject.IsInstanceValid(_previewViewport))
		{
			return;
		}
		Rect2 bounds;
		Rect2 rect = ((!GodotObject.IsInstanceValid(_cpuPreviewCanvas) || !_cpuPreviewCanvas.TryGetLocalRenderBounds(out bounds)) ? (AdobeAnimateDefinitionCache.GetOrBuild(_animation)?.LocalBounds ?? default(Rect2)) : bounds);
		if (rect.Size.X <= 0.001f || rect.Size.Y <= 0.001f)
		{
			_previewZoom = 1f;
			_previewPan = Vector2.Zero;
			UpdatePreviewTransform();
			return;
		}
		Vector2 vector = new Vector2(_previewViewport.Size.X, _previewViewport.Size.Y) - Vector2.One * 24f;
		float value = Mathf.Min(vector.X / rect.Size.X, vector.Y / rect.Size.Y);
		_previewZoom = Mathf.Clamp(value, 0.05f, 16f);
		_previewPan = -rect.GetCenter() * _previewZoom;
		UpdatePreviewTransform();
		if (_zoomOption.ItemCount > 0)
		{
			_zoomOption.Select(0);
		}
	}

	public void OnPreviewResized()
	{
		UpdatePreviewViewportLayout();
		if (_zoomOption.Selected == 0)
		{
			FitToPanel();
		}
		else
		{
			UpdatePreviewTransform();
		}
	}

	private void UpdatePreviewViewportLayout()
	{
		if (GodotObject.IsInstanceValid(_viewportContainer) && GodotObject.IsInstanceValid(_previewViewport))
		{
			Vector2 size = _viewportContainer.Size;
			if (!(size.X < 2f) && !(size.Y < 2f))
			{
				Vector2I size2 = new Vector2I(Math.Max(2, Mathf.RoundToInt(size.X)), Math.Max(2, Mathf.RoundToInt(size.Y)));
				_previewViewport.Size = size2;
				Vector2 vector = new Vector2((float)size2.X * 0.5f, (float)size2.Y * 0.5f);
				_background.Size = new Vector2(size2.X, size2.Y);
				_horizontalAxis.Position = new Vector2(0f, vector.Y - 1f);
				_horizontalAxis.Size = new Vector2(size2.X, 2f);
				_verticalAxis.Position = new Vector2(vector.X - 1f, 0f);
				_verticalAxis.Size = new Vector2(2f, size2.Y);
			}
		}
	}

	public void OnPreviewGuiInput(InputEvent inputEvent)
	{
		if (!GodotObject.IsInstanceValid(_sprite))
		{
			return;
		}
		if (inputEvent is InputEventMouseButton { ButtonIndex: var buttonIndex } inputEventMouseButton)
		{
			bool flag = (((ulong)(buttonIndex - 4) <= 1uL) ? true : false);
			if (flag && inputEventMouseButton.Pressed)
			{
				float num = ((inputEventMouseButton.ButtonIndex == MouseButton.WheelUp) ? 1.12f : (25f / 28f));
				SetPreviewZoom(_previewZoom * num, ToViewportPosition(inputEventMouseButton.Position));
				SelectClosestZoomPreset();
				_viewportContainer.AcceptEvent();
				return;
			}
			MouseButton buttonIndex2 = inputEventMouseButton.ButtonIndex;
			if ((buttonIndex2 == MouseButton.Left || buttonIndex2 == MouseButton.Middle) ? true : false)
			{
				_draggingPreview = inputEventMouseButton.Pressed;
				_viewportContainer.MouseDefaultCursorShape = (CursorShape)(_draggingPreview ? 6 : 13);
				_viewportContainer.AcceptEvent();
				return;
			}
			if (inputEventMouseButton.ButtonIndex == MouseButton.Right && inputEventMouseButton.Pressed)
			{
				FitToPanel();
				_viewportContainer.AcceptEvent();
			}
		}
		if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _draggingPreview)
		{
			_previewPan += ToViewportDelta(inputEventMouseMotion.Relative);
			UpdatePreviewTransform();
			_viewportContainer.AcceptEvent();
		}
	}

	private void SetPreviewZoom(float zoom, Vector2 pivot)
	{
		float num = Mathf.Clamp(zoom, 0.05f, 16f);
		if (!Mathf.IsEqualApprox(num, _previewZoom))
		{
			Vector2 vector = GetViewportCenter() + _previewPan;
			Vector2 vector2 = (pivot - vector) / _previewZoom;
			_previewZoom = num;
			_previewPan = pivot - vector2 * _previewZoom - GetViewportCenter();
			UpdatePreviewTransform();
		}
	}

	private void UpdatePreviewTransform()
	{
		if (GodotObject.IsInstanceValid(_previewRoot) && GodotObject.IsInstanceValid(_sprite))
		{
			_previewRoot.Position = GetViewportCenter() + _previewPan;
			_sprite.Scale = Vector2.One * _previewZoom;
		}
	}

	private Vector2 GetViewportCenter()
	{
		if (!GodotObject.IsInstanceValid(_previewViewport))
		{
			return Vector2.Zero;
		}
		return new Vector2((float)_previewViewport.Size.X * 0.5f, (float)_previewViewport.Size.Y * 0.5f);
	}

	private Vector2 ToViewportPosition(Vector2 controlPosition)
	{
		if (_viewportContainer.Size.X <= 0.001f || _viewportContainer.Size.Y <= 0.001f)
		{
			return controlPosition;
		}
		return controlPosition * new Vector2((float)_previewViewport.Size.X / _viewportContainer.Size.X, (float)_previewViewport.Size.Y / _viewportContainer.Size.Y);
	}

	private Vector2 ToViewportDelta(Vector2 controlDelta)
	{
		if (_viewportContainer.Size.X <= 0.001f || _viewportContainer.Size.Y <= 0.001f)
		{
			return controlDelta;
		}
		return controlDelta * new Vector2((float)_previewViewport.Size.X / _viewportContainer.Size.X, (float)_previewViewport.Size.Y / _viewportContainer.Size.Y);
	}

	private void SelectClosestZoomPreset()
	{
		if (_zoomOption.ItemCount <= 1)
		{
			return;
		}
		int idx = 1;
		float num = 3.4028235E+38f;
		for (int i = 1; i < _zoomOption.ItemCount; i++)
		{
			float num2 = Mathf.Abs((float)_zoomOption.GetItemId(i) / 100f - _previewZoom);
			if (num2 < num)
			{
				idx = i;
				num = num2;
			}
		}
		_zoomOption.Select(idx);
	}
}
