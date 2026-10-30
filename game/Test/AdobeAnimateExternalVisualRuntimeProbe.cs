using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateExternalVisualRuntimeProbe : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_EXTERNAL_VISUAL_RESULT";

	private const string RuntimeScenePath = "res://Asset/Anime/Character/Plant/Chapter3/SunPad/SunPad.tscn";

	private const string IcetrapTexturePath = "res://Asset/Texture/Character/Effect/Icetrap.png";

	private const string ButterSplatTexturePath = "res://Asset/Texture/Character/Effect/ButterSplat.png";

	private const string ButterGeneSplatTexturePath = "res://Asset/Texture/Character/Effect/ButterGeneSplat.png";

	private const string ZombieHeadGrossoutTexturePath = "res://Asset/Texture/Character/Effect/Zombie/ZombieHeadGrossout.png";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn";

	private const string DizzinessScenePath = "res://Asset/Anime/Effect/Star/Star.tscn";

	private const string SleepScenePath = "res://Asset/Anime/Effect/SleepZ/SleepZ.tscn";

	private const string IceTallnutAnimationScenePath = "res://Asset/Anime/Character/Plant/Gold/IceTallnut/IceTallnut.tscn";

	private const string IceTallnutReplacementPath = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_2.png";

	public override async void _Ready()
	{
		bool passed = false;
		string failure = string.Empty;
		try
		{
			string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
			if (Array.Exists(cmdlineUserArgs, (string arg) => arg == "--refresh-pose-atlas"))
			{
				if (!AdobeAnimateGlobalAtlasCache.RefreshProjectPoseData())
				{
					throw new InvalidOperationException("Adobe Animate pose atlas refresh failed.");
				}
				TextureLayered textureLayered = ResourceLoader.Load<TextureLayered>("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGpuPoseTextureArray.exr", null, ResourceLoader.CacheMode.Ignore);
				Image image = textureLayered?.GetLayerData(0);
				try
				{
					if (image == null || image.GetFormat() != Image.Format.Rgbah)
					{
						throw new InvalidOperationException("Adobe Animate pose atlas runtime format is " + (image?.GetFormat().ToString() ?? "unavailable") + ", expected RGBA16F.");
					}
					GD.Print($"[AdobeAnimate PoseAtlas] resourceType={textureLayered.GetClass()} layers={textureLayered.GetLayers()} format={image.GetFormat()} bytes={image.GetDataSize()}");
				}
				finally
				{
					image?.Dispose();
				}
			}
			else if (Array.Exists(cmdlineUserArgs, (string arg) => arg == "--refresh-external-visual-atlas"))
			{
				bool flag = AdobeAnimateGlobalAtlasCache.RefreshProjectAtlas("res://Asset/Anime", bakePoseTextures: false);
				int num = (flag ? AdobeAnimateGlobalAtlasCache.RefreshSharedVisualTextureArray() : 0);
				bool flag2 = flag && num > 0 && AdobeAnimateGlobalAtlasCache.RefreshProjectPoseData();
				bool flag3 = flag2 && AdobeAnimateGlobalAtlasCache.RefreshBootstrapAtlas();
				if (!flag || num <= 0 || !flag2 || !flag3)
				{
					throw new InvalidOperationException($"External visual atlas refresh failed: refreshed={flag} layers={num} pose={flag2} bootstrap={flag3}");
				}
			}
			AdobeAnimateSprite owner = new AdobeAnimateSprite
			{
				Name = "Owner",
				Position = new Vector2(20f, 30f)
			};
			Sprite2D rootSprite = new Sprite2D
			{
				Name = "RootVisual",
				Position = new Vector2(12f, -8f),
				Visible = true
			};
			Sprite2D worldSprite = new Sprite2D
			{
				Name = "WorldVisual",
				Position = new Vector2(100f, 60f),
				Visible = true
			};
			AddChild(owner, forceReadableName: false, InternalMode.Disabled);
			AddChild(rootSprite, forceReadableName: false, InternalMode.Disabled);
			AddChild(worldSprite, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			AdobeAnimateExternalVisualHandle handle = owner.RegisterExternalVisual(rootSprite, new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation));
			AdobeAnimateExternalVisualHandle handle2 = owner.RegisterExternalVisual(worldSprite, new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.World, AdobeAnimateExternalVisualDrawBand.BehindAnimation));
			ulong externalVisualStateVersionForRender = owner.GetExternalVisualStateVersionForRender();
			bool sameValueAccepted = owner.SetExternalVisualVisible(handle, visible: true);
			bool sameValueStable = owner.GetExternalVisualStateVersionForRender() == externalVisualStateVersionForRender;
			bool registered = handle.IsValid && handle2.IsValid && owner.GetActiveExternalVisualIndicesForRender().Length == 2;
			owner.BeginExternalVisualPreparationForRender(1L);
			owner.MarkExternalVisualPreparedForRender(handle.Index, 1L, preparedForCrowd: true);
			owner.MarkExternalVisualPreparedForRender(handle2.Index, 1L, preparedForCrowd: true);
			owner.CommitExternalVisualCrowdFrame(1L);
			bool takeoverHidden = !rootSprite.Visible && !worldSprite.Visible;
			owner.SetExternalVisualVisible(handle, visible: true);
			bool logicalVisibleDoesNotExposeNative = !rootSprite.Visible;
			owner.RestoreExternalVisualNativeFallbacks(2L);
			bool fallbackRestored = rootSprite.Visible && worldSprite.Visible;
			bool unregistered = owner.UnregisterExternalVisual(handle);
			bool oldHandleRejected = !owner.SetExternalVisualVisible(handle, visible: false);
			AdobeAnimateExternalVisualHandle handle3 = owner.RegisterExternalVisual(rootSprite, new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation));
			bool generationAdvanced = handle3.IsValid && !owner.SetExternalVisualVisible(handle, visible: false) && owner.SetExternalVisualVisible(handle3, visible: false);
			owner.UnregisterExternalVisual(handle3);
			owner.UnregisterExternalVisual(handle2);
			bool cleanupRestored = !rootSprite.Visible && worldSprite.Visible && owner.GetActiveExternalVisualIndicesForRender().Length == 0;
			bool gpuGraphIntegrated = await ValidateGpuGraphIntegration();
			bool characterBuffVisuals = await ValidateCharacterBuffVisuals();
			bool flag4 = await ValidateDirectAtlasMediaReplace();
			passed = registered && sameValueAccepted && sameValueStable && takeoverHidden && logicalVisibleDoesNotExposeNative && fallbackRestored && unregistered && oldHandleRejected && generationAdvanced && cleanupRestored && gpuGraphIntegrated && characterBuffVisuals && flag4;
			if (!passed)
			{
				failure = $"registered={registered} sameValueAccepted={sameValueAccepted} sameValueStable={sameValueStable} takeoverHidden={takeoverHidden} logicalVisibleDoesNotExposeNative={logicalVisibleDoesNotExposeNative} fallbackRestored={fallbackRestored} unregistered={unregistered} oldHandleRejected={oldHandleRejected} generationAdvanced={generationAdvanced} cleanupRestored={cleanupRestored} gpuGraphIntegrated={gpuGraphIntegrated} characterBuffVisuals={characterBuffVisuals} directAtlasMediaReplace={flag4}";
			}
		}
		catch (Exception ex)
		{
			failure = ex.ToString();
		}
		GD.Print(passed ? "ADOBE_ANIMATE_EXTERNAL_VISUAL_RESULT passed=True" : ("ADOBE_ANIMATE_EXTERNAL_VISUAL_RESULT passed=False failure=" + failure));
		GetTree().Quit((!passed) ? 2 : 0);
	}

	private async Task<bool> ValidateGpuGraphIntegration()
	{
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter3/SunPad/SunPad.tscn");
		Texture2D texture2D = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/Icetrap.png");
		if (packedScene == null || !GodotObject.IsInstanceValid(texture2D))
		{
			return false;
		}
		Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
		if (!(node is AdobeAnimateSprite sprite))
		{
			node.QueueFree();
			return false;
		}
		Sprite2D visual = new Sprite2D
		{
			Name = "GpuGraphRootVisual",
			Texture = texture2D,
			Position = new Vector2(18f, -12f)
		};
		AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
		AddChild(visual, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		AdobeAnimateExternalVisualHandle handle = sprite.RegisterExternalVisual(visual, new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation));
		AdobeAnimateGpuRenderGraphDefinition graph = null;
		AdobeAnimateSprite[] ownerSprites = Array.Empty<AdobeAnimateSprite>();
		string failureReason = string.Empty;
		bool flag = handle.IsValid && AdobeAnimateGpuRenderGraphBuilder.TryBuild(sprite, out graph, out ownerSprites, out failureReason);
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		if (flag)
		{
			flag5 = ValidateSharedExternalAtlas((graph.Owners.Length != 0) ? graph.Owners[0].Definition.AtlasTextureArrayRid : default(Rid));
			for (int i = 0; i < graph.ManagedVisualBindings.Length; i++)
			{
				AdobeAnimateGpuManagedVisualBinding adobeAnimateGpuManagedVisualBinding = graph.ManagedVisualBindings[i];
				if (adobeAnimateGpuManagedVisualBinding.SourceKind == AdobeAnimateGpuManagedVisualSourceKind.ExternalVisual)
				{
					flag2 = adobeAnimateGpuManagedVisualBinding.VisualIndex == handle.Index && adobeAnimateGpuManagedVisualBinding.AttachmentPoseBase < 0 && adobeAnimateGpuManagedVisualBinding.OwnerIndex < ownerSprites.Length;
					sprite.BeginExternalVisualPreparationForRender(77L);
					flag3 = sprite.TryBuildExternalVisualGpuStateForRender(adobeAnimateGpuManagedVisualBinding.VisualIndex, graph.Owners[adobeAnimateGpuManagedVisualBinding.OwnerIndex].Definition.AtlasTextureArrayRid, out var state, out var failureReason2) && state.Visible && state.SourceSize.X > 0f && state.SourceSize.Y > 0f;
					Texture2D texture2D2 = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/Icetrap.png");
					Transform2D transform2D = new Transform2D(0.2f, new Vector2(0.9f, 1.1f), 0f, new Vector2(31f, -17f));
					Color color = new Color(0.55f, 0.7f, 0.9f, 0.8f);
					AdobeAnimateExternalVisualSnapshot visual2;
					bool num = sprite.TryGetExternalVisualForRender(handle, out visual2);
					bool num2 = num && GodotObject.IsInstanceValid(texture2D2) && sprite.SetExternalVisualVisible(handle, visible: false) && sprite.SetExternalVisualTexture(handle, texture2D2) && sprite.SetExternalVisualTransform(handle, transform2D) && sprite.SetExternalVisualModulate(handle, color);
					sprite.BeginExternalVisualPreparationForRender(78L);
					flag4 = num2 && sprite.TryBuildExternalVisualGpuStateForRender(adobeAnimateGpuManagedVisualBinding.VisualIndex, graph.Owners[adobeAnimateGpuManagedVisualBinding.OwnerIndex].Definition.AtlasTextureArrayRid, out var state2, out failureReason2) && !state2.Visible && state2.LocalTransform.IsEqualApprox(transform2D) && state2.Modulate.IsEqualApprox(color);
					if (num)
					{
						sprite.SetExternalVisualTexture(handle, visual2.Texture);
						sprite.SetExternalVisualTransform(handle, visual2.Transform);
						sprite.SetExternalVisualModulate(handle, visual2.Modulate);
						sprite.SetExternalVisualVisible(handle, visual2.Visible);
					}
					break;
				}
			}
		}
		AdobeAnimateRenderSnapshot snapshot;
		bool flag6 = sprite.TryBuildRenderSnapshot(out snapshot);
		List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
		int num3 = (flag6 ? AdobeAnimateDrawItemBuilder.Build(snapshot, list, snapshot.Definition.GpuPoseTextureArray, 77L) : 0);
		bool flag7 = false;
		for (int j = 0; j < num3; j++)
		{
			if (!list[j].UseShaderPose)
			{
				flag7 = true;
				break;
			}
		}
		sprite.CommitExternalVisualCrowdFrame(77L);
		bool flag8 = !visual.Visible;
		sprite.UnregisterExternalVisual(handle);
		bool visible = visual.Visible;
		visual.QueueFree();
		sprite.QueueFree();
		GD.Print($"[ExternalVisualProbe] graphBuilt={flag} graphFailure={failureReason} hasExternalBinding={flag2} gpuStateValid={flag3} gpuStateMutationValid={flag4} sharedExternalAtlasValid={flag5} snapshotBuilt={flag6} compositeContainsExternal={flag7} takeoverCommitted={flag8} nativeRestored={visible}");
		return flag && flag2 && flag3 && flag4 && flag5 && flag7 && flag8 && visible;
	}

	private static bool ValidateSharedExternalAtlas(Rid ownerAtlasRid)
	{
		Texture2D texture2D = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/Icetrap.png");
		Texture2D texture2D2 = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/ButterSplat.png");
		Texture2D texture2D3 = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/ButterGeneSplat.png");
		Texture2D texture2D4 = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/Zombie/ZombieHeadGrossout.png");
		if (!ownerAtlasRid.IsValid || !GodotObject.IsInstanceValid(texture2D) || !GodotObject.IsInstanceValid(texture2D2) || !GodotObject.IsInstanceValid(texture2D3) || !GodotObject.IsInstanceValid(texture2D4))
		{
			return false;
		}
		AdobeAnimateExternalTextureAtlasAllocation allocation;
		bool flag = AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture2D, new Rect2(Vector2.Zero, texture2D.GetSize()), out allocation);
		AdobeAnimateExternalTextureAtlasAllocation allocation2;
		bool flag2 = AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture2D2, new Rect2(Vector2.Zero, texture2D2.GetSize()), out allocation2);
		AdobeAnimateExternalTextureAtlasAllocation allocation3;
		bool flag3 = AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture2D3, new Rect2(Vector2.Zero, texture2D3.GetSize()), out allocation3);
		AdobeAnimateExternalTextureAtlasAllocation allocation4;
		bool flag4 = AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(texture2D4, new Rect2(Vector2.Zero, texture2D4.GetSize()), out allocation4);
		bool result = flag && flag2 && flag3 && flag4 && allocation.UsesTextureArray && allocation2.UsesTextureArray && allocation3.UsesTextureArray && allocation4.UsesTextureArray && allocation.TextureArrayRid.IsValid && allocation.TextureArrayRid == ownerAtlasRid && allocation2.TextureArrayRid == ownerAtlasRid && allocation3.TextureArrayRid == ownerAtlasRid && allocation4.TextureArrayRid == ownerAtlasRid;
		GD.Print($"[ExternalVisualProbe] atlas ownerRidValid={ownerAtlasRid.IsValid} icetrapValid={flag} icetrapArray={allocation.UsesTextureArray} icetrapRidValid={allocation.TextureArrayRid.IsValid} icetrapRidSame={allocation.TextureArrayRid == ownerAtlasRid} butterValid={flag2} butterArray={allocation2.UsesTextureArray} butterRidSame={allocation2.TextureArrayRid == ownerAtlasRid} geneButterValid={flag3} geneButterArray={allocation3.UsesTextureArray} geneButterRidSame={allocation3.TextureArrayRid == ownerAtlasRid} grossoutValid={flag4} grossoutArray={allocation4.UsesTextureArray} grossoutRidSame={allocation4.TextureArrayRid == ownerAtlasRid}");
		return result;
	}

	private async Task<bool> ValidateDirectAtlasMediaReplace()
	{
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Gold/IceTallnut/IceTallnut.tscn");
		if (packedScene == null)
		{
			return false;
		}
		Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
		if (!(node is AdobeAnimateSprite sprite))
		{
			node.QueueFree();
			return false;
		}
		AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool flag = sprite.SetAtlasReplace("IceTallnut_skin1_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_2.png", queueUpdate: false);
		sprite.UpdateMediaReplaceData();
		AdobeAnimateExternalTextureAtlasAllocation allocation;
		bool flag2 = AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation("res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_2.png", out allocation);
		bool result = flag && flag2 && allocation.UsesTextureArray && allocation.TextureArrayRid.IsValid && sprite.GetReplace("IceTallnut_skin1_1.png") == null && sprite.GetAtlasReplacePath("IceTallnut_skin1_1.png") == "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_2.png" && sprite.mediaReplaceAtlasShared && sprite.mediaReplaceAtlasUsesTextureArray && GodotObject.IsInstanceValid(sprite.mediaReplaceAtlasArray) && sprite.mediaReplaceAtlasArray.GetRid() == allocation.TextureArrayRid;
		GD.Print($"[ExternalVisualProbe] directAtlasMediaReplace requested={flag} allocationValid={flag2} usesTextureArray={allocation.UsesTextureArray} pathStored={sprite.GetAtlasReplacePath("IceTallnut_skin1_1.png") == "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_2.png"} textureNotLoaded={sprite.GetReplace("IceTallnut_skin1_1.png") == null} shared={sprite.mediaReplaceAtlasShared} arrayRidSame={GodotObject.IsInstanceValid(sprite.mediaReplaceAtlasArray) && sprite.mediaReplaceAtlasArray.GetRid() == allocation.TextureArrayRid}");
		sprite.QueueFree();
		return result;
	}

	private async Task<bool> ValidateCharacterBuffVisuals()
	{
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		PackedScene packedScene2 = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
		if (packedScene == null || packedScene2 == null)
		{
			return false;
		}
		TowerDefenseCharacter zombie = packedScene.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		TowerDefenseCharacter plant = packedScene2.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		zombie.inGame = false;
		zombie.editorPreviewMode = true;
		plant.inGame = false;
		plant.editorPreviewMode = true;
		zombie.Position = new Vector2(120f, 180f);
		plant.Position = new Vector2(360f, 180f);
		AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		AddChild(plant, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		int zombieBaseline = zombie.sprite.GetActiveExternalVisualIndicesForRender().Length;
		int plantBaseline = plant.sprite.GetActiveExternalVisualIndicesForRender().Length;
		AdobeAnimateExternalVisualSnapshot visual;
		bool initiallyNoIceVisual = !GodotObject.IsInstanceValid(zombie.icetrapSprite) && !zombie.TryGetIceTrapExternalVisualForDiagnostics(out visual);
		TowerDefenseCharacterBuffFrozen towerDefenseCharacterBuffFrozen = new TowerDefenseCharacterBuffFrozen();
		towerDefenseCharacterBuffFrozen.character = zombie;
		towerDefenseCharacterBuffFrozen.iceSpeedDownTime = 0.0;
		towerDefenseCharacterBuffFrozen.Enter();
		Sprite2D cachedIceSprite = zombie.icetrapSprite;
		AdobeAnimateExternalVisualSnapshot visual2;
		bool flag = TryFindExternalVisual(zombie.sprite, cachedIceSprite, out visual2);
		bool iceCreated = GodotObject.IsInstanceValid(cachedIceSprite) && flag && zombie.sprite.GetActiveExternalVisualIndicesForRender().Length == zombieBaseline + 1;
		bool iceVisible = iceCreated && AdobeAnimateManagedSprite2D.GetLogicalVisible(cachedIceSprite) && visual2.Visible;
		Transform2D iceLocalTransform = visual2.Transform;
		Vector2 iceSpritePosition = (GodotObject.IsInstanceValid(cachedIceSprite) ? cachedIceSprite.Position : Vector2.Zero);
		zombie.Position += new Vector2(64f, 0f);
		zombie.Scale = new Vector2(0f - Mathf.Abs(zombie.Scale.X), zombie.Scale.Y);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		AdobeAnimateExternalVisualSnapshot visual3;
		bool iceAttachmentStable = TryFindExternalVisual(zombie.sprite, cachedIceSprite, out visual3) && visual3.Transform.IsEqualApprox(iceLocalTransform) && GodotObject.IsInstanceValid(cachedIceSprite) && cachedIceSprite.Position.IsEqualApprox(iceSpritePosition);
		zombie.buff.SetBuffVisualVisible("Frozen", visible: false);
		AdobeAnimateExternalVisualSnapshot visual4;
		bool flag2 = TryFindExternalVisual(zombie.sprite, cachedIceSprite, out visual4);
		bool iceHiddenAndCached = GodotObject.IsInstanceValid(cachedIceSprite) && cachedIceSprite == zombie.icetrapSprite && !AdobeAnimateManagedSprite2D.GetLogicalVisible(cachedIceSprite) && flag2 && !visual4.Visible && zombie.sprite.GetActiveExternalVisualIndicesForRender().Length == zombieBaseline + 1;
		TowerDefenseCharacterBuffFrozen towerDefenseCharacterBuffFrozen2 = new TowerDefenseCharacterBuffFrozen();
		towerDefenseCharacterBuffFrozen2.character = zombie;
		towerDefenseCharacterBuffFrozen2.iceSpeedDownTime = 0.0;
		towerDefenseCharacterBuffFrozen2.Enter();
		bool iceReused = GodotObject.IsInstanceValid(cachedIceSprite) && cachedIceSprite == zombie.icetrapSprite && AdobeAnimateManagedSprite2D.GetLogicalVisible(cachedIceSprite) && zombie.sprite.GetActiveExternalVisualIndicesForRender().Length == zombieBaseline + 1;
		zombie.buff.SetBuffVisualVisible("Frozen", visible: false);
		int length = zombie.sprite.GetActiveExternalVisualIndicesForRender().Length;
		TowerDefenseCharacterBuffButter towerDefenseCharacterBuffButter = new TowerDefenseCharacterBuffButter
		{
			character = zombie
		};
		towerDefenseCharacterBuffButter.Enter();
		AdobeAnimateExternalVisualSnapshot visual5;
		bool zombieButterSlot = GodotObject.IsInstanceValid(towerDefenseCharacterBuffButter.butterSprite) && TryFindExternalVisual(zombie.sprite, towerDefenseCharacterBuffButter.butterSprite, out visual5) && visual5.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot && visual5.Descriptor.Slot == zombie.headSlot;
		towerDefenseCharacterBuffButter.Exit();
		bool zombieButterReleased = zombie.sprite.GetActiveExternalVisualIndicesForRender().Length == length;
		TowerDefenseCharacterBuffButterGene towerDefenseCharacterBuffButterGene = new TowerDefenseCharacterBuffButterGene
		{
			character = zombie
		};
		towerDefenseCharacterBuffButterGene.Enter();
		AdobeAnimateExternalVisualSnapshot visual6;
		bool zombieGeneButterSlot = GodotObject.IsInstanceValid(towerDefenseCharacterBuffButterGene.butterSprite) && TryFindExternalVisual(zombie.sprite, towerDefenseCharacterBuffButterGene.butterSprite, out visual6) && visual6.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot && visual6.Descriptor.Slot == zombie.headSlot;
		towerDefenseCharacterBuffButterGene.Exit();
		bool zombieGeneButterReleased = zombie.sprite.GetActiveExternalVisualIndicesForRender().Length == length;
		TowerDefenseCharacterBuffButter towerDefenseCharacterBuffButter2 = new TowerDefenseCharacterBuffButter
		{
			character = plant
		};
		towerDefenseCharacterBuffButter2.Enter();
		AdobeAnimateExternalVisualSnapshot visual7;
		bool plantButterRoot = !GodotObject.IsInstanceValid(plant.headSlot) && GodotObject.IsInstanceValid(towerDefenseCharacterBuffButter2.butterSprite) && TryFindExternalVisual(plant.sprite, towerDefenseCharacterBuffButter2.butterSprite, out visual7) && visual7.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Root;
		towerDefenseCharacterBuffButter2.Exit();
		bool plantButterReleased = plant.sprite.GetActiveExternalVisualIndicesForRender().Length == plantBaseline;
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		plant.ProcessMode = ProcessModeEnum.Disabled;
		for (int i = 0; i < zombie.sprite.mediaReplaceUse.Count; i++)
		{
			zombie.sprite.mediaReplaceUse[i] = false;
		}
		zombie.sprite.UpdateMediaReplaceData();
		int zombieScanCount = zombie.CharacterExternalVisualCompatibilityScanCount;
		int plantScanCount = plant.CharacterExternalVisualCompatibilityScanCount;
		for (int j = 0; j < 120; j++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool compatibilityScanStable = zombieScanCount == 1 && plantScanCount == 1 && zombie.CharacterExternalVisualCompatibilityScanCount == zombieScanCount && plant.CharacterExternalVisualCompatibilityScanCount == plantScanCount;
		PackedScene packedScene3 = GD.Load<PackedScene>("res://Asset/Anime/Effect/Star/Star.tscn");
		PackedScene packedScene4 = GD.Load<PackedScene>("res://Asset/Anime/Effect/SleepZ/SleepZ.tscn");
		AdobeAnimateSprite dizziness = packedScene3?.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		AdobeAnimateSprite sleep = packedScene4?.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		if (GodotObject.IsInstanceValid(dizziness) && GodotObject.IsInstanceValid(sleep))
		{
			Transform2D dizzinessTarget = zombie.spriteGroup.GlobalTransform;
			dizzinessTarget.Origin = zombie.headSlot.GlobalPosition + new Vector2(-5f, -10f);
			Transform2D sleepTarget = plant.spriteGroup.GlobalTransform * new Transform2D(0f, Vector2.One, 0f, new Vector2(20f, 25f));
			int j = zombie.CharacterAnimatedStatusTopologyChangeCount;
			int plantTopologyBefore = plant.CharacterAnimatedStatusTopologyChangeCount;
			zombie.AttachAnimatedStatusVisual(dizziness, zombie.headSlot, dizzinessTarget);
			plant.AttachAnimatedStatusVisual(sleep, null, sleepTarget);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			bool num = GraphOwnsAnimatedChild(zombie.sprite, dizziness, "dizziness", expectRootTransform: false);
			bool flag6 = GraphOwnsAnimatedChild(plant.sprite, sleep, "sleep", expectRootTransform: true);
			flag3 = num && flag6 && zombie.CharacterAnimatedStatusVisualCount == 1 && plant.CharacterAnimatedStatusVisualCount == 1;
			zombie.AttachAnimatedStatusVisual(dizziness, zombie.headSlot, dizzinessTarget);
			plant.AttachAnimatedStatusVisual(sleep, null, sleepTarget);
			flag4 = zombie.CharacterAnimatedStatusTopologyChangeCount == j + 1 && plant.CharacterAnimatedStatusTopologyChangeCount == plantTopologyBefore + 1;
			zombie.DetachAnimatedStatusVisual(dizziness);
			plant.DetachAnimatedStatusVisual(sleep);
			flag5 = zombie.CharacterAnimatedStatusVisualCount == 0 && plant.CharacterAnimatedStatusVisualCount == 0 && zombie.CharacterAnimatedStatusTopologyChangeCount == j + 2 && plant.CharacterAnimatedStatusTopologyChangeCount == plantTopologyBefore + 2 && dizziness.GetParent() == null && sleep.GetParent() == null;
			dizziness.QueueFree();
			sleep.QueueFree();
		}
		zombie.QueueFree();
		plant.QueueFree();
		GD.Print($"[ExternalVisualProbe] character initiallyNoIceVisual={initiallyNoIceVisual} iceCreated={iceCreated} iceVisible={iceVisible} iceAttachmentStable={iceAttachmentStable} iceHiddenAndCached={iceHiddenAndCached} iceReused={iceReused} zombieButterSlot={zombieButterSlot} zombieButterReleased={zombieButterReleased} zombieGeneButterSlot={zombieGeneButterSlot} zombieGeneButterReleased={zombieGeneButterReleased} plantButterRoot={plantButterRoot} plantButterReleased={plantButterReleased} compatibilityScanStable={compatibilityScanStable} animatedStatusOwned={flag3} animatedStatusTopologyStable={flag4} animatedStatusReleased={flag5}");
		return initiallyNoIceVisual && iceCreated && iceVisible && iceAttachmentStable && iceHiddenAndCached && iceReused && zombieButterSlot && zombieButterReleased && zombieGeneButterSlot && zombieGeneButterReleased && plantButterRoot && plantButterReleased && compatibilityScanStable && flag3 && flag4 && flag5;
	}

	private static bool GraphOwnsAnimatedChild(AdobeAnimateSprite root, AdobeAnimateSprite child, string label, bool expectRootTransform)
	{
		if (!GodotObject.IsInstanceValid(root) || !GodotObject.IsInstanceValid(child))
		{
			return false;
		}
		AdobeAnimateGpuRenderGraphDefinition graph;
		AdobeAnimateSprite[] ownerSprites;
		string failureReason;
		bool flag = AdobeAnimateGpuRenderGraphBuilder.TryBuild(root, out graph, out ownerSprites, out failureReason);
		bool flag2 = child.GetRenderSortRootForRender() == root;
		bool flag3 = flag && Array.Exists(ownerSprites, (AdobeAnimateSprite owner) => owner == child);
		int num = (flag ? Array.FindIndex(ownerSprites, (AdobeAnimateSprite owner) => owner == child) : (-1));
		bool flag4 = !expectRootTransform || (num >= 0 && !graph.Owners[num].UsePos && !graph.Owners[num].UseRotate && graph.Owners[num].LocalTransform.IsEqualApprox(child.Transform));
		GD.Print($"[ExternalVisualProbe] animated label={label} graphBuilt={flag} failure={failureReason} renderRootOwned={flag2} ownerListed={flag3} rootTransformPreserved={flag4} ownerCount={ownerSprites.Length} parent={child.GetParent()?.Name}");
		return flag && flag2 && flag3 && flag4;
	}

	private static bool TryFindExternalVisual(AdobeAnimateSprite owner, Sprite2D visualSprite, out AdobeAnimateExternalVisualSnapshot visual)
	{
		visual = default(AdobeAnimateExternalVisualSnapshot);
		if (!GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(visualSprite))
		{
			return false;
		}
		ReadOnlySpan<int> activeExternalVisualIndicesForRender = owner.GetActiveExternalVisualIndicesForRender();
		for (int i = 0; i < activeExternalVisualIndicesForRender.Length; i++)
		{
			if (owner.TryGetExternalVisualForRender(activeExternalVisualIndicesForRender[i], out visual) && visual.Sprite == visualSprite)
			{
				return true;
			}
		}
		visual = default(AdobeAnimateExternalVisualSnapshot);
		return false;
	}
}
