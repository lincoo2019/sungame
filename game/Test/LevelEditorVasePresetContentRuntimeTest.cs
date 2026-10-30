using System;
using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class LevelEditorVasePresetContentRuntimeTest : Node
{
	private const string ResultMarker = "LEVEL_EDITOR_VASE_PRESET_CONTENT_RESULT";

	private const string VaseScenePath = "res://Asset/Anime/Character/Vase/Plant/Scene/TowerDefenseVasePlant.tscn";

	private const string VasePacketPath = "res://Asset/Anime/Character/Vase/Plant/Packet/VasePlant.tres";

	private const string ContentPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres";

	private const string ContentSpritePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/PeaShooterSingle.tscn";

	private const string CaptureEnvironmentName = "PVZHE_LEVEL_EDITOR_VASE_CAPTURE";

	private static readonly Vector2I VaseGrid = new Vector2I(4, 3);

	private Resource _previousContentSprite;

	private bool _contentSpriteWasMissing;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousEditor = Global.IsEditor;
		string previousScene = SceneManager.CurrentScene;
		AdobeAnimateRenderBackend previousBackend = Global.Instance?.adobeAnimateRenderBackend ?? AdobeAnimateRenderBackend.GpuCrowd;
		LevelEditorVasePresetContentControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseVase vase = null;
		TowerDefenseInGamePacketShow preview = null;
		PackedScene contentSprite = null;
		int contentPixels = 0;
		int shellPixels = 0;
		bool captureSaved = false;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(Global.Instance) && GodotObject.IsInstanceValid(SceneManager.Instance) && GodotObject.IsInstanceValid(ResourceManager.Instance), "关卡编辑器运行时依赖必须可用。");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(SceneManager.Instance) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0131;
				}
				Global.Instance.isEditor = true;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				SceneManager.Instance.currentScene = "LevelEditorStage";
				ColorRect node = new ColorRect
				{
					Color = new Color(0.07f, 0.13f, 0.09f),
					MouseFilter = Control.MouseFilterEnum.Ignore,
					Size = new Vector2(1280f, 720f)
				};
				AddChild(node, forceReadableName: false, InternalMode.Disabled);
				control = new LevelEditorVasePresetContentControlStub
				{
					Name = "LevelEditorVasePresetContentControl",
					isInit = true,
					isGameRunning = false,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(220f, 190f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Vase/Plant/Scene/TowerDefenseVasePlant.tscn", null, ResourceLoader.CacheMode.Ignore);
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Vase/Plant/Packet/VasePlant.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefensePacketConfig contentPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres", null, ResourceLoader.CacheMode.Ignore);
				contentSprite = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/PeaShooterSingle.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene) && GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(contentPacket) && GodotObject.IsInstanceValid(contentSprite), "真实罐子、罐子卡片及预设内容资源必须加载成功。");
				if (!GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(contentPacket) || !GodotObject.IsInstanceValid(contentSprite))
				{
					goto end_IL_0131;
				}
				RememberContentSprite(contentPacket.saveKey, contentSprite);
				vase = packedScene.Instantiate<TowerDefenseVase>(PackedScene.GenEditState.Disabled);
				vase.inGame = false;
				vase.editorPreviewMode = true;
				vase.editorMapPreviewMode = true;
				vase.ProcessMode = ProcessModeEnum.Disabled;
				vase.gridPos = VaseGrid;
				vase.cell = TowerDefenseManager.GetMapCell(VaseGrid);
				vase.Position = new Vector2(620f, 390f);
				vase.packet = towerDefensePacketConfig;
				vase.packetConfig = contentPacket;
				control.characterNode.AddChild(vase, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(12);
				preview = vase.GetNodeOrNull<TowerDefenseInGamePacketShow>("%PacketShow");
				Check(vase.editorPreviewMode && !vase.inGame && vase.ProcessMode == ProcessModeEnum.Disabled, "罐子必须保持无战斗逻辑的编辑器预览状态。");
				Check(GodotObject.IsInstanceValid(preview) && preview.config == contentPacket, "预设内容必须初始化真实 PacketShow 控件。");
				Check(vase.showPacket && preview.Visible && preview.IsVisibleInTree(), "关卡编辑器中的预设内容控件必须立即可见。");
				Check(GodotObject.IsInstanceValid(preview.sprite) && preview.sprite.IsFrozenPreview && preview.sprite.forceLocalRender, "预设内容必须使用冻结的本地预览渲染根。");
				Check(vase.sprite.Visible && vase.sprite.SelfModulate.A <= 0.001f && GodotObject.IsInstanceValid(vase.backSprite) && vase.backSprite.IsVisibleInTree(), "显示预设内容时必须只隐藏前壳自身并保留后壳轮廓。");
				using (Image visibleImage = await CaptureImage())
				{
					string capturePath = System.Environment.GetEnvironmentVariable("PVZHE_LEVEL_EDITOR_VASE_CAPTURE");
					if (!string.IsNullOrWhiteSpace(capturePath))
					{
						string directoryName = Path.GetDirectoryName(capturePath);
						if (!string.IsNullOrWhiteSpace(directoryName))
						{
							Directory.CreateDirectory(directoryName);
						}
						captureSaved = visibleImage.SavePng(capturePath) == Error.Ok;
					}
					preview.Visible = false;
					await WaitFrames(5);
					using Image hiddenImage = await CaptureImage();
					contentPixels = CountChangedPixels(visibleImage, hiddenImage);
					Check(contentPixels >= 80, $"隐藏预设内容后必须移除真实可见像素，当前差异像素为 {contentPixels}。");
					vase.backSprite.Visible = false;
					await WaitFrames(5);
					using Image second = await CaptureImage();
					shellPixels = CountChangedPixels(hiddenImage, second);
					Check(shellPixels >= 80, $"隐藏后壳后必须移除真实罐子轮廓像素，当前差异像素为 {shellPixels}。");
					Check(string.IsNullOrWhiteSpace(capturePath) || captureSaved, "指定的预设罐子截图必须成功保存。");
				}
				goto end_IL_010e;
				end_IL_0131:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[LevelEditorVasePresetContent] {value}");
				goto end_IL_010e;
			}
			return;
			end_IL_010e:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(preview))
			{
				ReleasePreview(preview);
			}
			if (GodotObject.IsInstanceValid(vase))
			{
				vase.skipDestroySet = true;
				vase.QueueFree();
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.isEditor = previousEditor;
				Global.Instance.adobeAnimateRenderBackend = previousBackend;
			}
			if (GodotObject.IsInstanceValid(SceneManager.Instance))
			{
				SceneManager.Instance.currentScene = previousScene;
			}
			RestoreContentSprite();
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(contentSprite))
			{
				contentSprite.Dispose();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"{"LEVEL_EDITOR_VASE_PRESET_CONTENT_RESULT"} passed={flag} checks={_checks} failures={_failures} contentPixels={contentPixels} shellPixels={shellPixels} captureSaved={captureSaved} renderer={RenderingServer.GetCurrentRenderingMethod()}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = new Vector2(220f, 190f),
				gridSize = new Vector2(100f, 76f)
			}
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<Image> CaptureImage()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		return GetViewport().GetTexture().GetImage();
	}

	private static int CountChangedPixels(Image first, Image second)
	{
		int num = Math.Min(first.GetWidth(), second.GetWidth());
		int num2 = Math.Min(first.GetHeight(), second.GetHeight());
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Color pixel = first.GetPixel(j, i);
				Color pixel2 = second.GetPixel(j, i);
				if (Math.Abs(pixel.R - pixel2.R) + Math.Abs(pixel.G - pixel2.G) + Math.Abs(pixel.B - pixel2.B) + Math.Abs(pixel.A - pixel2.A) > 0.08f)
				{
					num3++;
				}
			}
		}
		return num3;
	}

	private void RememberContentSprite(string key, PackedScene replacement)
	{
		if (ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue(key, out var value))
		{
			_previousContentSprite = value;
		}
		else
		{
			_contentSpriteWasMissing = true;
		}
		ResourceManager.Instance.CHARCTAER_SPRITE[key] = replacement;
	}

	private void RestoreContentSprite()
	{
		if (GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			if (_contentSpriteWasMissing)
			{
				ResourceManager.Instance.CHARCTAER_SPRITE.Remove("PlantPeaShooterSingle");
			}
			else if (GodotObject.IsInstanceValid(_previousContentSprite))
			{
				ResourceManager.Instance.CHARCTAER_SPRITE["PlantPeaShooterSingle"] = _previousContentSprite;
			}
		}
	}

	private static void ReleasePreview(TowerDefenseInGamePacketShow preview)
	{
		preview.Visible = false;
		if (GodotObject.IsInstanceValid(preview.sprite))
		{
			preview.sprite.pause = true;
			preview.sprite.ClearRenderClipControl();
			preview.sprite.ReleaseForcedCpuPoseData();
			AdobeAnimateRenderManager.ReleaseImmediateSubmission(preview.sprite);
		}
		preview.Clear();
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[LevelEditorVasePresetContent] " + message);
		}
	}
}
