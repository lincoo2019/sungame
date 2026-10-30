using System.Collections.Generic;
using Godot;

public sealed class PhonkComponent : CharacterComponentRuntime
{
	private const string DefaultDefinitionPath = "res://Script/Component/TowerDefense/Character/PhonkComponent/PhonkComponentDefinition.tres";

	public static bool phonkEnabled;

	public static float phonkIntensity = 1f;

	public float stiffness = 800f;

	public float damping = 3.5f;

	public float rotStiffness = 1400f;

	public float rotDamping = 5f;

	public float maxScaleDeform = 0.6f;

	public float maxRotDeform = 0.5f;

	public float hurtIntensity = 1.2f;

	public float dieIntensity = 1.8f;

	public float attackIntensity = 0.6f;

	public float produceIntensity = 0.5f;

	public float plantIntensity = 0.8f;

	public float hurtCooldown = 0.5f;

	public float maxSimulationStep = 1f / 60f;

	public int maxSimulationSubsteps = 4;

	public float transformResponse = 25f;

	public float scaleSleepThreshold = 0.001f;

	public float velocitySleepThreshold = 0.01f;

	public TowerDefenseCharacter parent;

	public float _scaleX;

	public float _scaleY;

	public float _rotVal;

	public float _velX;

	public float _velY;

	public float _velRot;

	public bool _active;

	public float _lastHurtTime = -1f;

	public FireComponent _fireComponent;

	public ProduceComponent _produceComponent;

	private static PhonkComponentDefinition _defaultDefinition;

	private Vector2 _restScale = Vector2.One;

	private float _restRotation;

	private bool _restTransformCaptured;

	private bool _signalsConnected;

	private bool _configured;

	private PhonkComponentDefinition Definition => base.ComponentDefinition as PhonkComponentDefinition;

	internal override bool WantsPhysicsProcess => _active;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
	}

	protected override void OnActivated()
	{
		ConnectSignals();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectSignals();
		_ResetTransform();
		parent = null;
	}

	protected override void OnReleased()
	{
		DisconnectSignals();
		_ResetTransform();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			ConnectSignals();
			return;
		}
		DisconnectSignals();
		_ResetTransform();
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			PhonkComponentDefinition definition = Definition;
			stiffness = definition?.stiffness ?? 800f;
			damping = definition?.damping ?? 3.5f;
			rotStiffness = definition?.rotStiffness ?? 1400f;
			rotDamping = definition?.rotDamping ?? 5f;
			maxScaleDeform = definition?.maxScaleDeform ?? 0.6f;
			maxRotDeform = definition?.maxRotDeform ?? 0.5f;
			hurtIntensity = definition?.hurtIntensity ?? 1.2f;
			dieIntensity = definition?.dieIntensity ?? 1.8f;
			attackIntensity = definition?.attackIntensity ?? 0.6f;
			produceIntensity = definition?.produceIntensity ?? 0.5f;
			plantIntensity = definition?.plantIntensity ?? 0.8f;
			hurtCooldown = definition?.hurtCooldown ?? 0.5f;
			maxSimulationStep = definition?.maxSimulationStep ?? (1f / 60f);
			maxSimulationSubsteps = definition?.maxSimulationSubsteps ?? 4;
			transformResponse = definition?.transformResponse ?? 25f;
			scaleSleepThreshold = definition?.scaleSleepThreshold ?? 0.001f;
			velocitySleepThreshold = definition?.velocitySleepThreshold ?? 0.01f;
			_configured = true;
		}
	}

	private void ConnectSignals()
	{
		if (!_signalsConnected && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			parent.OnDestroy += _OnCharacterDestroy;
			parent.OnBodyHurt += _OnBodyHurt;
			parent.OnArmorHurt += _OnArmorHurt;
			parent.OnRiseOver += _OnRiseOver;
			_ConnectOptionalComponents();
			_signalsConnected = true;
		}
	}

	private void DisconnectSignals()
	{
		if (_signalsConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.OnDestroy -= _OnCharacterDestroy;
			parent.OnBodyHurt -= _OnBodyHurt;
			parent.OnArmorHurt -= _OnArmorHurt;
			parent.OnRiseOver -= _OnRiseOver;
		}
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnFireVolley -= _OnFireVolley;
		}
		ProduceComponent produceComponent = _produceComponent;
		if (produceComponent != null && !produceComponent.IsReleased)
		{
			_produceComponent.OnProduct -= _OnProduct;
		}
		_fireComponent = null;
		_produceComponent = null;
		_signalsConnected = false;
	}

	public void _ConnectOptionalComponents()
	{
		if (GodotObject.IsInstanceValid(parent?.componentManager))
		{
			FireComponent fireComponent = _fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				_fireComponent.OnFireVolley -= _OnFireVolley;
			}
			ProduceComponent produceComponent = _produceComponent;
			if (produceComponent != null && !produceComponent.IsReleased)
			{
				_produceComponent.OnProduct -= _OnProduct;
			}
			_fireComponent = parent.componentManager.GetComponent<FireComponent>();
			fireComponent = _fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				_fireComponent.OnFireVolley += _OnFireVolley;
			}
			_produceComponent = parent.componentManager.GetRuntime<ProduceComponent>();
			ProduceComponent produceComponent2 = _produceComponent;
			if (produceComponent2 != null && !produceComponent2.IsReleased)
			{
				_produceComponent.OnProduct += _OnProduct;
			}
		}
	}

	public void _OnBodyHurt(int num)
	{
		TryHurtImpulse();
	}

	public void _OnArmorHurt(int num)
	{
		TryHurtImpulse();
	}

	private void TryHurtImpulse()
	{
		if (!CanImpulse())
		{
			return;
		}
		if (!(parent is TowerDefenseZombie) || parent.nearDie)
		{
			float num = (float)Time.GetTicksMsec() / 1000f;
			if (num - _lastHurtTime < Mathf.Max(0f, hurtCooldown))
			{
				return;
			}
			_lastHurtTime = num;
		}
		Impulse(hurtIntensity * phonkIntensity);
	}

	public void _OnCharacterDestroy(TowerDefenseCharacter character)
	{
		if (CanImpulse())
		{
			Impulse(dieIntensity * phonkIntensity);
		}
	}

	public void _OnRiseOver()
	{
		if (CanImpulse())
		{
			Impulse(plantIntensity * phonkIntensity);
		}
	}

	public void _OnFireVolley(ulong randomSeed)
	{
		if (CanImpulse())
		{
			Impulse(attackIntensity * phonkIntensity);
		}
	}

	public void _OnProduct(int pos, int num)
	{
		if (CanImpulse())
		{
			Impulse(produceIntensity * phonkIntensity);
		}
	}

	public void PlayHurtPhonk()
	{
		if (CanImpulse())
		{
			Impulse(hurtIntensity * phonkIntensity);
		}
	}

	public void Impulse(float force = 1f)
	{
		if (CanImpulse() && !Mathf.IsZeroApprox(force))
		{
			if (!_active)
			{
				_restScale = parent.transformPoint.Scale;
				_restRotation = parent.transformPoint.Rotation;
				_restTransformCaptured = true;
			}
			float num = RandomSign() * (float)GD.RandRange(0.5, 1.0);
			float num2 = (0f - num) * (float)GD.RandRange(0.5, 1.0);
			float num3 = RandomSign() * (float)GD.RandRange(0.5, 1.0);
			float num4 = (float)GD.RandRange(0.8, 1.3);
			_velX += force * 8f * num * num4;
			_velY += force * 6f * num2 * num4;
			_velRot += force * 7f * num3 * num4;
			_active = true;
			RefreshPhysicsProcessEligibility();
		}
	}

	private static float RandomSign()
	{
		if (!(GD.Randf() > 0.5f))
		{
			return -1f;
		}
		return 1f;
	}

	internal override void PhysicsProcess(double deltaDouble, ulong physicsFrame)
	{
		if (!_active || !TryGetTransformPoint(out var _))
		{
			_active = false;
			RefreshPhysicsProcessEligibility();
			return;
		}
		float num = Mathf.Max(0f, (float)deltaDouble);
		float b = Mathf.Max(0.001f, maxSimulationStep);
		int num2 = 0;
		while (num > 0f && num2 < Mathf.Max(1, maxSimulationSubsteps))
		{
			float num3 = Mathf.Min(num, b);
			SimulateStep(num3);
			num -= num3;
			num2++;
		}
		ApplyTransform(Mathf.Max(0f, (float)deltaDouble));
		if (IsSleeping())
		{
			_ResetTransform();
		}
	}

	private void SimulateStep(float delta)
	{
		_velX += ((0f - Mathf.Max(0f, stiffness)) * _scaleX - Mathf.Max(0f, damping) * _velX) * delta;
		_velY += ((0f - Mathf.Max(0f, stiffness)) * _scaleY - Mathf.Max(0f, damping) * _velY) * delta;
		_velRot += ((0f - Mathf.Max(0f, rotStiffness)) * _rotVal - Mathf.Max(0f, rotDamping) * _velRot) * delta;
		_scaleX += _velX * delta;
		_scaleY += _velY * delta;
		_rotVal += _velRot * delta;
		float num = Mathf.Max(0f, maxScaleDeform);
		_scaleX = Mathf.Clamp(_scaleX, 0f - num, num);
		_scaleY = Mathf.Clamp(_scaleY, 0f - num, num);
		float num2 = Mathf.Max(0f, maxRotDeform);
		_rotVal = Mathf.Clamp(_rotVal, 0f - num2, num2);
	}

	private void ApplyTransform(float delta)
	{
		if (TryGetTransformPoint(out var transformPoint))
		{
			Vector2 to = new Vector2(_restScale.X * (1f + _scaleX), _restScale.Y * (1f + _scaleY));
			float weight = Mathf.Clamp(delta * Mathf.Max(0f, transformResponse), 0f, 1f);
			transformPoint.Scale = transformPoint.Scale.Lerp(to, weight);
			transformPoint.Rotation = Mathf.Lerp(transformPoint.Rotation, _restRotation + _rotVal, weight);
		}
	}

	private bool IsSleeping()
	{
		float num = Mathf.Max(0f, scaleSleepThreshold);
		float num2 = Mathf.Max(0f, velocitySleepThreshold);
		if (Mathf.Abs(_scaleX) < num && Mathf.Abs(_scaleY) < num && Mathf.Abs(_rotVal) < num && Mathf.Abs(_velX) < num2 && Mathf.Abs(_velY) < num2)
		{
			return Mathf.Abs(_velRot) < num2;
		}
		return false;
	}

	public void _ResetTransform()
	{
		_scaleX = (_scaleY = (_rotVal = 0f));
		_velX = (_velY = (_velRot = 0f));
		if (_restTransformCaptured && TryGetTransformPoint(out var transformPoint))
		{
			transformPoint.Scale = _restScale;
			transformPoint.Rotation = _restRotation;
		}
		_restTransformCaptured = false;
		_active = false;
		RefreshPhysicsProcessEligibility();
	}

	private bool CanImpulse()
	{
		Node2D transformPoint;
		if (phonkEnabled && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			return TryGetTransformPoint(out transformPoint);
		}
		return false;
	}

	private bool TryGetTransformPoint(out Node2D transformPoint)
	{
		transformPoint = (GodotObject.IsInstanceValid(parent) ? parent.transformPoint : null);
		return GodotObject.IsInstanceValid(transformPoint);
	}

	private static PhonkComponentDefinition GetDefaultDefinition()
	{
		if (!GodotObject.IsInstanceValid(_defaultDefinition))
		{
			_defaultDefinition = ResourceLoader.Load<PhonkComponentDefinition>("res://Script/Component/TowerDefense/Character/PhonkComponent/PhonkComponentDefinition.tres", null, ResourceLoader.CacheMode.Reuse);
		}
		return _defaultDefinition;
	}

	public static void InjectToCharacter(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character?.componentManager))
		{
			return;
		}
		ComponentManager componentManager = character.componentManager;
		PhonkComponent runtime = componentManager.GetRuntime<PhonkComponent>();
		if (runtime != null && !runtime.IsReleased)
		{
			character.phonkComponent = runtime;
			return;
		}
		PhonkComponentDefinition defaultDefinition = GetDefaultDefinition();
		if (GodotObject.IsInstanceValid(defaultDefinition) && componentManager.AddRuntimeComponent(defaultDefinition) is PhonkComponent { IsReleased: false } phonkComponent)
		{
			character.phonkComponent = phonkComponent;
		}
	}

	public static void RemoveFromCharacter(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character?.componentManager))
		{
			ComponentManager componentManager = character.componentManager;
			PhonkComponent runtime = componentManager.GetRuntime<PhonkComponent>();
			if (runtime != null && !runtime.IsReleased)
			{
				componentManager.RemoveRuntimeComponent(runtime);
			}
			if (character.phonkComponent == runtime)
			{
				character.phonkComponent = null;
			}
		}
	}

	public static void InjectAll()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			return;
		}
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if ((towerDefenseCharacter is TowerDefensePlant || towerDefenseCharacter is TowerDefenseZombie) ? true : false)
			{
				InjectToCharacter(cleanCharactersList[i]);
			}
		}
	}

	public static void RemoveAll()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			return;
		}
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if ((towerDefenseCharacter is TowerDefensePlant || towerDefenseCharacter is TowerDefenseZombie) ? true : false)
			{
				RemoveFromCharacter(cleanCharactersList[i]);
			}
		}
	}
}
