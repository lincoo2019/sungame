using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieImpBase : TowerDefenseZombie, INetworkSpawnStateReceiver
{
	[Export(PropertyHint.None, "")]
	public string flyAnimeClip = "Fly";

	[Export(PropertyHint.None, "")]
	public string landAnimeClip = "Land";

	public int collectonFlagsSave;

	public int maskFlagsSave;

	public bool _throw;

	public bool landOver;

	public ImpFlightComponent impFlightComponent;

	private double _throwLandPosX;

	private double _throwFallDuration;

	private Tween.EaseType _throwEase = Tween.EaseType.Out;

	private Tween.TransitionType _throwTransition = Tween.TransitionType.Quad;

	private bool _throwTrajectoryPending;

	private bool _throwTrajectoryStarted;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			if (_throw && !landOver)
			{
				isGround = false;
			}
			base._Ready();
			if (GodotObject.IsInstanceValid(componentManager))
			{
				impFlightComponent = componentManager.GetRuntime<ImpFlightComponent>();
			}
			collectonFlagsSave = instance.collisionFlags;
			maskFlagsSave = instance.maskFlags;
			if (_throw)
			{
				StartThrowTrajectory();
				CallDeferred(MethodName.Fly);
			}
		}
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		_throw = data.GetValueOrDefault("throw", _throw).AsBool();
		if (_throw && !landOver)
		{
			isGround = false;
		}
		ySpeed = data.GetValueOrDefault("y_speed", ySpeed).AsDouble();
		if (data.ContainsKey("land_pos_x") && data.ContainsKey("fall_duration"))
		{
			_throwLandPosX = data["land_pos_x"].AsDouble();
			_throwFallDuration = Mathf.Max(0.001, data["fall_duration"].AsDouble());
			_throwEase = (Tween.EaseType)data.GetValueOrDefault("throw_ease", 1).AsInt32();
			_throwTransition = (Tween.TransitionType)data.GetValueOrDefault("throw_transition", 4).AsInt32();
			_throwTrajectoryPending = _throw;
		}
		if (IsNodeReady())
		{
			StartThrowTrajectory();
			if (_throw)
			{
				CallDeferred(MethodName.Fly);
			}
		}
	}

	private void StartThrowTrajectory()
	{
		if (_throwTrajectoryPending && !_throwTrajectoryStarted && IsInsideTree())
		{
			_throwTrajectoryPending = false;
			_throwTrajectoryStarted = true;
			Tween tween = CreateTween();
			tween.SetEase(_throwEase);
			tween.SetTrans(_throwTransition);
			tween.TweenMethod(Callable.From<double>(SetThrowTweenX), (double)GetLogicalGlobalPosition().X, _throwLandPosX, _throwFallDuration);
		}
	}

	private void SetThrowTweenX(double value)
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		logicalGlobalPosition.X = (float)value;
		SetLogicalGlobalPosition(logicalGlobalPosition);
	}

	public override bool IsNetworkSpecialMovementActive()
	{
		if (_throw)
		{
			return !landOver;
		}
		return false;
	}

	public override void WalkEntered()
	{
		base.WalkEntered();
	}

	public void Fly()
	{
		GetFlightRuntime()?.Fly();
	}

	public void Land()
	{
		ImpFlightComponent flightRuntime = GetFlightRuntime();
		if (flightRuntime != null)
		{
			flightRuntime.Land();
			return;
		}
		landOver = true;
		Walk();
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		ImpFlightComponent flightRuntime = GetFlightRuntime();
		if ((flightRuntime == null || !flightRuntime.AnimeCompleted(clip)) && clip == landAnimeClip)
		{
			Walk();
		}
	}

	private ImpFlightComponent GetFlightRuntime()
	{
		ImpFlightComponent obj = impFlightComponent;
		if (obj != null && !obj.IsReleased)
		{
			return impFlightComponent;
		}
		impFlightComponent = (GodotObject.IsInstanceValid(componentManager) ? componentManager.GetRuntime<ImpFlightComponent>() : null);
		return impFlightComponent;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["throw"] = _throw,
			["landOver"] = landOver,
			["land_pos_x"] = _throwLandPosX,
			["fall_duration"] = _throwFallDuration,
			["throw_ease"] = (int)_throwEase,
			["throw_transition"] = (int)_throwTransition
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_throw = data.GetValueOrDefault("throw", false).AsBool();
		landOver = data.GetValueOrDefault("landOver", false).AsBool();
		if (_throw && !landOver)
		{
			isGround = false;
		}
		if (data.ContainsKey("land_pos_x") && data.ContainsKey("fall_duration"))
		{
			_throwLandPosX = data.GetValueOrDefault("land_pos_x", GetLogicalGlobalPosition().X).AsDouble();
			_throwFallDuration = Mathf.Max(0.001, data.GetValueOrDefault("fall_duration", 0.001).AsDouble());
			_throwEase = (Tween.EaseType)data.GetValueOrDefault("throw_ease", 1).AsInt32();
			_throwTransition = (Tween.TransitionType)data.GetValueOrDefault("throw_transition", 4).AsInt32();
			_throwTrajectoryPending = _throw && !landOver && !_throwTrajectoryStarted;
			if (IsNodeReady())
			{
				StartThrowTrajectory();
			}
		}
	}
}
