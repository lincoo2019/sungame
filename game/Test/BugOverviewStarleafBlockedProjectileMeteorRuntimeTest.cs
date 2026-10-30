using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewStarleafBlockedProjectileMeteorRuntimeTest : Node
{
	private const string StarleafScenePath = "res://Asset/Anime/Character/Plant/Chapter6/Starleaf/Scene/TowerDefensePlantStarleaf.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I PlantGrid = new Vector2I(4, 2);

	private static readonly Vector2I ZombieGrid = new Vector2I(6, 2);

	private int _checks;

	private int _failures;

	private readonly List<StringName> _spawnedMeteorNames = new List<StringName>();

	private readonly List<int> _spawnedMeteorIndices = new List<int>();

	private BulletField _captureField;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ProcessModeEnum previousProjectileProcessMode = ProjectileUpdateManager.Instance?.ProcessMode ?? ProcessModeEnum.Inherit;
		BugOverviewStarleafBlockedProjectileMeteorRuntimeControlStub control = null;
		BulletField bulletField = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00f1;
				}
				TowerDefenseProjectileRegistry.Init();
				if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
				{
					ProjectileUpdateManager.Instance.ProcessMode = ProcessModeEnum.Disabled;
				}
				control = new BugOverviewStarleafBlockedProjectileMeteorRuntimeControlStub
				{
					Name = "StarleafBlockedProjectileMeteorRuntimeControl",
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
				bulletField = BulletField.EnsureMountedOnCharacterNode();
				Check(GodotObject.IsInstanceValid(bulletField), "The BulletField must mount for the Starleaf probe.");
				if (!GodotObject.IsInstanceValid(bulletField))
				{
					goto end_IL_00f1;
				}
				bulletField.ClearActiveBullets();
				TowerDefensePlantStarleaf plant = Instantiate<TowerDefensePlantStarleaf>("res://Asset/Anime/Character/Plant/Chapter6/Starleaf/Scene/TowerDefensePlantStarleaf.tscn");
				TowerDefenseZombieNormal zombie = Instantiate<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(zombie), "The real Starleaf and normal Zombie scenes must instantiate.");
				if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_00f1;
				}
				control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				PlaceAtGrid(plant, PlantGrid);
				PlaceAtGrid(zombie, ZombieGrid);
				await WaitFrames(5);
				control.isGameRunning = true;
				BlockComponent block = plant.componentManager?.GetRuntime<BlockComponent>("character.block");
				Check(block != null && block.Lifecycle == ComponentRuntimeLifecycle.Active, "The real Starleaf BlockComponent must be active.");
				Check(manager.GetCharacterTargetNearest(plant, TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION) == zombie, "The real Starleaf must acquire the live Zombie as its meteor target.");
				if (block == null || block.Lifecycle != ComponentRuntimeLifecycle.Active)
				{
					goto end_IL_00f1;
				}
				block.BindBulletField(bulletField);
				block.UpdateRect();
				_captureField = bulletField;
				bulletField.OnBulletSpawned += CaptureMeteor;
				RunBlockedProjectilePass(plant, zombie, block, bulletField, 1, out var firstIndex);
				RunBlockedProjectilePass(plant, zombie, block, bulletField, 2, out var overlappingIndex);
				await WaitFrames(3);
				bulletField.Update(0.0, Engine.GetPhysicsFrames());
				Check(_spawnedMeteorNames.Count == 2, "Two projectiles blocked during the same Block animation must each create a meteor; " + $"count={_spawnedMeteorNames.Count}.");
				Check(_spawnedMeteorNames.TrueForAll(IsMeteorName), "Every overlapping blocked projectile must create MeteorStar or MeteorStarS.");
				CheckMeteorRenderState(bulletField, "Overlapping blocked projectiles", 2);
				await WaitFrames(90);
				bulletField.ClearActiveBullets();
				_spawnedMeteorNames.Clear();
				_spawnedMeteorIndices.Clear();
				RunBlockedProjectilePass(plant, zombie, block, bulletField, 3, out var bulletIndex);
				Check(bulletIndex == firstIndex || bulletIndex == overlappingIndex, "The later pass must reuse a released BulletField slot.");
				await WaitFrames(3);
				bulletField.Update(0.0, Engine.GetPhysicsFrames());
				Check(_spawnedMeteorNames.Count == 1, $"A blocked projectile after slot reuse must create one meteor; count={_spawnedMeteorNames.Count}.");
				Check(IsMeteorName((_spawnedMeteorNames.Count > 0) ? _spawnedMeteorNames[0] : null), "The reused blocked projectile must create MeteorStar or MeteorStarS.");
				CheckMeteorRenderState(bulletField, "Reused blocked projectile");
				goto end_IL_00d6;
				end_IL_00f1:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[StarleafBlockedProjectileMeteor] Unexpected exception: {value}");
				goto end_IL_00d6;
			}
			return;
			end_IL_00d6:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.OnBulletSpawned -= CaptureMeteor;
				bulletField.ClearActiveBullets();
			}
			_spawnedMeteorNames.Clear();
			_spawnedMeteorIndices.Clear();
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
			{
				ProjectileUpdateManager.Instance.ProcessMode = previousProjectileProcessMode;
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(8);
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0;
		GD.Print($"STARLEAF_BLOCKED_PROJECTILE_METEOR_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void RunBlockedProjectilePass(TowerDefensePlantStarleaf plant, TowerDefenseZombieNormal zombie, BlockComponent block, BulletField bulletField, int pass, out int bulletIndex)
	{
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileCreateData(new StringName("Pea")).BuildConfig();
		Check(towerDefenseProjectileConfig != null, $"Pass {pass} must build the projectile fixture config.");
		bulletIndex = -1;
		if (towerDefenseProjectileConfig != null)
		{
			Vector2 globalPosition = plant.GlobalPosition;
			bulletIndex = bulletField.TrySpawnFromConfig(towerDefenseProjectileConfig, globalPosition, Vector2.Left * 200f, 200.0, null, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, plant.gridPos, plant.gridPos.Y, new Rect2(-10000f, -10000f, 20000f, 20000f), plant, 0.0, 0.0, towerDefenseProjectileConfig.collisionFlags);
			Check(bulletIndex >= 0, $"Pass {pass} must spawn the thrown projectile.");
			if (bulletIndex >= 0)
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(bulletIndex);
				bulletDataRef.catapultOpen = true;
				bulletDataRef.catapultTime = 1.0;
				bulletDataRef.catapultTimer = 0.8;
				bulletDataRef.z = 0.0;
				bulletDataRef.gridPos = plant.gridPos;
				bulletDataRef.gridY = plant.gridPos.Y;
				bulletDataRef.target = plant;
				bulletDataRef.camp = zombie.camp;
				block.OnBulletIntersect(ref bulletDataRef, bulletIndex);
				Check(bulletDataRef.blocked && bulletDataRef.hitOver, $"Pass {pass} must visibly bounce the thrown projectile.");
			}
		}
	}

	private void CaptureMeteor(int index)
	{
		if (GodotObject.IsInstanceValid(_captureField) && _captureField.IsBulletActive(index))
		{
			StringName stringName = _captureField.GetBulletDataRef(index).config?.NameSN ?? null;
			if (IsMeteorName(stringName))
			{
				_spawnedMeteorNames.Add(stringName);
				_spawnedMeteorIndices.Add(index);
			}
		}
	}

	private void CheckMeteorRenderState(BulletField bulletField, string context, int expectedMeteorCount = 1)
	{
		Check(_spawnedMeteorIndices.Count == expectedMeteorCount, $"{context} must retain {expectedMeteorCount} captured meteor indices.");
		if (_spawnedMeteorIndices.Count != expectedMeteorCount)
		{
			return;
		}
		for (int i = 0; i < _spawnedMeteorIndices.Count; i++)
		{
			int index = _spawnedMeteorIndices[i];
			Check(bulletField.IsBulletActive(index), $"{context} meteor {i + 1} must remain active while falling.");
			if (bulletField.IsBulletActive(index))
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(index);
				Check(bulletDataRef.renderMode == BulletRenderMode.ANIMATED_MESH, $"{context} meteor {i + 1} must use the animated-mesh render path.");
				Check(bulletDataRef.animDefId >= 0, $"{context} meteor {i + 1} must retain a valid animation definition.");
			}
		}
		Check(bulletField.GetAnimatedMeshBucketCountForTest() > 0, context + " meteor must create an animated-mesh draw bucket.");
		Check(bulletField.GetAnimatedMeshVisibleInstanceCountForTest() >= expectedMeteorCount, context + " must submit every meteor as a visible animated-mesh instance.");
	}

	private static bool IsMeteorName(StringName name)
	{
		if (!(name == new StringName("MeteorStar")))
		{
			return name == new StringName("MeteorStarS");
		}
		return true;
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

	private static void PlaceAtGrid(TowerDefenseCharacter character, Vector2I grid)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		character.inGame = true;
		character.editorPreviewMode = false;
		character.gridPos = grid;
		character.GlobalPosition = instance.GetMapCellPosCenter(grid);
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
			GD.PushError("[StarleafBlockedProjectileMeteor] " + message);
		}
	}
}
