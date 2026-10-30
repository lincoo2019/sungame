using Godot;

public sealed class MousePressComponent : CharacterComponentRuntime
{
	public delegate void PressedEventHandler(Vector2 pos);

	public delegate void DoublePressedEventHandler(Vector2 pos);

	public delegate void ReleasedEventHandler(Vector2 pos);

	public delegate void FinishPressedEventHandler(Vector2 pos);

	private const ulong EmulatedMouseSuppressionMsec = 250uL;

	private const float EmulatedMousePositionToleranceSquared = 64f;

	private static MousePressComponent _cursorOwner;

	private TowerDefenseCharacter _parent;

	private StringName _pressAction = "Press";

	private StringName _releaseAction = "Release";

	private float _doublePressWindow = 0.25f;

	private float _interactionCooldown = 0.1f;

	private float _hoverBrightness = 0.3f;

	private StringName _brightnessShaderParameter = "brightStrength";

	private bool _enableDoublePress = true;

	private bool _requireGroundForFinish = true;

	private bool _followPointerWhileAiming = true;

	private bool _hostAuthorityOnly = true;

	private Control.CursorShape _enabledCursorShape = Control.CursorShape.PointingHand;

	private Control.CursorShape _disabledCursorShape;

	private bool _inputReady;

	private bool _cooldown;

	private bool _doublePressBuffer;

	private bool _configured;

	private ulong _inputVersion;

	private ulong _suppressMouseUntilMsec;

	private bool _touchInputObserved;

	private bool _mousePressStartedInteraction;

	private ulong _mousePressMsec;

	private Vector2 _mousePressPosition;

	private int _capturedTouchIndex = -1;

	private bool _finishCapturedTouchAimOnRelease;

	internal override bool WantsInput => true;

	public AabbShape2DResource ClickShape { get; private set; }

	public bool ToggleMode { get; private set; }

	public Node2D ToggleTarget { get; private set; }

	public bool IsMouseInside { get; private set; }

	public bool IsPressed { get; private set; }

	private MousePressComponentDefinition Definition => base.ComponentDefinition as MousePressComponentDefinition;

	public event PressedEventHandler OnPressed;

	public event DoublePressedEventHandler OnDoublePressed;

	public new event ReleasedEventHandler OnReleased;

	public event FinishPressedEventHandler OnFinishPressed;

	protected override void OnBound()
	{
		_parent = base.Owner;
		ApplyDefinitionOnce();
		ResolveReferences();
		_inputReady = false;
	}

	protected override void OnActivated()
	{
		ulong version = ++_inputVersion;
		Callable.From(delegate
		{
			EnableInputDeferred(version);
		}).CallDeferred();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		InvalidatePendingInput();
		ResetInteraction(clearBrightness: true);
		ToggleTarget = null;
		_parent = null;
	}

	protected override void OnRuntimeReleased()
	{
		InvalidatePendingInput();
		ResetInteraction(clearBrightness: true);
		this.OnPressed = null;
		this.OnDoublePressed = null;
		this.OnReleased = null;
		this.OnFinishPressed = null;
		ClickShape = null;
		ToggleTarget = null;
		_parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			InvalidatePendingInput();
			ResetInteraction(clearBrightness: true);
		}
		else if (base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			_inputReady = true;
		}
		RefreshCursor();
	}

	private void ApplyDefinitionOnce()
	{
		if (!_configured && Definition != null)
		{
			MousePressComponentDefinition definition = Definition;
			ClickShape = definition.clickShape;
			ToggleMode = definition.toggleMode;
			_pressAction = definition.pressAction;
			_releaseAction = definition.releaseAction;
			_doublePressWindow = Mathf.Max(0f, definition.doublePressWindow);
			_interactionCooldown = Mathf.Max(0f, definition.interactionCooldown);
			_hoverBrightness = definition.hoverBrightness;
			_brightnessShaderParameter = definition.brightnessShaderParameter;
			_enableDoublePress = definition.enableDoublePress;
			_requireGroundForFinish = definition.requireGroundForFinish;
			_followPointerWhileAiming = definition.followPointerWhileAiming;
			_hostAuthorityOnly = definition.hostAuthorityOnly;
			_enabledCursorShape = definition.enabledCursorShape;
			_disabledCursorShape = definition.disabledCursorShape;
			_configured = true;
		}
	}

	private void ResolveReferences()
	{
		MousePressComponentDefinition definition = Definition;
		if (GodotObject.IsInstanceValid(_parent) && definition != null)
		{
			ToggleTarget = ResolveOwnerNode<Node2D>(definition.toggleTargetPath);
		}
	}

	private T ResolveOwnerNode<T>(NodePath path) where T : Node
	{
		if (!(path == null) && !path.IsEmpty && GodotObject.IsInstanceValid(_parent))
		{
			return _parent.GetNodeOrNull<T>(path);
		}
		return null;
	}

	private void EnableInputDeferred(ulong version)
	{
		if (version == _inputVersion && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(_parent) && _parent.IsInsideTree())
		{
			_inputReady = true;
		}
	}

	private void RefreshCursor()
	{
		if (_cursorOwner == this && IsMouseInside)
		{
			Input.SetDefaultCursorShape(ToInputCursorShape(base.Alive ? _enabledCursorShape : _disabledCursorShape));
		}
	}

	internal override void ProcessInput(InputEvent inputEvent)
	{
		if (inputEvent == null || inputEvent.IsEcho())
		{
			return;
		}
		if (inputEvent is InputEventScreenTouch touch)
		{
			ProcessTouch(touch);
		}
		else if (inputEvent is InputEventScreenDrag drag)
		{
			ProcessTouchDrag(drag);
		}
		else
		{
			if (inputEvent is InputEventMouse && (_touchInputObserved || (_suppressMouseUntilMsec != 0 && Time.GetTicksMsec() <= _suppressMouseUntilMsec)))
			{
				return;
			}
			if (inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton && IsPointerOverInteractiveInterface(inputEventMouseButton.Position))
			{
				SetMouseInside(inside: false);
				return;
			}
			if (inputEvent is InputEventMouse)
			{
				UpdateMouseHover(inputEvent);
			}
			if (!CanInteract())
			{
				return;
			}
			Vector2 vector = ResolvePointerCanvasPosition(inputEvent);
			UpdateAimTarget(vector);
			if (!_pressAction.IsEmpty && inputEvent.IsActionPressed(_pressAction))
			{
				bool flag = IsPointerInsideClickShape(inputEvent);
				bool flag2 = ToggleMode && IsPressed;
				bool flag3 = flag && ((ToggleMode && !flag2) || (!ToggleMode && !_doublePressBuffer));
				HandlePress(vector, flag);
				if (inputEvent is InputEventMouse && flag3 && ((ToggleMode && IsPressed) || (!ToggleMode && _doublePressBuffer)))
				{
					_mousePressStartedInteraction = true;
					_mousePressMsec = Time.GetTicksMsec();
					_mousePressPosition = vector;
				}
			}
			else if (ToggleMode && IsPressed && !_releaseAction.IsEmpty && inputEvent.IsActionReleased(_releaseAction))
			{
				CancelAim(vector);
			}
		}
	}

	private bool IsPointerOverInteractiveInterface(Vector2 screenPosition)
	{
		Control control = _parent?.GetViewport()?.GuiGetHoveredControl();
		if (!GodotObject.IsInstanceValid(control) || !control.GetGlobalRect().HasPoint(screenPosition))
		{
			return false;
		}
		while (GodotObject.IsInstanceValid(control))
		{
			bool flag = control.MouseFilter != Control.MouseFilterEnum.Ignore;
			if (flag)
			{
				bool flag2 = ((control is BaseButton || control is Range || control is LineEdit || control is TextEdit || control is ItemList || control is Tree || control is TabBar || control is ScrollContainer) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return true;
			}
			control = control.GetParent() as Control;
		}
		return false;
	}

	private void ProcessTouch(InputEventScreenTouch touch)
	{
		_touchInputObserved = true;
		_suppressMouseUntilMsec = Time.GetTicksMsec() + 250;
		if (!touch.Pressed)
		{
			if (touch.Index == _capturedTouchIndex)
			{
				Vector2 pointerPosition = ResolvePointerCanvasPosition(touch);
				bool num = _finishCapturedTouchAimOnRelease && CanInteract() && ToggleMode && IsPressed;
				_capturedTouchIndex = -1;
				_finishCapturedTouchAimOnRelease = false;
				if (num)
				{
					UpdateAimTarget(pointerPosition);
					FinishAim(pointerPosition);
				}
			}
		}
		else
		{
			if (!CanInteract() || _capturedTouchIndex >= 0)
			{
				return;
			}
			bool flag = ToggleMode && IsPressed;
			bool flag2 = IsPointerInsideClickShape(touch);
			if (!flag2 && !flag)
			{
				return;
			}
			_capturedTouchIndex = touch.Index;
			Vector2 vector = ResolvePointerCanvasPosition(touch);
			UpdateAimTarget(vector);
			if (ConsumeMouseInitiatedInteractionDuplicate(vector))
			{
				_finishCapturedTouchAimOnRelease = false;
				return;
			}
			if (flag)
			{
				_finishCapturedTouchAimOnRelease = true;
				return;
			}
			_finishCapturedTouchAimOnRelease = false;
			if (!TryHandleNativeTouchDoublePress(touch, vector, flag2))
			{
				HandlePress(vector, flag2);
			}
		}
	}

	private void ProcessTouchDrag(InputEventScreenDrag drag)
	{
		_touchInputObserved = true;
		_suppressMouseUntilMsec = Time.GetTicksMsec() + 250;
		if (CanInteract() && drag.Index == _capturedTouchIndex)
		{
			UpdateAimTarget(ResolvePointerCanvasPosition(drag));
		}
	}

	private bool ConsumeMouseInitiatedInteractionDuplicate(Vector2 touchPosition)
	{
		if (!_mousePressStartedInteraction)
		{
			return false;
		}
		_mousePressStartedInteraction = false;
		ulong ticksMsec = Time.GetTicksMsec();
		if (ticksMsec >= _mousePressMsec && ticksMsec - _mousePressMsec <= 250)
		{
			return _mousePressPosition.DistanceSquaredTo(touchPosition) <= 64f;
		}
		return false;
	}

	private bool TryHandleNativeTouchDoublePress(InputEventScreenTouch touch, Vector2 touchPosition, bool pointerInside)
	{
		if (!touch.DoubleTap || ToggleMode || !_enableDoublePress || !pointerInside)
		{
			return false;
		}
		_doublePressBuffer = false;
		this.OnDoublePressed?.Invoke(touchPosition);
		StartCooldown();
		return true;
	}

	private void UpdateAimTarget(Vector2 pointerPosition)
	{
		if (ToggleMode && IsPressed && _followPointerWhileAiming && GodotObject.IsInstanceValid(ToggleTarget))
		{
			ToggleTarget.Visible = true;
			ToggleTarget.GlobalPosition = pointerPosition;
		}
	}

	private void HandlePress(Vector2 pointerPosition, bool pointerInside)
	{
		if (_cooldown)
		{
			return;
		}
		if (ToggleMode && IsPressed)
		{
			FinishAim(pointerPosition);
		}
		else
		{
			if (!pointerInside)
			{
				return;
			}
			if (!ToggleMode && _doublePressBuffer)
			{
				_doublePressBuffer = false;
				this.OnDoublePressed?.Invoke(pointerPosition);
				StartCooldown();
				return;
			}
			if (ToggleMode)
			{
				IsPressed = true;
				UpdateAimTarget(pointerPosition);
			}
			this.OnPressed?.Invoke(pointerPosition);
			if (!ToggleMode && _enableDoublePress && _doublePressWindow > 0f)
			{
				StartDoublePressWindow();
			}
			else if (!ToggleMode)
			{
				StartCooldown();
			}
		}
	}

	private void FinishAim(Vector2 pointerPosition)
	{
		HideToggleTarget();
		IsPressed = false;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (_requireGroundForFinish && (!GodotObject.IsInstanceValid(instance) || !instance.GetGroundRect().HasPoint(pointerPosition)))
		{
			this.OnReleased?.Invoke(pointerPosition);
			StartCooldown();
		}
		else
		{
			this.OnFinishPressed?.Invoke(pointerPosition);
			SetBrightness(0f);
			StartCooldown();
		}
	}

	private void CancelAim(Vector2 pointerPosition)
	{
		HideToggleTarget();
		IsPressed = false;
		this.OnReleased?.Invoke(pointerPosition);
		StartCooldown();
	}

	private void StartDoublePressWindow()
	{
		_doublePressBuffer = true;
		ulong version = ++_inputVersion;
		SceneTree sceneTree = (GodotObject.IsInstanceValid(_parent) ? _parent.GetTree() : null);
		if (!GodotObject.IsInstanceValid(sceneTree))
		{
			_doublePressBuffer = false;
			return;
		}
		sceneTree.CreateTimer(_doublePressWindow, processAlways: false).Timeout += delegate
		{
			if (version == _inputVersion)
			{
				_doublePressBuffer = false;
			}
		};
	}

	private void StartCooldown()
	{
		_doublePressBuffer = false;
		ulong version = ++_inputVersion;
		float num = Mathf.Max(0f, _interactionCooldown);
		if (num <= 0f)
		{
			_cooldown = false;
			return;
		}
		_cooldown = true;
		SceneTree sceneTree = (GodotObject.IsInstanceValid(_parent) ? _parent.GetTree() : null);
		if (!GodotObject.IsInstanceValid(sceneTree))
		{
			_cooldown = false;
			return;
		}
		sceneTree.CreateTimer(num, processAlways: false).Timeout += delegate
		{
			if (version == _inputVersion)
			{
				_cooldown = false;
			}
		};
	}

	private void UpdateMouseHover(InputEvent inputEvent)
	{
		bool mouseInside = CanHighlight() && IsPointerInsideClickShape(inputEvent);
		SetMouseInside(mouseInside);
	}

	private void SetMouseInside(bool inside)
	{
		if (IsMouseInside != inside)
		{
			IsMouseInside = inside;
			SetBrightness(inside ? _hoverBrightness : 0f);
			if (inside)
			{
				_cursorOwner = this;
				Input.SetDefaultCursorShape(ToInputCursorShape(base.Alive ? _enabledCursorShape : _disabledCursorShape));
			}
			else if (_cursorOwner == this)
			{
				_cursorOwner = null;
				Input.SetDefaultCursorShape(ToInputCursorShape(_disabledCursorShape));
			}
		}
	}

	private bool CanHighlight()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(_parent) && _parent.inGame)
		{
			return _parent.componentAlive;
		}
		return false;
	}

	private bool CanInteract()
	{
		if (!_inputReady || _cooldown || !base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(_parent) || !_parent.inGame || !_parent.componentAlive)
		{
			return false;
		}
		if (Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage")
		{
			return false;
		}
		if (_hostAuthorityOnly && Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.IsGameRunning();
		}
		return false;
	}

	private bool IsPointerInsideClickShape(InputEvent inputEvent)
	{
		if (!TryGetScreenPosition(inputEvent, out var screenPosition))
		{
			return IsMouseInside;
		}
		return IsScreenPositionInsideClickShape(screenPosition);
	}

	public bool IsScreenPositionInsideClickShape(Vector2 screenPosition)
	{
		if (!screenPosition.IsFinite() || !GodotObject.IsInstanceValid(_parent) || !TryGetClickShapeWorldRect(out var worldRect))
		{
			return false;
		}
		Vector2 point = _parent.GetCanvasTransform().AffineInverse() * screenPosition;
		return worldRect.HasPoint(point);
	}

	public bool TryGetClickShapeWorldRect(out Rect2 worldRect)
	{
		worldRect = default(Rect2);
		if (GodotObject.IsInstanceValid(ClickShape) && GodotObject.IsInstanceValid(_parent))
		{
			return ClickShape.TryGetWorldRect(_parent.GetLogicalGlobalTransform(_parent), out worldRect);
		}
		return false;
	}

	public bool TryGetClickShapeScreenCenter(out Vector2 screenCenter)
	{
		screenCenter = default(Vector2);
		if (!GodotObject.IsInstanceValid(_parent) || !TryGetClickShapeWorldRect(out var worldRect))
		{
			return false;
		}
		screenCenter = _parent.GetCanvasTransform() * worldRect.GetCenter();
		return screenCenter.IsFinite();
	}

	private Vector2 ResolvePointerCanvasPosition(InputEvent inputEvent)
	{
		if (TryGetScreenPosition(inputEvent, out var screenPosition) && GodotObject.IsInstanceValid(_parent))
		{
			return _parent.GetCanvasTransform().AffineInverse() * screenPosition;
		}
		if (!GodotObject.IsInstanceValid(_parent))
		{
			return Vector2.Zero;
		}
		return _parent.GetGlobalMousePosition();
	}

	private static bool TryGetScreenPosition(InputEvent inputEvent, out Vector2 screenPosition)
	{
		if (!(inputEvent is InputEventScreenTouch inputEventScreenTouch))
		{
			if (!(inputEvent is InputEventScreenDrag inputEventScreenDrag))
			{
				if (inputEvent is InputEventMouse inputEventMouse)
				{
					screenPosition = inputEventMouse.Position;
					return true;
				}
				screenPosition = default(Vector2);
				return false;
			}
			screenPosition = inputEventScreenDrag.Position;
			return true;
		}
		screenPosition = inputEventScreenTouch.Position;
		return true;
	}

	private void InvalidatePendingInput()
	{
		_inputVersion++;
		_inputReady = false;
		_cooldown = false;
		_doublePressBuffer = false;
		_capturedTouchIndex = -1;
		_finishCapturedTouchAimOnRelease = false;
		_suppressMouseUntilMsec = 0uL;
		_touchInputObserved = false;
		_mousePressStartedInteraction = false;
		_mousePressMsec = 0uL;
		_mousePressPosition = Vector2.Zero;
	}

	private void ResetInteraction(bool clearBrightness)
	{
		IsPressed = false;
		bool isMouseInside = IsMouseInside;
		IsMouseInside = false;
		HideToggleTarget();
		if (clearBrightness)
		{
			SetBrightness(0f);
		}
		if (isMouseInside && _cursorOwner == this)
		{
			_cursorOwner = null;
			Input.SetDefaultCursorShape(ToInputCursorShape(_disabledCursorShape));
		}
	}

	private void HideToggleTarget()
	{
		if (GodotObject.IsInstanceValid(ToggleTarget))
		{
			ToggleTarget.Visible = false;
		}
	}

	private static Input.CursorShape ToInputCursorShape(Control.CursorShape cursorShape)
	{
		return (Input.CursorShape)(int)cursorShape;
	}

	private void SetBrightness(float value)
	{
		if (GodotObject.IsInstanceValid(_parent) && !_brightnessShaderParameter.IsEmpty)
		{
			_parent.SetSpriteGroupShaderParameter(_brightnessShaderParameter.ToString(), value);
		}
	}
}
