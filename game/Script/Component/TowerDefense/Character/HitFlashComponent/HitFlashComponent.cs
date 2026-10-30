using Godot;

public sealed class HitFlashComponent : CharacterComponentRuntime
{
	private static readonly StringName StandardBrightShaderParameter = "brightStrength";

	private static readonly StringName StandardWhiteShaderParameter = "whiteStrength";

	public TowerDefenseCharacter parent;

	private bool _configured;

	public Tween brightTween;

	public Tween whiteTween;

	private bool _brightActive;

	private float _brightElapsed;

	private float _brightDelay;

	private float _brightInitialStrength;

	private float _brightRiseStrength;

	private float _brightRiseDuration;

	private float _brightFadeDuration;

	private float _brightStrength;

	private bool _whiteActive;

	private float _whiteElapsed;

	private float _whiteDelay;

	private float _whiteInitialStrength;

	private float _whiteFadeDuration;

	private float _whiteStrength;

	private bool _gpuBrightActive;

	private float _gpuBrightStartTime;

	private float _gpuBrightStrength;

	private float _gpuBrightDuration;

	private bool _gpuWhiteActive;

	private float _gpuWhiteStartTime;

	private float _gpuWhiteStrength;

	private float _gpuWhiteDuration;

	private HitFlashComponentDefinition Definition => base.ComponentDefinition as HitFlashComponentDefinition;

	public StringName brightShaderParameter { get; set; } = StandardBrightShaderParameter;

	public StringName whiteShaderParameter { get; set; } = StandardWhiteShaderParameter;

	public bool restartBrightOnHit { get; set; }

	public bool restartWhiteOnHit { get; set; } = true;

	public float minimumTweenDuration { get; set; } = 0.001f;

	internal bool IsBatchUpdateActive
	{
		get
		{
			if (!_brightActive)
			{
				return _whiteActive;
			}
			return true;
		}
	}

	internal bool IsGpuFlashEnvelopeActive
	{
		get
		{
			if (!IsGpuBrightActive())
			{
				return IsGpuWhiteActive();
			}
			return true;
		}
	}

	internal bool IsVisualFlashActive
	{
		get
		{
			if (!IsBatchUpdateActive)
			{
				return IsGpuFlashEnvelopeActive;
			}
			return true;
		}
	}

	internal float CurrentBrightStrength => Mathf.Max(_brightStrength, ResolveGpuBrightStrength());

	internal float CurrentWhiteStrength => Mathf.Max(_whiteStrength, ResolveGpuWhiteStrength());

	private bool UsesStandardShaderParameters
	{
		get
		{
			if (brightShaderParameter == StandardBrightShaderParameter)
			{
				return whiteShaderParameter == StandardWhiteShaderParameter;
			}
			return false;
		}
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			HitFlashComponentDefinition definition = Definition;
			brightShaderParameter = definition?.brightShaderParameter ?? StandardBrightShaderParameter;
			whiteShaderParameter = definition?.whiteShaderParameter ?? StandardWhiteShaderParameter;
			restartBrightOnHit = definition?.restartBrightOnHit ?? false;
			restartWhiteOnHit = definition?.restartWhiteOnHit ?? true;
			minimumTweenDuration = definition?.minimumTweenDuration ?? 0.001f;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CancelFlashes(resetShader: true);
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			CancelFlashes(resetShader: true);
		}
	}

	public void Bright(float init = 0.5f, float delay = 0f, float rise = 0.5f, float riseDuration = 0f, float duration = 0.2f)
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active)
		{
			return;
		}
		bool flag = IsGpuBrightActive();
		if (((_brightActive || flag) && !restartBrightOnHit) || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.sprite))
		{
			return;
		}
		float duration2 = GetDuration(duration);
		bool flag2 = IsGpuWhiteActive();
		if (UsesStandardShaderParameters && delay <= 0f && riseDuration <= 0f && !_brightActive && !_whiteActive && !flag2 && TryStartGpuEnvelope(white: false, Mathf.Max(0f, rise), duration2, out var startTime))
		{
			_gpuBrightActive = true;
			_gpuBrightStartTime = startTime;
			_gpuBrightStrength = Mathf.Max(0f, rise);
			_gpuBrightDuration = duration2;
			return;
		}
		if (flag || flag2)
		{
			MigrateGpuEnvelopesToCpu();
		}
		_brightActive = true;
		_brightElapsed = 0f;
		_brightDelay = Mathf.Max(0f, delay);
		_brightInitialStrength = Mathf.Max(0f, init);
		_brightRiseStrength = Mathf.Max(0f, rise);
		_brightRiseDuration = ((riseDuration > 0f) ? GetDuration(riseDuration) : 0f);
		_brightFadeDuration = duration2;
		brightTween = null;
		if (!TowerDefenseHitFlashBatch.Register(this))
		{
			_brightActive = false;
			_brightStrength = 0f;
			ApplyShaderStrengths(brightChanged: true, whiteChanged: false);
		}
	}

	public void White(float init = 1f, float delay = 0f, float duration = 0.5f)
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active)
		{
			return;
		}
		bool flag = IsGpuWhiteActive();
		if (((_whiteActive || flag) && !restartWhiteOnHit) || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.sprite))
		{
			return;
		}
		float duration2 = GetDuration(duration);
		bool flag2 = IsGpuBrightActive();
		if (UsesStandardShaderParameters && delay <= 0f && !_brightActive && !_whiteActive && !flag2 && TryStartGpuEnvelope(white: true, Mathf.Max(0f, init), duration2, out var startTime))
		{
			_gpuWhiteActive = true;
			_gpuWhiteStartTime = startTime;
			_gpuWhiteStrength = Mathf.Max(0f, init);
			_gpuWhiteDuration = duration2;
			return;
		}
		if (flag2 || flag)
		{
			MigrateGpuEnvelopesToCpu();
		}
		_whiteActive = true;
		_whiteElapsed = 0f;
		_whiteDelay = Mathf.Max(0f, delay);
		_whiteInitialStrength = Mathf.Max(0f, init);
		_whiteFadeDuration = duration2;
		whiteTween = null;
		if (!TowerDefenseHitFlashBatch.Register(this))
		{
			_whiteActive = false;
			_whiteStrength = 0f;
			ApplyShaderStrengths(brightChanged: false, whiteChanged: true);
		}
	}

	internal bool UpdateBatch(double delta)
	{
		if (!IsBatchUpdateActive)
		{
			return false;
		}
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.sprite))
		{
			_brightActive = false;
			_whiteActive = false;
			return false;
		}
		float delta2 = Mathf.Max(0f, (float)delta);
		bool flag = AdvanceBright(delta2);
		bool flag2 = AdvanceWhite(delta2);
		if (flag || flag2)
		{
			ApplyShaderStrengths(flag, flag2);
		}
		return IsBatchUpdateActive;
	}

	private bool AdvanceBright(float delta)
	{
		if (!_brightActive)
		{
			return false;
		}
		float brightStrength = _brightStrength;
		_brightElapsed += delta;
		float num = _brightElapsed - _brightDelay;
		if (num < 0f)
		{
			return false;
		}
		if (_brightRiseDuration > 0f && num < _brightRiseDuration)
		{
			_brightStrength = Mathf.Lerp(_brightInitialStrength, _brightRiseStrength, num / _brightRiseDuration);
		}
		else
		{
			float num2 = num - _brightRiseDuration;
			if (num2 >= _brightFadeDuration)
			{
				_brightStrength = 0f;
				_brightActive = false;
			}
			else
			{
				_brightStrength = Mathf.Lerp(_brightRiseStrength, 0f, num2 / _brightFadeDuration);
			}
		}
		return !Mathf.IsEqualApprox(brightStrength, _brightStrength);
	}

	private bool AdvanceWhite(float delta)
	{
		if (!_whiteActive)
		{
			return false;
		}
		float whiteStrength = _whiteStrength;
		_whiteElapsed += delta;
		float num = _whiteElapsed - _whiteDelay;
		if (num < 0f)
		{
			return false;
		}
		if (num >= _whiteFadeDuration)
		{
			_whiteStrength = 0f;
			_whiteActive = false;
		}
		else
		{
			_whiteStrength = Mathf.Lerp(_whiteInitialStrength, 0f, num / _whiteFadeDuration);
		}
		return !Mathf.IsEqualApprox(whiteStrength, _whiteStrength);
	}

	private float GetDuration(float value)
	{
		return Mathf.Max(Mathf.Max(1E-06f, minimumTweenDuration), value);
	}

	private bool TryStartGpuEnvelope(bool white, float strength, float duration, out float startTime)
	{
		startTime = 0f;
		if (strength > 0f)
		{
			ShaderEffectComponent shaderEffectComponent = parent.shaderEffectComponent;
			if (shaderEffectComponent != null && !shaderEffectComponent.IsReleased)
			{
				return parent.shaderEffectComponent.TryStartGpuHitFlashEnvelope(white, strength, duration, out startTime);
			}
		}
		return false;
	}

	private static float GetGpuClock()
	{
		return Mathf.Max(0f, (float)AdobeAnimateRuntimeManager.AnimationClockSeconds);
	}

	private bool IsGpuBrightActive()
	{
		if (_gpuBrightActive)
		{
			return GetGpuClock() < _gpuBrightStartTime + _gpuBrightDuration;
		}
		return false;
	}

	private bool IsGpuWhiteActive()
	{
		if (_gpuWhiteActive)
		{
			return GetGpuClock() < _gpuWhiteStartTime + _gpuWhiteDuration;
		}
		return false;
	}

	private float ResolveGpuBrightStrength()
	{
		return ResolveGpuStrength(_gpuBrightActive, _gpuBrightStartTime, _gpuBrightStrength, _gpuBrightDuration);
	}

	private float ResolveGpuWhiteStrength()
	{
		return ResolveGpuStrength(_gpuWhiteActive, _gpuWhiteStartTime, _gpuWhiteStrength, _gpuWhiteDuration);
	}

	private static float ResolveGpuStrength(bool active, float startTime, float strength, float duration)
	{
		if (!active || strength <= 0f || duration <= 0f)
		{
			return 0f;
		}
		float num = Mathf.Max(0f, GetGpuClock() - startTime);
		return strength * Mathf.Max(1f - num / duration, 0f);
	}

	private void MigrateGpuEnvelopesToCpu()
	{
		float gpuClock = GetGpuClock();
		float num = ResolveGpuBrightStrength();
		float num2 = ResolveGpuWhiteStrength();
		float brightFadeDuration = Mathf.Max(1E-06f, _gpuBrightStartTime + _gpuBrightDuration - gpuClock);
		float whiteFadeDuration = Mathf.Max(1E-06f, _gpuWhiteStartTime + _gpuWhiteDuration - gpuClock);
		if (num > 0f)
		{
			_brightActive = true;
			_brightElapsed = 0f;
			_brightDelay = 0f;
			_brightInitialStrength = num;
			_brightRiseStrength = num;
			_brightRiseDuration = 0f;
			_brightFadeDuration = brightFadeDuration;
			_brightStrength = num;
		}
		if (num2 > 0f)
		{
			_whiteActive = true;
			_whiteElapsed = 0f;
			_whiteDelay = 0f;
			_whiteInitialStrength = num2;
			_whiteFadeDuration = whiteFadeDuration;
			_whiteStrength = num2;
		}
		parent.shaderEffectComponent?.ClearGpuHitFlashEnvelopes();
		_gpuBrightActive = false;
		_gpuWhiteActive = false;
		ApplyShaderStrengths(num > 0f, num2 > 0f);
		if (IsBatchUpdateActive)
		{
			TowerDefenseHitFlashBatch.Register(this);
		}
	}

	internal void EnsureCpuHitFlashCompatibility()
	{
		if (IsGpuBrightActive() || IsGpuWhiteActive())
		{
			MigrateGpuEnvelopesToCpu();
		}
	}

	private void ApplyShaderStrengths(bool brightChanged, bool whiteChanged)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		ShaderEffectComponent shaderEffectComponent = parent.shaderEffectComponent;
		if (shaderEffectComponent == null || shaderEffectComponent.IsReleased)
		{
			return;
		}
		if (UsesStandardShaderParameters)
		{
			parent.shaderEffectComponent.SetHitFlashStrengths(_brightStrength, _whiteStrength);
			return;
		}
		if (brightChanged)
		{
			parent.SetSpriteGroupShaderParameter(brightShaderParameter, _brightStrength);
		}
		if (whiteChanged)
		{
			parent.SetSpriteGroupShaderParameter(whiteShaderParameter, _whiteStrength);
		}
	}

	private void CancelFlashes(bool resetShader)
	{
		TowerDefenseHitFlashBatch.Unregister(this);
		parent?.shaderEffectComponent?.ClearGpuHitFlashEnvelopes();
		_brightActive = false;
		_whiteActive = false;
		_gpuBrightActive = false;
		_gpuWhiteActive = false;
		brightTween = null;
		whiteTween = null;
		bool flag = !Mathf.IsZeroApprox(_brightStrength);
		bool flag2 = !Mathf.IsZeroApprox(_whiteStrength);
		_brightStrength = 0f;
		_whiteStrength = 0f;
		if (resetShader)
		{
			ApplyShaderStrengths(flag || UsesStandardShaderParameters, flag2 || UsesStandardShaderParameters);
		}
	}
}
