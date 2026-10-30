using System;
using Godot;

public sealed class EntryAnimationComponent : CharacterComponentRuntime
{
	public float defaultFallHeight = 900f;

	public float defaultFallDelay;

	public float defaultFallDuration = 0.25f;

	public Vector2 preImpactScale = new Vector2(0.75f, 1.25f);

	public Vector2 impactScale = new Vector2(1.5f, 0.5f);

	public Vector2 settleScale = Vector2.One;

	public float impactDuration = 0.1f;

	public float settleDuration = 0.2f;

	public TowerDefenseCharacter parent;

	private SceneTreeTimer _fallDelayTimer;

	private Action _fallDelayHandler;

	private Tween _fallTween;

	private ulong _playVersion;

	private bool _ownsVerticalMotion;

	private bool _savedGravityUse;

	private bool _configured;

	private EntryAnimationComponentDefinition Definition => base.ComponentDefinition as EntryAnimationComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			EntryAnimationComponentDefinition definition = Definition;
			defaultFallHeight = definition?.defaultFallHeight ?? 900f;
			defaultFallDelay = definition?.defaultFallDelay ?? 0f;
			defaultFallDuration = definition?.defaultFallDuration ?? 0.25f;
			preImpactScale = definition?.preImpactScale ?? new Vector2(0.75f, 1.25f);
			impactScale = definition?.impactScale ?? new Vector2(1.5f, 0.5f);
			settleScale = definition?.settleScale ?? Vector2.One;
			impactDuration = definition?.impactDuration ?? 0.1f;
			settleDuration = definition?.settleDuration ?? 0.2f;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CancelPendingFall();
		RestoreGroundState();
		parent = null;
	}

	protected override void OnReleased()
	{
		CancelPendingFall();
		RestoreGroundState();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			CancelPendingFall();
			RestoreGroundState();
		}
	}

	public void PlayConfiguredFallBounce(float fallDelay = -1f)
	{
		PlayFallBounce(defaultFallHeight, (fallDelay < 0f) ? defaultFallDelay : fallDelay, defaultFallDuration);
	}

	public void PlayFallBounce(float fallZ = 900f, float fallDelay = 0f, float fallDuration = 0.25f)
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		CancelPendingFall();
		ulong version = ++_playVersion;
		BeginVerticalMotionOwnership();
		parent.z = fallZ;
		if (fallDelay <= 0f)
		{
			StartFallTween(version, fallDuration);
			return;
		}
		SceneTree tree = parent.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			RestoreGroundState();
			return;
		}
		_fallDelayTimer = tree.CreateTimer(fallDelay, processAlways: false);
		_fallDelayHandler = delegate
		{
			DetachFallDelay();
			StartFallTween(version, fallDuration);
		};
		_fallDelayTimer.Timeout += _fallDelayHandler;
	}

	private void StartFallTween(ulong version, float fallDuration)
	{
		if (version != _playVersion || !base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active)
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.transformPoint))
		{
			RestoreGroundState();
			return;
		}
		_fallTween = parent.CreateTween();
		_fallTween.SetParallel();
		_fallTween.SetEase(Tween.EaseType.Out);
		_fallTween.SetTrans(Tween.TransitionType.Quad);
		double duration = Math.Max(0.001, fallDuration);
		_fallTween.TweenProperty(parent, "z", parent.groundHeight, duration);
		Node nodeOrNull = parent.GetNodeOrNull("PacketShow");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			_fallTween.TweenProperty(nodeOrNull, "position:y", 0.0 - parent.groundHeight, duration);
		}
		_fallTween.SetTrans(Tween.TransitionType.Bounce);
		_fallTween.TweenProperty(parent.transformPoint, "scale", preImpactScale, duration);
		_fallTween.SetParallel(parallel: false);
		_fallTween.SetTrans(Tween.TransitionType.Bounce);
		_fallTween.TweenProperty(parent.transformPoint, "scale", impactScale, Math.Max(0.001, impactDuration));
		_fallTween.TweenProperty(parent.transformPoint, "scale", settleScale, Math.Max(0.001, settleDuration));
		_fallTween.TweenCallback(Callable.From(delegate
		{
			CompleteFall(version);
		}));
	}

	private void CompleteFall(ulong version)
	{
		if (version == _playVersion)
		{
			RestoreGroundState();
			_fallTween = null;
		}
	}

	private void RestoreGroundState()
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			_ownsVerticalMotion = false;
			return;
		}
		parent.ySpeed = 0.0;
		parent.isGround = true;
		parent.z = parent.groundHeight;
		if (GodotObject.IsInstanceValid(parent.transformPoint))
		{
			parent.transformPoint.Scale = settleScale;
		}
		RestoreVerticalMotionOwnership();
	}

	private void CancelPendingFall()
	{
		_playVersion++;
		DetachFallDelay();
		if (GodotObject.IsInstanceValid(_fallTween))
		{
			_fallTween.Kill();
		}
		_fallTween = null;
		RestoreVerticalMotionOwnership();
	}

	private void BeginVerticalMotionOwnership()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			if (!_ownsVerticalMotion)
			{
				_savedGravityUse = parent.gravityUse;
				_ownsVerticalMotion = true;
			}
			parent.gravityUse = false;
			parent.ySpeed = 0.0;
			parent.isGround = false;
		}
	}

	private void RestoreVerticalMotionOwnership()
	{
		if (_ownsVerticalMotion)
		{
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.gravityUse = _savedGravityUse;
			}
			_ownsVerticalMotion = false;
		}
	}

	private void DetachFallDelay()
	{
		if (GodotObject.IsInstanceValid(_fallDelayTimer) && _fallDelayHandler != null)
		{
			_fallDelayTimer.Timeout -= _fallDelayHandler;
		}
		_fallDelayTimer = null;
		_fallDelayHandler = null;
	}
}
