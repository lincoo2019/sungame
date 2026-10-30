using System;
using Godot;

[GlobalClass]
public partial class ViewManager : Node
{
	public static ViewManager Instance;

	private ColorRect _fullScreenColorRect;

	private Tween _fullScreenBlinkTween;

	private bool _cameraShakeRunning;

	private Vector2 _cameraShakeDirection;

	private double _cameraShakeForce;

	private double _cameraShakeInterval;

	private int _cameraShakeStepsRemaining;

	public override void _Ready()
	{
		Instance = this;
		_fullScreenColorRect = GetNode<ColorRect>("%FullScreenColorRect");
	}

	public void FullScreenColorBlink(Color color, double duration = 0.2, bool rise = true)
	{
		if (GodotObject.IsInstanceValid(_fullScreenColorRect))
		{
			duration = Math.Max(0.0, duration);
			if (GodotObject.IsInstanceValid(_fullScreenBlinkTween))
			{
				_fullScreenBlinkTween.Kill();
			}
			_fullScreenColorRect.Visible = true;
			_fullScreenColorRect.Color = color;
			_fullScreenBlinkTween = CreateTween();
			if (rise)
			{
				_fullScreenBlinkTween.TweenProperty(_fullScreenColorRect, "modulate:a", color.A, duration / 2.0).From(0f);
				_fullScreenBlinkTween.TweenProperty(_fullScreenColorRect, "modulate:a", 0f, duration / 2.0).From(color.A);
			}
			else
			{
				_fullScreenBlinkTween.TweenProperty(_fullScreenColorRect, "modulate:a", 0f, duration).From(color.A);
			}
			_fullScreenBlinkTween.TweenCallback(Callable.From(FinishFullScreenColorBlink));
		}
	}

	private void FinishFullScreenColorBlink()
	{
		if (GodotObject.IsInstanceValid(_fullScreenColorRect))
		{
			_fullScreenColorRect.Visible = false;
		}
		_fullScreenBlinkTween = null;
	}

	public void CameraShake(Vector2 dir, double force, double interval = 0.05, int time = 1)
	{
		force = Math.Max(0.0, force);
		interval = Math.Max(0.0, interval);
		time = Math.Max(0, time);
		if (!(force <= 0.0) && time != 0)
		{
			if (!dir.IsZeroApprox())
			{
				_cameraShakeDirection = dir.Normalized();
			}
			_cameraShakeForce = Math.Max(_cameraShakeForce, force);
			_cameraShakeInterval = (_cameraShakeRunning ? Math.Min(_cameraShakeInterval, interval) : interval);
			_cameraShakeStepsRemaining = Math.Max(_cameraShakeStepsRemaining, time);
			if (!_cameraShakeRunning)
			{
				_cameraShakeRunning = true;
				Callable.From(RunCameraShake).CallDeferred();
			}
		}
	}

	private async void RunCameraShake()
	{
		try
		{
			bool firstStep = true;
			while (_cameraShakeStepsRemaining > 0 && IsInsideTree())
			{
				Camera2D camera2D = GetViewport().GetCamera2D();
				if (GodotObject.IsInstanceValid(camera2D))
				{
					camera2D.Offset = (firstStep ? (_cameraShakeDirection * (float)_cameraShakeForce) : (-camera2D.Offset / 2f));
					firstStep = false;
					_cameraShakeStepsRemaining--;
					double cameraShakeInterval = _cameraShakeInterval;
					await ToSignal(GetTree().CreateTimer(cameraShakeInterval, processAlways: false), SceneTreeTimer.SignalName.Timeout);
					continue;
				}
				break;
			}
		}
		finally
		{
			Camera2D camera2D2 = (IsInsideTree() ? GetViewport().GetCamera2D() : null);
			if (GodotObject.IsInstanceValid(camera2D2))
			{
				camera2D2.Offset = Vector2.Zero;
			}
			_cameraShakeRunning = false;
			_cameraShakeForce = 0.0;
			_cameraShakeInterval = 0.0;
			_cameraShakeStepsRemaining = 0;
		}
	}

	public ViewManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/ViewManager");
	}
}
