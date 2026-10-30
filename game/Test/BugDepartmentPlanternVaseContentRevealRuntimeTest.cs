using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentPlanternVaseContentRevealRuntimeTest : Node
{
	private const string PlanternScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Plantern/Scene/TowerDefensePlantern.tscn";

	private const string PlantVaseScenePath = "res://Asset/Anime/Character/Vase/Plant/Scene/TowerDefenseVasePlant.tscn";

	private const string ZombieVaseScenePath = "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn";

	private const string PlantContentPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres";

	private const string ZombieContentPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string PlantContentSpritePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/PeaShooterSingle.tscn";

	private const string ZombieContentSpritePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn";

	private static readonly Vector2I CenterGrid = new Vector2I(3, 3);

	private static readonly Vector2I AdjacentGrid = new Vector2I(4, 3);

	private static readonly Vector2I OutsideGrid = new Vector2I(5, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugDepartmentPlanternVaseContentRevealRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseCharacter plantern = null;
		TowerDefenseVase plantVase = null;
		TowerDefenseVase zombieVase = null;
		TowerDefenseInGamePacketShow plantPreview = null;
		TowerDefenseInGamePacketShow zombiePreview = null;
		AdobeAnimateRenderBackend previousBackend = AdobeAnimateRenderBackend.GpuCrowd;
		bool backendChanged = false;
		System.Collections.Generic.Dictionary<string, Resource> previousSprites = new System.Collections.Generic.Dictionary<string, Resource>();
		HashSet<string> missingSprites = new HashSet<string>();
		List<PackedScene> fixtureSprites = new List<PackedScene>();
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(Global.Instance), "TowerDefenseManager and Global autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(Global.Instance))
				{
					goto end_IL_012c;
				}
				previousBackend = Global.Instance.adobeAnimateRenderBackend;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.CpuPose;
				backendChanged = true;
				Check(Global.Instance.adobeAnimateRenderBackend == AdobeAnimateRenderBackend.CpuPose, "Closing GPU optimization must select the production CPU Pose backend.");
				control = new BugDepartmentPlanternVaseContentRevealRuntimeControlStub
				{
					Name = "PlanternVaseContentRevealRuntimeControl",
					isGameRunning = false,
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
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				plantern = Instantiate<TowerDefenseCharacter>("res://Asset/Anime/Character/Plant/Chapter0/Plantern/Scene/TowerDefensePlantern.tscn");
				plantVase = Instantiate<TowerDefenseVase>("res://Asset/Anime/Character/Vase/Plant/Scene/TowerDefenseVasePlant.tscn");
				zombieVase = Instantiate<TowerDefenseVase>("res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn");
				TowerDefensePacketConfig plantContent = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres");
				TowerDefensePacketConfig zombieContent = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
				PackedScene packedScene = LoadScene("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/PeaShooterSingle.tscn");
				PackedScene packedScene2 = LoadScene("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(plantern) && GodotObject.IsInstanceValid(plantVase) && GodotObject.IsInstanceValid(zombieVase) && plantern.config?.name == "PlantPlantern" && plantVase.config?.name == "VasePlant" && zombieVase.config?.name == "VaseZombie", "The fixture must instantiate the real Plantern, plant vase, and zombie vase scenes.");
				Check(GodotObject.IsInstanceValid(plantContent) && GodotObject.IsInstanceValid(zombieContent) && plantContent.saveKey == "PlantPeaShooterSingle" && zombieContent.saveKey == "ZombieNormal", "The fixture must use real plant and zombie packet content resources.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(packedScene) && GodotObject.IsInstanceValid(packedScene2), "The fixture must load the real plant and zombie packet sprite scenes.");
				if (!GodotObject.IsInstanceValid(plantern) || !GodotObject.IsInstanceValid(plantVase) || !GodotObject.IsInstanceValid(zombieVase) || !GodotObject.IsInstanceValid(plantContent) || !GodotObject.IsInstanceValid(zombieContent) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(packedScene2))
				{
					goto end_IL_012c;
				}
				RememberAndReplaceSprite(ResourceManager.Instance.CHARCTAER_SPRITE, previousSprites, missingSprites, plantContent.saveKey, packedScene);
				RememberAndReplaceSprite(ResourceManager.Instance.CHARCTAER_SPRITE, previousSprites, missingSprites, zombieContent.saveKey, packedScene2);
				fixtureSprites.Add(packedScene);
				fixtureSprites.Add(packedScene2);
				PrepareCharacter(plantern, CenterGrid);
				PrepareCharacter(plantVase, CenterGrid);
				PrepareCharacter(zombieVase, AdjacentGrid);
				control.characterNode.AddChild(plantern, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(plantVase, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(zombieVase, forceReadableName: false, InternalMode.Disabled);
				AddToCell(plantVase, CenterGrid);
				AddToCell(zombieVase, AdjacentGrid);
				await WaitFrames(5);
				plantVase.SetContentConfig(plantContent);
				zombieVase.SetContentConfig(zombieContent);
				await WaitFrames(2);
				plantPreview = plantVase.GetNodeOrNull<TowerDefenseInGamePacketShow>("%PacketShow");
				zombiePreview = zombieVase.GetNodeOrNull<TowerDefenseInGamePacketShow>("%PacketShow");
				plantPreview?.OnMouseExited();
				zombiePreview?.OnMouseExited();
				await WaitFrames(2);
				GD.Print($"PLANTERN_VASE_PREVIEW_STATE stage=initialized plant={DescribePreview(plantPreview)} zombie={DescribePreview(zombiePreview)}");
				Check((plantern.instance.physiqueTypeFlags & 0x40) != 0, "The real Plantern instance must retain the LIGHT physique used by vase reveal.");
				BugDepartmentPlanternVaseContentRevealRuntimeTest bugDepartmentPlanternVaseContentRevealRuntimeTest = this;
				LightDetectionComponent lightDetectionComponent = plantVase.lightDetectionComponent;
				int condition;
				if (lightDetectionComponent != null && !lightDetectionComponent.IsReleased && lightDetectionComponent.detectionRadius == 1 && lightDetectionComponent.includeCenter)
				{
					lightDetectionComponent = zombieVase.lightDetectionComponent;
					condition = ((lightDetectionComponent != null && !lightDetectionComponent.IsReleased && lightDetectionComponent.detectionRadius == 1 && lightDetectionComponent.includeCenter) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugDepartmentPlanternVaseContentRevealRuntimeTest.Check((byte)condition != 0, "Both real vase types must use the active 3x3 LightDetection runtime including the center cell.");
				Check(GodotObject.IsInstanceValid(plantPreview) && GodotObject.IsInstanceValid(zombiePreview) && plantPreview.config == plantContent && zombiePreview.config == zombieContent, "Assigning real vase content must initialize the matching plant and zombie preview controls.");
				Check(GodotObject.IsInstanceValid(plantPreview?.sprite) && GodotObject.IsInstanceValid(zombiePreview?.sprite) && plantPreview.sprite.IsFrozenPreview && zombiePreview.sprite.IsFrozenPreview && plantPreview.sprite.forceLocalRender && zombiePreview.sprite.forceLocalRender, "Both real vase content previews must be frozen local-render roots.");
				plantVase.BatchUpdate(0.5);
				zombieVase.BatchUpdate(0.5);
				Check(!plantVase.showPacket && !zombieVase.showPacket && !plantPreview.Visible && !zombiePreview.Visible, "Without a registered light source, both real vase contents must remain hidden.");
				Check(TryBuildCpuPreview(plantVase.sprite, out var snapshot, out var drawItems) && drawItems.Count > 0 && TryBuildCpuPreview(zombieVase.sprite, out snapshot, out var drawItems2) && drawItems2.Count > 0, "Under CPU Pose, both unlit real vase shells must retain nonempty draw items.");
				List<AdobeAnimateDrawItem> drawItems3;
				bool flag = TryBuildCpuPreview(plantPreview.sprite, out snapshot, out drawItems3);
				bool flag2 = TryBuildCpuPreview(zombiePreview.sprite, out snapshot, out drawItems3);
				Check(!flag && !flag2 && CountVisibleImmediateFallbackInstances(plantPreview.previewClip) == 0 && CountVisibleImmediateFallbackInstances(zombiePreview.previewClip) == 0, "Hidden content must reject CPU snapshots and publish no visible local fallback instances.");
				AddToCell(plantern, CenterGrid);
				Check(TowerDefenseManager.GetMapCell(CenterGrid).HasLight(), "The production map cell must discover the real Plantern through its LIGHT physique.");
				plantVase.BatchUpdate(0.5);
				zombieVase.BatchUpdate(0.5);
				Check(plantVase.lightDetectionComponent.CheckShow(), "A plant vase sharing the Plantern's center cell must be revealed.");
				Check(plantVase.showPacket && plantPreview.Visible && plantVase.sprite.Visible && plantVase.sprite.SelfModulate.A <= 0.001f && GodotObject.IsInstanceValid(plantVase.backSprite) && plantVase.backSprite.IsVisibleInTree(), "The center plant vase must hide only its front shell while retaining the revealed rear silhouette.");
				Check(plantPreview.config == plantContent && plantPreview.config.characterConfig is TowerDefensePlantConfig, "The revealed center preview must still represent the real plant content.");
				Check(zombieVase.lightDetectionComponent.CheckShow(), "A zombie vase in an adjacent 3x3 cell must be revealed.");
				Check(zombieVase.showPacket && zombiePreview.Visible && zombieVase.sprite.Visible && zombieVase.sprite.SelfModulate.A <= 0.001f && GodotObject.IsInstanceValid(zombieVase.backSprite) && zombieVase.backSprite.IsVisibleInTree(), "The adjacent zombie vase must hide only its front shell while retaining the revealed rear silhouette.");
				Check(zombiePreview.config == zombieContent && zombiePreview.config.characterConfig is TowerDefenseZombieConfig, "The revealed adjacent preview must still represent the real zombie content.");
				await WaitFrames(4);
				AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				Check(aggregateRenderStats.CrowdRoots == 0 && aggregateRenderStats.CpuRoots > 0 && aggregateRenderStats.CpuValidationFailures == 0, $"The lit real scene must render exclusively through valid CPU Pose roots (crowd={aggregateRenderStats.CrowdRoots}, cpu={aggregateRenderStats.CpuRoots}, failures={aggregateRenderStats.CpuValidationFailures}).");
				Check(TryBuildCpuPreview(plantPreview.sprite, out var snapshot2, out var drawItems4) && snapshot2.RenderMountParent == plantPreview.previewClip && drawItems4.Count > 0 && drawItems4.TrueForAll((AdobeAnimateDrawItem item) => !item.UseShaderPose), "The lit plant content must build nonempty CPU draw items on its real PreviewClip mount.");
				Check(CountVisibleImmediateFallbackInstances(plantPreview.previewClip) > 0, "The lit plant content must publish visible local CPU fallback instances.");
				Check(TryBuildCpuPreview(zombiePreview.sprite, out var snapshot3, out var drawItems5) && snapshot3.RenderMountParent == zombiePreview.previewClip && drawItems5.Count > 0 && drawItems5.TrueForAll((AdobeAnimateDrawItem item) => !item.UseShaderPose), "The lit zombie content must build nonempty CPU draw items on its real PreviewClip mount.");
				Check(CountVisibleImmediateFallbackInstances(zombiePreview.previewClip) > 0, "The lit zombie content must publish visible local CPU fallback instances.");
				Check(TryBuildCpuPreview(plantVase.sprite, out var snapshot4, out drawItems3) && snapshot4.Modulate.A <= 0.001f && TryBuildCpuPreview(plantVase.backSprite, out var snapshot5, out var drawItems6) && snapshot5.Modulate.A > 0.99f && drawItems6.Count > 0 && TryBuildCpuPreview(zombieVase.sprite, out var snapshot6, out drawItems3) && snapshot6.Modulate.A <= 0.001f && TryBuildCpuPreview(zombieVase.backSprite, out var snapshot7, out var drawItems7) && snapshot7.Modulate.A > 0.99f && drawItems7.Count > 0, "Revealing both vase types must keep the nested rear silhouettes opaque while only the front shells become transparent.");
				MoveToCell(zombieVase, OutsideGrid);
				zombieVase.BatchUpdate(0.5);
				await WaitFrames(3);
				Check(!zombieVase.lightDetectionComponent.CheckShow(), "A vase two columns away must remain outside the configured 3x3 reveal range.");
				Check(!zombieVase.showPacket && !zombiePreview.Visible && zombieVase.sprite.Visible && zombieVase.sprite.SelfModulate.A > 0.99f && zombieVase.backSprite.IsVisibleInTree(), "The out-of-range zombie vase must hide its content and restore its shell.");
				Check(!zombiePreview.IsVisibleInTree() && GodotObject.IsInstanceValid(zombiePreview.sprite) && !zombiePreview.sprite.IsVisibleInTree() && CountVisibleImmediateFallbackInstances(zombiePreview.previewClip) == 0, "Moving out of Plantern range must hide the zombie content and its visible fallback.");
				Check(TryBuildCpuPreview(zombieVase.sprite, out snapshot, out var drawItems8) && drawItems8.Count > 0, "After moving out of range, the real zombie vase shell must remain CPU-renderable.");
				MoveToCell(zombieVase, AdjacentGrid);
				zombieVase.BatchUpdate(0.5);
				await WaitFrames(4);
				Check(zombieVase.showPacket && zombiePreview.Visible && zombieVase.sprite.Visible && zombieVase.sprite.SelfModulate.A <= 0.001f && zombieVase.backSprite.IsVisibleInTree() && TryBuildCpuPreview(zombiePreview.sprite, out snapshot, out var drawItems9) && drawItems9.Count > 0 && CountVisibleImmediateFallbackInstances(zombiePreview.previewClip) > 0, "Returning to Plantern range must republish the retained frozen CPU content without a blank frame.");
				goto end_IL_0109;
				end_IL_012c:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PlanternVaseContentReveal] Unexpected exception: {value}");
				goto end_IL_0109;
			}
			return;
			end_IL_0109:;
		}
		finally
		{
			ReleasePreview(plantPreview);
			ReleasePreview(zombiePreview);
			RemoveFromCell(plantern);
			RemoveFromCell(plantVase);
			RemoveFromCell(zombieVase);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
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
			if (backendChanged && GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.adobeAnimateRenderBackend = previousBackend;
			}
			await WaitFrames(8);
			RestoreSpriteRegistry(previousSprites, missingSprites);
			foreach (PackedScene item in fixtureSprites)
			{
				if (GodotObject.IsInstanceValid(item))
				{
					item.Dispose();
				}
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag3 = _failures == 0 && _checks == 30;
		GD.Print($"PLANTERN_VASE_CONTENT_REVEAL_RESULT version=3 passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private static PackedScene LoadScene(string path)
	{
		return ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private static void RememberAndReplaceSprite(System.Collections.Generic.Dictionary<string, Resource> registry, System.Collections.Generic.Dictionary<string, Resource> previous, HashSet<string> missing, string key, PackedScene replacement)
	{
		if (registry.TryGetValue(key, out var value))
		{
			previous[key] = value;
		}
		else
		{
			missing.Add(key);
		}
		registry[key] = replacement;
	}

	private static void RestoreSpriteRegistry(System.Collections.Generic.Dictionary<string, Resource> previous, HashSet<string> missing)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (string item in missing)
		{
			instance.CHARCTAER_SPRITE.Remove(item);
		}
		foreach (KeyValuePair<string, Resource> previou in previous)
		{
			instance.CHARCTAER_SPRITE[previou.Key] = previou.Value;
		}
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I grid)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		character.inGame = true;
		character.editorPreviewMode = false;
		character.gridPos = grid;
		character.cell = TowerDefenseManager.GetMapCell(grid);
		character.Position = instance.gridBeginPos + new Vector2(((float)grid.X - 0.5f) * instance.gridSize.X, ((float)grid.Y - 0.5f) * instance.gridSize.Y);
	}

	private static void AddToCell(TowerDefenseCharacter character, Vector2I grid)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(grid);
		if (GodotObject.IsInstanceValid(mapCell) && !mapCell.characterList.Contains(character))
		{
			mapCell.characterList.Add(character);
		}
	}

	private static void MoveToCell(TowerDefenseCharacter character, Vector2I grid)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			RemoveFromCell(character);
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			character.gridPos = grid;
			character.cell = TowerDefenseManager.GetMapCell(grid);
			character.Position = instance.gridBeginPos + new Vector2(((float)grid.X - 0.5f) * instance.gridSize.X, ((float)grid.Y - 0.5f) * instance.gridSize.Y);
			AddToCell(character, grid);
		}
	}

	private static void RemoveFromCell(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character?.cell))
		{
			character.cell.characterList.Remove(character);
		}
	}

	private static bool TryBuildCpuPreview(AdobeAnimateSprite sprite, out AdobeAnimateRenderSnapshot snapshot, out List<AdobeAnimateDrawItem> drawItems)
	{
		snapshot = default(AdobeAnimateRenderSnapshot);
		drawItems = new List<AdobeAnimateDrawItem>();
		if (!GodotObject.IsInstanceValid(sprite) || !sprite.TryBuildRenderSnapshot(out snapshot, allowUnchanged: false))
		{
			return false;
		}
		AdobeAnimateDrawItemBuilder.Build(snapshot, drawItems);
		return drawItems.Count > 0;
	}

	private static int CountVisibleImmediateFallbackInstances(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return 0;
		}
		int num = 0;
		if (node is AdobeAnimateMultiMeshBatcher adobeAnimateMultiMeshBatcher && adobeAnimateMultiMeshBatcher.IsVisibleInTree() && node.Name.ToString().StartsWith("AdobeAnimateSnapshotFallbackZ_", StringComparison.Ordinal))
		{
			num += adobeAnimateMultiMeshBatcher.GetVisibleInstanceCountForTest();
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			num += CountVisibleImmediateFallbackInstances(child);
		}
		return num;
	}

	private static string DescribePreview(TowerDefenseInGamePacketShow preview)
	{
		if (!GodotObject.IsInstanceValid(preview))
		{
			return "preview_invalid";
		}
		AdobeAnimateSprite sprite = preview.sprite;
		if (GodotObject.IsInstanceValid(sprite))
		{
			return $"preview_visible={preview.Visible},preview_tree_visible={preview.IsVisibleInTree()},sprite_visible={sprite.Visible},sprite_tree_visible={sprite.IsVisibleInTree()},frozen={sprite.IsFrozenPreview},local={sprite.forceLocalRender},paused={sprite.pause},clip_valid={GodotObject.IsInstanceValid(preview.previewClip)}";
		}
		return $"preview_visible={preview.Visible},sprite_invalid";
	}

	private static void ReleasePreview(TowerDefenseInGamePacketShow preview)
	{
		if (GodotObject.IsInstanceValid(preview))
		{
			ReleasePreviewNode(preview);
			preview.Clear();
		}
	}

	private static void ReleasePreviewNode(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.Visible = false;
			adobeAnimateSprite.pause = true;
			adobeAnimateSprite.ClearRenderClipControl();
			adobeAnimateSprite.ReleaseForcedCpuPoseData();
			AdobeAnimateRenderManager.ReleaseImmediateSubmission(adobeAnimateSprite);
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			ReleasePreviewNode(child);
		}
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PlanternVaseContentReveal] " + message);
		}
	}
}
