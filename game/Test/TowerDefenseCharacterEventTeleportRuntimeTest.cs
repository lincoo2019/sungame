using System;
using System.Threading.Tasks;
using Godot;

public partial class TowerDefenseCharacterEventTeleportRuntimeTest : Node
{
	private const string ProjectileResourcePath = "res://Asset/Config/Projectile/CatTailTP/CatTailTP.tres";

	private static readonly Vector2I NormalZombieGrid = new Vector2I(5, 1);

	private static readonly Vector2I BossZombieGrid = new Vector2I(5, 2);

	private static readonly Vector2I WeakZombieGrid = new Vector2I(5, 3);

	private static readonly Vector2I PlantGrid = new Vector2I(5, 4);

	private static readonly Vector2I HypnotizedShooterGrid = new Vector2I(2, 5);

	private static readonly Vector2I HypnotizedTargetGrid = new Vector2I(5, 5);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		TowerDefenseCharacterEventTeleportRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseProjectile neutralProjectile = null;
		TowerDefenseProjectile hypnotizedProjectile = null;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00f0;
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("TowerDefenseCharacterEventTeleportRuntimeTest");
				Check(ResourceManager.Instance.AreFullGameplayResourcesReady && ResourceManager.Instance.LateCharacterResourceLoadCount == 0, "Full gameplay resources must be ready without late character loads.");
				control = new TowerDefenseCharacterEventTeleportRuntimeControlStub
				{
					Name = "TowerDefenseCharacterEventTeleportRuntimeControl",
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
				TowerDefenseProjectileData towerDefenseProjectileData = ResourceLoader.Load<TowerDefenseProjectileData>("res://Asset/Config/Projectile/CatTailTP/CatTailTP.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefenseCharacterEventTeleport teleportEvent = ((towerDefenseProjectileData != null && towerDefenseProjectileData.hitTargetEventList.Count == 1) ? (towerDefenseProjectileData.hitTargetEventList[0] as TowerDefenseCharacterEventTeleport) : null);
				Check(GodotObject.IsInstanceValid(teleportEvent) && GodotObject.IsInstanceValid(teleportEvent.teleportEffectScene) && GodotObject.IsInstanceValid(teleportEvent.eraseEffectScene) && Mathf.IsEqualApprox(teleportEvent.bossDamage, 500.0) && Mathf.IsEqualApprox(teleportEvent.hpPercentDamage, 0.05) && Mathf.IsEqualApprox(teleportEvent.eraseHpRatio, 0.1) && teleportEvent.teleportLength == 1, "CatTailTP must load one fully configured teleport event.");
				if (!GodotObject.IsInstanceValid(teleportEvent))
				{
					goto end_IL_00f0;
				}
				neutralProjectile = new TowerDefenseProjectile();
				TowerDefenseZombie towerDefenseZombie = await Spawn<TowerDefenseZombie>("ZombieNormal", NormalZombieGrid);
				Check(GodotObject.IsInstanceValid(towerDefenseZombie), "The normal zombie damage and teleport fixture must spawn.");
				PrepareTarget(towerDefenseZombie, NormalZombieGrid);
				double totalHitPoint = towerDefenseZombie.GetTotalHitPoint();
				double currentHitPoint = towerDefenseZombie.GetCurrentHitPoint();
				int childCount = control.characterNode.GetChildCount();
				teleportEvent.ExecuteProject(neutralProjectile, towerDefenseZombie);
				Check(Mathf.IsEqualApprox(towerDefenseZombie.GetCurrentHitPoint(), currentHitPoint - totalHitPoint * teleportEvent.hpPercentDamage), "A normal target must take five percent of its maximum hitpoints.");
				Check(towerDefenseZombie.gridPos == NormalZombieGrid + Vector2I.Right && towerDefenseZombie.GetLogicalGlobalPosition().IsEqualApprox(TowerDefenseManager.GetMapCellPlantPos(NormalZombieGrid + Vector2I.Right)), "A normal zombie must teleport one cell away from the house.");
				Check(control.characterNode.GetChildCount() > childCount, "A successful teleport hit must create its authored teleport effect.");
				TowerDefenseZombie towerDefenseZombie2 = await Spawn<TowerDefenseZombie>("ZombieNormal", BossZombieGrid);
				Check(GodotObject.IsInstanceValid(towerDefenseZombie2), "The Boss boundary fixture must spawn.");
				PrepareTarget(towerDefenseZombie2, BossZombieGrid, 10.0);
				towerDefenseZombie2.instance.zombiePhysique = TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
				double currentHitPoint2 = towerDefenseZombie2.GetCurrentHitPoint();
				Vector2 logicalGlobalPosition = towerDefenseZombie2.GetLogicalGlobalPosition();
				teleportEvent.ExecuteProject(neutralProjectile, towerDefenseZombie2);
				Check(Mathf.IsEqualApprox(towerDefenseZombie2.GetCurrentHitPoint(), currentHitPoint2 - teleportEvent.bossDamage), "A Boss target must take exactly the configured 500 damage.");
				Check(towerDefenseZombie2.gridPos == BossZombieGrid && towerDefenseZombie2.GetLogicalGlobalPosition().IsEqualApprox(logicalGlobalPosition), "A Boss target must never teleport or enter the erase branch.");
				TowerDefenseZombie weakZombie = await Spawn<TowerDefenseZombie>("ZombieNormal", WeakZombieGrid);
				Check(GodotObject.IsInstanceValid(weakZombie), "The weak-target erase fixture must spawn.");
				PrepareTarget(weakZombie, WeakZombieGrid);
				weakZombie.instance.hitpoints = weakZombie.GetTotalHitPoint() * teleportEvent.eraseHpRatio;
				int weakDeathEvents = 0;
				weakZombie.instance.hitpointsEmpty += delegate
				{
					weakDeathEvents++;
				};
				teleportEvent.ExecuteProject(neutralProjectile, weakZombie);
				Check(weakZombie.skipDestroySet && weakZombie.isDestroy && weakDeathEvents == 0, "A weak target must enter silent erase without firing its death callback.");
				await WaitFrames(4);
				Check(!GodotObject.IsInstanceValid(weakZombie) && !manager.GetCharacterLineList(WeakZombieGrid.Y, fliterGraveStone: false).Contains(weakZombie), "A silently erased target must leave the scene and battle registry.");
				TowerDefensePlant towerDefensePlant = await Spawn<TowerDefensePlant>("PlantWallnut", PlantGrid);
				Check(GodotObject.IsInstanceValid(towerDefensePlant), "The non-zombie target fixture must spawn.");
				PrepareTarget(towerDefensePlant, PlantGrid);
				double totalHitPoint2 = towerDefensePlant.GetTotalHitPoint();
				double currentHitPoint3 = towerDefensePlant.GetCurrentHitPoint();
				Vector2 logicalGlobalPosition2 = towerDefensePlant.GetLogicalGlobalPosition();
				teleportEvent.ExecuteProject(neutralProjectile, towerDefensePlant);
				Check(Mathf.IsEqualApprox(towerDefensePlant.GetCurrentHitPoint(), currentHitPoint3 - totalHitPoint2 * teleportEvent.hpPercentDamage), "A non-zombie target must still take percentage damage.");
				Check(towerDefensePlant.gridPos == PlantGrid && towerDefensePlant.GetLogicalGlobalPosition().IsEqualApprox(logicalGlobalPosition2), "A non-zombie target must not be teleported.");
				TowerDefensePlant hypnotizedShooter = await Spawn<TowerDefensePlant>("PlantCatTailTP", HypnotizedShooterGrid);
				TowerDefenseZombie towerDefenseZombie3 = await Spawn<TowerDefenseZombie>("ZombieNormal", HypnotizedTargetGrid);
				Check(GodotObject.IsInstanceValid(hypnotizedShooter) && GodotObject.IsInstanceValid(towerDefenseZombie3), "The hypnotized reverse-teleport fixtures must spawn.");
				PrepareTarget(towerDefenseZombie3, HypnotizedTargetGrid);
				hypnotizedShooter.instance.hypnoses = true;
				hypnotizedProjectile = new TowerDefenseProjectile
				{
					fireCharacter = hypnotizedShooter
				};
				teleportEvent.ExecuteProject(hypnotizedProjectile, towerDefenseZombie3);
				Check(towerDefenseZombie3.gridPos == HypnotizedTargetGrid + Vector2I.Left && towerDefenseZombie3.GetLogicalGlobalPosition().IsEqualApprox(TowerDefenseManager.GetMapCellPlantPos(HypnotizedTargetGrid + Vector2I.Left)), "A hypnotized shooter must teleport its zombie target one cell toward the house.");
				Check(Mathf.IsEqualApprox(towerDefenseZombie3.GetCurrentHitPoint(), towerDefenseZombie3.GetTotalHitPoint() * (1.0 - teleportEvent.hpPercentDamage)), "Reverse teleport must retain the same percentage damage contract.");
				goto end_IL_00c5;
				end_IL_00f0:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[TowerDefenseCharacterEventTeleportRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00c5;
			}
			return;
			end_IL_00c5:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(neutralProjectile))
			{
				neutralProjectile.Free();
			}
			if (GodotObject.IsInstanceValid(hypnotizedProjectile))
			{
				hypnotizedProjectile.Free();
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
		bool flag = _failures == 0 && _checks == 20;
		GD.Print($"CHARACTER_EVENT_TELEPORT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task<T> Spawn<T>(string packetName, Vector2I gridPos) where T : TowerDefenseCharacter
	{
		T character = TowerDefenseManager.GetPacketConfig(packetName)?.Plant(gridPos, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as T;
		await WaitFrames(5);
		return character;
	}

	private static void PrepareTarget(TowerDefenseCharacter target, Vector2I gridPos, double hitpointScale = 1.0)
	{
		target.gridPos = gridPos;
		target.cell = TowerDefenseManager.GetMapCell(gridPos);
		target.instance.hitpointScale = hitpointScale;
		target.instance.hitpoints = target.GetTotalHitPoint();
		target.instance.hitpointsBase = target.GetTotalHitPoint();
		target.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(gridPos));
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum
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
			GD.PushError("[TowerDefenseCharacterEventTeleportRuntimeTest] " + message);
		}
	}
}
