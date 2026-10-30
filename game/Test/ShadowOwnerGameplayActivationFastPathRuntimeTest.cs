using System;
using System.Reflection;
using Godot;

public partial class ShadowOwnerGameplayActivationFastPathRuntimeTest : Node
{
	private const string NormalComponentSetPath = "res://Prefab/TowerDefense/Character/ComponentSets/TowerDefenseZombieComponentSet.tres";

	private const string NormalCharacterScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private static readonly FieldInfo VisualCacheReadyField = typeof(ShadowComponent).GetField("_ownerActivationVisualCacheReady", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo UsingMultiMeshField = typeof(ShadowComponent).GetField("_usingMultiMeshShadow", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo LevelEditorPreviewField = typeof(ShadowComponent).GetField("_levelEditorPreview", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo InvisibleField = typeof(TowerDefenseCharacter).GetField("_invisible", BindingFlags.Instance | BindingFlags.NonPublic);

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private readonly ShadowOwnerGameplayActivationBareCharacter[] _owners = new ShadowOwnerGameplayActivationBareCharacter[1000];

	private readonly ShadowComponent[] _shadows = new ShadowComponent[1000];

	private readonly Sprite2D[] _shadowSprites = new Sprite2D[1000];

	private readonly TowerDefenseCharacterTransformPoint[] _transformPoints = new TowerDefenseCharacterTransformPoint[1000];

	private Texture2D _shadowTexture;

	private bool _baselineMode;

	private bool _previousUseMultiMesh;

	private bool _previousRendererEnabled;

	private long _dispatchSink;

	private int _checks;

	private int _failures;

	private bool _fixturePassed;

	private bool _productionScenePassed;

	private bool _trackerNotificationPassed;

	private bool _baselineRecapturePassed;

	private bool _fallbackPassed;

	private bool _replacementPassed;

	private bool _temporaryReentryPassed;

	private bool _visualCacheInvalidationPassed;

	private bool _topLevelFallbackPassed;

	private bool _spriteMultiMeshPassed;

	private bool _previewModeFallbackPassed;

	private bool _gatePassed;

	private bool _directGatePassed;

	private long _directMeasuredFastHits;

	private long _directMeasuredFallbacks;

	private long _helperMeasuredFastHits;

	private long _helperMeasuredFallbacks;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		_baselineMode = string.Equals(System.Environment.GetEnvironmentVariable("PVZHE_SHADOW_ACTIVATION_BASELINE"), "1", StringComparison.Ordinal);
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		_previousUseMultiMesh = ShadowComponent.UseMultiMesh;
		_previousRendererEnabled = TowerDefenseShadowMultiMeshRenderer.Enabled;
		try
		{
			ShadowComponent.UseMultiMesh = false;
			TowerDefenseShadowMultiMeshRenderer.Enabled = true;
			PrepareNormalShadowRuntimes();
			RunTrackedSpriteNotificationContract();
			RunChangedVisualBaselineRecaptureContract();
			RunPositionActivationPerformance();
			if (!_baselineMode)
			{
				RunTopLevelFallback();
				RunInvalidReferenceFallback();
				RunVisualReplacementFallback();
				RunVisualTreeReentryFallback();
				RunManagerTemporaryReentryContract();
				RunPreviewAndMultiMeshModeFallback();
			}
		}
		catch (Exception ex)
		{
			_failures++;
			GD.PushError(ex.ToString());
		}
		finally
		{
			ShadowComponent.UseMultiMesh = _previousUseMultiMesh;
			TowerDefenseShadowMultiMeshRenderer.Enabled = _previousRendererEnabled;
		}
		bool flag = _failures == 0;
		GD.Print($"SHADOW_OWNER_ACTIVATION_FAST_PATH_RESULT passed={flag} baselineMode={_baselineMode} checks={_checks} failures={_failures} fixture={_fixturePassed} productionScene={_productionScenePassed} trackerNotification={_trackerNotificationPassed} baselineRecapture={_baselineRecapturePassed} fallback={_fallbackPassed} replacement={_replacementPassed} temporaryReentry={_temporaryReentryPassed} visualCacheInvalidation={_visualCacheInvalidationPassed} topLevelFallback={_topLevelFallbackPassed} spriteMultiMesh={_spriteMultiMeshPassed} previewModeFallback={_previewModeFallbackPassed} directFastHits={_directMeasuredFastHits} directFallbacks={_directMeasuredFallbacks} helperFastHits={_helperMeasuredFastHits} helperFallbacks={_helperMeasuredFallbacks} directGatePassed={_directGatePassed} gatePassed={_gatePassed}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void PrepareNormalShadowRuntimes()
	{
		CharacterComponentSet characterComponentSet = GD.Load<CharacterComponentSet>("res://Prefab/TowerDefense/Character/ComponentSets/TowerDefenseZombieComponentSet.tres");
		Check(characterComponentSet != null, "Normal production CharacterComponentSet must load.");
		if (characterComponentSet != null)
		{
			Node node = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn")?.Instantiate(PackedScene.GenEditState.Disabled);
			_productionScenePassed = node?.GetNodeOrNull<Sprite2D>("%ShadowSprite") is TowerDefenseCharacterShadowSprite;
			node?.Free();
			Check(_productionScenePassed, "The real Normal derived scene must inherit the tracked production ShadowSprite without adding a Node.");
			Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8);
			image.Fill(Colors.White);
			_shadowTexture = ImageTexture.CreateFromImage(image);
			int count = characterComponentSet.GetCreationPlan().Count;
			Node2D node2D = new Node2D
			{
				Name = "NormalShadowActivationOwners"
			};
			AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			bool flag = count > 0;
			for (int i = 0; i < 1000; i++)
			{
				ShadowOwnerGameplayActivationBareCharacter shadowOwnerGameplayActivationBareCharacter = new ShadowOwnerGameplayActivationBareCharacter
				{
					Name = $"NormalShadowOwner{i}",
					Position = new Vector2(i % 25, i / 25)
				};
				Node2D node2D2 = new Node2D
				{
					Name = "SpriteGroup"
				};
				TowerDefenseCharacterTransformPoint towerDefenseCharacterTransformPoint = new TowerDefenseCharacterTransformPoint
				{
					Name = "TransformPoint"
				};
				TowerDefenseCharacterShadowSprite towerDefenseCharacterShadowSprite = new TowerDefenseCharacterShadowSprite
				{
					Name = "ShadowSprite",
					Texture = _shadowTexture,
					Position = new Vector2(3f, 7f),
					Scale = new Vector2(0.9f, 0.8f),
					VisibilityLayer = 5u
				};
				ComponentManager componentManager = new ComponentManager
				{
					Name = "ComponentManager",
					ComponentSet = characterComponentSet
				};
				node2D2.AddChild(towerDefenseCharacterTransformPoint, forceReadableName: false, InternalMode.Disabled);
				shadowOwnerGameplayActivationBareCharacter.AddChild(towerDefenseCharacterShadowSprite, forceReadableName: false, InternalMode.Disabled);
				shadowOwnerGameplayActivationBareCharacter.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
				shadowOwnerGameplayActivationBareCharacter.spriteGroup = node2D2;
				shadowOwnerGameplayActivationBareCharacter.transformPoint = towerDefenseCharacterTransformPoint;
				shadowOwnerGameplayActivationBareCharacter.shadowSprite = towerDefenseCharacterShadowSprite;
				shadowOwnerGameplayActivationBareCharacter.componentManager = componentManager;
				componentManager.AttachOwner(shadowOwnerGameplayActivationBareCharacter);
				node2D.AddChild(shadowOwnerGameplayActivationBareCharacter, forceReadableName: false, InternalMode.Disabled);
				componentManager.InitializeResourceComponents();
				componentManager.ActivateResourceComponents();
				ShadowComponent runtime = componentManager.GetRuntime<ShadowComponent>("character.shadow");
				flag &= componentManager.ResourceComponents.Count == count && runtime != null && runtime.Manager == componentManager && runtime.Owner == shadowOwnerGameplayActivationBareCharacter && runtime.Lifecycle == ComponentRuntimeLifecycle.Active && runtime.Alive && ReadPrivateBool(VisualCacheReadyField, runtime);
				_managers[i] = componentManager;
				_owners[i] = shadowOwnerGameplayActivationBareCharacter;
				_shadows[i] = runtime;
				_shadowSprites[i] = towerDefenseCharacterShadowSprite;
				_transformPoints[i] = towerDefenseCharacterTransformPoint;
			}
			_fixturePassed = flag;
			Check(flag, "Exactly 1000 real Normal production Shadow runtimes must be active under isolated ComponentManager Resources.");
		}
	}

	private void RunTrackedSpriteNotificationContract()
	{
		TowerDefenseCharacterShadowSprite towerDefenseCharacterShadowSprite = _shadowSprites[0] as TowerDefenseCharacterShadowSprite;
		bool flag = towerDefenseCharacterShadowSprite != null;
		if (towerDefenseCharacterShadowSprite != null)
		{
			ulong localTransformRevision = towerDefenseCharacterShadowSprite.LocalTransformRevision;
			towerDefenseCharacterShadowSprite.Position = new Vector2(13f, -7f);
			flag &= towerDefenseCharacterShadowSprite.LocalTransformRevision != localTransformRevision && towerDefenseCharacterShadowSprite.CachedLocalTransform.IsEqualApprox(towerDefenseCharacterShadowSprite.Transform);
			localTransformRevision = towerDefenseCharacterShadowSprite.LocalTransformRevision;
			towerDefenseCharacterShadowSprite.Scale = new Vector2(1.25f, 0.65f);
			flag &= towerDefenseCharacterShadowSprite.LocalTransformRevision != localTransformRevision && towerDefenseCharacterShadowSprite.CachedLocalTransform.IsEqualApprox(towerDefenseCharacterShadowSprite.Transform);
			localTransformRevision = towerDefenseCharacterShadowSprite.LocalTransformRevision;
			towerDefenseCharacterShadowSprite.Rotation = 0.2f;
			flag &= towerDefenseCharacterShadowSprite.LocalTransformRevision != localTransformRevision && towerDefenseCharacterShadowSprite.CachedLocalTransform.IsEqualApprox(towerDefenseCharacterShadowSprite.Transform);
			localTransformRevision = towerDefenseCharacterShadowSprite.LocalTransformRevision;
			towerDefenseCharacterShadowSprite.Skew = -0.1f;
			flag &= towerDefenseCharacterShadowSprite.LocalTransformRevision != localTransformRevision && towerDefenseCharacterShadowSprite.CachedLocalTransform.IsEqualApprox(towerDefenseCharacterShadowSprite.Transform);
		}
		_trackerNotificationPassed = flag;
		Check(flag, "The production ShadowSprite tracker must synchronously cache Position, Scale, Rotation, and Skew notifications.");
	}

	private void RunChangedVisualBaselineRecaptureContract()
	{
		MutateAllShadowVisuals(0);
		DispatchAll();
		_baselineRecapturePassed = ValidateAllRecapturedBaselines();
		Check(_baselineRecapturePassed, "Changed Sprite position/scale and TransformPoint scale must be recaptured before the position-write performance gates.");
	}

	private void RunPositionActivationPerformance()
	{
		RunDirectGlobalPositionActivation();
		RunPreparedPositionHelperActivation();
	}

	private OptimizationResultIdentity CreateActivationIdentity(string task, string phase)
	{
		return new OptimizationResultIdentity(task, OptimizationWorkloadKind.BareComponent, "builtin.zombie.normal", "res://Prefab/TowerDefense/Character/ComponentSets/TowerDefenseZombieComponentSet.tres", "ShadowComponent", "character.shadow.default", "character.shadow", "none", "none", phase, OptimizationScheduleKind.BackToBack, "headless-mobile", 60, 240);
	}

	private void RunDirectGlobalPositionActivation()
	{
		OptimizationResultIdentity identity = CreateActivationIdentity("shadow_owner_activation_direct_global_position", "direct-global-position-owner-gameplay-activated");
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			SetAllOwnerPositionsDirect(i);
			long startTicks = OptimizationBatchSampler.BeginSample();
			DispatchAll();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		long dispatchSink = _dispatchSink;
		ulong num = SumFastPathHits();
		ulong num2 = SumFallbacks();
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			SetAllOwnerPositionsDirect(j + 240);
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			DispatchAll();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		OptimizationBatchResult result = optimizationBatchSampler.Complete();
		bool flag;
		checked
		{
			_directMeasuredFastHits = (long)(SumFastPathHits() - num);
			_directMeasuredFallbacks = (long)(SumFallbacks() - num2);
			flag = ValidateAllRecapturedBaselines();
			_baselineRecapturePassed &= flag;
		}
		bool flag2 = _fixturePassed && _productionScenePassed && _trackerNotificationPassed && flag && _dispatchSink - dispatchSink == 1200000 && _directMeasuredFastHits == 1200000 && _directMeasuredFallbacks == 0;
		_directGatePassed = OptimizationPerformanceGate.IsBareResultPassed(in result, flag2, 1000, 240, 1000, 1000, in identity);
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, flag2, _directGatePassed, 1000, 240, 1000, 1000));
		Check(flag2 && result.AllocatedBytes == 0L && result.Gen0Collections == 0 && result.Gen1Collections == 0 && result.Gen2Collections == 0, "Direct production GlobalPosition writes must report an exact real activation route with zero steady allocation and GC.");
	}

	private void RunPreparedPositionHelperActivation()
	{
		OptimizationResultIdentity identity = CreateActivationIdentity("shadow_owner_activation_prepared_position_helper", "prepared-position-helper-owner-gameplay-activated");
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			SetAllOwnerPositionsCached(i);
			long startTicks = OptimizationBatchSampler.BeginSample();
			DispatchAll();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		long dispatchSink = _dispatchSink;
		ulong num = SumFastPathHits();
		ulong num2 = SumFallbacks();
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			SetAllOwnerPositionsCached(j + 240);
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			DispatchAll();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		OptimizationBatchResult result = optimizationBatchSampler.Complete();
		bool flag;
		checked
		{
			_helperMeasuredFastHits = (long)(SumFastPathHits() - num);
			_helperMeasuredFallbacks = (long)(SumFallbacks() - num2);
			flag = ValidateAllRecapturedBaselines();
			_baselineRecapturePassed &= flag;
		}
		bool flag2 = _fixturePassed && _productionScenePassed && _trackerNotificationPassed && _baselineRecapturePassed && flag && _dispatchSink - dispatchSink == 1200000 && _helperMeasuredFastHits == 1200000 && _helperMeasuredFallbacks == 0;
		_gatePassed = OptimizationPerformanceGate.IsBareResultPassed(in result, flag2, 1000, 240, 1000, 1000, in identity);
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, flag2, _gatePassed, 1000, 240, 1000, 1000));
		Check(flag2, "Cache-aware position writes must preserve exact activation routing and recaptured Shadow baselines.");
		if (!_baselineMode)
		{
			Check(_gatePassed, "1000 real Normal Shadow callbacks after cache-aware position writes must remain below 0.2 ms P99 with zero steady allocation and GC.");
		}
	}

	private void MutateAllShadowVisuals(int sample)
	{
		float num = (((sample & 1) == 0) ? 0.25f : (-0.25f));
		for (int i = 0; i < 1000; i++)
		{
			float num2 = i & 0xF;
			_shadowSprites[i].Position = new Vector2(num2 * 0.125f + num, (0f - num2) * 0.0625f - num);
			_shadowSprites[i].Scale = new Vector2(0.75f + (float)((sample + i) & 3) * 0.05f, 0.8f + (float)((sample + i) & 1) * 0.1f);
			_transformPoints[i].Scale = new Vector2(0.9f + (float)((sample + i) & 1) * 0.1f, 0.85f + (float)((sample + i) & 3) * 0.025f);
		}
	}

	private void SetAllOwnerPositionsDirect(int sample)
	{
		float num = (((sample & 1) == 0) ? 0.25f : (-0.25f));
		for (int i = 0; i < 1000; i++)
		{
			_owners[i].GlobalPosition = new Vector2((float)(i % 25) + num, (float)(i / 25) - num);
		}
	}

	private void SetAllOwnerPositionsCached(int sample)
	{
		float num = (((sample & 1) == 0) ? 0.25f : (-0.25f));
		for (int i = 0; i < 1000; i++)
		{
			TowerDefensePacketConfig.SetPreparedCharacterGlobalPosition(_owners[i], new Vector2((float)(i % 25) + num, (float)(i / 25) - num));
		}
	}

	private bool ValidateAllRecapturedBaselines()
	{
		bool flag = true;
		for (int i = 0; i < 1000; i++)
		{
			ShadowComponent shadowComponent = _shadows[i];
			Sprite2D sprite2D = _shadowSprites[i];
			TowerDefenseCharacterTransformPoint towerDefenseCharacterTransformPoint = _transformPoints[i];
			flag &= shadowComponent.saveShadowPosition.IsEqualApprox(sprite2D.GlobalPosition) && shadowComponent.saveShadowScale.IsEqualApprox(sprite2D.Scale) && shadowComponent.saveTransformPointScale.IsEqualApprox(towerDefenseCharacterTransformPoint.CachedScale);
		}
		return flag;
	}

	private void DispatchAll()
	{
		for (int i = 0; i < 1000; i++)
		{
			_managers[i].NotifyOwnerGameplayActivated();
		}
		_dispatchSink += 1000L;
	}

	private ulong SumFastPathHits()
	{
		ulong num = 0uL;
		for (int i = 0; i < 1000; i++)
		{
			num += _shadows[i].OwnerGameplayActivationFastPathHits;
		}
		return num;
	}

	private ulong SumFallbacks()
	{
		ulong num = 0uL;
		for (int i = 0; i < 1000; i++)
		{
			num += _shadows[i].OwnerGameplayActivationFallbacks;
		}
		return num;
	}

	private void RunTopLevelFallback()
	{
		ComponentManager componentManager = _managers[0];
		ShadowComponent shadowComponent = _shadows[0];
		Sprite2D sprite2D = _shadowSprites[0];
		ulong ownerGameplayActivationFastPathHits = shadowComponent.OwnerGameplayActivationFastPathHits;
		ulong ownerGameplayActivationFallbacks = shadowComponent.OwnerGameplayActivationFallbacks;
		sprite2D.TopLevel = true;
		sprite2D.GlobalPosition = new Vector2(73f, -41f);
		bool num = sprite2D is TowerDefenseCharacterShadowSprite towerDefenseCharacterShadowSprite && towerDefenseCharacterShadowSprite.CachedLocalPose.TopLevel;
		componentManager.NotifyOwnerGameplayActivated();
		bool flag = num && shadowComponent.OwnerGameplayActivationFallbacks == ownerGameplayActivationFallbacks + 1 && !ReadPrivateBool(VisualCacheReadyField, shadowComponent) && shadowComponent.saveShadowPosition.IsEqualApprox(sprite2D.GlobalPosition);
		sprite2D.TopLevel = false;
		sprite2D.Position = new Vector2(5f, 11f);
		bool num2 = sprite2D is TowerDefenseCharacterShadowSprite towerDefenseCharacterShadowSprite2 && !towerDefenseCharacterShadowSprite2.CachedLocalPose.TopLevel;
		componentManager.NotifyOwnerGameplayActivated();
		bool flag2 = num2 && shadowComponent.OwnerGameplayActivationFallbacks == ownerGameplayActivationFallbacks + 2 && ReadPrivateBool(VisualCacheReadyField, shadowComponent) && shadowComponent.saveShadowPosition.IsEqualApprox(sprite2D.GlobalPosition);
		componentManager.NotifyOwnerGameplayActivated();
		_topLevelFallbackPassed = flag && flag2 && shadowComponent.OwnerGameplayActivationFastPathHits == ownerGameplayActivationFastPathHits + 1;
		Check(_topLevelFallbackPassed, "A TopLevel ShadowSprite must fail closed through Init and only rebuild the owner-relative cache after TopLevel is restored.");
	}

	private void RunInvalidReferenceFallback()
	{
		ShadowOwnerGameplayActivationBareCharacter obj = _owners[0];
		ComponentManager componentManager = _managers[0];
		ShadowComponent shadowComponent = _shadows[0];
		Sprite2D sprite2D = _shadowSprites[0];
		Vector2 saveShadowPosition = shadowComponent.saveShadowPosition;
		obj.shadowSprite = null;
		componentManager.NotifyOwnerGameplayActivated();
		bool flag = shadowComponent.saveShadowPosition.IsEqualApprox(saveShadowPosition);
		sprite2D.Position = new Vector2(31f, -17f);
		obj.shadowSprite = sprite2D;
		componentManager.NotifyOwnerGameplayActivated();
		_fallbackPassed = flag && shadowComponent.saveShadowPosition.IsEqualApprox(sprite2D.GlobalPosition) && ReadPrivateBool(VisualCacheReadyField, shadowComponent);
		Check(_fallbackPassed, "An invalid owner visual reference must fail closed, then recover through the existing Init path.");
	}

	private void RunVisualReplacementFallback()
	{
		ShadowOwnerGameplayActivationBareCharacter obj = _owners[0];
		ComponentManager componentManager = _managers[0];
		ShadowComponent shadowComponent = _shadows[0];
		Sprite2D sprite2D = _shadowSprites[0];
		TowerDefenseCharacterTransformPoint towerDefenseCharacterTransformPoint = _transformPoints[0];
		ulong ownerGameplayActivationFallbacks = shadowComponent.OwnerGameplayActivationFallbacks;
		Sprite2D sprite2D2 = new Sprite2D
		{
			Name = "OrdinaryReplacementShadowSprite",
			Texture = _shadowTexture,
			Position = new Vector2(-23f, 41f),
			Scale = new Vector2(1.2f, 0.65f),
			Rotation = 0.125f,
			Skew = -0.05f,
			VisibilityLayer = 5u
		};
		Marker2D marker2D = new Marker2D
		{
			Name = "OrdinaryReplacementTransformPoint",
			Scale = new Vector2(0.7f, 1.15f)
		};
		obj.AddChild(sprite2D2, forceReadableName: false, InternalMode.Disabled);
		obj.AddChild(marker2D, forceReadableName: false, InternalMode.Disabled);
		obj.shadowSprite = sprite2D2;
		obj.transformPoint = marker2D;
		componentManager.NotifyOwnerGameplayActivated();
		bool flag = shadowComponent.OwnerGameplayActivationFallbacks == ownerGameplayActivationFallbacks + 1 && !ReadPrivateBool(VisualCacheReadyField, shadowComponent) && shadowComponent.saveShadowPosition.IsEqualApprox(sprite2D2.GlobalPosition) && shadowComponent.saveShadowScale.IsEqualApprox(sprite2D2.Scale) && shadowComponent.saveTransformPointScale.IsEqualApprox(marker2D.Scale);
		sprite2D2.Position = new Vector2(28f, -32f);
		sprite2D2.Scale = new Vector2(0.6f, 1.4f);
		marker2D.Scale = new Vector2(1.3f, 0.75f);
		componentManager.NotifyOwnerGameplayActivated();
		flag &= shadowComponent.OwnerGameplayActivationFallbacks == ownerGameplayActivationFallbacks + 2 && shadowComponent.saveShadowPosition.IsEqualApprox(sprite2D2.GlobalPosition) && shadowComponent.saveShadowScale.IsEqualApprox(sprite2D2.Scale) && shadowComponent.saveTransformPointScale.IsEqualApprox(marker2D.Scale);
		TowerDefenseCharacterShadowSprite towerDefenseCharacterShadowSprite = new TowerDefenseCharacterShadowSprite
		{
			Name = "TrackedReplacementShadowSprite",
			Texture = _shadowTexture,
			Position = new Vector2(-11f, 22f),
			Scale = new Vector2(1.1f, 0.8f),
			Rotation = -0.15f,
			Skew = 0.075f,
			VisibilityLayer = 5u
		};
		TowerDefenseCharacterTransformPoint towerDefenseCharacterTransformPoint2 = new TowerDefenseCharacterTransformPoint
		{
			Name = "TrackedReplacementTransformPoint",
			Scale = new Vector2(0.8f, 1.05f)
		};
		obj.AddChild(towerDefenseCharacterShadowSprite, forceReadableName: false, InternalMode.Disabled);
		obj.AddChild(towerDefenseCharacterTransformPoint2, forceReadableName: false, InternalMode.Disabled);
		obj.shadowSprite = towerDefenseCharacterShadowSprite;
		obj.transformPoint = towerDefenseCharacterTransformPoint2;
		componentManager.NotifyOwnerGameplayActivated();
		_replacementPassed = flag && shadowComponent.OwnerGameplayActivationFallbacks == ownerGameplayActivationFallbacks + 3 && shadowComponent.saveShadowPosition.IsEqualApprox(towerDefenseCharacterShadowSprite.GlobalPosition) && shadowComponent.saveShadowScale.IsEqualApprox(towerDefenseCharacterShadowSprite.Scale) && shadowComponent.saveTransformPointScale.IsEqualApprox(towerDefenseCharacterTransformPoint2.CachedScale) && ReadPrivateBool(VisualCacheReadyField, shadowComponent);
		Check(_replacementPassed, "Replacing shadowSprite and transformPoint must reject the stale cache and recapture the replacements.");
		_shadowSprites[0] = towerDefenseCharacterShadowSprite;
		_transformPoints[0] = towerDefenseCharacterTransformPoint2;
		sprite2D.QueueFree();
		towerDefenseCharacterTransformPoint.QueueFree();
		sprite2D2.QueueFree();
		marker2D.QueueFree();
	}

	private void RunVisualTreeReentryFallback()
	{
		ShadowOwnerGameplayActivationBareCharacter obj = _owners[0];
		ComponentManager componentManager = _managers[0];
		ShadowComponent shadowComponent = _shadows[0];
		Sprite2D sprite2D = _shadowSprites[0];
		obj.RemoveChild(sprite2D);
		bool flag = !ReadPrivateBool(VisualCacheReadyField, shadowComponent);
		sprite2D.Position = new Vector2(12f, 19f);
		obj.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		bool flag2 = !ReadPrivateBool(VisualCacheReadyField, shadowComponent);
		componentManager.NotifyOwnerGameplayActivated();
		_visualCacheInvalidationPassed = flag && flag2 && ReadPrivateBool(VisualCacheReadyField, shadowComponent) && shadowComponent.saveShadowPosition.IsEqualApprox(sprite2D.GlobalPosition);
		Check(_visualCacheInvalidationPassed, "A cached visual leaving and re-entering the tree must stay fail-closed until Init revalidates it.");
	}

	private void RunManagerTemporaryReentryContract()
	{
		ShadowOwnerGameplayActivationBareCharacter shadowOwnerGameplayActivationBareCharacter = _owners[0];
		ComponentManager obj = _managers[0];
		ShadowComponent shadowComponent = _shadows[0];
		Sprite2D sprite2D = _shadowSprites[0];
		TowerDefenseCharacterTransformPoint towerDefenseCharacterTransformPoint = _transformPoints[0];
		obj.DetachOwner();
		bool flag = shadowComponent.Lifecycle == ComponentRuntimeLifecycle.Detached;
		sprite2D.Position = new Vector2(-9f, 27f);
		sprite2D.Scale = new Vector2(0.55f, 1.3f);
		towerDefenseCharacterTransformPoint.Scale = new Vector2(1.1f, 0.6f);
		obj.AttachOwner(shadowOwnerGameplayActivationBareCharacter);
		obj.ActivateResourceComponents();
		obj.NotifyOwnerGameplayActivated();
		_temporaryReentryPassed = flag && shadowComponent.Lifecycle == ComponentRuntimeLifecycle.Active && shadowComponent.Owner == shadowOwnerGameplayActivationBareCharacter && shadowComponent.saveShadowPosition.IsEqualApprox(sprite2D.GlobalPosition) && shadowComponent.saveShadowScale.IsEqualApprox(sprite2D.Scale) && shadowComponent.saveTransformPointScale.IsEqualApprox(towerDefenseCharacterTransformPoint.CachedScale) && ReadPrivateBool(VisualCacheReadyField, shadowComponent);
		Check(_temporaryReentryPassed, "Temporary ComponentManager tree exit must rebind the same runtime and recapture its final pose.");
	}

	private void RunPreviewAndMultiMeshModeFallback()
	{
		ShadowOwnerGameplayActivationBareCharacter obj = _owners[0];
		ComponentManager obj2 = _managers[0];
		ShadowComponent shadowComponent = _shadows[0];
		Sprite2D sprite2D = _shadowSprites[0];
		LevelEditorPreviewField?.SetValue(shadowComponent, true);
		obj2.NotifyOwnerGameplayActivated();
		_previewModeFallbackPassed = !ReadPrivateBool(LevelEditorPreviewField, shadowComponent);
		Check(_previewModeFallbackPassed, "A changed editor-preview mode must reject the fast path and refresh Init semantics.");
		ShadowComponent.UseMultiMesh = true;
		obj2.NotifyOwnerGameplayActivated();
		bool flag = ReadPrivateBool(UsingMultiMeshField, shadowComponent) && sprite2D.VisibilityLayer == 5;
		shadowComponent.preferMultiMesh = false;
		obj2.NotifyOwnerGameplayActivated();
		bool flag2 = !ReadPrivateBool(UsingMultiMeshField, shadowComponent) && sprite2D.VisibilityLayer == 5;
		shadowComponent.preferMultiMesh = true;
		obj2.NotifyOwnerGameplayActivated();
		bool flag3 = ReadPrivateBool(UsingMultiMeshField, shadowComponent) && sprite2D.VisibilityLayer == 5;
		TowerDefenseShadowMultiMeshRenderer.Enabled = false;
		shadowComponent.PhysicsProcess(0.0, Engine.GetPhysicsFrames());
		bool flag4 = !ReadPrivateBool(UsingMultiMeshField, shadowComponent) && sprite2D.VisibilityLayer == 5;
		TowerDefenseShadowMultiMeshRenderer.Enabled = true;
		shadowComponent.PhysicsProcess(0.0, Engine.GetPhysicsFrames() + 1);
		bool flag5 = ReadPrivateBool(UsingMultiMeshField, shadowComponent) && sprite2D.VisibilityLayer == 0;
		ShadowComponent.UseMultiMesh = false;
		obj2.NotifyOwnerGameplayActivated();
		bool flag6 = !ReadPrivateBool(UsingMultiMeshField, shadowComponent) && sprite2D.VisibilityLayer == 5;
		InvisibleField?.SetValue(obj, true);
		obj2.NotifyOwnerGameplayActivated();
		bool flag7 = !sprite2D.Visible;
		InvisibleField?.SetValue(obj, false);
		obj2.NotifyOwnerGameplayActivated();
		bool visible = sprite2D.Visible;
		_spriteMultiMeshPassed = flag && flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && visible;
		Check(_spriteMultiMeshPassed, "Mode changes must preserve native Sprite fallback, MultiMesh handoff, and logical visibility.");
	}

	private static bool ReadPrivateBool(FieldInfo field, ShadowComponent shadow)
	{
		if (field != null)
		{
			object value = field.GetValue(shadow);
			if (value is bool)
			{
				return (bool)value;
			}
			return false;
		}
		return false;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError(message);
		}
	}
}
