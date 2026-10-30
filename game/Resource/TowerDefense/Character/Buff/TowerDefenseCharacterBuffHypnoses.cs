using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterBuffHypnoses : TowerDefenseCharacterBuffConfig
{
	private static readonly Color CHARM_COLOR = new Color(0.83f, 0.34f, 0.81f);

	private static readonly Vector2 IceFireTorchwoodHeadOffset = new Vector2(0f, 34f);

	private static readonly Vector2 IceFireTorchwoodFallbackOffset = new Vector2(-20f, 28f);

	private const float IceFireTorchwoodScale = 0.35f;

	[Export(PropertyHint.None, "")]
	public double time = -1.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.CHARACTER_CAMP saveCamp = TowerDefenseEnum.CHARACTER_CAMP.NOONE;

	[Export(PropertyHint.None, "")]
	public bool enableTorchwood;

	[Export(PropertyHint.None, "")]
	public string torchwoodChangeName = "IceFire";

	[Export(PropertyHint.None, "")]
	public string torchwoodAudio = "";

	[Export(PropertyHint.None, "")]
	public Vector2 torchwoodAreaSize = new Vector2(80f, 80f);

	[Export(PropertyHint.None, "")]
	public bool enableDeathFreeze;

	[Export(PropertyHint.None, "")]
	public double deathFreezeTime = 3.0;

	[Export(PropertyHint.None, "")]
	public double deathSlowTime = 15.0;

	[Export(PropertyHint.None, "")]
	public double deathDamage = 1800.0;

	[Export(PropertyHint.None, "")]
	public bool enableSunProduce;

	[Export(PropertyHint.None, "")]
	public double sunProduceInterval = 5.0;

	[Export(PropertyHint.None, "")]
	public int sunProduceNum = 25;

	private ProduceComponent _produceComponent;

	private ChangeProjectileComponent _changeProjectileComponent;

	private ExplodeComponent _explodeComponent;

	public Node2D torchwoodSprite;

	private bool _deathFreezeOver;

	private static StateMachineDefinition _explodeStateMachine;

	private static PackedScene _iceFireTorchwoodScene;

	private const string MirrorAnchorOriginalOffsetMeta = "__hypnosesTwoCellOriginalOffset";

	public override Color FrameMeshColorMultiplier => CHARM_COLOR;

	[Export(PropertyHint.None, "")]
	public bool deathFreezeOver
	{
		get
		{
			return _deathFreezeOver;
		}
		set
		{
			_deathFreezeOver = value;
		}
	}

	public static event Action<TowerDefenseCharacter> OnHypnotized;

	public override void _Init()
	{
		key = "Hypnoses";
	}

	public override void Enter()
	{
		if (canFliter && (character.instance.unUseBuffFlags & 8) != 0)
		{
			if (saveCamp == TowerDefenseEnum.CHARACTER_CAMP.NOONE)
			{
				saveCamp = character.camp;
			}
			character.buff.DeleteBuff("Hypnoses");
			return;
		}
		character.buff.DeleteBuff("Butter");
		character.buff.DeleteBuff("Dizziness");
		character.buff.DeleteBuff("Frozen");
		character.instance.hypnoses = true;
		if (saveCamp == TowerDefenseEnum.CHARACTER_CAMP.NOONE)
		{
			saveCamp = character.camp;
		}
		if (character.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			character.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		}
		else if (character.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			character.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		}
		ApplyCharacterFacingSign(-1f);
		if (saveCamp == character.camp)
		{
			character.buff.DeleteBuff("Hypnoses");
			return;
		}
		RefreshGroundMoveDirectionCache();
		character.OnHypnosisStateChanged();
		if (enableSunProduce)
		{
			SetupSunProduce();
		}
		if (enableTorchwood)
		{
			SetupTorchwood();
		}
		if (enableDeathFreeze)
		{
			SetupDeathFreeze();
		}
		TowerDefenseCharacterBuffHypnoses.OnHypnotized?.Invoke(character);
		TowerDefensePlantHypnoShroomGhost.OnCharacterHypnotized(character);
	}

	public override void EnterReadOnlyClient()
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			if (saveCamp == TowerDefenseEnum.CHARACTER_CAMP.NOONE)
			{
				saveCamp = character.camp;
			}
			character.instance.hypnoses = true;
			if (saveCamp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
			{
				character.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
			}
			else if (saveCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				character.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
			}
			ApplyCharacterFacingSign(-1f);
			RefreshGroundMoveDirectionCache();
			character.OnHypnosisStateChanged();
			SyncTorchwoodVisualReadOnlyClient();
		}
	}

	private void ApplyCharacterFacingSign(float sign)
	{
		bool num = character.Scale.X < 0f;
		bool flag = sign < 0f;
		float num2 = Mathf.Abs(character.Scale.X);
		character.Scale = new Vector2((sign < 0f) ? (0f - num2) : num2, character.Scale.Y);
		if (num != flag)
		{
			ApplyTwoCellPlantMirrorAnchor(flag);
		}
		if (GodotObject.IsInstanceValid(character.sprite))
		{
			character.sprite.NotifyAncestorTransformChangedForRender();
		}
	}

	private void ApplyTwoCellPlantMirrorAnchor(bool mirrored)
	{
		if (GodotObject.IsInstanceValid(character) && character.config is TowerDefensePlantConfig plantConfig && HasHorizontalGridExtension(plantConfig) && GodotObject.IsInstanceValid(character.sprite))
		{
			ApplyMirrorAnchorRecursive(character.sprite, mirrored);
		}
	}

	private static void ApplyMirrorAnchorRecursive(Node node, bool mirrored)
	{
		if (node is AdobeAnimateSprite sprite)
		{
			ApplySpriteMirrorAnchor(sprite, mirrored);
		}
		int childCount = node.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			ApplyMirrorAnchorRecursive(node.GetChild(i), mirrored);
		}
	}

	private static void ApplySpriteMirrorAnchor(AdobeAnimateSprite sprite, bool mirrored)
	{
		if (mirrored)
		{
			if (!sprite.HasMeta("__hypnosesTwoCellOriginalOffset"))
			{
				sprite.SetMeta("__hypnosesTwoCellOriginalOffset", sprite.offset);
			}
			Vector2 vector = (Vector2)sprite.GetMeta("__hypnosesTwoCellOriginalOffset");
			sprite.offset = new Vector2(0f - vector.X - 2f * ResolveSpritePlaybackArtCenterX(sprite), vector.Y);
		}
		else if (sprite.HasMeta("__hypnosesTwoCellOriginalOffset"))
		{
			sprite.offset = (Vector2)sprite.GetMeta("__hypnosesTwoCellOriginalOffset");
		}
	}

	private static bool HasHorizontalGridExtension(TowerDefensePlantConfig plantConfig)
	{
		foreach (Vector2I item in plantConfig.extendGrid)
		{
			if (item.X != 0)
			{
				return true;
			}
		}
		return false;
	}

	private static float ResolveSpritePlaybackArtCenterX(AdobeAnimateSprite sprite)
	{
		Rect2[] array = AdobeAnimateDefinitionCache.GetOrBuild(sprite.flashAnimeData)?.FrameLocalBounds;
		if (array == null || array.Length == 0)
		{
			return 0f;
		}
		Vector2I clipRange = sprite.clipRange;
		int num = clipRange.X;
		int num2 = clipRange.Y;
		if (num > num2 || num < 0 || num2 >= array.Length)
		{
			num = 0;
			num2 = array.Length - 1;
		}
		float[] array2 = new float[num2 - num + 1];
		int num3 = 0;
		for (int i = num; i <= num2; i++)
		{
			Rect2 rect = array[i];
			if (!(rect.Size.X <= 0f) || !(rect.Size.Y <= 0f))
			{
				array2[num3] = rect.GetCenter().X;
				num3++;
			}
		}
		if (num3 == 0)
		{
			return array[0].GetCenter().X;
		}
		System.Array.Sort(array2, 0, num3);
		return array2[num3 / 2];
	}

	public override void SyncPresentationReadOnlyClient()
	{
		SyncTorchwoodVisualReadOnlyClient();
	}

	public static void CleanupBattleState()
	{
		TowerDefensePlantHypnoShroomGhost.ResetBattleState();
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		if (time != -1.0)
		{
			return currentTime >= time;
		}
		return false;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
		CleanupSunProduce();
		CleanupTorchwood();
		CleanupDeathFreeze();
		character.instance.hypnoses = false;
		character.camp = saveCamp;
		ApplyCharacterFacingSign(1f);
		RefreshGroundMoveDirectionCache();
		character.OnHypnosisStateChanged();
		if (character is TowerDefenseZombie towerDefenseZombie && !character.die && GodotObject.IsInstanceValid(towerDefenseZombie))
		{
			AttackComponent attackComponent = towerDefenseZombie.attackComponent;
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				attackComponent.target = null;
			}
			towerDefenseZombie.Walk();
		}
	}

	public override void ExitReadOnlyClient()
	{
		CleanupTorchwoodVisual();
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.instance))
		{
			return;
		}
		character.instance.hypnoses = false;
		if (saveCamp != TowerDefenseEnum.CHARACTER_CAMP.NOONE)
		{
			character.camp = saveCamp;
		}
		ApplyCharacterFacingSign(1f);
		if (character is TowerDefenseZombie towerDefenseZombie)
		{
			AttackComponent attackComponent = towerDefenseZombie.attackComponent;
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				towerDefenseZombie.attackComponent.target = null;
			}
		}
		RefreshGroundMoveDirectionCache();
		character.OnHypnosisStateChanged();
	}

	public override void Cancel()
	{
		CleanupSunProduce();
		CleanupTorchwood();
		CleanupDeathFreeze();
		ExitReadOnlyClient();
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		if (!(config is TowerDefenseCharacterBuffHypnoses towerDefenseCharacterBuffHypnoses))
		{
			return;
		}
		if (time != -1.0)
		{
			time = ((towerDefenseCharacterBuffHypnoses.time == -1.0) ? (-1.0) : Mathf.Max(time, towerDefenseCharacterBuffHypnoses.time));
		}
		currentTime = 0.0;
		if (enableSunProduce != towerDefenseCharacterBuffHypnoses.enableSunProduce)
		{
			CleanupSunProduce();
		}
		enableSunProduce = towerDefenseCharacterBuffHypnoses.enableSunProduce;
		sunProduceInterval = towerDefenseCharacterBuffHypnoses.sunProduceInterval;
		sunProduceNum = towerDefenseCharacterBuffHypnoses.sunProduceNum;
		if (enableSunProduce)
		{
			ProduceComponent produceComponent = _produceComponent;
			if (produceComponent == null || produceComponent.IsReleased)
			{
				SetupSunProduce();
			}
			else
			{
				_produceComponent.produceInterval = (float)sunProduceInterval;
				_produceComponent.num = sunProduceNum;
			}
		}
		if (enableTorchwood != towerDefenseCharacterBuffHypnoses.enableTorchwood)
		{
			CleanupTorchwood();
		}
		enableTorchwood = towerDefenseCharacterBuffHypnoses.enableTorchwood;
		torchwoodChangeName = towerDefenseCharacterBuffHypnoses.torchwoodChangeName;
		torchwoodAudio = towerDefenseCharacterBuffHypnoses.torchwoodAudio;
		torchwoodAreaSize = towerDefenseCharacterBuffHypnoses.torchwoodAreaSize;
		if (enableTorchwood)
		{
			ChangeProjectileComponent changeProjectileComponent = _changeProjectileComponent;
			if (changeProjectileComponent == null || changeProjectileComponent.IsReleased)
			{
				SetupTorchwood();
			}
			else
			{
				_changeProjectileComponent.changeName = new StringName(torchwoodChangeName);
				_changeProjectileComponent.changeAudio = torchwoodAudio;
				if (_changeProjectileComponent.checkShape?.Geometry is RectangleShape2D rectangleShape2D)
				{
					rectangleShape2D.Size = torchwoodAreaSize;
				}
			}
		}
		SyncTorchwoodVisualReadOnlyClient();
		if (enableDeathFreeze != towerDefenseCharacterBuffHypnoses.enableDeathFreeze)
		{
			CleanupDeathFreeze();
		}
		enableDeathFreeze = towerDefenseCharacterBuffHypnoses.enableDeathFreeze;
		deathFreezeTime = towerDefenseCharacterBuffHypnoses.deathFreezeTime;
		deathSlowTime = towerDefenseCharacterBuffHypnoses.deathSlowTime;
		deathDamage = towerDefenseCharacterBuffHypnoses.deathDamage;
		if (enableDeathFreeze)
		{
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent == null || explodeComponent.IsReleased)
			{
				SetupDeathFreeze();
			}
			else
			{
				RefreshDeathFreezeRuntime();
			}
		}
	}

	private void RefreshGroundMoveDirectionCache()
	{
		if (character is TowerDefenseZombie towerDefenseZombie)
		{
			GroundMoveComponent groundMoveComponent = towerDefenseZombie.groundMoveComponent;
			if (groundMoveComponent != null && !groundMoveComponent.IsReleased)
			{
				towerDefenseZombie.groundMoveComponent.RefreshDirectionCache();
			}
		}
	}

	public override void Destroy()
	{
		if (enableDeathFreeze && !_deathFreezeOver && GodotObject.IsInstanceValid(character) && (character.die || character.nearDie))
		{
			_deathFreezeOver = true;
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased)
			{
				_explodeComponent.Explode();
			}
		}
		CleanupSunProduce();
		CleanupTorchwood();
		CleanupDeathFreeze();
	}

	private void SetupSunProduce()
	{
		if (GodotObject.IsInstanceValid(character))
		{
			ComponentManager componentManager = character.componentManager;
			if (GodotObject.IsInstanceValid(componentManager))
			{
				ProduceComponentDefinition definition = new ProduceComponentDefinition
				{
					ComponentTypeId = "ProduceComponent",
					DefinitionId = "runtime.buff.hypnoses.produce",
					InstanceId = "buff.hypnoses.produce",
					WireIndex = -1,
					produceType = ((character.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE) ? "BrainSun" : "Sun"),
					produceInterval = (float)sunProduceInterval,
					num = sunProduceNum
				};
				_produceComponent = componentManager.AddRuntimeComponent(definition) as ProduceComponent;
			}
		}
	}

	private void CleanupSunProduce()
	{
		ProduceComponent produceComponent = _produceComponent;
		if (produceComponent != null && !produceComponent.IsReleased)
		{
			ComponentManager componentManager = _produceComponent.Manager;
			if (!GodotObject.IsInstanceValid(componentManager) && GodotObject.IsInstanceValid(character))
			{
				componentManager = character.componentManager;
			}
			if (GodotObject.IsInstanceValid(componentManager))
			{
				componentManager.RemoveRuntimeComponent(_produceComponent);
			}
		}
		_produceComponent = null;
	}

	private void SetupTorchwood()
	{
		if (GodotObject.IsInstanceValid(character))
		{
			ComponentManager componentManager = character.componentManager;
			if (GodotObject.IsInstanceValid(componentManager))
			{
				RectangleShape2D rectangleShape2D = new RectangleShape2D();
				rectangleShape2D.Size = torchwoodAreaSize;
				ChangeProjectileComponentDefinition definition = new ChangeProjectileComponentDefinition
				{
					ComponentTypeId = "ChangeProjectileComponent",
					DefinitionId = "runtime.buff.hypnoses.change_projectile",
					InstanceId = "buff.hypnoses.change_projectile",
					WireIndex = -1,
					changeName = new StringName(torchwoodChangeName),
					changeAudio = torchwoodAudio,
					checkShape = new AabbShape2DResource
					{
						Geometry = rectangleShape2D,
						LocalTransform = Transform2D.Identity
					}
				};
				_changeProjectileComponent = componentManager.AddRuntimeComponent(definition) as ChangeProjectileComponent;
				SetupTorchwoodVisual();
			}
		}
	}

	private void SyncTorchwoodVisualReadOnlyClient()
	{
		if (enableTorchwood && torchwoodChangeName == "IceFire")
		{
			SetupTorchwoodVisual();
		}
		else
		{
			CleanupTorchwoodVisual();
		}
	}

	private void SetupTorchwoodVisual()
	{
		if (!enableTorchwood || torchwoodChangeName != "IceFire" || !(character is TowerDefenseZombie) || GodotObject.IsInstanceValid(torchwoodSprite))
		{
			return;
		}
		if (_iceFireTorchwoodScene == null)
		{
			_iceFireTorchwoodScene = GD.Load<PackedScene>("res://Asset/Anime/Effect/Fire/Ice/IceFire.tscn");
		}
		if (!GodotObject.IsInstanceValid(_iceFireTorchwoodScene))
		{
			return;
		}
		torchwoodSprite = _iceFireTorchwoodScene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		torchwoodSprite.Name = "HypnosesIceFireTorchwood";
		torchwoodSprite.Scale = new Vector2(character.Scale.X * character.sprite.Scale.X * 0.35f, 0.35f);
		if (torchwoodSprite is AdobeAnimateSprite adobeAnimateSprite)
		{
			AdobeAnimateSlot adobeAnimateSlot = (GodotObject.IsInstanceValid(character.headSlot) ? character.headSlot : null);
			Transform2D targetGlobalTransform;
			if (GodotObject.IsInstanceValid(adobeAnimateSlot))
			{
				adobeAnimateSlot.Update();
				adobeAnimateSprite.Position = IceFireTorchwoodHeadOffset;
				targetGlobalTransform = character.GetLogicalGlobalTransform(adobeAnimateSlot) * adobeAnimateSprite.Transform;
			}
			else
			{
				targetGlobalTransform = character.GetLogicalGlobalTransform(character.spriteGroup) * adobeAnimateSprite.Transform;
				targetGlobalTransform.Origin = character.GetLogicalGlobalPosition(character.sprite) + IceFireTorchwoodFallbackOffset;
			}
			character.AttachAnimatedStatusVisual(adobeAnimateSprite, adobeAnimateSlot, targetGlobalTransform);
		}
		else if (GodotObject.IsInstanceValid(character.headSlot))
		{
			character.headSlot.Update();
			character.headSlot.AddChild(torchwoodSprite, forceReadableName: false, Node.InternalMode.Disabled);
			torchwoodSprite.Position = IceFireTorchwoodHeadOffset;
		}
		else
		{
			character.spriteGroup.AddChild(torchwoodSprite, forceReadableName: false, Node.InternalMode.Disabled);
			torchwoodSprite.Position = character.GetLogicalGlobalTransform(character.spriteGroup).AffineInverse() * (character.GetLogicalGlobalPosition(character.sprite) + IceFireTorchwoodFallbackOffset);
		}
	}

	private void CleanupTorchwood()
	{
		ChangeProjectileComponent changeProjectileComponent = _changeProjectileComponent;
		if (changeProjectileComponent != null && !changeProjectileComponent.IsReleased)
		{
			ComponentManager componentManager = _changeProjectileComponent.Manager;
			if (!GodotObject.IsInstanceValid(componentManager) && GodotObject.IsInstanceValid(character))
			{
				componentManager = character.componentManager;
			}
			if (GodotObject.IsInstanceValid(componentManager))
			{
				componentManager.RemoveRuntimeComponent(_changeProjectileComponent);
			}
		}
		_changeProjectileComponent = null;
		CleanupTorchwoodVisual();
	}

	private void CleanupTorchwoodVisual()
	{
		if (GodotObject.IsInstanceValid(torchwoodSprite))
		{
			if (torchwoodSprite is AdobeAnimateSprite child && GodotObject.IsInstanceValid(character))
			{
				character.DetachAnimatedStatusVisual(child);
			}
			torchwoodSprite.QueueFree();
		}
		torchwoodSprite = null;
	}

	private void SetupDeathFreeze()
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		ComponentManager componentManager = character.componentManager;
		if (GodotObject.IsInstanceValid(componentManager))
		{
			if (_explodeStateMachine == null)
			{
				_explodeStateMachine = GD.Load<StateMachineDefinition>("res://Script/Component/TowerDefense/Character/ExplodeComponent/ExplodeComponentStateMachine.tres");
			}
			TowerDefenseCharacterEventForzen towerDefenseCharacterEventForzen = new TowerDefenseCharacterEventForzen();
			towerDefenseCharacterEventForzen.time = deathFreezeTime;
			towerDefenseCharacterEventForzen.iceSpeedDownTime = deathSlowTime;
			ExplodeComponentDefinition definition = new ExplodeComponentDefinition
			{
				ComponentTypeId = "ExplodeComponent",
				DefinitionId = "buff.hypnoses.death_freeze.explode",
				InstanceId = "buff.hypnoses.death_freeze.explode",
				WireIndex = -1,
				StateMachineDefinition = _explodeStateMachine,
				reload = false,
				explodeMethod = "Line",
				explodeJalaFireType = "IceFire",
				explodeJalaOffset = new Array<int> { 0 },
				explodeJalaNum = (float)deathDamage,
				explodeAudio = "",
				cameraShakeUse = false,
				screenColorBlinkUse = false,
				craterCreateUse = false,
				explodeEvent = new Array<TowerDefenseCharacterEventBase> { towerDefenseCharacterEventForzen }
			};
			_explodeComponent = componentManager.AddRuntimeComponent(definition) as ExplodeComponent;
		}
	}

	private void RefreshDeathFreezeRuntime()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent == null || explodeComponent.IsReleased)
		{
			return;
		}
		_explodeComponent.explodeJalaNum = (float)deathDamage;
		TowerDefenseCharacterEventForzen towerDefenseCharacterEventForzen = null;
		for (int i = 0; i < _explodeComponent.explodeEvent.Count; i++)
		{
			if (_explodeComponent.explodeEvent[i] is TowerDefenseCharacterEventForzen towerDefenseCharacterEventForzen2)
			{
				towerDefenseCharacterEventForzen = towerDefenseCharacterEventForzen2;
				break;
			}
		}
		if (towerDefenseCharacterEventForzen == null)
		{
			towerDefenseCharacterEventForzen = new TowerDefenseCharacterEventForzen();
			_explodeComponent.explodeEvent.Add(towerDefenseCharacterEventForzen);
		}
		towerDefenseCharacterEventForzen.time = deathFreezeTime;
		towerDefenseCharacterEventForzen.iceSpeedDownTime = deathSlowTime;
	}

	private void CleanupDeathFreeze()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			ComponentManager componentManager = _explodeComponent.Manager;
			if (!GodotObject.IsInstanceValid(componentManager) && GodotObject.IsInstanceValid(character))
			{
				componentManager = character.componentManager;
			}
			if (GodotObject.IsInstanceValid(componentManager))
			{
				componentManager.RemoveRuntimeComponent(_explodeComponent);
			}
		}
		_explodeComponent = null;
	}
}
