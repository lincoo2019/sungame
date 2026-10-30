using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewStarCaltropLeftTargetRuntimeTest : Node
{
	private const string StarCaltropScenePath = "res://Asset/Anime/Character/Plant/Other/StarCaltrop/Scene/TowerDefensePlantStarCaltrop.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I PlantGrid = new Vector2I(6, 3);

	private static readonly Vector2I LeftZombieGrid = new Vector2I(3, 3);

	private static readonly Vector2I RightDeadZoneGrid = new Vector2I(8, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ProcessModeEnum previousProjectileProcessMode = ProjectileUpdateManager.Instance?.ProcessMode ?? ProcessModeEnum.Inherit;
		BugOverviewStarCaltropLeftTargetRuntimeControlStub control = null;
		TowerDefensePlantStarCaltrop plant = null;
		BulletField bulletField = null;
		List<BulletData> spawnedProjectiles = new List<BulletData>();
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_011a;
				}
				TowerDefenseProjectileRegistry.Init();
				if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
				{
					ProjectileUpdateManager.Instance.ProcessMode = ProcessModeEnum.Disabled;
				}
				control = new BugOverviewStarCaltropLeftTargetRuntimeControlStub
				{
					Name = "StarCaltropLeftTargetRuntimeControl",
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
				Check(GodotObject.IsInstanceValid(bulletField), "The BulletField must mount for the Star Caltrop probe.");
				if (!GodotObject.IsInstanceValid(bulletField))
				{
					goto end_IL_011a;
				}
				bulletField.ClearActiveBullets();
				plant = Instantiate<TowerDefensePlantStarCaltrop>("res://Asset/Anime/Character/Plant/Other/StarCaltrop/Scene/TowerDefensePlantStarCaltrop.tscn");
				TowerDefenseZombieNormal zombie = Instantiate<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(zombie) && plant.config?.name == "PlantStarCaltrop" && zombie.config?.name == "ZombieNormal", "The fixture must instantiate the real Star Caltrop and normal Zombie scenes.");
				if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_011a;
				}
				control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				PlaceAtGrid(plant, PlantGrid);
				PlaceAtGrid(zombie, LeftZombieGrid);
				await WaitFrames(5);
				plant.ProcessMode = ProcessModeEnum.Disabled;
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				control.isGameRunning = true;
				Check(manager.GetMapGridPos(plant.GlobalPosition) == PlantGrid && manager.GetMapGridPos(zombie.GlobalPosition) == LeftZombieGrid && zombie.GlobalPosition.X < plant.GlobalPosition.X && Mathf.IsEqualApprox(zombie.GlobalPosition.Y, plant.GlobalPosition.Y), "The real characters must occupy aligned map cells with the Zombie only on the plant's left.");
				FireComponent fire = plant.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(fire != null && !fire.IsReleased, "The real Star Caltrop scene must expose its resource-backed FireComponent.");
				if (fire == null || fire.IsReleased)
				{
					goto end_IL_011a;
				}
				Check(fire.checkRayResources.Count == 5 && fire.fireProjectileList.Count == 5 && fire.firePosMarker.Count == 5 && fire.checkAllLine, "Star Caltrop must retain five targeting rays, five projectile routes, and all-line ray checks.");
				Check(fire.TryGetCheckRayLocalTransform(2, out var localTransform) && localTransform.X.X < -0.999f && fire.checkRayResources[2].TargetPosition.X > 1999f, "The third production ray must remain the 2000-pixel backward ray.");
				FireComponentFireProjectileConfig fireComponentFireProjectileConfig = fire.fireProjectileList[2];
				Check(GodotObject.IsInstanceValid(fireComponentFireProjectileConfig) && fireComponentFireProjectileConfig.firePosId == 2 && Mathf.IsEqualApprox(fireComponentFireProjectileConfig.dir, 180f) && fireComponentFireProjectileConfig.speed > 0f, "The third production projectile route must remain the left-facing 180-degree shot.");
				FireComponentCheckConfig fireComponentCheckConfig = ((fire.fireCheckList.Count == 1) ? fire.fireCheckList[0] : null);
				int collisionFlags = (GodotObject.IsInstanceValid(fireComponentCheckConfig) ? fireComponentCheckConfig.GetCollisionFlags() : 0);
				Check(GodotObject.IsInstanceValid(fireComponentCheckConfig) && zombie.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && plant.CanTarget(zombie) && (collisionFlags & zombie.instance.maskFlags) != 0 && zombie.instance.canBeCollection && !zombie.instance.invincible && zombie.HasHitBox, "The only left-side Zombie must satisfy the production camp, mask, and hit-box filters.");
				int num = 0;
				foreach (TowerDefenseCharacter cleanCharacters in manager.characterRegistry.GetCleanCharactersList())
				{
					if (GodotObject.IsInstanceValid(cleanCharacters) && cleanCharacters.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
					{
						num++;
					}
				}
				Check(num == 1, $"The reported setup must contain exactly one opposing character; count={num}.");
				Check(TryGetWorldRay(fire, plant, 2, out var origin, out var end) && end.X < origin.X && AabbShapeUtil.SegmentIntersectsRect(origin, end, zombie.WorldHitRect, out var enterT), "The real backward ray must geometrically intersect the only left-side Zombie hit box.");
				PrepareFire(fire);
				Check(fire.CanFireCheckOnce(null, collisionFlags) && fire.firstCharacter == zombie, "Star Caltrop must acquire a real Zombie when it is the only target on the left.");
				fire.IdleProcessing(0.0);
				Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack" && fire.runningCheck == fireComponentCheckConfig, "Left-side acquisition must drive the production FireComponent into fire.attack.");
				Check(fire.fireEventName.Split('&', StringSplitOptions.RemoveEmptyEntries).Length != 0, "The real Star Caltrop animation must retain its production fire-event binding.");
				int volleyCount = 0;
				bulletField.OnBulletSpawned += CaptureProjectile;
				fire.OnFireVolley += CaptureVolley;
				DispatchRealFireEvent(fire);
				bulletField.OnBulletSpawned -= CaptureProjectile;
				fire.OnFireVolley -= CaptureVolley;
				Check(spawnedProjectiles.Count == 5 && volleyCount == 5, $"One real animation event must create the complete five-direction volley; projectiles={spawnedProjectiles.Count}, volleys={volleyCount}.");
				Check(HasExpectedDirections(spawnedProjectiles), "The five MultiMesh projectiles must preserve the authored 330, 270, 180, 90, and 30 degree directions.");
				Check(spawnedProjectiles.Count > 2 && spawnedProjectiles[2].vel.X < -499f && Mathf.Abs(spawnedProjectiles[2].vel.Y) < 0.01f, "The third real projectile must travel horizontally left at the authored speed.");
				Check(AllProjectilesOwnedByPlant(spawnedProjectiles, plant), "Every direction in the real volley must keep Star Caltrop as its plant-camp owner.");
				bulletField.ClearActiveBullets();
				spawnedProjectiles.Clear();
				PlaceAtGrid(zombie, RightDeadZoneGrid);
				await WaitFrames(2);
				PrepareFire(fire);
				Check(!fire.CanFireCheckOnce(null, collisionFlags) && fire.firstCharacter == null, "A same-row Zombie directly to the right must remain outside Star Caltrop's five authored rays.");
				Vector2 worldPosition = plant.GlobalPosition + Vector2.FromAngle(Mathf.DegToRad(330f)) * 300f;
				PlaceAtWorld(zombie, worldPosition);
				await WaitFrames(2);
				Check(TryGetWorldRay(fire, plant, 0, out var origin2, out var end2) && AabbShapeUtil.SegmentIntersectsRect(origin2, end2, zombie.WorldHitRect, out enterT), "The positive control Zombie must intersect the authored upper-right ray.");
				PrepareFire(fire);
				Check(fire.CanFireCheckOnce(null, collisionFlags) && fire.firstCharacter == zombie, "Star Caltrop must still acquire a real target on another authored ray.");
				PlaceAtGrid(zombie, LeftZombieGrid);
				zombie.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
				await WaitFrames(2);
				Check(!plant.CanTarget(zombie), "The same-camp control must be rejected by the production character target filter.");
				PrepareFire(fire);
				Check(!fire.CanFireCheckOnce(null, collisionFlags) && fire.firstCharacter == null, "A same-camp character on the backward ray must not trigger Star Caltrop.");
				zombie.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
				await WaitFrames(2);
				PrepareFire(fire);
				Check(fire.CanFireCheckOnce(null, collisionFlags) && fire.firstCharacter == zombie, "Restoring the left character to Zombie camp must immediately restore backward acquisition.");
				goto end_IL_00fb;
				end_IL_011a:
				void CaptureVolley(ulong _)
				{
					volleyCount++;
				}
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[StarCaltropLeftTarget] Unexpected exception: {value}");
				goto end_IL_00fb;
			}
			return;
			end_IL_00fb:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
			}
			spawnedProjectiles.Clear();
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
		}
		bool flag = _failures == 0 && _checks == 24;
		GD.Print($"STAR_CALTROP_LEFT_TARGET_RESULT version=1 passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
		void CaptureProjectile(int index)
		{
			if (bulletField.IsBulletActive(index))
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(index);
				if (bulletDataRef.fireCharacter == plant)
				{
					spawnedProjectiles.Add(bulletDataRef);
				}
			}
		}
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

	private static void PlaceAtWorld(TowerDefenseCharacter character, Vector2 worldPosition)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		character.GlobalPosition = worldPosition;
		character.gridPos = instance.GetMapGridPos(worldPosition);
	}

	private static void PrepareFire(FireComponent fire)
	{
		fire.alive = true;
		fire.groundRight = 10000f;
		fire.timer = 0f;
		fire.checkInterval = 0;
		fire.currentFireNum = 0;
		fire.runningCheck = null;
		fire.runningCheckId = 0;
	}

	private static bool TryGetWorldRay(FireComponent fire, TowerDefenseCharacter owner, int index, out Vector2 origin, out Vector2 end)
	{
		origin = default(Vector2);
		end = default(Vector2);
		if (fire == null || !GodotObject.IsInstanceValid(owner) || index < 0 || index >= fire.checkRayResources.Count || !fire.TryGetCheckRayLocalTransform(index, out var localTransform))
		{
			return false;
		}
		AabbRay2DResource aabbRay2DResource = fire.checkRayResources[index];
		if (GodotObject.IsInstanceValid(aabbRay2DResource))
		{
			return aabbRay2DResource.TryGetWorldSegment(owner.GlobalTransform, localTransform, aabbRay2DResource.TargetPosition, out origin, out end);
		}
		return false;
	}

	private static void DispatchRealFireEvent(FireComponent fire)
	{
		string command = fire.fireEventName.Split('&', StringSplitOptions.RemoveEmptyEntries)[0];
		fire.AnimeEvent(command, default(Variant));
	}

	private static bool HasExpectedDirections(List<BulletData> projectiles)
	{
		if (projectiles.Count != 5)
		{
			return false;
		}
		float[] array = new float[5] { 330f, 270f, 180f, 90f, 30f };
		for (int i = 0; i < array.Length; i++)
		{
			Vector2 other = Vector2.FromAngle(Mathf.DegToRad(array[i])) * 500f;
			if (!projectiles[i].vel.IsEqualApprox(other))
			{
				return false;
			}
		}
		return true;
	}

	private static bool AllProjectilesOwnedByPlant(List<BulletData> projectiles, TowerDefensePlantStarCaltrop plant)
	{
		if (projectiles.Count != 5)
		{
			return false;
		}
		foreach (BulletData projectile in projectiles)
		{
			if (projectile.fireCharacter != plant || projectile.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				return false;
			}
		}
		return true;
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
			GD.PushError("[StarCaltropLeftTarget] " + message);
		}
	}
}
