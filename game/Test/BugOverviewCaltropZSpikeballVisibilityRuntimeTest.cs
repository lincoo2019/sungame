using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewCaltropZSpikeballVisibilityRuntimeTest : Node
{
	private readonly record struct VisibilityPixelMeasure(int ChangedPixels, int VisibleNonBackground, int HiddenNonBackground, ulong VisibleHash, ulong HiddenHash);

	private const string CaltropPacketPath = "res://Asset/Anime/Character/Plant/Chapter8/CaltropZ/Packet/PlantCaltropZ.tres";

	private const string CaltropScenePath = "res://Asset/Anime/Character/Plant/Chapter8/CaltropZ/Scene/TowerDefensePlantCaltropZ.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string SpikeballPacketPath = "res://Asset/Anime/Character/Item/Spikeball/Packet/ItemSpikeball.tres";

	private const string SpikeballScenePath = "res://Asset/Anime/Character/Item/Spikeball/Scene/TowerDefenseItemSpikeball.tscn";

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		BugOverviewCaltropZSpikeballVisibilityRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		bool focusedCarryOnly = OS.GetEnvironment("PVZHE_SPIKEBALL_FOCUSED_CARRY") == "1";
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0091;
				}
				RegisterRealFixtures();
				control = new BugOverviewCaltropZSpikeballVisibilityRuntimeControlStub
				{
					Name = "CaltropZSpikeballVisibilityRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(60f, 0f);
				manager.gridSize = new Vector2(75f, 70f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum, manager.gridBeginPos, manager.gridSize);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				Check(TowerDefenseManager.GetMapFeature() == mapFeature && TowerDefenseManager.GetCharacterNode() == control.characterNode, "The fixture must expose its real production map and character container.");
				await VerifyDirectSpikeballVisual();
				if (!focusedCarryOnly)
				{
					await RunScenario("normal-caltrop-hypnotized-zombie", new Vector2I(3, 2), hypnotizeCaltrop: false, hypnotizeCarrier: true);
					await RunScenario("hypnotized-caltrop-normal-zombie", new Vector2I(6, 4), hypnotizeCaltrop: true, hypnotizeCarrier: false);
				}
				goto end_IL_007a;
				end_IL_0091:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewCaltropZSpikeballVisibilityRuntimeTest] Unexpected exception: {value}");
				goto end_IL_007a;
			}
			return;
			end_IL_007a:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
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
			await WaitFrames(5);
			RestoreRealFixtures();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		int num = (focusedCarryOnly ? 5 : 37);
		bool flag = _failures == 0 && _checks == num;
		GD.Print($"BUG_OVERVIEW_CALTROPZ_SPIKEBALL_VISIBILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyDirectSpikeballVisual()
	{
		PackedScene packed = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Item/Spikeball/Scene/TowerDefenseItemSpikeball.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
		TowerDefenseItemSpikeball spike = packed?.Instantiate<TowerDefenseItemSpikeball>(PackedScene.GenEditState.Disabled);
		AdobeAnimateSprite sprite = (GodotObject.IsInstanceValid(spike) ? spike.GetNodeOrNull<AdobeAnimateSprite>("SpriteGroup/TransformPoint/ItemSpikeball") : null);
		Check(GodotObject.IsInstanceValid(packed) && GodotObject.IsInstanceValid(spike) && GodotObject.IsInstanceValid(sprite), "The direct control must instantiate the full production ItemSpikeball scene and its real Adobe animation node.");
		if (!GodotObject.IsInstanceValid(spike) || !GodotObject.IsInstanceValid(sprite))
		{
			if (GodotObject.IsInstanceValid(spike))
			{
				spike.Free();
			}
			packed?.Dispose();
			return;
		}
		Vector2 vector = new Vector2(256f, 180f);
		spike.Name = "DirectProductionItemSpikeball";
		spike.Position = vector;
		spike.ProcessMode = ProcessModeEnum.Disabled;
		CanvasItem nodeOrNull = spike.GetNodeOrNull<CanvasItem>("ShadowSprite");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.Visible = false;
		}
		TowerDefenseManager.GetCharacterNode().AddChild(spike, forceReadableName: false, InternalMode.Disabled);
		spike.sprite = sprite;
		spike.inGame = true;
		TowerDefenseZombieNormal carrier = InstantiateDirectCharacter<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", new Vector2I(4, 3));
		bool carryFollowPassed = false;
		if (GodotObject.IsInstanceValid(carrier))
		{
			carrier.inGame = true;
			carrier.ProcessMode = ProcessModeEnum.Disabled;
			spike.camp = carrier.camp;
			Vector2 vector2 = vector + new Vector2(42f, 18f);
			carrier.gridPos = new Vector2I(5, 4);
			carrier.SetLogicalGlobalPosition(vector2);
			spike.Carry(carrier);
			spike.BatchUpdate(0.0);
			vector = vector2;
			carryFollowPassed = spike.GetLogicalGlobalPosition().IsEqualApprox(vector2) && spike.gridPos == carrier.gridPos;
		}
		VisibilityPixelMeasure visibilityPixelMeasure = await MeasureSpikeVisibilityPixels(spike, vector);
		GD.Print($"CALTROPZ_SPIKEBALL_DIRECT_VISUAL_METRIC visible_non_background={visibilityPixelMeasure.VisibleNonBackground} hidden_non_background={visibilityPixelMeasure.HiddenNonBackground} visible_hash={visibilityPixelMeasure.VisibleHash:X16} hidden_hash={visibilityPixelMeasure.HiddenHash:X16} diff={visibilityPixelMeasure.ChangedPixels} clip={sprite.clip} frame={sprite.frameIndex}");
		Check(carryFollowPassed && visibilityPixelMeasure.ChangedPixels >= 12 && visibilityPixelMeasure.VisibleNonBackground >= 12, "The directly mounted full production ItemSpikeball must submit real " + $"visible pixels and follow its carrier; carry={carryFollowPassed}, changed={visibilityPixelMeasure.ChangedPixels}, " + $"visible_non_background={visibilityPixelMeasure.VisibleNonBackground}, " + $"hidden_non_background={visibilityPixelMeasure.HiddenNonBackground}, " + $"hashes={visibilityPixelMeasure.VisibleHash:X16}/{visibilityPixelMeasure.HiddenHash:X16}.");
		spike.Visible = false;
		sprite.EnsureFrozenPreviewRenderSubmission();
		await WaitRenderFrames(2);
		spike.Free();
		if (GodotObject.IsInstanceValid(carrier))
		{
			carrier.Free();
		}
		packed.Dispose();
	}

	private async Task RunScenario(string label, Vector2I grid, bool hypnotizeCaltrop, bool hypnotizeCarrier)
	{
		TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter8/CaltropZ/Packet/PlantCaltropZ.tres");
		TowerDefensePacketConfig towerDefensePacketConfig2 = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		TowerDefensePlantCaltropZ caltrop = towerDefensePacketConfig?.Plant(grid, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as TowerDefensePlantCaltropZ;
		TowerDefenseZombieNormal carrier = towerDefensePacketConfig2?.Plant(grid, playAudio: false) as TowerDefenseZombieNormal;
		bool caltropSpawnedImmediately = GodotObject.IsInstanceValid(caltrop);
		bool carrierSpawnedImmediately = GodotObject.IsInstanceValid(carrier);
		if (GodotObject.IsInstanceValid(caltrop))
		{
			caltrop.inGame = false;
		}
		if (GodotObject.IsInstanceValid(carrier))
		{
			carrier.inGame = false;
		}
		await WaitFrames(8);
		Check(GodotObject.IsInstanceValid(caltrop) && GodotObject.IsInstanceValid(carrier), $"{label}: real PlantCaltropZ and ZombieNormal scenes must spawn; immediate={caltropSpawnedImmediately}/{carrierSpawnedImmediately}, after_wait={GodotObject.IsInstanceValid(caltrop)}/{GodotObject.IsInstanceValid(carrier)}.");
		if (!GodotObject.IsInstanceValid(caltrop) || !GodotObject.IsInstanceValid(carrier))
		{
			return;
		}
		caltrop.ProcessMode = ProcessModeEnum.Disabled;
		carrier.ProcessMode = ProcessModeEnum.Disabled;
		if (hypnotizeCaltrop)
		{
			caltrop.Hypnoses();
		}
		if (hypnotizeCarrier)
		{
			carrier.Hypnoses();
		}
		await WaitFrames(4);
		TowerDefenseEnum.CHARACTER_CAMP expectedCamp = ((!hypnotizeCarrier) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		Check(caltrop.camp == expectedCamp && carrier.camp == expectedCamp && caltrop.instance.hypnoses == hypnotizeCaltrop && carrier.instance.hypnoses == hypnotizeCarrier, $"{label}: CaltropZ and its cross-faction carrier must share camp {expectedCamp}; caltrop={caltrop.camp}/{caltrop.instance.hypnoses}, carrier={carrier.camp}/{carrier.instance.hypnoses}.");
		BugOverviewCaltropZSpikeballVisibilityRuntimeTest bugOverviewCaltropZSpikeballVisibilityRuntimeTest = this;
		int condition;
		if (caltrop.config.name == "PlantCaltropZ" && carrier.config.name == "ZombieNormal")
		{
			AttackComponent attackComponent = caltrop.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				attackComponent = carrier.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				condition = ((attackComponent != null && !attackComponent.IsReleased) ? 1 : 0);
				goto IL_04e0;
			}
		}
		condition = 0;
		goto IL_04e0;
		IL_04e0:
		bugOverviewCaltropZSpikeballVisibilityRuntimeTest.Check((byte)condition != 0, label + ": the production scenes must retain their authored attack runtimes.");
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(grid);
		caltrop.SetLogicalGlobalPosition(mapCellPlantPos);
		carrier.SetLogicalGlobalPosition(mapCellPlantPos);
		caltrop.gridPos = grid;
		carrier.gridPos = grid;
		caltrop.inGame = true;
		carrier.inGame = true;
		RefreshEncounterRegistration(caltrop, carrier);
		await WaitFrames(2);
		List<TowerDefenseItemSpikeball> before = GetSpikeballs();
		caltrop.BatchUpdate(0.0);
		await WaitFrames(4);
		List<TowerDefenseItemSpikeball> after = GetSpikeballs();
		Check(after.Count == before.Count + 1, $"{label}: crossing the real CaltropZ must create exactly one real ItemSpikeball; before={before.Count}, after={after.Count}.");
		TowerDefenseItemSpikeball spike = FindNewSpikeball(before, after, carrier);
		Check(GodotObject.IsInstanceValid(spike), label + ": the unique new ItemSpikeball must target the crossing zombie.");
		if (GodotObject.IsInstanceValid(spike))
		{
			spike.ProcessMode = ProcessModeEnum.Disabled;
			spike.BatchUpdate(0.0);
			await WaitFrames(2);
			Check(spike.carryCharacter == carrier && !GodotObject.IsInstanceValid(spike.targetZombie), label + ": the ItemSpikeball must resolve its production carry relation.");
			caltrop.BatchUpdate(0.0);
			await WaitFrames(3);
			Check(GetSpikeballs().Count == after.Count, label + ": repeated overlap scans must not create a second spike for one carrier.");
			Check(carrier.hasSpikeball, label + ": the real carrier must publish hasSpikeball=true.");
			Check(spike.camp == expectedCamp && spike.camp == carrier.camp && spike.camp == caltrop.camp, $"{label}: CaltropZ, carrier, and ItemSpikeball must share camp {expectedCamp}; actual={caltrop.camp}/{carrier.camp}/{spike.camp}.");
			Check(spike.itemLayer == TowerDefenseEnum.LAYER_GROUNDITEM.DAMAGEPART, $"{label}: a carried spike must use the foreground DAMAGEPART layer; got {spike.itemLayer}.");
			Check(spike.ZIndex == carrier.ZIndex + 1, $"{label}: same-row ItemSpikeball must render exactly one ZIndex above its carrier; spike={spike.ZIndex}, carrier={carrier.ZIndex}.");
			Check(spike.Visible && spike.IsVisibleInTree() && GodotObject.IsInstanceValid(spike.sprite) && spike.sprite.Visible && spike.sprite.IsVisibleInTree(), label + ": the real ItemSpikeball root and Adobe sprite must be visible in-tree.");
			Vector2I movedGrid = new Vector2I(Math.Min(grid.X + 1, 9), Math.Min(grid.Y + 1, 5));
			Vector2 movedPosition = TowerDefenseManager.GetMapCellPlantPos(movedGrid) + new Vector2(12f, -4f);
			carrier.gridPos = movedGrid;
			carrier.SetLogicalGlobalPosition(movedPosition);
			carrier.groundHeight = 3.0;
			spike.BatchUpdate(0.0);
			await WaitFrames(2);
			Vector2 logicalGlobalPosition = spike.GetLogicalGlobalPosition();
			Check(spike.gridPos == movedGrid && logicalGlobalPosition.IsEqualApprox(movedPosition) && Math.Abs(spike.groundHeight - carrier.groundHeight) < 0.001 && Math.Abs(spike.z - 10.0) < 0.001, $"{label}: carried spike must follow position, row, ground height, and carry height; grid={spike.gridPos}/{movedGrid}, pos={logicalGlobalPosition}/{movedPosition}, ground={spike.groundHeight}/{carrier.groundHeight}, z={spike.z}.");
			Check(spike.ZIndex == carrier.ZIndex + 1, $"{label}: crossing rows must preserve the foreground ZIndex relation; spike={spike.ZIndex}, carrier={carrier.ZIndex}.");
			TowerDefenseManager.GetCharacterNode().MoveChild(spike, Math.Max(0, carrier.GetIndex()));
			await WaitFrames(2);
			Check(spike.GetIndex() < carrier.GetIndex() && spike.ZIndex > carrier.ZIndex, $"{label}: restore/network tree order may put the spike before its carrier, but the render layer must remain in front; indexes={spike.GetIndex()}/{carrier.GetIndex()}, z={spike.ZIndex}/{carrier.ZIndex}.");
			VisibilityPixelMeasure visibilityPixelMeasure = await MeasureSpikeVisibilityPixels(spike, carrier.GetLogicalGlobalPosition());
			GD.Print($"CALTROPZ_SPIKEBALL_VISIBILITY_METRIC label={label} visible_non_background={visibilityPixelMeasure.VisibleNonBackground} hidden_non_background={visibilityPixelMeasure.HiddenNonBackground} visible_hash={visibilityPixelMeasure.VisibleHash:X16} hidden_hash={visibilityPixelMeasure.HiddenHash:X16} diff={visibilityPixelMeasure.ChangedPixels} clip={spike.sprite.clip} frame={spike.sprite.frameIndex} layer={(int)spike.itemLayer} spike_z={spike.ZIndex} carrier_z={carrier.ZIndex}");
			Check(visibilityPixelMeasure.ChangedPixels >= 12 && visibilityPixelMeasure.VisibleNonBackground >= 12, $"{label}: hiding the real carried spike must change rendered viewport pixels; changed={visibilityPixelMeasure.ChangedPixels}, visible_non_background={visibilityPixelMeasure.VisibleNonBackground}, hidden_non_background={visibilityPixelMeasure.HiddenNonBackground}, hashes={visibilityPixelMeasure.VisibleHash:X16}/{visibilityPixelMeasure.HiddenHash:X16}.");
		}
	}

	private async Task<VisibilityPixelMeasure> MeasureSpikeVisibilityPixels(TowerDefenseItemSpikeball spike, Vector2 center)
	{
		AdobeAnimateSprite sprite = spike.sprite;
		sprite.ProcessMode = ProcessModeEnum.Always;
		sprite.SetFrozenPreview(frozen: false);
		sprite.SetAnimation("Idle");
		sprite.ResetAnimation();
		sprite.SetFrozenPreview(frozen: true);
		spike.Visible = true;
		sprite.EnsureFrozenPreviewRenderSubmission();
		await WaitRenderFrames(8);
		Image visible = GetViewport().GetTexture().GetImage();
		spike.Visible = false;
		sprite.EnsureFrozenPreviewRenderSubmission();
		await WaitRenderFrames(8);
		Image hidden = GetViewport().GetTexture().GetImage();
		spike.Visible = true;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		sprite.EnsureFrozenPreviewRenderSubmission();
		await WaitRenderFrames(4);
		Rect2 visibleRect = GetViewport().GetVisibleRect();
		float num = (float)visible.GetWidth() / visibleRect.Size.X;
		float num2 = (float)visible.GetHeight() / visibleRect.Size.Y;
		Vector2 vector = new Vector2((center.X - visibleRect.Position.X) * num, (center.Y - visibleRect.Position.Y) * num2);
		int num3 = Math.Clamp((int)Math.Floor(vector.X - 80f * num), 0, visible.GetWidth());
		int num4 = Math.Clamp((int)Math.Ceiling(vector.X + 80f * num), 0, visible.GetWidth());
		int num5 = Math.Clamp((int)Math.Floor(vector.Y - 120f * num2), 0, visible.GetHeight());
		int num6 = Math.Clamp((int)Math.Ceiling(vector.Y + 70f * num2), 0, visible.GetHeight());
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		ulong num10 = 1469598103934665603uL;
		ulong num11 = 1469598103934665603uL;
		Color pixel = visible.GetPixel(0, 0);
		Color pixel2 = hidden.GetPixel(0, 0);
		for (int i = num5; i < num6; i++)
		{
			for (int j = num3; j < num4; j++)
			{
				Color pixel3 = visible.GetPixel(j, i);
				Color pixel4 = hidden.GetPixel(j, i);
				if (ColorDelta(pixel3, pixel) > 0.05f)
				{
					num8++;
				}
				if (ColorDelta(pixel4, pixel2) > 0.05f)
				{
					num9++;
				}
				num10 = HashColor(num10, pixel3);
				num11 = HashColor(num11, pixel4);
				if (Math.Abs(pixel3.R - pixel4.R) + Math.Abs(pixel3.G - pixel4.G) + Math.Abs(pixel3.B - pixel4.B) + Math.Abs(pixel3.A - pixel4.A) > 0.08f)
				{
					num7++;
				}
			}
		}
		return new VisibilityPixelMeasure(num7, num8, num9, num10, num11);
	}

	private static float ColorDelta(Color first, Color second)
	{
		return Math.Abs(first.R - second.R) + Math.Abs(first.G - second.G) + Math.Abs(first.B - second.B) + Math.Abs(first.A - second.A);
	}

	private static ulong HashColor(ulong hash, Color color)
	{
		hash = HashByte(hash, ToColorByte(color.R));
		hash = HashByte(hash, ToColorByte(color.G));
		hash = HashByte(hash, ToColorByte(color.B));
		return HashByte(hash, ToColorByte(color.A));
	}

	private static byte ToColorByte(float value)
	{
		return (byte)Math.Clamp((int)Math.Round(value * 255f), 0, 255);
	}

	private static ulong HashByte(ulong hash, byte value)
	{
		return (hash ^ value) * 1099511628211L;
	}

	private async Task WaitRenderFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum, Vector2 gridBegin, Vector2 gridSize)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum,
			gridBeginPos = gridBegin,
			gridSize = gridSize,
			plantOffset = 50.0
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j))?.Init(new TowerDefenseCellConfig());
			}
		}
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static void RefreshEncounterRegistration(TowerDefenseCharacter first, TowerDefenseCharacter second)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		instance.CharacterUnregister(first);
		instance.CharacterUnregister(second);
		instance.CharacterRegister(first);
		instance.CharacterRegister(second);
	}

	private static T InstantiateDirectCharacter<T>(string path, Vector2I grid) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.IgnoreDeep);
		T val = ((packedScene != null) ? packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled) : null);
		Node characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(val) || !GodotObject.IsInstanceValid(characterNode))
		{
			val?.QueueFree();
			return null;
		}
		val.inGame = false;
		val.editorPreviewMode = false;
		val.gridPos = grid;
		val.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(grid));
		characterNode.AddChild(val, forceReadableName: false, InternalMode.Disabled);
		return val;
	}

	private static List<TowerDefenseItemSpikeball> GetSpikeballs()
	{
		List<TowerDefenseItemSpikeball> list = new List<TowerDefenseItemSpikeball>();
		Node characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return list;
		}
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is TowerDefenseItemSpikeball towerDefenseItemSpikeball && GodotObject.IsInstanceValid(towerDefenseItemSpikeball))
			{
				list.Add(towerDefenseItemSpikeball);
			}
		}
		return list;
	}

	private static TowerDefenseItemSpikeball FindNewSpikeball(List<TowerDefenseItemSpikeball> before, List<TowerDefenseItemSpikeball> after, TowerDefenseZombie carrier)
	{
		foreach (TowerDefenseItemSpikeball item in after)
		{
			if (!before.Contains(item) && (item.targetZombie == carrier || item.carryCharacter == carrier))
			{
				return item;
			}
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantCaltropZ", "res://Asset/Anime/Character/Plant/Chapter8/CaltropZ/Packet/PlantCaltropZ.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterPacket("ItemSpikeball", "res://Asset/Anime/Character/Item/Spikeball/Packet/ItemSpikeball.tres");
		RegisterCharacter("PlantCaltropZ", "res://Asset/Anime/Character/Plant/Chapter8/CaltropZ/Scene/TowerDefensePlantCaltropZ.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		RegisterCharacter("ItemSpikeball", "res://Asset/Anime/Character/Item/Spikeball/Scene/TowerDefenseItemSpikeball.tscn");
	}

	private void RegisterPacket(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
		{
			_previousPackets[key] = value;
		}
		else
		{
			_missingPackets.Add(key);
		}
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RegisterCharacter(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			_previousCharacters[key] = value;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (string missingPacket in _missingPackets)
		{
			instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
		}
		foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
		{
			instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
		}
		foreach (string missingCharacter in _missingCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS.Remove(missingCharacter);
		}
		foreach (KeyValuePair<string, Resource> previousCharacter in _previousCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS[previousCharacter.Key] = previousCharacter.Value;
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewCaltropZSpikeballVisibilityRuntimeTest] " + message);
		}
	}
}
