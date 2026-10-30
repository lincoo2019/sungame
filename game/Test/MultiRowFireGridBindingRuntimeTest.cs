using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class MultiRowFireGridBindingRuntimeTest : Node
{
	private readonly record struct AuditCase(string Name, string ScenePath, string FireInstanceId, bool UsesArea, int PositiveIndex);

	private static readonly AuditCase[] Cases = new AuditCase[12]
	{
		new AuditCase("ThreePeater", "res://Asset/Anime/Character/Plant/Chapter0/ThreePeater/Scene/TowerDefensePlantThreePeater.tscn", "character.fire", UsesArea: false, 1),
		new AuditCase("ThreePeaterG", "res://Asset/Anime/Character/Plant/Chapter5/ThreePeaterG/Scene/TowerDefensePlantThreePeaterG.tscn", "character.fire", UsesArea: false, 1),
		new AuditCase("PiratePeater", "res://Asset/Anime/Character/Plant/Chapter7/PiratePeater/Scene/TowerDefensePlantPiratePeater.tscn", "character.fire", UsesArea: false, 1),
		new AuditCase("ThreePeaterZ", "res://Asset/Anime/Character/Plant/Chapter8/ThreePeaterZ/Scene/TowerDefensePlantThreePeaterZ.tscn", "character.fire", UsesArea: false, 1),
		new AuditCase("ThreePeaterU", "res://Asset/Anime/Character/Plant/Gold/ThreePeaterU/Scene/TowerDefensePlantThreePeaterU.tscn", "character.fire", UsesArea: false, 1),
		new AuditCase("StarPeater", "res://Asset/Anime/Character/Plant/Other/StarPeater/Scene/TowerDefensePlantStarPeater.tscn", "character.fire", UsesArea: false, 1),
		new AuditCase("ThreeHybirdPeater", "res://Asset/Anime/Character/Plant/Other/ThreeHybirdPeater/Scene/TowerDefensePlantThreeHybirdPeater.tscn", "character.fire", UsesArea: false, 1),
		new AuditCase("SunCactus", "res://Asset/Anime/Character/Plant/Gold/SunCactus/Scene/TowerDefensePlantSunCactus.tscn", "character.fire", UsesArea: false, 0),
		new AuditCase("IceCactus", "res://Asset/Anime/Character/Plant/Chapter5/IceCactus/Scene/TowerDefensePlantIceCactus.tscn", "character.fire", UsesArea: false, 0),
		new AuditCase("ThreeCornpult", "res://Asset/Anime/Character/Plant/Other/ThreeCornpult/Scene/TowerDefensePlantThreeCornpult.tscn", "character.fire", UsesArea: true, 0),
		new AuditCase("Robot", "res://Asset/Anime/Character/Plant/Star/Robot/Scene/TowerDefensePlantRobot.tscn", "character.fire.1", UsesArea: true, 0),
		new AuditCase("ZombieThreePeater", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/ThreePeater/TowerDefenseZombieNormalThreePeater.tscn", "character.fire", UsesArea: false, 1)
	};

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		bool threeCornpultLaneHitOnly = Array.IndexOf(OS.GetCmdlineUserArgs(), "--three-cornpult-lane-hit-only") >= 0;
		try
		{
			if (!threeCornpultLaneHitOnly)
			{
				await RunGeometryScenarios();
				await RunGroundTargetScenario(Cases[0]);
				await RunGroundTargetScenario(Cases[9]);
				await RunRobotCatapultScenario();
			}
			else
			{
				await RunThreeCornpultLaneHitScenario();
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[MultiRowFireGridBindingRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		string value2 = (threeCornpultLaneHitOnly ? "THREE_CORNPULT_LANE_HIT_RESULT" : "MULTI_ROW_FIRE_GRID_RESULT");
		GD.Print($"{value2} passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunGeometryScenarios()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
		if (!GodotObject.IsInstanceValid(manager))
		{
			return;
		}
		TowerDefenseControlNew control = (manager.currentControl = new TowerDefenseControlNew
		{
			isGameRunning = false
		});
		manager.gridBeginPos = new Vector2(0f, 100f);
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		AuditCase[] cases = Cases;
		for (int i = 0; i < cases.Length; i++)
		{
			AuditCase item = cases[i];
			TowerDefenseCharacter towerDefenseCharacter = ResourceLoader.Load<PackedScene>(item.ScenePath, null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(towerDefenseCharacter), item.Name + " scene must instantiate.");
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.Position = new Vector2(100f, 100f);
				towerDefenseCharacter.gridPos = new Vector2I(1, 1);
				towerDefenseCharacter.inGame = true;
				AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
				FireComponent fireComponent = towerDefenseCharacter.componentManager?.GetRuntime<FireComponent>(item.FireInstanceId);
				Check(fireComponent != null && !fireComponent.IsReleased, item.Name + " FireComponent must be active.");
				if (fireComponent == null || fireComponent.IsReleased)
				{
					towerDefenseCharacter.QueueFree();
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					continue;
				}
				float b = ReadDefinitionY(fireComponent, item);
				float y;
				bool flag = TryReadPositiveY(fireComponent, item, out y);
				Check(flag && Mathf.IsEqualApprox(y, 76f), $"{item.Name} must bind to cached grid height 76, got {y}.");
				float y2;
				bool flag2 = TryReadNegativeY(fireComponent, item, out y2);
				Check(flag2 && Mathf.IsEqualApprox(y2, -76f), $"{item.Name} must bind its negative row to cached grid height -76, got {y2}.");
				manager.gridSize = new Vector2(100f, 84f);
				fireComponent.CheckTarget(null, 1, checkInterval: false);
				float y3;
				bool flag3 = TryReadPositiveY(fireComponent, item, out y3);
				Check(flag3 && Mathf.IsEqualApprox(y3, 84f), $"{item.Name} must refresh to changed grid height 84, got {y3}.");
				float y4;
				bool flag4 = TryReadNegativeY(fireComponent, item, out y4);
				Check(flag4 && Mathf.IsEqualApprox(y4, -84f), $"{item.Name} must refresh its negative row to changed grid height -84, got {y4}.");
				Check(Mathf.IsEqualApprox(ReadDefinitionY(fireComponent, item), b), item.Name + " runtime binding must not mutate its shared FireComponentDefinition geometry.");
				manager.gridSize = new Vector2(100f, 76f);
				towerDefenseCharacter.QueueFree();
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		manager.currentControl = null;
		control.Free();
	}

	private async Task RunGroundTargetScenario(AuditCase item)
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew control = (manager.currentControl = new TowerDefenseControlNew
		{
			isGameRunning = true
		});
		manager.gridBeginPos = new Vector2(0f, 100f);
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(item.ScenePath, null, ResourceLoader.CacheMode.Ignore);
		PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
		TowerDefenseCharacter attacker = packedScene?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		TowerDefenseCharacter target = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(attacker) && GodotObject.IsInstanceValid(target), item.Name + " and adjacent-row ground target must instantiate.");
		if (GodotObject.IsInstanceValid(attacker) && GodotObject.IsInstanceValid(target))
		{
			attacker.Position = new Vector2(100f, 100f);
			attacker.gridPos = new Vector2I(1, 1);
			attacker.inGame = true;
			target.Position = new Vector2(500f, 176f);
			target.gridPos = new Vector2I(5, 2);
			target.inGame = true;
			AddChild(attacker, forceReadableName: false, InternalMode.Disabled);
			AddChild(target, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			FireComponent fireComponent = attacker.componentManager?.GetRuntime<FireComponent>(item.FireInstanceId);
			Check(fireComponent != null && !fireComponent.IsReleased, item.Name + " target scenario FireComponent must be active.");
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				fireComponent.alive = true;
				fireComponent.groundRight = 10000f;
				fireComponent.timer = 0f;
				fireComponent.checkInterval = 0;
				int collectionFlag = 9;
				Check(fireComponent.CanFireCheckOnce(null, collectionFlag), item.Name + " must acquire a real adjacent-row ground target.");
			}
			attacker.QueueFree();
			target.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			manager.currentControl = null;
			control.Free();
		}
	}

	private async Task RunRobotCatapultScenario()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew control = (manager.currentControl = new TowerDefenseControlNew
		{
			isGameRunning = true
		});
		manager.gridBeginPos = new Vector2(0f, 100f);
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(Cases[10].ScenePath, null, ResourceLoader.CacheMode.Ignore);
		PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
		TowerDefenseCharacter robot = packedScene?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		TowerDefenseCharacter adjacentTarget = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		TowerDefenseCharacter distantTarget = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(robot) && GodotObject.IsInstanceValid(adjacentTarget) && GodotObject.IsInstanceValid(distantTarget), "Robot and both catapult targets must instantiate.");
		if (!GodotObject.IsInstanceValid(robot) || !GodotObject.IsInstanceValid(adjacentTarget) || !GodotObject.IsInstanceValid(distantTarget))
		{
			return;
		}
		robot.Position = new Vector2(100f, 100f);
		robot.gridPos = new Vector2I(1, 1);
		robot.inGame = true;
		adjacentTarget.Position = new Vector2(500f, 176f);
		adjacentTarget.gridPos = new Vector2I(5, 2);
		adjacentTarget.inGame = true;
		distantTarget.Position = new Vector2(200f, 328f);
		distantTarget.gridPos = new Vector2I(2, 4);
		distantTarget.inGame = true;
		AddChild(robot, forceReadableName: false, InternalMode.Disabled);
		AddChild(adjacentTarget, forceReadableName: false, InternalMode.Disabled);
		AddChild(distantTarget, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		FireComponent fireComponent = robot.componentManager?.GetRuntime<FireComponent>("character.fire.1");
		Check(fireComponent != null && !fireComponent.IsReleased, "Robot catapult FireComponent must be active.");
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			fireComponent.alive = true;
			fireComponent.groundRight = 10000f;
			fireComponent.timer = 0f;
			fireComponent.checkInterval = 0;
			fireComponent.randomChoose = false;
			int num = 9;
			Check(fireComponent.CanFireCheckOnce(null, num), "Robot must acquire the adjacent-row catapult target through its bound check shape.");
			Check(fireComponent.FindCatapultTarget(num, robot.GlobalPosition) == adjacentTarget, "Robot catapult targeting must exclude a closer target outside the configured adjacent rows.");
			int firedVolleys = 0;
			FireComponent.FireVolleyEventHandler value = delegate
			{
				firedVolleys++;
			};
			try
			{
				fireComponent.OnFireVolley += value;
				fireComponent.IdleProcessing(0.0);
				fireComponent.Fire();
				Check(firedVolleys == 1 && !fireComponent.checkAllLine, "Robot must execute one catapult volley after selecting the adjacent-row target while keeping projectile checks lane-scoped.");
			}
			finally
			{
				fireComponent.OnFireVolley -= value;
			}
		}
		robot.QueueFree();
		adjacentTarget.QueueFree();
		distantTarget.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		manager.currentControl = null;
		control.Free();
	}

	private async Task RunThreeCornpultLaneHitScenario()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousGridBegin = manager.gridBeginPos;
		Vector2 previousGridSize = manager.gridSize;
		Vector2I previousGridNum = manager.gridNum;
		ProcessModeEnum previousProjectileProcessMode = ProjectileUpdateManager.Instance?.ProcessMode ?? ProcessModeEnum.Inherit;
		MultiRowFireGridBindingControlStub control = new MultiRowFireGridBindingControlStub
		{
			Name = "ThreeCornpultLaneHitControl",
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		TowerDefenseCharacter upperTarget = null;
		TowerDefenseCharacter centerTarget = null;
		BulletField bulletField = null;
		List<int> spawnedIndices;
		int volleyCount;
		int upperHurtCount;
		int centerHurtCount;
		try
		{
			if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
			{
				ProjectileUpdateManager.Instance.ProcessMode = ProcessModeEnum.Disabled;
			}
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
			TowerDefenseProjectileRegistry.Init();
			PackedScene packedScene = ResourceLoader.Load<PackedScene>(Cases[9].ScenePath, null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
			TowerDefenseCharacter attacker = packedScene?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			upperTarget = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			centerTarget = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(attacker) && GodotObject.IsInstanceValid(upperTarget) && GodotObject.IsInstanceValid(centerTarget), "ThreeCornpult and two row-separated zombies must instantiate.");
			if (!GodotObject.IsInstanceValid(attacker) || !GodotObject.IsInstanceValid(upperTarget) || !GodotObject.IsInstanceValid(centerTarget))
			{
				return;
			}
			attacker.Position = new Vector2(100f, 252f);
			attacker.gridPos = new Vector2I(1, 3);
			attacker.inGame = true;
			upperTarget.Position = new Vector2(500f, 176f);
			upperTarget.gridPos = new Vector2I(5, 2);
			upperTarget.inGame = true;
			centerTarget.Position = new Vector2(500f, 252f);
			centerTarget.gridPos = new Vector2I(5, 3);
			centerTarget.inGame = true;
			control.characterNode.AddChild(attacker, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(upperTarget, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(centerTarget, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			attacker.ProcessMode = ProcessModeEnum.Disabled;
			upperTarget.ProcessMode = ProcessModeEnum.Disabled;
			centerTarget.ProcessMode = ProcessModeEnum.Disabled;
			manager.CharacterRegister(upperTarget);
			manager.CharacterRegister(centerTarget);
			TowerDefenseCharacter[] array = new TowerDefenseCharacter[2] { upperTarget, centerTarget };
			foreach (TowerDefenseCharacter obj in array)
			{
				obj.targetRegistrationComponent.canProjectileCheck = true;
				obj.hurtComponent.flashOnDamage = false;
				obj.hurtComponent.markHealthBarDirty = false;
				obj.instance.keepAlive = true;
				obj.instance.hitpointsNearDeath = 0.0;
				obj.instance.hitpointsBase = 1000000.0;
				obj.instance.hitpointsSave = 1000000.0;
				obj.instance.hitpoints = 1000000.0;
				obj.InvalidateHitBoxBounds();
			}
			FireComponent fireComponent = attacker.componentManager?.GetRuntime<FireComponent>("character.fire");
			Check(fireComponent != null && !fireComponent.IsReleased, "ThreeCornpult lane-hit scenario FireComponent must be active.");
			if (fireComponent == null || fireComponent.IsReleased)
			{
				return;
			}
			bulletField = BulletField.EnsureMountedOnCharacterNode();
			Check(GodotObject.IsInstanceValid(bulletField), "ThreeCornpult lane-hit scenario must mount BulletField.");
			if (!GodotObject.IsInstanceValid(bulletField))
			{
				return;
			}
			spawnedIndices = new List<int>();
			volleyCount = 0;
			fireComponent.alive = true;
			fireComponent.groundRight = 10000f;
			fireComponent.timer = 0f;
			fireComponent.checkInterval = 0;
			fireComponent.runningCheck = ((fireComponent.fireCheckList.Count > 0) ? fireComponent.fireCheckList[0] : null);
			fireComponent.runningCheckId = 0;
			fireComponent.currentFireNum = 0;
			int collisionFlags = fireComponent.runningCheck?.GetCollisionFlags() ?? 9;
			Check(fireComponent.FindCatapultTarget(collisionFlags, attacker.GlobalPosition, -1, filterByLine: true) == upperTarget, "ThreeCornpult must acquire the upper-row zombie for its upper projectile.");
			Check(fireComponent.FindCatapultTarget(collisionFlags, attacker.GlobalPosition, 0, filterByLine: true) == centerTarget, "ThreeCornpult must acquire the center-row zombie for its center projectile.");
			Check(attacker.IsInsideComponentBattlefield, "ThreeCornpult must be inside the component gameplay battlefield before firing.");
			bulletField.OnBulletSpawned += CaptureSpawn;
			fireComponent.OnFireVolley += CaptureVolley;
			fireComponent.Fire();
			fireComponent.OnFireVolley -= CaptureVolley;
			bulletField.OnBulletSpawned -= CaptureSpawn;
			Check(volleyCount == 3, $"ThreeCornpult must execute all three authored projectile entries; volleys={volleyCount}.");
			Check(spawnedIndices.Count == 2, $"ThreeCornpult must create one projectile for each occupied row and skip the empty row; spawned={spawnedIndices.Count}.");
			if (spawnedIndices.Count != 2)
			{
				return;
			}
			int num = -1;
			int num2 = -1;
			foreach (int item in spawnedIndices)
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(item);
				Check(!bulletDataRef.checkAll && bulletDataRef.lockGridY, $"ThreeCornpult projectile {item} must use lane-scoped collision and retain its authored row.");
				if (bulletDataRef.target == upperTarget)
				{
					num = item;
				}
				else if (bulletDataRef.target == centerTarget)
				{
					num2 = item;
				}
			}
			Check(num >= 0 && num2 >= 0, "ThreeCornpult must bind distinct projectiles to the upper and center zombies.");
			if (num < 0 || num2 < 0)
			{
				return;
			}
			ref BulletData bulletDataRef2 = ref bulletField.GetBulletDataRef(num);
			ref BulletData bulletDataRef3 = ref bulletField.GetBulletDataRef(num2);
			Check(bulletDataRef2.gridY == upperTarget.gridPos.Y && bulletDataRef3.gridY == centerTarget.gridPos.Y && bulletDataRef2.gridY != bulletDataRef3.gridY, "ThreeCornpult projectiles must enter separate logical row buckets before collision.");
			Check(centerTarget.TryGetActiveWorldHitRect(out var rect) && upperTarget.TryGetActiveWorldHitRect(out var rect2), "Both ThreeCornpult target hit boxes must be available.");
			if (!centerTarget.TryGetActiveWorldHitRect(out rect) || !upperTarget.TryGetActiveWorldHitRect(out rect2))
			{
				return;
			}
			upperHurtCount = 0;
			centerHurtCount = 0;
			upperTarget.OnBodyHurt += CaptureUpperHurt;
			centerTarget.OnBodyHurt += CaptureCenterHurt;
			try
			{
				PlaceDescendingCatapultAtCollision(ref bulletDataRef2, rect.GetCenter());
				PlaceDescendingCatapultAtCollision(ref bulletDataRef3, rect.GetCenter());
				bulletField.Update(0.0, Engine.GetPhysicsFrames() + 1);
				Check(centerHurtCount == 1 && upperHurtCount == 0 && bulletField.IsBulletActive(num) && !bulletField.IsBulletActive(num2), "Overlapping upper and center projectiles must damage the center zombie only once; the upper-row projectile must remain active.");
				if (bulletField.IsBulletActive(num))
				{
					PlaceDescendingCatapultAtCollision(ref bulletField.GetBulletDataRef(num), rect2.GetCenter());
					bulletField.Update(0.0, Engine.GetPhysicsFrames() + 2);
				}
				Check(upperHurtCount == 1 && centerHurtCount == 1 && !bulletField.IsBulletActive(num), "Each ThreeCornpult projectile must ultimately damage only the zombie in its authored row.");
			}
			finally
			{
				upperTarget.OnBodyHurt -= CaptureUpperHurt;
				centerTarget.OnBodyHurt -= CaptureCenterHurt;
			}
		}
		finally
		{
			bulletField?.ClearActiveBullets();
			if (GodotObject.IsInstanceValid(upperTarget))
			{
				manager.CharacterUnregister(upperTarget);
			}
			if (GodotObject.IsInstanceValid(centerTarget))
			{
				manager.CharacterUnregister(centerTarget);
			}
			if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
			{
				ProjectileUpdateManager.Instance.ProcessMode = previousProjectileProcessMode;
			}
			manager.currentControl = previousControl;
			manager.gridBeginPos = previousGridBegin;
			manager.gridSize = previousGridSize;
			manager.gridNum = previousGridNum;
			control.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		void CaptureCenterHurt(int _)
		{
			centerHurtCount++;
		}
		void CaptureSpawn(int index)
		{
			bulletField.GetBulletDataRef(index);
			spawnedIndices.Add(index);
		}
		void CaptureUpperHurt(int _)
		{
			upperHurtCount++;
		}
		void CaptureVolley(ulong _)
		{
			volleyCount++;
		}
	}

	private static void PlaceDescendingCatapultAtCollision(ref BulletData bullet, Vector2 collisionPosition)
	{
		bullet.vel = Vector2.Zero;
		bullet.ySpeed = 1.0;
		bullet.z = bullet.groundHeight + 50.0;
		bullet.pos = collisionPosition + new Vector2(0f, (float)bullet.z);
		bullet.collisionEnabled = true;
	}

	private static bool TryReadPositiveY(FireComponent fire, AuditCase item, out float y)
	{
		Transform2D localTransform = default(Transform2D);
		bool flag = ((!item.UsesArea) ? (fire?.TryGetCheckRayLocalTransform(item.PositiveIndex, out localTransform) ?? false) : (fire?.TryGetCheckAreaShapeLocalTransform(item.PositiveIndex, out localTransform) ?? false));
		y = (flag ? localTransform.Origin.Y : (0f / 0f));
		return flag;
	}

	private static bool TryReadNegativeY(FireComponent fire, AuditCase item, out float y)
	{
		Transform2D localTransform = default(Transform2D);
		bool flag = ((!item.UsesArea) ? (fire?.TryGetCheckRayLocalTransform(2, out localTransform) ?? false) : (fire?.TryGetCheckAreaShapeLocalTransform(2, out localTransform) ?? false));
		y = (flag ? localTransform.Origin.Y : (0f / 0f));
		return flag;
	}

	private static float ReadDefinitionY(FireComponent fire, AuditCase item)
	{
		if (!(fire?.ComponentDefinition is FireComponentDefinition fireComponentDefinition))
		{
			return 0f / 0f;
		}
		if (!item.UsesArea)
		{
			return fireComponentDefinition.checkRayResources[item.PositiveIndex].LocalTransform.Origin.Y;
		}
		return fireComponentDefinition.checkShapeResources[item.PositiveIndex].LocalTransform.Origin.Y;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[MultiRowFireGridBindingRuntimeTest] " + message);
		}
	}
}
