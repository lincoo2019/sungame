using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class PeriodicAreaEventComponent : CharacterComponentRuntime
{
	private enum AttackMode
	{
		StaticTime,
		Dps
	}

	private enum EffectMode
	{
		Particles,
		Sprite,
		Disabled
	}

	public enum TargetEffectSpawnPolicy
	{
		PerEvent,
		PerTarget
	}

	public string attackMethod = "StaticTime";

	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public float staticTime = 1f;

	public float dpsEffectInterval = 0.1f;

	public int maxIntervalTriggersPerFrame = 1;

	public string targetEffectType = "Particles";

	public PackedScene targetEffectScene;

	public TargetEffectSpawnPolicy targetEffectSpawnPolicy;

	public AabbShape2DResource checkShape;

	public bool checkAllLine;

	public bool skipVases = true;

	public TowerDefenseCharacter parent;

	public float timer;

	private AttackMode _attackMode;

	private EffectMode _effectMode;

	private readonly List<TowerDefenseCharacter> _targetBuffer = new List<TowerDefenseCharacter>(16);

	private float _dpsEffectTimer;

	private bool _configured;

	private bool CanSpawnTargetEffect
	{
		get
		{
			if (_effectMode != EffectMode.Disabled)
			{
				return GodotObject.IsInstanceValid(targetEffectScene);
			}
			return false;
		}
	}

	private PeriodicAreaEventComponentDefinition Definition => base.ComponentDefinition as PeriodicAreaEventComponentDefinition;

	private static bool HasGameplayAuthority
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return MultiPlayerManager.IsHost;
			}
			return true;
		}
	}

	internal override bool WantsPhysicsProcess => true;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_targetBuffer.Clear();
		parent = null;
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			ResetRuntimeState();
		}
	}

	protected override void OnReleased()
	{
		_targetBuffer.Clear();
		eventList.Clear();
		checkShape = null;
		targetEffectScene = null;
		parent = null;
		ResetRuntimeState();
	}

	private void ConfigureOnce()
	{
		if (_configured)
		{
			return;
		}
		PeriodicAreaEventComponentDefinition definition = Definition;
		if (definition == null)
		{
			return;
		}
		attackMethod = definition.attackMethod ?? "StaticTime";
		_attackMode = ParseAttackMode(attackMethod);
		staticTime = definition.staticTime;
		dpsEffectInterval = definition.dpsEffectInterval;
		maxIntervalTriggersPerFrame = definition.maxIntervalTriggersPerFrame;
		targetEffectType = definition.targetEffectType ?? "Particles";
		_effectMode = ParseEffectMode(targetEffectType);
		targetEffectScene = definition.targetEffectScene;
		targetEffectSpawnPolicy = definition.targetEffectSpawnPolicy;
		checkShape = definition.checkShape;
		checkAllLine = definition.checkAllLine;
		skipVases = definition.skipVases;
		eventList.Clear();
		if (definition.eventList != null)
		{
			for (int i = 0; i < definition.eventList.Count; i++)
			{
				eventList.Add(definition.eventList[i]);
			}
		}
		_configured = true;
	}

	private void ResetRuntimeState()
	{
		timer = 0f;
		_dpsEffectTimer = 0f;
	}

	internal override void PhysicsProcess(double deltaDouble, ulong physicsFrame)
	{
		if (TryGetRuntime(out var manager))
		{
			float delta = Mathf.Max(0f, (float)(deltaDouble * (parent.buff?.GetAttackSpeedMultiplier() ?? 1.0)));
			switch (_attackMode)
			{
			case AttackMode.StaticTime:
				ProcessPeriodic(delta, manager);
				break;
			case AttackMode.Dps:
				ProcessDps(delta, manager);
				break;
			}
		}
	}

	private void ProcessPeriodic(float delta, TowerDefenseManager manager)
	{
		float num = Mathf.Max(0.001f, staticTime);
		timer += delta;
		int num2 = Mathf.Max(1, maxIntervalTriggersPerFrame);
		bool hasRegisteredCharacters = manager.characterRegistry.HasRegisteredCharacters;
		bool hasGameplayAuthority = HasGameplayAuthority;
		bool canSpawnTargetEffect = CanSpawnTargetEffect;
		bool flag = hasRegisteredCharacters && (hasGameplayAuthority || canSpawnTargetEffect);
		bool flag2 = false;
		Rect2 rect = default(Rect2);
		int num3 = 0;
		while (timer >= num && num3 < num2)
		{
			timer -= num;
			num3++;
			if (!flag)
			{
				continue;
			}
			if (!flag2)
			{
				flag2 = true;
				if (!TryResolveCheckRect(out rect))
				{
					continue;
				}
			}
			ExecuteTargetsValidated(manager, 0f, continuous: false, canSpawnTargetEffect, hasGameplayAuthority, rect);
		}
	}

	private void ProcessDps(float delta, TowerDefenseManager manager)
	{
		float num = Mathf.Max(0.001f, dpsEffectInterval);
		_dpsEffectTimer += delta;
		bool flag = _dpsEffectTimer >= num;
		if (flag)
		{
			_dpsEffectTimer %= num;
		}
		bool hasGameplayAuthority = HasGameplayAuthority;
		flag &= CanSpawnTargetEffect;
		if (manager.characterRegistry.HasRegisteredCharacters && (hasGameplayAuthority || flag) && TryResolveCheckRect(out var rect))
		{
			ExecuteTargetsValidated(manager, delta, continuous: true, flag, hasGameplayAuthority, rect);
		}
	}

	public void EventExecute()
	{
		ExecuteTargets(0f, continuous: false, spawnEffect: true, HasGameplayAuthority);
	}

	public void EventExecuteDps(float delta, bool spawnEffect)
	{
		ExecuteTargets(Mathf.Max(0f, delta), continuous: true, spawnEffect, HasGameplayAuthority);
	}

	private void ExecuteTargets(float delta, bool continuous, bool spawnEffect, bool executeEvents)
	{
		if (TryGetRuntime(out var manager) && TryResolveCheckRect(out var rect))
		{
			ExecuteTargetsValidated(manager, delta, continuous, spawnEffect, executeEvents, rect);
		}
	}

	private void ExecuteTargetsValidated(TowerDefenseManager manager, float delta, bool continuous, bool spawnEffect, bool executeEvents)
	{
		if (TryResolveCheckRect(out var rect))
		{
			ExecuteTargetsValidated(manager, delta, continuous, spawnEffect, executeEvents, rect);
		}
	}

	private void ExecuteTargetsValidated(TowerDefenseManager manager, float delta, bool continuous, bool spawnEffect, bool executeEvents, Rect2 checkRect)
	{
		_targetBuffer.Clear();
		if (!manager.characterRegistry.HasRegisteredCharacters)
		{
			return;
		}
		manager.characterRegistry.FillCharactersIntersectingRectListExcludingCamp(checkRect, parent.camp, _targetBuffer, (!checkAllLine) ? parent.gridPos.Y : (-2147483648), !checkAllLine);
		for (int i = 0; i < _targetBuffer.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _targetBuffer[i];
			if (!CanAffectTarget(towerDefenseCharacter))
			{
				continue;
			}
			int num = 0;
			if (executeEvents)
			{
				for (int j = 0; j < eventList.Count; j++)
				{
					TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = eventList[j];
					if (GodotObject.IsInstanceValid(towerDefenseCharacterEventBase))
					{
						if (continuous)
						{
							towerDefenseCharacterEventBase.ExecuteDps(towerDefenseCharacter.GetLogicalGlobalPosition(), towerDefenseCharacter, delta);
						}
						else
						{
							towerDefenseCharacterEventBase.Execute(towerDefenseCharacter.GetLogicalGlobalPosition(), towerDefenseCharacter);
						}
						num++;
						if (spawnEffect && targetEffectSpawnPolicy == TargetEffectSpawnPolicy.PerEvent)
						{
							CreateSplat(towerDefenseCharacter);
						}
					}
				}
			}
			else if (eventList.Count > 0)
			{
				num = eventList.Count;
				if (spawnEffect && targetEffectSpawnPolicy == TargetEffectSpawnPolicy.PerEvent)
				{
					for (int k = 0; k < eventList.Count; k++)
					{
						CreateSplat(towerDefenseCharacter);
					}
				}
			}
			if (spawnEffect && num > 0 && targetEffectSpawnPolicy == TargetEffectSpawnPolicy.PerTarget)
			{
				CreateSplat(towerDefenseCharacter);
			}
		}
	}

	private bool CanAffectTarget(TowerDefenseCharacter target)
	{
		if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(target.instance) || (skipVases && target is TowerDefenseVase))
		{
			return false;
		}
		if (parent.CanTarget(target))
		{
			return parent.CanCollision(target.instance.maskFlags);
		}
		return false;
	}

	public void CreateSplat(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(targetEffectScene) && _effectMode != EffectMode.Disabled)
		{
			Node2D node2D = _effectMode switch
			{
				EffectMode.Particles => TowerDefenseManager.CreateEffectParticlesOnce(targetEffectScene, character.gridPos), 
				EffectMode.Sprite => TowerDefenseManager.CreateEffectSpriteOnce(targetEffectScene, character.gridPos), 
				_ => null, 
			};
			Node2D characterNode = TowerDefenseManager.GetCharacterNode();
			if (!GodotObject.IsInstanceValid(node2D) || !GodotObject.IsInstanceValid(characterNode))
			{
				node2D?.QueueFree();
				return;
			}
			characterNode.AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
			node2D.GlobalPosition = character.GetLogicalGlobalPosition();
		}
	}

	private bool TryGetRuntime(out TowerDefenseManager manager)
	{
		manager = TowerDefenseManager.Instance;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.instance) && parent.componentAlive && GodotObject.IsInstanceValid(manager) && manager.IsGameRunning() && eventList.Count > 0)
		{
			return HasCheckGeometry();
		}
		return false;
	}

	private bool HasCheckGeometry()
	{
		if (!GodotObject.IsInstanceValid(checkShape))
		{
			if (GodotObject.IsInstanceValid(parent))
			{
				return parent.HasHitBox;
			}
			return false;
		}
		return true;
	}

	private bool TryResolveCheckRect(out Rect2 rect)
	{
		if (GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(checkShape))
		{
			ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
			if (checkShape.TryGetWorldRect(parent.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery), out rect))
			{
				return true;
			}
		}
		if (GodotObject.IsInstanceValid(parent) && parent.TryGetActiveWorldHitRect(out rect))
		{
			return true;
		}
		rect = default(Rect2);
		return false;
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			["timer"] = timer,
			["dps_effect_timer"] = _dpsEffectTimer
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (data != null)
		{
			timer = data.GetValueOrDefault("timer", timer).AsSingle();
			_dpsEffectTimer = data.GetValueOrDefault("dps_effect_timer", _dpsEffectTimer).AsSingle();
		}
	}

	public override Dictionary SyncSerialize()
	{
		return ExportComponentSave();
	}

	public override void SyncDeserialize(Dictionary data)
	{
		ImportComponentSave(data, null);
	}

	private static AttackMode ParseAttackMode(string value)
	{
		if (!string.Equals(value, "Dps", StringComparison.OrdinalIgnoreCase))
		{
			return AttackMode.StaticTime;
		}
		return AttackMode.Dps;
	}

	private static EffectMode ParseEffectMode(string value)
	{
		if (string.Equals(value, "Sprite", StringComparison.OrdinalIgnoreCase))
		{
			return EffectMode.Sprite;
		}
		if (string.Equals(value, "Disabled", StringComparison.OrdinalIgnoreCase))
		{
			return EffectMode.Disabled;
		}
		return EffectMode.Particles;
	}
}
