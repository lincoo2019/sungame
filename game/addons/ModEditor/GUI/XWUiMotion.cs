using System;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.GUI;

public partial class XWUiMotion : Node
{
	public enum MotionRole
	{
		NavigationButton,
		Card,
		Panel
	}

	private const string BindingNodeName = "__XWUiMotion";

	private static bool _reducedMotion;

	private static bool _lowPerformanceMode;

	private Control _target;

	private BaseButton _button;

	private Window _hostWindow;

	private ModEditorManager _editorManager;

	private MotionRole _role;

	private Tween _activeTween;

	private Vector2 _restScale = Vector2.One;

	private Vector2 _restPosition;

	private Color _restSelfModulate = Colors.White;

	private bool _pointerInside;

	private bool _initialized;

	private bool _suspended;

	public static bool ReducedMotion => _reducedMotion;

	public static bool LowPerformanceMode => _lowPerformanceMode;

	public static bool MotionAllowed
	{
		get
		{
			if (!_reducedMotion)
			{
				return !_lowPerformanceMode;
			}
			return false;
		}
	}

	public MotionRole Role => _role;

	public bool IsAnimating => GodotObject.IsInstanceValid(_activeTween);

	public bool IsSuspended => _suspended;

	public int AnimationStartCount { get; private set; }

	public int HiddenCancelCount { get; private set; }

	public int ImmediateStateCount { get; private set; }

	private static event Action MotionPolicyChanged;

	public static void ConfigurePolicy(bool reducedMotion, bool lowPerformanceMode)
	{
		if (_reducedMotion != reducedMotion || _lowPerformanceMode != lowPerformanceMode)
		{
			_reducedMotion = reducedMotion;
			_lowPerformanceMode = lowPerformanceMode;
			XWUiMotion.MotionPolicyChanged?.Invoke();
		}
	}

	public static void SetReducedMotion(bool reducedMotion)
	{
		ConfigurePolicy(reducedMotion, _lowPerformanceMode);
	}

	public static void SetLowPerformanceMode(bool lowPerformanceMode)
	{
		ConfigurePolicy(_reducedMotion, lowPerformanceMode);
	}

	public static XWUiMotion BindButton(BaseButton button, MotionRole role = MotionRole.NavigationButton)
	{
		if (!GodotObject.IsInstanceValid(button))
		{
			return null;
		}
		XWUiMotion xWUiMotion = FindBinding(button);
		if (xWUiMotion != null)
		{
			return xWUiMotion;
		}
		XWUiMotion xWUiMotion2 = new XWUiMotion
		{
			Name = "__XWUiMotion"
		};
		xWUiMotion2.Initialize(button, role);
		button.AddChild(xWUiMotion2, forceReadableName: false, InternalMode.Disabled);
		return xWUiMotion2;
	}

	public static XWUiMotion BindPanel(Control panel)
	{
		if (!GodotObject.IsInstanceValid(panel))
		{
			return null;
		}
		XWUiMotion xWUiMotion = FindBinding(panel);
		if (xWUiMotion != null)
		{
			return xWUiMotion;
		}
		XWUiMotion xWUiMotion2 = new XWUiMotion
		{
			Name = "__XWUiMotion"
		};
		xWUiMotion2.Initialize(panel, MotionRole.Panel);
		panel.AddChild(xWUiMotion2, forceReadableName: false, InternalMode.Disabled);
		return xWUiMotion2;
	}

	public static XWUiMotion FindBinding(Control target)
	{
		return target?.GetNodeOrNull<XWUiMotion>("__XWUiMotion");
	}

	private void Initialize(Control target, MotionRole role)
	{
		_target = target;
		_button = target as BaseButton;
		_role = role;
		_restScale = target.Scale;
		_restPosition = target.Position;
		_restSelfModulate = target.SelfModulate;
		_initialized = true;
	}

	public override void _Ready()
	{
		if (_initialized && GodotObject.IsInstanceValid(_target))
		{
			_target.PivotOffset = _target.Size * 0.5f;
			_target.Resized += UpdatePivot;
			_target.VisibilityChanged += OnVisibilityChanged;
			MotionPolicyChanged += OnMotionPolicyChanged;
			_hostWindow = FindHostWindow(_target);
			if (GodotObject.IsInstanceValid(_hostWindow))
			{
				_hostWindow.VisibilityChanged += OnVisibilityChanged;
			}
			_editorManager = ModEditorManager.Instance;
			if (GodotObject.IsInstanceValid(_editorManager))
			{
				_editorManager.EditorOpened += ResumeVisibleHost;
				_editorManager.EditorClosed += SuspendForHiddenHost;
			}
			if (GodotObject.IsInstanceValid(_button))
			{
				_button.MouseEntered += OnMouseEntered;
				_button.MouseExited += OnMouseExited;
				_button.ButtonDown += OnButtonDown;
				_button.ButtonUp += OnButtonUp;
				_button.Toggled += OnButtonToggled;
				ApplyButtonState(animate: false);
			}
			OnVisibilityChanged();
		}
	}

	public override void _ExitTree()
	{
		StopTween(restore: false);
		MotionPolicyChanged -= OnMotionPolicyChanged;
		if (GodotObject.IsInstanceValid(_target))
		{
			_target.Resized -= UpdatePivot;
			_target.VisibilityChanged -= OnVisibilityChanged;
		}
		if (GodotObject.IsInstanceValid(_hostWindow))
		{
			_hostWindow.VisibilityChanged -= OnVisibilityChanged;
		}
		if (GodotObject.IsInstanceValid(_editorManager))
		{
			_editorManager.EditorOpened -= ResumeVisibleHost;
			_editorManager.EditorClosed -= SuspendForHiddenHost;
		}
		if (GodotObject.IsInstanceValid(_button))
		{
			_button.MouseEntered -= OnMouseEntered;
			_button.MouseExited -= OnMouseExited;
			_button.ButtonDown -= OnButtonDown;
			_button.ButtonUp -= OnButtonUp;
			_button.Toggled -= OnButtonToggled;
		}
		base._ExitTree();
	}

	public void SyncButtonState(bool animate = true)
	{
		if (GodotObject.IsInstanceValid(_button))
		{
			ApplyButtonState(animate);
		}
	}

	public void PlayPanelReveal(float slidePixels = 10f)
	{
		if (_role != MotionRole.Panel || !CanAnimate())
		{
			RestorePanelState();
			ImmediateStateCount++;
			return;
		}
		bool num = GodotObject.IsInstanceValid(_activeTween);
		StopTween(restore: false);
		if (num)
		{
			RestorePanelState();
		}
		else
		{
			_restPosition = _target.Position;
		}
		_target.Position = _restPosition + new Vector2(0f, Mathf.Max(0f, slidePixels));
		_target.SelfModulate = WithAlpha(_restSelfModulate, 0.28f);
		Tween tween = (_activeTween = _target.CreateTween());
		AnimationStartCount++;
		tween.SetParallel();
		tween.TweenProperty(_target, "position", _restPosition, 0.16).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(_target, "self_modulate", _restSelfModulate, 0.14).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.Finished += OnTweenFinished;
	}

	public void SuspendForHiddenHost()
	{
		if (!_suspended)
		{
			_suspended = true;
			if (GodotObject.IsInstanceValid(_activeTween))
			{
				HiddenCancelCount++;
				StopTween(restore: false);
			}
			RestoreVisualState();
		}
	}

	private void ResumeVisibleHost()
	{
		_suspended = false;
		RestoreVisualState();
	}

	private void OnVisibilityChanged()
	{
		if (GodotObject.IsInstanceValid(_target) && _target.IsVisibleInTree() && (!GodotObject.IsInstanceValid(_hostWindow) || _hostWindow.Visible))
		{
			ResumeVisibleHost();
		}
		else
		{
			SuspendForHiddenHost();
		}
	}

	private void OnMotionPolicyChanged()
	{
		StopTween(restore: false);
		RestoreVisualState();
		if (GodotObject.IsInstanceValid(_button))
		{
			ApplyButtonState(animate: false);
		}
	}

	private void UpdatePivot()
	{
		if (GodotObject.IsInstanceValid(_target))
		{
			_target.PivotOffset = _target.Size * 0.5f;
		}
	}

	private void OnMouseEntered()
	{
		_pointerInside = true;
		ApplyButtonState(animate: true);
	}

	private void OnMouseExited()
	{
		_pointerInside = false;
		ApplyButtonState(animate: true);
	}

	private void OnButtonDown()
	{
		if (GodotObject.IsInstanceValid(_button) && !_button.Disabled)
		{
			AnimateButtonTo(_restScale * 0.965f, new Color(1f, 0.91f, 0.7f, _restSelfModulate.A), 0.07);
		}
	}

	private void OnButtonUp()
	{
		ApplyButtonState(animate: true, rebound: true);
	}

	private void OnButtonToggled(bool pressed)
	{
		ApplyButtonState(animate: true, pressed);
	}

	private void ApplyButtonState(bool animate, bool rebound = false)
	{
		if (!GodotObject.IsInstanceValid(_button))
		{
			return;
		}
		int num;
		float num2;
		if (_button.ToggleMode)
		{
			num = (_button.ButtonPressed ? 1 : 0);
			if (num != 0)
			{
				num2 = 1.026f;
				goto IL_0057;
			}
		}
		else
		{
			num = 0;
		}
		num2 = ((!_pointerInside) ? 1f : ((_role == MotionRole.Card) ? 1.026f : 1.018f));
		goto IL_0057;
		IL_0057:
		float num3 = num2;
		Color color = ((num != 0) ? new Color(1f, 0.94f, 0.72f, _restSelfModulate.A) : (_pointerInside ? new Color(1f, 1f, 0.92f, _restSelfModulate.A) : _restSelfModulate));
		double duration = (rebound ? 0.15 : 0.11);
		if (!animate || !CanAnimate())
		{
			StopTween(restore: false);
			_target.Scale = _restScale;
			_target.SelfModulate = color;
			ImmediateStateCount++;
		}
		else
		{
			AnimateButtonTo(_restScale * num3, color, duration, rebound);
		}
	}

	private void AnimateButtonTo(Vector2 scale, Color tint, double duration, bool rebound = false)
	{
		if (!CanAnimate())
		{
			_target.Scale = _restScale;
			_target.SelfModulate = tint;
			ImmediateStateCount++;
			return;
		}
		StopTween(restore: false);
		Tween tween = (_activeTween = _target.CreateTween());
		AnimationStartCount++;
		tween.SetParallel();
		tween.TweenProperty(_target, "scale", scale, duration).SetTrans((Tween.TransitionType)(rebound ? 10 : 4)).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(_target, "self_modulate", tint, duration).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.Finished += OnTweenFinished;
	}

	private bool CanAnimate()
	{
		if (MotionAllowed && !_suspended && GodotObject.IsInstanceValid(_target) && _target.IsVisibleInTree())
		{
			if (GodotObject.IsInstanceValid(_hostWindow))
			{
				return _hostWindow.Visible;
			}
			return true;
		}
		return false;
	}

	private void StopTween(bool restore)
	{
		if (GodotObject.IsInstanceValid(_activeTween))
		{
			_activeTween.Kill();
		}
		_activeTween = null;
		if (restore)
		{
			RestoreVisualState();
		}
	}

	private void OnTweenFinished()
	{
		_activeTween = null;
	}

	private void RestoreVisualState()
	{
		if (GodotObject.IsInstanceValid(_target))
		{
			if (_role == MotionRole.Panel)
			{
				RestorePanelState();
				return;
			}
			_target.Scale = _restScale;
			_target.SelfModulate = _restSelfModulate;
		}
	}

	private void RestorePanelState()
	{
		if (GodotObject.IsInstanceValid(_target))
		{
			_target.Position = _restPosition;
			_target.Scale = _restScale;
			_target.SelfModulate = _restSelfModulate;
		}
	}

	private static Window FindHostWindow(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is Window result)
			{
				return result;
			}
			node2 = node2.GetParent();
		}
		return null;
	}

	private static Color WithAlpha(Color color, float alphaMultiplier)
	{
		return new Color(color.R, color.G, color.B, color.A * alphaMultiplier);
	}
}
