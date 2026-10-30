using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewSharkWaterlineRuntimeTest : Node
{
	private const string SharkScenePath = "res://Asset/Anime/Character/Zombie/Chapter4/ZombieShark/Scene/TowerDefenseZombieShark.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		SharkWaterlineRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseZombieShark shark = null;
		TowerDefenseZombieShark spawnInWaterShark = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					return;
				}
				control = new SharkWaterlineRuntimeControlStub
				{
					Name = "SharkWaterlineRuntimeControl",
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
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefenseCellInstance waterCell = TowerDefenseManager.GetMapCell(TestGrid);
				Check(GodotObject.IsInstanceValid(waterCell), "The real map fixture must publish the Shark test cell before character initialization.");
				if (!GodotObject.IsInstanceValid(waterCell))
				{
					return;
				}
				PackedScene scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter4/ZombieShark/Scene/TowerDefenseZombieShark.tscn", null, ResourceLoader.CacheMode.Ignore);
				shark = scene?.Instantiate<TowerDefenseZombieShark>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(shark), "The real Shark Zombie character scene must instantiate.");
				if (!GodotObject.IsInstanceValid(shark))
				{
					return;
				}
				shark.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(TestGrid);
				shark.gridPos = TestGrid;
				control.characterNode.AddChild(shark, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				waterCell.isWater = true;
				Check(GodotObject.IsInstanceValid(waterCell) && waterCell.isWater && shark.cell == waterCell, "The Shark must use the real map cell selected as water for this fixture.");
				WaterInteractionComponent water = shark.waterInteractionComponent;
				GroundHeightComponent groundHeight = shark.groundHeightComponent;
				BugOverviewSharkWaterlineRuntimeTest bugOverviewSharkWaterlineRuntimeTest = this;
				int condition;
				if (water != null && water.Lifecycle == ComponentRuntimeLifecycle.Active)
				{
					SwimComponent swimComponent = shark.swimComponent;
					if (swimComponent != null && swimComponent.Lifecycle == ComponentRuntimeLifecycle.Active)
					{
						if (groundHeight != null && groundHeight.Lifecycle == ComponentRuntimeLifecycle.Active)
						{
							condition = (GodotObject.IsInstanceValid(shark.sprite) ? 1 : 0);
							goto IL_042b;
						}
					}
				}
				condition = 0;
				goto IL_042b;
				IL_042b:
				bugOverviewSharkWaterlineRuntimeTest.Check((byte)condition != 0, "The real WaterInteraction, Swim, GroundHeight, and Adobe sprite runtimes must be active.");
				if (water == null || water.Lifecycle != ComponentRuntimeLifecycle.Active)
				{
					goto end_IL_00e0;
				}
				SwimComponent swimComponent2 = shark.swimComponent;
				if (swimComponent2 == null || swimComponent2.Lifecycle != ComponentRuntimeLifecycle.Active)
				{
					goto end_IL_00e0;
				}
				if (groundHeight == null || groundHeight.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(shark.sprite))
				{
					goto end_IL_00e0;
				}
				Check(Mathf.IsEqualApprox((float)shark.waterHeight, 47f) && shark.sprite.HasClip("SwimRun"), "The real Shark scene must retain waterHeight=47 and its authored SwimRun clip.");
				float landSpriteGroupY = shark.spriteGroup.Position.Y;
				Check(Mathf.IsZeroApprox((float)shark.groundHeight) && Mathf.IsZeroApprox((float)shark.z), "The Shark fixture must begin on the land plane before entering water.");
				shark.inWater = true;
				await WaitFrames(2);
				Check(shark.inWater && water.isInWater, "Changing the real Shark to water must activate WaterInteraction.");
				Check(Mathf.IsEqualApprox((float)shark.originalWaterHeight, 47f) && Mathf.IsEqualApprox((float)shark.waterHeight, 47f), "Shark.InWater must preserve the authored 47-pixel submerge height.");
				Check(Mathf.IsEqualApprox(groundHeight.GetTargetHeight(), -47f), $"The real water cell must target groundHeight=-47; actual={groundHeight.GetTargetHeight()}.");
				Check(Mathf.IsEqualApprox((float)shark.groundHeight, -47f) && Mathf.IsEqualApprox((float)shark.z, -47f), $"Swim entry must place the Shark 47 pixels below the terrain plane; ground={shark.groundHeight}, z={shark.z}.");
				Check(Mathf.IsEqualApprox(shark.spriteGroup.Position.Y, landSpriteGroupY + 47f), $"The Shark sprite group must move down by the authored 47 pixels; landY={landSpriteGroupY}, waterY={shark.spriteGroup.Position.Y}.");
				Check((shark.instance.maskFlags & 0x20) != 0, "Shark.InWater must switch the live character mask to UNDER_WATER.");
				Check(!shark.shadowSprite.Visible, "The real WaterInteraction must hide the Shark shadow while submerged.");
				float expectedWaterline = GetExpectedWaterline(shark, water);
				VerticalClipState verticalClipState = shark.sprite.GetVerticalClipState();
				Check(verticalClipState.Enabled && Mathf.IsEqualApprox(verticalClipState.DownY, expectedWaterline), $"WaterInteraction must publish the screen-space waterline; actual={verticalClipState.DownY}, expected={expectedWaterline}.");
				shark.sprite.SetAnimation("SwimRun");
				float startX = shark.GlobalPosition.X;
				bool everySnapshotClipped = true;
				bool everyDrawItemClipped = true;
				int minimumDrawItems = 2147483647;
				for (int sample = 0; sample < 12; sample++)
				{
					shark.GlobalPosition += new Vector2(-3f, 0f);
					await WaitFrames(1);
					AdobeAnimateRenderSnapshot snapshot;
					bool flag = shark.sprite.TryBuildRenderSnapshot(out snapshot, allowUnchanged: false);
					everySnapshotClipped &= flag && snapshot.Clip == "SwimRun" && snapshot.VerticalClip.Enabled && Mathf.IsEqualApprox(snapshot.VerticalClip.DownY, expectedWaterline);
					List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
					if (flag)
					{
						AdobeAnimateDrawItemBuilder.Build(snapshot, list, snapshot.Definition?.GpuPoseTextureArray);
					}
					minimumDrawItems = Math.Min(minimumDrawItems, list.Count);
					bool flag2 = list.Count > 0;
					foreach (AdobeAnimateDrawItem item in list)
					{
						flag2 &= item.ClipEnabled && Mathf.IsEqualApprox(item.ClipDown, expectedWaterline);
					}
					everyDrawItemClipped = everyDrawItemClipped && flag2;
				}
				Check(everySnapshotClipped, "All 12 moving SwimRun frames must retain the same screen-space waterline snapshot.");
				Check(everyDrawItemClipped, $"Every real SwimRun draw item must carry the waterline clip; minimum items={minimumDrawItems}.");
				Check(shark.GlobalPosition.X < startX && Mathf.IsEqualApprox((float)shark.groundHeight, -47f) && Mathf.IsEqualApprox((float)shark.z, -47f) && Mathf.IsEqualApprox(shark.spriteGroup.Position.Y, landSpriteGroupY + 47f), "The Shark must remain submerged at z=-47 while moving horizontally through all samples.");
				Vector2I gridPos = new Vector2I(5, 3);
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
				if (GodotObject.IsInstanceValid(mapCell))
				{
					mapCell.isWater = true;
				}
				spawnInWaterShark = scene?.Instantiate<TowerDefenseZombieShark>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(spawnInWaterShark), "The production Shark scene must also instantiate directly on an existing water cell.");
				if (!GodotObject.IsInstanceValid(mapCell) || !GodotObject.IsInstanceValid(spawnInWaterShark))
				{
					goto end_IL_00e0;
				}
				List<string> spawnStartedClips = new List<string>();
				spawnInWaterShark.sprite.OnAnimeStarted += spawnStartedClips.Add;
				spawnInWaterShark.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos);
				spawnInWaterShark.gridPos = gridPos;
				spawnInWaterShark.cell = mapCell;
				control.characterNode.AddChild(spawnInWaterShark, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				Check(spawnInWaterShark.inWater && Mathf.IsEqualApprox((float)spawnInWaterShark.originalWaterHeight, 47f) && Mathf.IsEqualApprox((float)spawnInWaterShark.waterHeight, 47f) && Mathf.IsEqualApprox((float)spawnInWaterShark.groundHeight, -47f) && Mathf.IsEqualApprox((float)spawnInWaterShark.z, -47f), $"A Shark born on water must retain the authored 47-pixel depth instead of resetting it during Ready; original={spawnInWaterShark.originalWaterHeight}, water={spawnInWaterShark.waterHeight}, ground={spawnInWaterShark.groundHeight}, z={spawnInWaterShark.z}.");
				Check(spawnStartedClips.Contains("JumpInWater"), "A Shark born on water must visibly start JumpInWater; started=" + string.Join(',', spawnStartedClips) + ".");
				bool flag3 = spawnInWaterShark.sprite.clip == "SwimRun" || spawnInWaterShark.sprite.track.Exists((AdobeAnimateTrack track) => track.clip == "SwimRun");
				Check(spawnInWaterShark.inSwimPlay && flag3, $"JumpInWater must queue SwimRun exactly through the character animation track; clip={spawnInWaterShark.sprite.clip}, queued={spawnInWaterShark.sprite.track.Count}, inSwimPlay={spawnInWaterShark.inSwimPlay}.");
				Check(spawnInWaterShark.sprite.GetVerticalClipState().Enabled && (spawnInWaterShark.waterInteractionComponent?.isInWater ?? false), "A Shark born on water must enable its real waterline clipping runtime on the first visible pose.");
				goto end_IL_00c5;
				end_IL_00e0:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSharkWaterlineRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00c5;
			}
			return;
			end_IL_00c5:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(spawnInWaterShark) && !spawnInWaterShark.IsQueuedForDeletion())
			{
				spawnInWaterShark.QueueFree();
			}
			if (GodotObject.IsInstanceValid(shark) && !shark.IsQueuedForDeletion())
			{
				shark.QueueFree();
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
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag4 = _failures == 0 && _checks == 23;
		GD.Print($"SHARK_WATERLINE_RESULT passed={flag4} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag4) ? 2 : 0);
	}

	private static float GetExpectedWaterline(TowerDefenseZombieShark shark, WaterInteractionComponent water)
	{
		Transform2D screenTransform = shark.GetViewport().GetScreenTransform();
		screenTransform.Origin = Vector2.Zero;
		float y = water.discardOffsetIn * water.GetScaleRatioY() + (float)shark.groundHeight;
		Vector2 vector = shark.spriteGroup.GlobalPosition + new Vector2(0f, y);
		return (screenTransform * vector).Y;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = new Vector2(100f, 76f),
				plantOffset = 50.0
			}
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewSharkWaterlineRuntimeTest] " + message);
		}
	}
}
