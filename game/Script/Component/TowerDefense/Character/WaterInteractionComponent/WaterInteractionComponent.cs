using System;
using Godot;
using Godot.Collections;

public sealed class WaterInteractionComponent : CharacterComponentRuntime
{
	public delegate void WaterEnterEventHandler();

	public delegate void WaterExitEventHandler();

	private static readonly Godot.Collections.Array DuckInWaterFilters = new Godot.Collections.Array { "Zombie_duckytube_inwater" };

	private static readonly Godot.Collections.Array DuckAboveWaterFilters = new Godot.Collections.Array { "Zombie_duckytube" };

	private static readonly Godot.Collections.Array DuckAllFilters = new Godot.Collections.Array { "Zombie_duckytube_inwater", "Zombie_duckytube" };

	public AdobeAnimateSpriteBase waterLineSprite;

	public AdobeAnimateSpriteBase duckytobeSprite;

	public bool inWaterLine;

	public bool handleDuckytobe;

	public float discardOffsetIn = 36f;

	public float discardOffsetOut = 56f;

	public float discardOffsetOutTarget = 86f;

	public float exitDuration = 1f;

	public float discardResetPosition = 10000f;

	public StringName discardShaderParameter = "discardDownPos";

	public bool createSplashOnEntry = true;

	public bool monitorScaleChanges = true;

	public float scaleChangeEpsilon = 0.001f;

	public Tween.EaseType exitEase = Tween.EaseType.Out;

	public Tween.TransitionType exitTransition = Tween.TransitionType.Cubic;

	public TowerDefenseCharacter parent;

	public bool outFromWater;

	public bool isInWater;

	public ShadowComponent shadowComponent;

	public float saveTransformPointScaleY = 1f;

	public float saveSpriteGroupScaleY = 1f;

	private float _lastScaleRatioY = 1f;

	private Tween _exitTween;

	private Action _exitFinishedHandler;

	private ulong _exitVersion;

	private bool _eventBusConnected;

	private bool _configured;

	private string _discardShaderProperty = "discardDownPos";

	public Tween activeTween => _exitTween;

	private WaterInteractionComponentDefinition Definition => base.ComponentDefinition as WaterInteractionComponentDefinition;

	internal override bool WantsPhysicsProcess
	{
		get
		{
			if (monitorScaleChanges)
			{
				return isInWater;
			}
			return false;
		}
	}

	public event WaterEnterEventHandler OnWaterEnter;

	public event WaterExitEventHandler OnWaterExit;

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			WaterInteractionComponentDefinition definition = Definition;
			inWaterLine = definition?.inWaterLine ?? false;
			handleDuckytobe = definition?.handleDuckytobe ?? false;
			discardOffsetIn = definition?.discardOffsetIn ?? 36f;
			discardOffsetOut = definition?.discardOffsetOut ?? 56f;
			discardOffsetOutTarget = definition?.discardOffsetOutTarget ?? 86f;
			exitDuration = definition?.exitDuration ?? 1f;
			discardResetPosition = definition?.discardResetPosition ?? 10000f;
			discardShaderParameter = definition?.discardShaderParameter ?? new StringName("discardDownPos");
			_discardShaderProperty = discardShaderParameter.ToString();
			createSplashOnEntry = definition?.createSplashOnEntry ?? true;
			monitorScaleChanges = definition?.monitorScaleChanges ?? true;
			scaleChangeEpsilon = definition?.scaleChangeEpsilon ?? 0.001f;
			exitEase = definition?.exitEase ?? Tween.EaseType.Out;
			exitTransition = definition?.exitTransition ?? Tween.TransitionType.Cubic;
			_configured = true;
		}
		InitializeOwnerState();
	}

	protected override void OnActivated()
	{
		InitializeOwnerState();
		RefreshPhysicsProcessEligibility();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectEventBus();
		CancelExitTween(resetDiscard: false);
		parent = null;
		shadowComponent = null;
		waterLineSprite = null;
		duckytobeSprite = null;
	}

	protected override void OnReleased()
	{
		DisconnectEventBus();
		CancelExitTween(resetDiscard: false);
		parent = null;
		shadowComponent = null;
		waterLineSprite = null;
		duckytobeSprite = null;
	}

	private void InitializeOwnerState()
	{
		if (TryGetParent(out var character))
		{
			if (GodotObject.IsInstanceValid(character.transformPoint))
			{
				saveTransformPointScaleY = character.transformPoint.Scale.Y;
			}
			if (GodotObject.IsInstanceValid(character.spriteGroup))
			{
				saveSpriteGroupScaleY = character.spriteGroup.Scale.Y;
			}
			_lastScaleRatioY = GetScaleRatioY();
			ConnectEventBus();
		}
	}

	private void ConnectEventBus()
	{
		if (!_eventBusConnected && GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnScreenTransformChanged += OnScreenTransformChanged;
			_eventBusConnected = true;
		}
	}

	private void DisconnectEventBus()
	{
		if (_eventBusConnected && GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnScreenTransformChanged -= OnScreenTransformChanged;
		}
		_eventBusConnected = false;
	}

	private void OnScreenTransformChanged()
	{
		if (isInWater)
		{
			RefreshInWaterDiscard();
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (monitorScaleChanges && isInWater && TryGetParent(out var _))
		{
			float scaleRatioY = GetScaleRatioY();
			if (!(Mathf.Abs(scaleRatioY - _lastScaleRatioY) <= Mathf.Max(0.0001f, scaleChangeEpsilon)))
			{
				_lastScaleRatioY = scaleRatioY;
				RefreshInWaterDiscard();
			}
		}
	}

	public float GetScaleRatioY()
	{
		if (!TryGetParent(out var character) || !GodotObject.IsInstanceValid(character.transformPoint))
		{
			return 1f;
		}
		float num = Mathf.Abs(character.spriteGroup.Scale.Y) * Mathf.Abs(character.transformPoint.Scale.Y);
		float a = Mathf.Abs(saveSpriteGroupScaleY) * Mathf.Abs(saveTransformPointScaleY);
		return num / Mathf.Max(a, 0.001f);
	}

	public void InWater()
	{
		if (!TryGetParent(out var character))
		{
			return;
		}
		bool num = isInWater;
		CancelExitTween(resetDiscard: false);
		isInWater = true;
		RefreshPhysicsProcessEligibility();
		_lastScaleRatioY = GetScaleRatioY();
		SetShadowVisible(character, visible: false);
		RefreshInWaterDiscard();
		if (GodotObject.IsInstanceValid(waterLineSprite))
		{
			waterLineSprite.Visible = true;
		}
		ApplyDuckVisual(inWater: true);
		if (!num)
		{
			if (createSplashOnEntry)
			{
				character.CreateSplash();
			}
			this.OnWaterEnter?.Invoke();
		}
	}

	public void OutWater()
	{
		if (!TryGetParent(out var character) || (!isInWater && !GodotObject.IsInstanceValid(_exitTween)))
		{
			return;
		}
		CancelExitTween(resetDiscard: false);
		ulong version = _exitVersion;
		isInWater = false;
		RefreshPhysicsProcessEligibility();
		outFromWater = true;
		SetShadowVisible(character, !character.invisible);
		float viewportDiscardPosition = GetViewportDiscardPosition(discardOffsetOut);
		float viewportDiscardPosition2 = GetViewportDiscardPosition(discardOffsetOutTarget);
		SetDiscardDownPos(viewportDiscardPosition);
		if (GodotObject.IsInstanceValid(waterLineSprite))
		{
			waterLineSprite.Visible = false;
		}
		ApplyDuckVisual(inWater: false);
		if (exitDuration <= 0f)
		{
			CompleteExit(version);
			return;
		}
		_exitTween = parent.CreateTween();
		_exitTween.SetEase(exitEase);
		_exitTween.SetTrans(exitTransition);
		_exitFinishedHandler = delegate
		{
			CompleteExit(version);
		};
		_exitTween.Finished += _exitFinishedHandler;
		_exitTween.TweenMethod(Callable.From<double>(SetDiscardDownPos), viewportDiscardPosition, viewportDiscardPosition2, exitDuration);
	}

	private void CompleteExit(ulong version)
	{
		if (version == _exitVersion && !isInWater && TryGetParent(out var _))
		{
			CleanupExitTween(killTween: false);
			SetDiscardDownPos(discardResetPosition);
			this.OnWaterExit?.Invoke();
		}
	}

	public void InWaterDiscardSet()
	{
		if (TryGetParent(out var _))
		{
			CancelExitTween(resetDiscard: false);
			isInWater = true;
			RefreshPhysicsProcessEligibility();
			_lastScaleRatioY = GetScaleRatioY();
			RefreshInWaterDiscard();
		}
	}

	public void OutWaterDiscardSet()
	{
		CancelExitTween(resetDiscard: false);
		isInWater = false;
		RefreshPhysicsProcessEligibility();
		SetDiscardDownPos(discardResetPosition);
	}

	private void RefreshInWaterDiscard()
	{
		if (TryGetParent(out var _))
		{
			_lastScaleRatioY = GetScaleRatioY();
			SetDiscardDownPos(GetViewportDiscardPosition(discardOffsetIn));
		}
	}

	private float GetViewportDiscardPosition(float offset)
	{
		if (!TryGetParent(out var character))
		{
			return discardResetPosition;
		}
		Viewport viewport = character.GetViewport();
		if (!GodotObject.IsInstanceValid(viewport))
		{
			return discardResetPosition;
		}
		Transform2D screenTransform = viewport.GetScreenTransform();
		screenTransform.Origin = Vector2.Zero;
		float y = offset * GetScaleRatioY() + (float)character.groundHeight;
		Vector2 vector = character.GetLogicalGlobalPosition(character.spriteGroup) + new Vector2(0f, y);
		return (screenTransform * vector).Y;
	}

	private void ApplyDuckVisual(bool inWater)
	{
		if (handleDuckytobe && GodotObject.IsInstanceValid(duckytobeSprite))
		{
			if (inWater && !duckytobeSprite.Visible)
			{
				duckytobeSprite.Visible = true;
			}
			if (!inWaterLine)
			{
				duckytobeSprite.SetFliters(DuckAllFilters, open: false);
				return;
			}
			duckytobeSprite.SetFliters(DuckInWaterFilters, inWater);
			duckytobeSprite.SetFliters(DuckAboveWaterFilters, !inWater);
		}
	}

	private void SetShadowVisible(TowerDefenseCharacter character, bool visible)
	{
		ShadowComponent obj = shadowComponent;
		if (obj != null && !obj.IsReleased)
		{
			shadowComponent.SetShadowVisible(visible);
		}
		else if (GodotObject.IsInstanceValid(character.shadowSprite))
		{
			character.shadowSprite.Visible = visible;
		}
	}

	private void CancelExitTween(bool resetDiscard)
	{
		_exitVersion++;
		CleanupExitTween(killTween: true);
		if (resetDiscard)
		{
			SetDiscardDownPos(discardResetPosition);
		}
	}

	private void CleanupExitTween(bool killTween)
	{
		if (GodotObject.IsInstanceValid(_exitTween) && _exitFinishedHandler != null)
		{
			_exitTween.Finished -= _exitFinishedHandler;
		}
		if (killTween && GodotObject.IsInstanceValid(_exitTween))
		{
			_exitTween.Kill();
		}
		_exitTween = null;
		_exitFinishedHandler = null;
	}

	private void SetDiscardDownPos(double value)
	{
		if (TryGetParent(out var character) && !string.IsNullOrEmpty(_discardShaderProperty))
		{
			character.SetSpriteGroupShaderParameter(_discardShaderProperty, (float)value);
		}
	}

	private bool TryGetParent(out TowerDefenseCharacter character)
	{
		character = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(character))
		{
			return GodotObject.IsInstanceValid(character.spriteGroup);
		}
		return false;
	}
}
