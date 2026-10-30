using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseArmorInstance : Resource
{
	private const double ShieldImpactDurationSeconds = 0.025;

	public TowerDefenseCharacter character;

	public AdobeAnimatePart sprite;

	public TowerDefenseArmorTypeData typeData;

	public ArmorSlotConfig slotConfig;

	public double damagePointBase;

	[Export(PropertyHint.None, "")]
	public double hitpointsSave;

	[Export(PropertyHint.None, "")]
	public double hitPoints;

	[Export(PropertyHint.None, "")]
	public int armorMethodFlags;

	private Array<double> _stagePersontage = new Array<double>();

	private double[] _stageThresholds = System.Array.Empty<double>();

	[Export(PropertyHint.None, "")]
	public int stageIndex;

	[Export(PropertyHint.None, "")]
	public bool isRemove;

	public bool damagePartDropped;

	[Export(PropertyHint.None, "")]
	public double hitpointScaleSave = 1.0;

	private double _hitpointScale = 1.0;

	private ulong _lastShieldImpactFrame = 18446744073709551615uL;

	private AdobeAnimateSlot _shieldImpactSlot;

	private readonly List<ArmorBehaviorRuntime> _behaviorRuntimes = new List<ArmorBehaviorRuntime>();

	private bool _behaviorsRemoved;

	[Export(PropertyHint.None, "")]
	public Array<double> stagePersontage
	{
		get
		{
			return _stagePersontage;
		}
		set
		{
			_stagePersontage = value ?? new Array<double>();
			RebuildStageThresholds();
		}
	}

	[Export(PropertyHint.None, "")]
	public double hitpointScale
	{
		get
		{
			return _hitpointScale;
		}
		set
		{
			if (_hitpointScale != value)
			{
				_hitpointScale = value;
				hitPoints *= value / hitpointScaleSave;
				hitpointsSave *= value / hitpointScaleSave;
				hitpointScaleSave = _hitpointScale;
			}
		}
	}

	public event Action<TowerDefenseArmorInstance, int> damagePointReach;

	public event Action<TowerDefenseArmorInstance> hitpointsEmpty;

	public event Action<TowerDefenseArmorInstance> remove;

	public TowerDefenseArmorInstance()
	{
	}

	public TowerDefenseArmorInstance(TowerDefenseCharacter _character, ArmorSlotConfig _slotConfig)
	{
		character = _character;
		slotConfig = _slotConfig;
		if (slotConfig == null)
		{
			isRemove = true;
			return;
		}
		TowerDefenseArmorRegistry.Init();
		typeData = TowerDefenseArmorRegistry.GetArmorType(slotConfig.armorName);
		if (typeData == null)
		{
			isRemove = true;
			return;
		}
		damagePointBase = ((slotConfig.damagePoint >= 0.0) ? slotConfig.damagePoint : typeData.damagePoint);
		hitPoints = damagePointBase;
		hitpointsSave = damagePointBase;
		armorMethodFlags = typeData.armorMethodFlags;
		if ((armorMethodFlags & 0x40) != 0)
		{
			stagePersontage = typeData.stagePersontage;
		}
		string replaceMethod = slotConfig.replaceMethod;
		if (!(replaceMethod == "Media"))
		{
			if (replaceMethod == "Sprite")
			{
				character.ClearArmor(slotConfig.armorName);
				sprite = CharacterArmorData.CreateArmorPartNode(character.sprite, slotConfig, typeData);
			}
		}
		else
		{
			character.SetArmor(slotConfig.armorName, 0);
		}
		if ((armorMethodFlags & 4) != 0)
		{
			TowerDefenseShieldImpactBatch.Prepare(character);
		}
		BindBehaviors(TowerDefenseBehaviorRegistry.Resolve(typeData.behaviorIds, typeData.behaviors));
		BindBehaviors(TowerDefenseBehaviorRegistry.Resolve(slotConfig.behaviorIds, slotConfig.behaviors));
	}

	public double Hurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true, bool ignoreLimit = false)
	{
		num = DealHurt(num, playSplatAudio, velocity, createDamagePart, ignoreLimit);
		return num;
	}

	private bool TryApplyStableDamage(double num, bool playSplatAudio, Vector2 velocity, bool createDamagePart, bool ignoreLimit, out double remainingDamage)
	{
		remainingDamage = num;
		if (isRemove || _behaviorRuntimes.Count != 0 || character == null || typeData == null || !double.IsFinite(num) || num < 0.0)
		{
			return false;
		}
		if (!ignoreLimit && typeData.limitMaxHit != -1.0)
		{
			num = Mathf.Min(num, typeData.limitMaxHit);
		}
		if (hitPoints > num)
		{
			if (character.HasArmorHurtSubscribers)
			{
				character.EmitArmorHurt((int)num);
			}
			hitPoints -= num;
			num = 0.0;
		}
		else
		{
			double num2 = hitPoints;
			if (character.HasArmorHurtSubscribers)
			{
				character.EmitArmorHurt((int)num2);
			}
			num -= num2;
			hitPoints = 0.0;
		}
		if ((armorMethodFlags & 4) != 0)
		{
			QueueShieldImpactVisual();
		}
		if (playSplatAudio && !string.IsNullOrEmpty(typeData.impactAudio))
		{
			AudioManager.Instance.AudioPlay(typeData.impactAudio);
		}
		if (hitPoints > 0.0)
		{
			RefreshDamageStageFromHitPoints();
		}
		else if (hitPoints <= 0.0)
		{
			if (createDamagePart && (armorMethodFlags & 0x80) != 0)
			{
				velocity = ((!(velocity == Vector2.Zero)) ? new Vector2(velocity.X * (float)GD.RandRange(0.75, 1.25), velocity.Y * (float)GD.RandRange(0.75, 1.25)) : new Vector2((float)GD.RandRange(-100, 100) * (float)GD.RandRange(0.75, 1.25), -300f * (float)GD.RandRange(0.75, 1.25)));
				DamagePartCreate(velocity);
			}
			RemoveArmor(ArmorRemovalReason.Broken, dispatchBehavior: false);
		}
		remainingDamage = (((armorMethodFlags & 0x200) != 0) ? 0.0 : num);
		if (hitPoints <= 0.0)
		{
			DispatchRemoved(ArmorRemovalReason.Broken);
			this.hitpointsEmpty?.Invoke(this);
		}
		return true;
	}

	private void RebuildStageThresholds()
	{
		int count = _stagePersontage.Count;
		if (count == 0)
		{
			_stageThresholds = System.Array.Empty<double>();
			return;
		}
		_stageThresholds = new double[count];
		for (int i = 0; i < count; i++)
		{
			_stageThresholds[i] = _stagePersontage[i];
		}
	}

	public void RefreshDamageStageFromHitPoints()
	{
		if (isRemove || typeData == null || hitPoints <= 0.0 || (armorMethodFlags & 0x40) == 0)
		{
			return;
		}
		double num = damagePointBase * hitpointScale;
		if (double.IsFinite(num) && !(num <= 0.0))
		{
			double num2 = hitPoints / num;
			int i;
			for (i = 0; (uint)i < (uint)_stageThresholds.Length && num2 <= _stageThresholds[i]; i++)
			{
			}
			while (stageIndex < i)
			{
				stageIndex++;
				SetDamageStage(stageIndex);
			}
		}
	}

	public double DealHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true, bool ignoreLimit = false)
	{
		if (isRemove)
		{
			return num;
		}
		if (TryApplyStableDamage(num, playSplatAudio, velocity, createDamagePart, ignoreLimit, out var remainingDamage))
		{
			return remainingDamage;
		}
		ArmorDamageContext context = new ArmorDamageContext(this, num, playSplatAudio, velocity, createDamagePart, ignoreLimit);
		DispatchBeforeDamage(ref context);
		if (context.Cancel)
		{
			return context.RemainingDamage;
		}
		num = context.Damage;
		playSplatAudio = context.PlaySplatAudio;
		velocity = context.Velocity;
		createDamagePart = context.CreateDamagePart;
		ignoreLimit = context.IgnoreLimit;
		double num2 = hitPoints;
		if (!ignoreLimit && typeData.limitMaxHit != -1.0)
		{
			num = Mathf.Min(num, typeData.limitMaxHit);
		}
		if (hitPoints > num)
		{
			character.EmitArmorHurt((int)num);
			hitPoints -= num;
			num = 0.0;
		}
		else
		{
			character.EmitArmorHurt((int)hitPoints);
			num -= hitPoints;
			hitPoints = 0.0;
		}
		if ((armorMethodFlags & 4) != 0)
		{
			QueueShieldImpactVisual();
		}
		string impactAudio = typeData.impactAudio;
		if (playSplatAudio && impactAudio != "")
		{
			AudioManager.Instance.AudioPlay(impactAudio);
		}
		if (hitPoints > 0.0)
		{
			RefreshDamageStageFromHitPoints();
		}
		else
		{
			if (createDamagePart && (armorMethodFlags & 0x80) != 0)
			{
				velocity = ((!(velocity == Vector2.Zero)) ? new Vector2(velocity.X * (float)GD.RandRange(0.75, 1.25), velocity.Y * (float)GD.RandRange(0.75, 1.25)) : new Vector2((float)GD.RandRange(-100, 100) * (float)GD.RandRange(0.75, 1.25), -300f * (float)GD.RandRange(0.75, 1.25)));
				DamagePartCreate(velocity);
			}
			RemoveArmor(ArmorRemovalReason.Broken, dispatchBehavior: false);
		}
		context.AppliedDamage = num2 - hitPoints;
		if ((armorMethodFlags & 0x200) != 0)
		{
			context.RemainingDamage = 0.0;
		}
		else
		{
			context.RemainingDamage = num;
		}
		DispatchAfterDamage(ref context);
		if (hitPoints <= 0.0)
		{
			DispatchRemoved(ArmorRemovalReason.Broken);
			this.hitpointsEmpty?.Invoke(this);
		}
		return context.RemainingDamage;
	}

	private void QueueShieldImpactVisual()
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (_lastShieldImpactFrame == physicsFrames)
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(_shieldImpactSlot))
		{
			if (character == null || slotConfig == null || !character.damagePartSlot.TryGetValue(slotConfig.armorName, out var value))
			{
				return;
			}
			_shieldImpactSlot = character.GetNodeOrNull<AdobeAnimateSlot>(value.AsString());
		}
		if (GodotObject.IsInstanceValid(_shieldImpactSlot) && _shieldImpactSlot.IsInsideTree())
		{
			_lastShieldImpactFrame = physicsFrames;
			_shieldImpactSlot.AddRuntimeVisualOffset(new Vector2(GD.RandRange(-2, 2), GD.RandRange(-2, 2)));
			if (!TowerDefenseShieldImpactBatch.Schedule(this, _shieldImpactSlot, 0.025))
			{
				_shieldImpactSlot.SetRuntimeVisualOffset(Vector2.Zero);
			}
		}
	}

	public bool IsMetallic()
	{
		return (armorMethodFlags & 0x10) != 0;
	}

	public void RemoveArmor()
	{
		RemoveArmor(ArmorRemovalReason.Unknown);
	}

	public void RemoveArmor(ArmorRemovalReason reason, bool dispatchBehavior = true)
	{
		string damageAudio = typeData.damageAudio;
		if (damageAudio != "")
		{
			AudioManager.Instance.AudioPlay(damageAudio);
		}
		string replaceMethod = slotConfig.replaceMethod;
		if (!(replaceMethod == "Media"))
		{
			if (replaceMethod == "Sprite")
			{
				character.ClearArmor(slotConfig.armorName);
				if (GodotObject.IsInstanceValid(sprite))
				{
					if (sprite.GetParent() != null)
					{
						sprite.GetParent().RemoveChild(sprite);
					}
					sprite.QueueFree();
				}
				sprite = null;
			}
		}
		else
		{
			character.ClearArmor(slotConfig.armorName);
			if (GodotObject.IsInstanceValid(character.sprite))
			{
				character.sprite.SetAtlasReplace(slotConfig.replaceMediaName, string.Empty);
			}
		}
		if (dispatchBehavior)
		{
			DispatchRemoved(reason);
		}
	}

	public TowerDefenseMagnet Draw()
	{
		if (isRemove)
		{
			return null;
		}
		TowerDefenseMagnet towerDefenseMagnet = character.MagnetCreate(this, sprite);
		if (!GodotObject.IsInstanceValid(towerDefenseMagnet))
		{
			return null;
		}
		sprite = null;
		RemoveArmor(ArmorRemovalReason.Drawn);
		isRemove = true;
		this.remove?.Invoke(this);
		return towerDefenseMagnet;
	}

	public void DamagePartCreate(Vector2 velocity = default(Vector2))
	{
		character.DamagePartCreate(slotConfig.armorName, sprite, velocity, keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
	}

	public void SetDamageStage(int index)
	{
		int num = ((typeData != null && typeData.stageAnimeTexturePaths != null && typeData.stageAnimeTexturePaths.Count > 0) ? Mathf.Clamp(index, 0, typeData.stageAnimeTexturePaths.Count - 1) : index);
		string replaceMethod = slotConfig.replaceMethod;
		if (!(replaceMethod == "Media"))
		{
			if (replaceMethod == "Sprite" && sprite != null && typeData != null && typeData.stageAnimeTexturePaths != null && typeData.stageAnimeTexturePaths.Count > 0)
			{
				sprite.externalAtlasTexturePath = typeData.stageAnimeTexturePaths[num];
			}
		}
		else
		{
			character.SetArmor(slotConfig.armorName, num);
		}
		this.damagePointReach?.Invoke(this, stageIndex);
		DispatchStageChanged(stageIndex);
	}

	private void BindBehaviors(IEnumerable<ArmorBehaviorDefinition> definitions)
	{
		if (definitions == null)
		{
			return;
		}
		foreach (ArmorBehaviorDefinition definition in definitions)
		{
			if (!GodotObject.IsInstanceValid(definition) || !definition.InitiallyEnabled)
			{
				continue;
			}
			try
			{
				ArmorBehaviorRuntime armorBehaviorRuntime = definition.CreateRuntime();
				if (armorBehaviorRuntime != null)
				{
					armorBehaviorRuntime.Bind(definition, this);
					_behaviorRuntimes.Add(armorBehaviorRuntime);
				}
			}
			catch (Exception ex)
			{
				GD.PushError($"[ArmorBehavior:E_BIND] behavior='{definition.GetDiagnosticName()}' armor='{slotConfig?.armorName}' reason='{ex.Message}'");
			}
		}
	}

	private void DispatchBeforeDamage(ref ArmorDamageContext context)
	{
		for (int i = 0; i < _behaviorRuntimes.Count; i++)
		{
			ArmorBehaviorRuntime armorBehaviorRuntime = _behaviorRuntimes[i];
			if (armorBehaviorRuntime.Enabled)
			{
				try
				{
					armorBehaviorRuntime.BeforeDamage(ref context);
				}
				catch (Exception exception)
				{
					DisableFaultedBehavior(armorBehaviorRuntime, "E_BEFORE_DAMAGE", exception);
				}
			}
		}
	}

	private void DispatchAfterDamage(ref ArmorDamageContext context)
	{
		for (int i = 0; i < _behaviorRuntimes.Count; i++)
		{
			ArmorBehaviorRuntime armorBehaviorRuntime = _behaviorRuntimes[i];
			if (armorBehaviorRuntime.Enabled)
			{
				try
				{
					armorBehaviorRuntime.AfterDamage(ref context);
				}
				catch (Exception exception)
				{
					DisableFaultedBehavior(armorBehaviorRuntime, "E_AFTER_DAMAGE", exception);
				}
			}
		}
	}

	private void DispatchStageChanged(int stage)
	{
		for (int i = 0; i < _behaviorRuntimes.Count; i++)
		{
			ArmorBehaviorRuntime armorBehaviorRuntime = _behaviorRuntimes[i];
			if (armorBehaviorRuntime.Enabled)
			{
				try
				{
					armorBehaviorRuntime.OnStageChanged(stage);
				}
				catch (Exception exception)
				{
					DisableFaultedBehavior(armorBehaviorRuntime, "E_STAGE", exception);
				}
			}
		}
	}

	private void DispatchRemoved(ArmorRemovalReason reason)
	{
		if (_behaviorsRemoved)
		{
			return;
		}
		_behaviorsRemoved = true;
		for (int i = 0; i < _behaviorRuntimes.Count; i++)
		{
			ArmorBehaviorRuntime armorBehaviorRuntime = _behaviorRuntimes[i];
			if (armorBehaviorRuntime.Enabled)
			{
				try
				{
					armorBehaviorRuntime.OnRemoved(reason);
				}
				catch (Exception exception)
				{
					DisableFaultedBehavior(armorBehaviorRuntime, "E_REMOVE", exception);
				}
			}
			try
			{
				armorBehaviorRuntime.Release();
			}
			catch (Exception ex)
			{
				GD.PushError($"[ArmorBehavior:E_RELEASE] armor='{slotConfig?.armorName}' reason='{ex.Message}'");
			}
		}
		_behaviorRuntimes.Clear();
	}

	private void DisableFaultedBehavior(ArmorBehaviorRuntime runtime, string errorCode, Exception exception)
	{
		runtime.Disable();
		GD.PushError($"[ArmorBehavior:{errorCode}] behavior='{runtime.Definition?.GetDiagnosticName()}' armor='{slotConfig?.armorName}' reason='{exception.Message}'");
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["slotConfig"] = slotConfig,
			["hitpointsSave"] = hitpointsSave,
			["hitPoints"] = hitPoints,
			["armorMethodFlags"] = armorMethodFlags,
			["stageIndex"] = stageIndex,
			["isRemove"] = isRemove
		};
	}

	public Dictionary ExportSave()
	{
		return new Dictionary
		{
			["armorName"] = slotConfig?.armorName ?? "",
			["hitpointsSave"] = hitpointsSave,
			["hitPoints"] = hitPoints,
			["stageIndex"] = stageIndex,
			["isRemove"] = isRemove,
			["hitpointScale"] = hitpointScale,
			["hitpointScaleSave"] = hitpointScaleSave
		};
	}

	public void ImportSave(Dictionary data)
	{
		hitpointScaleSave = data.GetValueOrDefault("hitpointScaleSave", hitpointScaleSave).AsDouble();
		hitpointScale = data.GetValueOrDefault("hitpointScale", hitpointScale).AsDouble();
		hitpointsSave = data.GetValueOrDefault("hitpointsSave", hitpointsSave).AsDouble();
		hitPoints = data.GetValueOrDefault("hitPoints", hitPoints).AsDouble();
		int num = data.GetValueOrDefault("stageIndex", 0).AsInt32();
		if (num > stageIndex)
		{
			stageIndex = num;
			SetDamageStage(stageIndex);
		}
		RefreshDamageStageFromHitPoints();
	}
}
