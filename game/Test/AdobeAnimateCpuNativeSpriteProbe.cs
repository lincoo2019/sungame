using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateCpuNativeSpriteProbe : Node
{
	private const string Result = "ADOBE_ANIMATE_CPU_NATIVE_SPRITE_RESULT";

	private const string SharedTexturePath = "res://Asset/Texture/Character/Effect/Icetrap.png";

	private const string AnimationScenePath = "res://Asset/Anime/Character/Zombie/Challenge/JacksonX/ZombieJacksonX.tscn";

	private readonly List<string> _failures = new List<string>();

	private readonly List<Node> _ownedNodes = new List<Node>();

	private readonly List<AdobeAnimateExternalVisualHandle> _externalHandles = new List<AdobeAnimateExternalVisualHandle>();

	private AdobeAnimateRenderBackend _originalBackend;

	private AdobeAnimateSprite _root;

	private int _checks;

	private int _merged;

	private int _native;

	private int _ignored;

	private int _cpuFallbackRoots;

	public override async void _Ready()
	{
		try
		{
			if (Global.Instance == null)
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			await RunProbe();
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		finally
		{
			await Cleanup();
		}
		bool flag = _failures.Count == 0 && _checks == 10;
		GD.Print($"{"ADOBE_ANIMATE_CPU_NATIVE_SPRITE_RESULT"} passed={flag} checks={_checks} failures={_failures.Count} merged={_merged} native={_native} ignored={_ignored} cpuFallbackRoots={_cpuFallbackRoots}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("ADOBE_ANIMATE_CPU_NATIVE_SPRITE_RESULT failure=" + failure);
		}
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunProbe()
	{
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/JacksonX/ZombieJacksonX.tscn");
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			throw new InvalidOperationException("JacksonX animation scene must load.");
		}
		Node2D node2D = new Node2D
		{
			Name = "CpuNativeSpriteHost"
		};
		AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
		_ownedNodes.Add(node2D);
		_root = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		_root.Name = "CpuNativeSpriteRoot";
		node2D.AddChild(_root, forceReadableName: false, InternalMode.Disabled);
		AdobeAnimateSlot nodeOrNull = _root.GetNodeOrNull<AdobeAnimateSlot>("HeadSlot");
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			throw new InvalidOperationException("JacksonX HeadSlot fixture must exist.");
		}
		Texture2D texture2D = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/Icetrap.png");
		Sprite2D mergedSprite = new Sprite2D
		{
			Name = "MergedManagedSprite",
			Texture = texture2D,
			Visible = true
		};
		nodeOrNull.AddChild(mergedSprite, forceReadableName: false, InternalMode.Disabled);
		Sprite2D materialNative = new Sprite2D
		{
			Name = "MaterialNativeSprite",
			Texture = texture2D,
			Visible = true,
			Material = new ShaderMaterial()
		};
		Sprite2D foreignNative = new Sprite2D
		{
			Name = "ForeignNativeSprite",
			Texture = CreateTexture(Colors.Magenta),
			Visible = true
		};
		Sprite2D hidden = new Sprite2D
		{
			Name = "HiddenExternalSprite",
			Texture = CreateTexture(Colors.Cyan),
			Visible = false
		};
		node2D.AddChild(materialNative, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(foreignNative, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(hidden, forceReadableName: false, InternalMode.Disabled);
		int materialOriginalZIndex = materialNative.ZIndex;
		bool materialOriginalZRelative = materialNative.ZAsRelative;
		int foreignOriginalZIndex = foreignNative.ZIndex;
		bool foreignOriginalZRelative = foreignNative.ZAsRelative;
		AdobeAnimateExternalVisualDescriptor descriptor = new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation);
		_externalHandles.Add(_root.RegisterExternalVisual(materialNative, descriptor));
		_externalHandles.Add(_root.RegisterExternalVisual(foreignNative, descriptor));
		_externalHandles.Add(_root.RegisterExternalVisual(hidden, descriptor));
		TextureLayered textureLayered = AdobeAnimateGlobalAtlasCache.PreloadVisualTextureArray();
		Check(GodotObject.IsInstanceValid(textureLayered) && textureLayered.GetRid().IsValid && GodotObject.IsInstanceValid(texture2D) && AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture2D, new Rect2(Vector2.Zero, texture2D.GetSize()), out var allocation) && allocation.UsesTextureArray && allocation.TextureArrayRid == textureLayered.GetRid(), "The managed fixture texture must use the shared visual atlas array.");
		AdobeAnimateCpuVisualClassification adobeAnimateCpuVisualClassification = AdobeAnimateCpuVisualClassifier.ClassifyManagedSlot(new AdobeAnimateManagedSlotSprite(nodeOrNull, mergedSprite), textureLayered.GetRid(), default(AdobeAnimateDrawItem), default(AdobeAnimateSortPath));
		AdobeAnimateCpuVisualClassification adobeAnimateCpuVisualClassification2 = ClassifyExternal(_externalHandles[0], textureLayered.GetRid());
		AdobeAnimateCpuVisualClassification adobeAnimateCpuVisualClassification3 = ClassifyExternal(_externalHandles[1], textureLayered.GetRid());
		AdobeAnimateCpuVisualClassification adobeAnimateCpuVisualClassification4 = ClassifyExternal(_externalHandles[2], textureLayered.GetRid());
		RecordDisposition(adobeAnimateCpuVisualClassification.Disposition);
		RecordDisposition(adobeAnimateCpuVisualClassification2.Disposition);
		RecordDisposition(adobeAnimateCpuVisualClassification3.Disposition);
		RecordDisposition(adobeAnimateCpuVisualClassification4.Disposition);
		Check(adobeAnimateCpuVisualClassification.Disposition == AdobeAnimateCpuVisualDisposition.MergedMesh, "The compatible managed Slot sprite must merge into the CPU root Mesh.");
		Check(adobeAnimateCpuVisualClassification2.Disposition == AdobeAnimateCpuVisualDisposition.NativeSprite, "The material-bearing Sprite2D must remain native.");
		Check(adobeAnimateCpuVisualClassification3.Disposition == AdobeAnimateCpuVisualDisposition.NativeSprite, "The foreign-texture Sprite2D must remain native.");
		Check(adobeAnimateCpuVisualClassification4.Disposition == AdobeAnimateCpuVisualDisposition.IgnoredInvisible, "The hidden registered visual must be ignored.");
		Check(_merged + _native == 3 && _ignored == 1, "Every mixed-visual fixture must receive exactly one classification.");
		_root.SetAnimation("Walk");
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.CpuPose;
		await WaitProcessFrames(5);
		AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		_cpuFallbackRoots = aggregateRenderStats.CpuFallbackRoots;
		Check(aggregateRenderStats.CpuRoots == 1 && aggregateRenderStats.CpuFallbackRoots == 0 && aggregateRenderStats.CrowdRoots == 0 && aggregateRenderStats.FallbackRoots == aggregateRenderStats.CpuRoots, $"The live CPU Pose transaction must publish through the ordered fallback batcher (cpuRoots={aggregateRenderStats.CpuRoots}, cpuFallback={aggregateRenderStats.CpuFallbackRoots}, crowd={aggregateRenderStats.CrowdRoots}, fallback={aggregateRenderStats.FallbackRoots}).");
		Check(AdobeAnimateManagedSprite2D.IsCrowdManaged(mergedSprite) && !mergedSprite.Visible, "The merged managed Sprite2D must be hidden by CPU Pose batch takeover.");
		Check(!materialNative.Visible && foreignNative.Visible && AdobeAnimateManagedSprite2D.IsCrowdManaged(materialNative) && !AdobeAnimateManagedSprite2D.IsCrowdManaged(foreignNative), $"CPU Pose must merge the shared-atlas visual and leave the foreign-texture visual native (materialVisible={materialNative.Visible}, foreignVisible={foreignNative.Visible}, materialManaged={AdobeAnimateManagedSprite2D.IsCrowdManaged(materialNative)}, foreignManaged={AdobeAnimateManagedSprite2D.IsCrowdManaged(foreignNative)}).");
		_root.GetExternalVisualDiagnosticStats(out var activeExternal, out var nativeExternal);
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
		await WaitProcessFrames(3);
		Check(!hidden.Visible && activeExternal == 2 && nativeExternal == 1 && materialNative.ZIndex == materialOriginalZIndex && materialNative.ZAsRelative == materialOriginalZRelative && foreignNative.ZIndex == foreignOriginalZIndex && foreignNative.ZAsRelative == foreignOriginalZRelative, $"The ignored visual must stay hidden and GPU publication must restore native order states (active={activeExternal}, native={nativeExternal}).");
	}

	private AdobeAnimateCpuVisualClassification ClassifyExternal(AdobeAnimateExternalVisualHandle handle, Rid atlasRid)
	{
		if (!_root.TryGetExternalVisualForRender(handle, out var visual))
		{
			throw new InvalidOperationException("Registered external visual snapshot must resolve.");
		}
		return AdobeAnimateCpuVisualClassifier.ClassifyExternalVisual(in visual, atlasRid, default(AdobeAnimateDrawItem), default(AdobeAnimateSortPath));
	}

	private void RecordDisposition(AdobeAnimateCpuVisualDisposition disposition)
	{
		switch (disposition)
		{
		case AdobeAnimateCpuVisualDisposition.MergedMesh:
			_merged++;
			break;
		case AdobeAnimateCpuVisualDisposition.NativeSprite:
			_native++;
			break;
		case AdobeAnimateCpuVisualDisposition.IgnoredInvisible:
			_ignored++;
			break;
		}
	}

	private async Task Cleanup()
	{
		if (Global.Instance != null)
		{
			Global.Instance.adobeAnimateRenderBackend = _originalBackend;
		}
		if (GodotObject.IsInstanceValid(_root))
		{
			for (int i = 0; i < _externalHandles.Count; i++)
			{
				_root.UnregisterExternalVisual(_externalHandles[i]);
			}
		}
		for (int num = _ownedNodes.Count - 1; num >= 0; num--)
		{
			Node node = _ownedNodes[num];
			if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
			{
				node.QueueFree();
			}
		}
		await WaitProcessFrames(2);
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static ImageTexture CreateTexture(Color color)
	{
		using Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(color);
		return ImageTexture.CreateFromImage(image);
	}

	private void Check(bool condition, string failure)
	{
		_checks++;
		if (!condition)
		{
			_failures.Add(failure);
		}
	}
}
