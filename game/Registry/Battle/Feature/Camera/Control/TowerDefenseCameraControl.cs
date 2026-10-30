using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TowerDefenseCameraControl : Node2D
{
	private const float MinimumBattleZoom = 0.4f;

	private const float MaximumBattleZoom = 2f;

	private const float MouseWheelZoomFactor = 1.12f;

	private const float MinimumPinchDistance = 4f;

	public Marker2D cameraBeginMarker;

	public Marker2D cameraRightViewMarker;

	public Marker2D cameraPreViewMarker;

	public Camera2D camera;

	public Marker2D downRightMarker;

	public Vector2 size;

	private Viewport _viewport;

	private TowerDefenseMapConfig _mapConfig;

	private bool _battleZoomEnabled;

	private bool _cameraStateInitialized;

	private readonly Dictionary<int, Vector2> _touchPositions = new Dictionary<int, Vector2>();

	private float _previousPinchDistance;

	private Vector2 _previousPinchCenter;

	private bool _pinchActive;

	private bool _mousePanActive;

	private Transform2D _publishedViewportCanvasTransform;

	private bool _hasPublishedViewportCanvasTransform;

	private MouseButton _mousePanButton;

	private Vector2 _previousMousePanPosition;

	public bool BattleZoomEnabled => _battleZoomEnabled;

	public bool IsMousePanActive => _mousePanActive;

	public override void _Ready()
	{
		cameraBeginMarker = GetNode<Marker2D>("%CameraBeginMarker");
		cameraRightViewMarker = GetNode<Marker2D>("%CameraRightViewMarker");
		cameraPreViewMarker = GetNode<Marker2D>("%CameraPreViewMarker");
		camera = GetNode<Camera2D>("%Camera");
		downRightMarker = GetNode<Marker2D>("%DownRightMarker");
		_viewport = GetViewport();
		if (GodotObject.IsInstanceValid(_viewport))
		{
			_viewport.SizeChanged += OnViewportSizeChanged;
		}
		if (GodotObject.IsInstanceValid(_mapConfig))
		{
			ApplyMapConfig(_mapConfig);
		}
		else
		{
			ApplySize();
		}
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_viewport))
		{
			_viewport.SizeChanged -= OnViewportSizeChanged;
		}
		_viewport = null;
		_hasPublishedViewportCanvasTransform = false;
		_cameraStateInitialized = false;
		ResetMousePan();
		ResetTouchGesture();
	}

	public override void _Process(double delta)
	{
		if (GodotObject.IsInstanceValid(_viewport))
		{
			Transform2D canvasTransform = _viewport.GetCanvasTransform();
			if (!_hasPublishedViewportCanvasTransform || !(_publishedViewportCanvasTransform == canvasTransform))
			{
				_publishedViewportCanvasTransform = canvasTransform;
				_hasPublishedViewportCanvasTransform = true;
				AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
			}
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is InputEventScreenTouch || inputEvent is InputEventScreenDrag)
		{
			if (!CanProcessBattleZoomInput())
			{
				ResetTouchGesture();
			}
			else
			{
				HandleTouchInput(inputEvent);
			}
		}
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!Global.Instance.isMobile)
		{
			if (inputEvent is InputEventMouseButton mouseButton)
			{
				HandleMousePanButton(mouseButton);
				HandleMouseWheelInput(mouseButton);
			}
			else if (inputEvent is InputEventMouseMotion mouseMotion)
			{
				HandleMousePanMotion(mouseMotion);
			}
		}
	}

	private void HandleMousePanButton(InputEventMouseButton mouseButton)
	{
		if (!mouseButton.Pressed && _mousePanActive && mouseButton.ButtonIndex == _mousePanButton)
		{
			ResetMousePan();
			_viewport.SetInputAsHandled();
		}
		else if (mouseButton.Pressed && CanProcessBattleZoomInput() && !IsPointerOverBattleInterface(mouseButton.Position) && IsMousePanButtonAvailable(mouseButton.ButtonIndex))
		{
			_mousePanActive = true;
			_mousePanButton = mouseButton.ButtonIndex;
			_previousMousePanPosition = mouseButton.Position;
			_viewport.SetInputAsHandled();
		}
	}

	private void HandleMousePanMotion(InputEventMouseMotion mouseMotion)
	{
		if (_mousePanActive && CanProcessBattleZoomInput())
		{
			Vector2 vector = mouseMotion.Position - _previousMousePanPosition;
			camera.GlobalPosition -= vector / camera.Zoom;
			_previousMousePanPosition = mouseMotion.Position;
			ClampCameraToMap();
			_viewport.SetInputAsHandled();
		}
	}

	private static bool IsMousePanButtonAvailable(MouseButton button)
	{
		switch (button)
		{
		case MouseButton.Right:
		case MouseButton.Middle:
			return true;
		default:
			return false;
		case MouseButton.Left:
		{
			if (GodotObject.IsInstanceValid((TowerDefenseManager.Instance?.currentControl)?.GetFeature("GemMatch")))
			{
				return false;
			}
			TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
			if (GodotObject.IsInstanceValid(mapFeature?.packetPickControl))
			{
				return !mapFeature.packetPickControl.IsPicking();
			}
			return true;
		}
		}
	}

	private bool IsPointerOverBattleInterface(Vector2 screenPosition)
	{
		Control control = _viewport?.GuiGetHoveredControl();
		if (!GodotObject.IsInstanceValid(control) || !control.GetGlobalRect().HasPoint(screenPosition))
		{
			return false;
		}
		Control control2 = control;
		while (GodotObject.IsInstanceValid(control2))
		{
			bool flag = control2.MouseFilter != Control.MouseFilterEnum.Ignore;
			if (flag)
			{
				bool flag2 = ((control2 is BaseButton || control2 is Range || control2 is LineEdit || control2 is TextEdit || control2 is ItemList || control2 is Tree || control2 is TabBar || control2 is ScrollContainer) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return true;
			}
			control2 = control2.GetParent() as Control;
		}
		return false;
	}

	private void ResetMousePan()
	{
		_mousePanActive = false;
		_mousePanButton = MouseButton.None;
		_previousMousePanPosition = Vector2.Zero;
	}

	private void HandleMouseWheelInput(InputEventMouseButton mouseButton)
	{
		if (CanProcessBattleZoomInput() && mouseButton.Pressed && !IsPointerOverBattleInterface(mouseButton.Position) && (mouseButton.ButtonIndex == MouseButton.WheelUp || mouseButton.ButtonIndex == MouseButton.WheelDown))
		{
			float y = Mathf.Max(1f, mouseButton.Factor);
			float x = ((mouseButton.ButtonIndex == MouseButton.WheelUp) ? 1.12f : (25f / 28f));
			float targetZoom = camera.Zoom.X * Mathf.Pow(x, y);
			ApplyZoomAtScreenAnchor(targetZoom, mouseButton.Position, mouseButton.Position);
			_viewport.SetInputAsHandled();
		}
	}

	public void ApplyMapConfig(TowerDefenseMapConfig mapConfig)
	{
		if (GodotObject.IsInstanceValid(mapConfig))
		{
			bool num = _mapConfig != mapConfig || !_cameraStateInitialized;
			_mapConfig = mapConfig;
			size = mapConfig.mapSize;
			_battleZoomEnabled = mapConfig.enableBattleZoom;
			ResetMousePan();
			ResetTouchGesture();
			if (num && GodotObject.IsInstanceValid(camera))
			{
				camera.Zoom = Vector2.One * (_battleZoomEnabled ? 0.4f : 1f);
				camera.GlobalPosition = Vector2.Zero;
				_cameraStateInitialized = true;
			}
			ApplySize();
			ClampCameraToMap();
		}
	}

	private void OnViewportSizeChanged()
	{
		ApplySize();
		ClampCameraToMap();
	}

	private void ApplySize()
	{
		if (GodotObject.IsInstanceValid(cameraBeginMarker) && GodotObject.IsInstanceValid(cameraRightViewMarker) && GodotObject.IsInstanceValid(cameraPreViewMarker) && GodotObject.IsInstanceValid(downRightMarker) && GodotObject.IsInstanceValid(camera) && GodotObject.IsInstanceValid(_viewport) && !(size.X <= 0f))
		{
			float num = _viewport.GetVisibleRect().Size.X / Mathf.Max(camera.Zoom.X, 0.001f);
			float num2 = ClampCameraAxis(0f, 0f, size.X, num);
			float num3 = ((num >= size.X) ? num2 : (size.X - num));
			float x = Mathf.Clamp(num3 - 56f, num2, num3);
			cameraBeginMarker.GlobalPosition = new Vector2(num2, cameraBeginMarker.GlobalPosition.Y);
			cameraRightViewMarker.GlobalPosition = new Vector2(num3, cameraRightViewMarker.GlobalPosition.Y);
			cameraPreViewMarker.GlobalPosition = new Vector2(x, cameraPreViewMarker.GlobalPosition.Y);
			downRightMarker.GlobalPosition = size;
		}
	}

	private bool CanProcessBattleZoomInput()
	{
		if (!_battleZoomEnabled || !GodotObject.IsInstanceValid(camera) || !GodotObject.IsInstanceValid(_viewport))
		{
			return false;
		}
		TowerDefenseControlNew towerDefenseControlNew = TowerDefenseManager.Instance?.currentControl;
		if (GodotObject.IsInstanceValid(towerDefenseControlNew) && towerDefenseControlNew.isGameRunning)
		{
			return !towerDefenseControlNew.isGameFail;
		}
		return false;
	}

	private void HandleTouchInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventScreenTouch inputEventScreenTouch)
		{
			if (inputEventScreenTouch.Pressed)
			{
				if (!IsPointerOverBattleInterface(inputEventScreenTouch.Position))
				{
					_touchPositions[inputEventScreenTouch.Index] = inputEventScreenTouch.Position;
				}
			}
			else
			{
				_touchPositions.Remove(inputEventScreenTouch.Index);
			}
			RefreshPinchBaseline();
			return;
		}
		InputEventScreenDrag inputEventScreenDrag = (InputEventScreenDrag)inputEvent;
		if (!_touchPositions.ContainsKey(inputEventScreenDrag.Index))
		{
			return;
		}
		_touchPositions[inputEventScreenDrag.Index] = inputEventScreenDrag.Position;
		if (_touchPositions.Count != 2)
		{
			RefreshPinchBaseline();
			return;
		}
		GetPinchGeometry(out var distance, out var center);
		if (!_pinchActive || _previousPinchDistance < 4f || distance < 4f)
		{
			_previousPinchDistance = distance;
			_previousPinchCenter = center;
			_pinchActive = distance >= 4f;
		}
		else
		{
			float targetZoom = camera.Zoom.X * distance / _previousPinchDistance;
			ApplyZoomAtScreenAnchor(targetZoom, center, _previousPinchCenter);
			_previousPinchDistance = distance;
			_previousPinchCenter = center;
			_viewport.SetInputAsHandled();
		}
	}

	private void RefreshPinchBaseline()
	{
		if (_touchPositions.Count != 2)
		{
			_pinchActive = false;
			_previousPinchDistance = 0f;
			_previousPinchCenter = Vector2.Zero;
		}
		else
		{
			GetPinchGeometry(out _previousPinchDistance, out _previousPinchCenter);
			_pinchActive = _previousPinchDistance >= 4f;
		}
	}

	private void GetPinchGeometry(out float distance, out Vector2 center)
	{
		using Dictionary<int, Vector2>.ValueCollection.Enumerator enumerator = _touchPositions.Values.GetEnumerator();
		enumerator.MoveNext();
		Vector2 current = enumerator.Current;
		enumerator.MoveNext();
		Vector2 current2 = enumerator.Current;
		distance = current.DistanceTo(current2);
		center = (current + current2) * 0.5f;
	}

	private void ApplyZoomAtScreenAnchor(float targetZoom, Vector2 currentScreenAnchor, Vector2 previousScreenAnchor)
	{
		float num = Mathf.Max(camera.Zoom.X, 0.001f);
		float num2 = Mathf.Clamp(targetZoom, 0.4f, 2f);
		Vector2 vector = camera.GlobalPosition + previousScreenAnchor / num;
		camera.Zoom = Vector2.One * num2;
		camera.GlobalPosition = vector - currentScreenAnchor / num2;
		ApplySize();
		ClampCameraToMap();
	}

	private void ClampCameraToMap()
	{
		if (GodotObject.IsInstanceValid(camera) && GodotObject.IsInstanceValid(_viewport) && !(size.X <= 0f) && !(size.Y <= 0f))
		{
			Vector2 vector = _viewport.GetVisibleRect().Size / camera.Zoom;
			camera.GlobalPosition = new Vector2(ClampCameraAxis(camera.GlobalPosition.X, 0f, size.X, vector.X), ClampCameraAxis(camera.GlobalPosition.Y, 0f, size.Y, vector.Y));
		}
	}

	private static float ClampCameraAxis(float position, float mapStart, float mapLength, float visibleLength)
	{
		if (visibleLength >= mapLength)
		{
			return mapStart + (mapLength - visibleLength) * 0.5f;
		}
		return Mathf.Clamp(position, mapStart, mapStart + mapLength - visibleLength);
	}

	private void ResetTouchGesture()
	{
		_touchPositions.Clear();
		_pinchActive = false;
		_previousPinchDistance = 0f;
		_previousPinchCenter = Vector2.Zero;
	}
}
