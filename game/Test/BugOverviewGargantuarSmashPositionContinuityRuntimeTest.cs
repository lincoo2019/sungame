using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGargantuarSmashPositionContinuityRuntimeTest : Node
{
	private const string GargantuarPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres";

	private const string GargantuarScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn";

	private const string WallnutPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private static readonly Vector2I ScenarioGrid = new Vector2I(4, 2);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		GargantuarSmashPositionContinuityControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlant wallnut = null;
		TowerDefenseZombieGargantuar gargantuar = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00e0;
				}
				RegisterRealFixtures();
				control = new GargantuarSmashPositionContinuityControlStub
				{
					Name = "GargantuarSmashPositionContinuityControl",
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
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
				TowerDefensePacketConfig gargantuarPacket = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres");
				wallnut = towerDefensePacketConfig?.Plant(ScenarioGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(5);
				gargantuar = gargantuarPacket?.Plant(ScenarioGrid, playAudio: false) as TowerDefenseZombieGargantuar;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(wallnut) && wallnut.config?.name == "PlantWallnut", "The fixture must use the real Wall-nut scene and packet.");
				Check(GodotObject.IsInstanceValid(gargantuar) && gargantuar.config?.name == "ZombieGargantuar", "The fixture must use the real Gargantuar scene and packet.");
				if (!GodotObject.IsInstanceValid(wallnut) || !GodotObject.IsInstanceValid(gargantuar))
				{
					goto end_IL_00e0;
				}
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(ScenarioGrid);
				Check(GodotObject.IsInstanceValid(mapCell) && mapCell.characterList.Contains(wallnut) && wallnut.cell == mapCell, "The real Wall-nut must occupy the smash target cell.");
				BugOverviewGargantuarSmashPositionContinuityRuntimeTest bugOverviewGargantuarSmashPositionContinuityRuntimeTest = this;
				AttackComponent attackComponent = gargantuar.attackComponent;
				int condition;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					GargantuarSmashComponent gargantuarSmashComponent = gargantuar.gargantuarSmashComponent;
					condition = ((gargantuarSmashComponent != null && !gargantuarSmashComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewGargantuarSmashPositionContinuityRuntimeTest.Check((byte)condition != 0, "The real Gargantuar attack and smash runtimes must be active.");
				BugOverviewGargantuarSmashPositionContinuityRuntimeTest bugOverviewGargantuarSmashPositionContinuityRuntimeTest2 = this;
				GroundMoveComponent groundMoveComponent = gargantuar.groundMoveComponent;
				bugOverviewGargantuarSmashPositionContinuityRuntimeTest2.Check(groundMoveComponent != null && !groundMoveComponent.IsReleased && groundMoveComponent.HasMovementSource, "The real Gargantuar must retain its authored GroundSlot movement source.");
				if ((gargantuar.attackComponent?.IsReleased ?? true) || (gargantuar.gargantuarSmashComponent?.IsReleased ?? true))
				{
					goto end_IL_00e0;
				}
				gargantuar.Walk();
				await WaitFrames(12);
				Check(gargantuar.CurrentStateHandle?.StableId == "zombie.walk" && (gargantuar.groundMoveComponent?.Alive ?? false), "The precondition must exercise live authored walk/root motion before Smash.");
				wallnut.ProcessMode = ProcessModeEnum.Disabled;
				gargantuar.ProcessMode = ProcessModeEnum.Disabled;
				wallnut.instance.hitpointsBase = 10000.0;
				wallnut.instance.hitpointsSave = 10000.0;
				wallnut.instance.hitpoints = 10000.0;
				gargantuar.GlobalPosition = wallnut.GlobalPosition + new Vector2(45f, 0f);
				gargantuar.gridPos = ScenarioGrid;
				gargantuar.attackComponent.target = wallnut;
				Check(gargantuar.CanTarget(wallnut) && gargantuar.CanCollision(wallnut.instance.maskFlags) && gargantuar.CheckSameLine(wallnut.gridPos.Y), "The opposing real Wall-nut must be a valid same-line Smash target.");
				Vector2 attackStart = gargantuar.GlobalPosition;
				Vector2 spriteGroupStart = gargantuar.spriteGroup.Position;
				Vector2 transformPointStart = gargantuar.transformPoint.Position;
				Vector2 spriteStart = gargantuar.sprite.Position;
				Vector2 centroid;
				int count;
				AdobeAnimateRenderSnapshot snapshot;
				bool flag = TryGetVisualCentroid(gargantuar.sprite, out centroid, out count, out snapshot);
				gargantuar.Attack();
				Check(gargantuar.CurrentStateHandle?.StableId == "zombie.attack" && gargantuar.sprite.clip == gargantuar.attackAnimeClip, $"The real Gargantuar must enter Smash; state={gargantuar.CurrentStateHandle?.StableId}, clip={gargantuar.sprite?.clip}.");
				BugOverviewGargantuarSmashPositionContinuityRuntimeTest bugOverviewGargantuarSmashPositionContinuityRuntimeTest3 = this;
				GroundMoveComponent groundMoveComponent2 = gargantuar.groundMoveComponent;
				bugOverviewGargantuarSmashPositionContinuityRuntimeTest3.Check(groundMoveComponent2 != null && !groundMoveComponent2.Alive, "Leaving Walk for Smash must stop authored root motion before changing clips.");
				Check(gargantuar.GlobalPosition.IsEqualApprox(attackStart) && gargantuar.spriteGroup.Position.IsEqualApprox(spriteGroupStart) && gargantuar.transformPoint.Position.IsEqualApprox(transformPointStart) && gargantuar.sprite.Position.IsEqualApprox(spriteStart), "Entering Smash must not move the character root or its scene-level visual anchors.");
				Vector2 centroid2;
				int count2;
				AdobeAnimateRenderSnapshot snapshot2;
				bool flag2 = TryGetVisualCentroid(gargantuar.sprite, out centroid2, out count2, out snapshot2);
				Check(flag && flag2 && count > 0 && count2 > 0 && snapshot2.ClipBlend.Enabled && snapshot2.ClipBlend.Weight < 0.001f && Math.Abs(snapshot2.ClipBlend.FromFrameFloat - snapshot.FrameFloat) < 0.01f && centroid.DistanceTo(centroid2) < 1f, $"Smash must begin from the last rendered Walk pose instead of flashing to its back-lean pose; sourceItems={count}, smashItems={count2}, centroidStep={centroid.DistanceTo(centroid2):F3}, blend={snapshot2.ClipBlend.Enabled}/{snapshot2.ClipBlend.Weight:F3}.");
				double hitpointsBefore = wallnut.instance.hitpoints;
				gargantuar.timeScale = 8.0;
				control.isGameRunning = true;
				gargantuar.ProcessMode = ProcessModeEnum.Inherit;
				float maxStepDistance = 0f;
				float maxTotalDistance = 0f;
				Vector2 previousPosition = gargantuar.GlobalPosition;
				bool impactObserved = false;
				for (int frame = 0; frame < 120; frame++)
				{
					await WaitFrames(1);
					Vector2 globalPosition = gargantuar.GlobalPosition;
					maxStepDistance = Math.Max(maxStepDistance, globalPosition.DistanceTo(previousPosition));
					maxTotalDistance = Math.Max(maxTotalDistance, globalPosition.DistanceTo(attackStart));
					previousPosition = globalPosition;
					if (wallnut.instance.hitpoints < hitpointsBefore)
					{
						impactObserved = true;
						break;
					}
				}
				Check(impactObserved, "The authored Smash animation event must reach and damage the real Wall-nut.");
				Check(gargantuar.config is TowerDefenseZombieConfig towerDefenseZombieConfig && Math.Abs(towerDefenseZombieConfig.smashAttack - 1800.0) < 0.001 && wallnut.die && wallnut.nearDie && wallnut.instance.hitpoints <= 0.0, $"The first real Smash must retain its authored 1800 payload and crush the Wall-nut; payload={(gargantuar.config as TowerDefenseZombieConfig)?.smashAttack}, remaining={wallnut.instance.hitpoints:F3}.");
				Check(maxStepDistance < 0.01f && maxTotalDistance < 0.01f, $"The Gargantuar root must remain continuous through windup and impact; maxStep={maxStepDistance:F4}, maxTotal={maxTotalDistance:F4}.");
				Check(gargantuar.CurrentStateHandle?.StableId == "zombie.attack" && gargantuar.sprite.clip == gargantuar.attackAnimeClip && gargantuar.attackComponent.target == wallnut, "The successful impact must preserve the committed Smash action and target.");
				Check(gargantuar.spriteGroup.Position.IsEqualApprox(spriteGroupStart) && gargantuar.transformPoint.Position.IsEqualApprox(transformPointStart) && gargantuar.sprite.Position.IsEqualApprox(spriteStart), "Smash playback must not rewrite the scene-level visual anchors.");
				goto end_IL_00c5;
				end_IL_00e0:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGargantuarSmashPositionContinuityRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00c5;
			}
			return;
			end_IL_00c5:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(wallnut) && !wallnut.IsQueuedForDeletion())
			{
				wallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(gargantuar) && !gargantuar.IsQueuedForDeletion())
			{
				gargantuar.QueueFree();
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
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag3 = _failures == 0 && _checks == 18;
		GD.Print($"GARGANTUAR_SMASH_POSITION_CONTINUITY_RESULT passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
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

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static bool TryGetVisualCentroid(AdobeAnimateSprite sprite, out Vector2 centroid, out int count, out AdobeAnimateRenderSnapshot snapshot)
	{
		centroid = Vector2.Zero;
		count = 0;
		snapshot = default(AdobeAnimateRenderSnapshot);
		if (!GodotObject.IsInstanceValid(sprite) || !sprite.TryBuildRenderSnapshot(out snapshot, allowUnchanged: false))
		{
			return false;
		}
		List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
		AdobeAnimateDrawItemBuilder.Build(snapshot, list);
		foreach (AdobeAnimateDrawItem item in list)
		{
			if (item.Owner == sprite)
			{
				centroid += item.Transform.Origin;
				count++;
			}
		}
		if (count <= 0)
		{
			return false;
		}
		centroid /= (float)count;
		return true;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
		RegisterPacket("ZombieGargantuar", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres");
		RegisterCharacter("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
		RegisterCharacter("ZombieGargantuar", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn");
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
			GD.PushError("[BugOverviewGargantuarSmashPositionContinuityRuntimeTest] " + message);
		}
	}
}
