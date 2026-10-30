using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class IceCactusAirFireRuntimeTest : Node
{
	private const string IceCactusScenePath = "res://Asset/Anime/Character/Plant/Chapter5/IceCactus/Scene/TowerDefensePlantIceCactus.tscn";

	private const string SunCactusScenePath = "res://Asset/Anime/Character/Plant/Gold/SunCactus/Scene/TowerDefensePlantSunCactus.tscn";

	private const string SpikeConfigPath = "res://Asset/Config/Projectile/Spike/SpikeDefault.tres";

	private const string IceSpikeConfigPath = "res://Asset/Config/Projectile/Spike/IceSpike.tres";

	private const string BalloonScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Balloon/Scene/TowerDefenseZombieBalloon.tscn";

	private const string GroundZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		BulletField bulletField = null;
		ResourceManager resources = ResourceManager.Instance;
		Resource previousIceSpike;
		bool hadPreviousIceSpike = resources.PROJECTILE_CONFIG.TryGetValue("IceSpike", out previousIceSpike);
		try
		{
			_ = 1;
			try
			{
				resources.PROJECTILE_CONFIG["IceSpike"] = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Spike/IceSpike.tres", null, ResourceLoader.CacheMode.Ignore);
				bulletField = new BulletField
				{
					Name = "IceCactusAirFireBulletField"
				};
				AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
				Check(BulletField.Instance == bulletField, "The real BulletField must be mounted for cactus projectile verification.");
				await RunAirFireScenario(bulletField);
				bulletField.ClearActiveBullets();
				await RunSunCactusProjectileCollisionScenario(bulletField);
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[IceCactusAirFireRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			FreeTransientEffects(GetTree().Root);
			if (hadPreviousIceSpike)
			{
				resources.PROJECTILE_CONFIG["IceSpike"] = previousIceSpike;
			}
			else
			{
				resources.PROJECTILE_CONFIG.Remove("IceSpike");
			}
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
				bulletField.QueueFree();
			}
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0;
		GD.Print($"ICE_CACTUS_AIR_FIRE_RUNTIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static void FreeTransientEffects(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			FreeTransientEffects(child);
			if (child is TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce && GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				towerDefenseEffectSpriteOnce.Free();
			}
		}
	}

	private async Task RunSunCactusProjectileCollisionScenario(BulletField bulletField)
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew control = new TowerDefenseControlNew
		{
			isGameRunning = true
		};
		Node2D characterNode = new Node2D
		{
			Name = "SunCactusProjectileMount"
		};
		TowerDefensePlantSunCactus plant = null;
		ResourceManager resources = ResourceManager.Instance;
		Resource previousProjectile;
		bool hadPreviousProjectile = resources.PROJECTILE_CONFIG.TryGetValue("WhiteFireSpike", out previousProjectile);
		try
		{
			resources.PROJECTILE_CONFIG["WhiteFireSpike"] = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Spike/SpikeDefault.tres", null, ResourceLoader.CacheMode.Ignore);
			AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
			control.characterNode = characterNode;
			manager.currentControl = control;
			manager.gridBeginPos = new Vector2(0f, 100f);
			manager.gridSize = new Vector2(100f, 76f);
			manager.gridNum = new Vector2I(9, 5);
			plant = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Gold/SunCactus/Scene/TowerDefensePlantSunCactus.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefensePlantSunCactus>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(plant), "Real Sun Cactus scene must instantiate for projectile collision verification.");
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.Position = new Vector2(100f, 100f);
				plant.gridPos = new Vector2I(1, 1);
				plant.inGame = true;
				AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				FireComponent fireComponent = plant.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(fireComponent != null && !fireComponent.IsReleased, "Sun Cactus FireComponent must be active.");
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					bulletField.ClearActiveBullets();
					fireComponent.runningCheck = fireComponent.fireCheckList[0];
					int activeCount = bulletField.ActiveCount;
					plant.FireVolley(1uL);
					int lastSpawnedIndex = bulletField.LastSpawnedIndex;
					bool flag = bulletField.ActiveCount > activeCount && lastSpawnedIndex >= 0 && bulletField.IsBulletActive(lastSpawnedIndex);
					Check(flag, "Sun Cactus air volley must create an active BulletField projectile.");
					int num = 2;
					int num2 = (flag ? bulletField.GetBulletDataRef(lastSpawnedIndex).collisionFlags : (-1));
					Check(num2 == num, $"Sun Cactus air volley must stay air-only; got collisionFlags={num2}.");
				}
			}
		}
		finally
		{
			bulletField.ClearActiveBullets();
			if (hadPreviousProjectile)
			{
				resources.PROJECTILE_CONFIG["WhiteFireSpike"] = previousProjectile;
			}
			else
			{
				resources.PROJECTILE_CONFIG.Remove("WhiteFireSpike");
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(characterNode))
			{
				characterNode.QueueFree();
			}
			control.Free();
			manager.currentControl = null;
		}
	}

	private async Task RunAirFireScenario(BulletField bulletField)
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload is unavailable.");
		if (!GodotObject.IsInstanceValid(manager))
		{
			return;
		}
		TowerDefenseControlNew control = new TowerDefenseControlNew
		{
			isGameRunning = true
		};
		Node2D characterNode = new Node2D
		{
			Name = "IceCactusAirFireCharacterNode"
		};
		AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
		control.characterNode = characterNode;
		manager.currentControl = control;
		manager.gridBeginPos = new Vector2(0f, 100f);
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter5/IceCactus/Scene/TowerDefensePlantIceCactus.tscn", null, ResourceLoader.CacheMode.Ignore);
		PackedScene balloonPacked = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Balloon/Scene/TowerDefenseZombieBalloon.tscn", null, ResourceLoader.CacheMode.Ignore);
		PackedScene groundTargetPacked = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
		TowerDefensePlantIceCactus plant = packedScene?.Instantiate<TowerDefensePlantIceCactus>(PackedScene.GenEditState.Disabled);
		TowerDefenseZombieBalloon balloon = balloonPacked?.Instantiate<TowerDefenseZombieBalloon>(PackedScene.GenEditState.Disabled);
		TowerDefenseCharacter groundTarget = groundTargetPacked?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(balloon) && GodotObject.IsInstanceValid(groundTarget), "Real Ice Cactus, balloon, and ground target scenes must instantiate.");
		if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(balloon) || !GodotObject.IsInstanceValid(groundTarget))
		{
			return;
		}
		plant.Position = new Vector2(100f, 100f);
		plant.gridPos = new Vector2I(1, 1);
		plant.inGame = true;
		balloon.Position = new Vector2(500f, 176f);
		balloon.gridPos = new Vector2I(5, 2);
		balloon.inGame = true;
		characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
		characterNode.AddChild(balloon, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		FireComponent fire = plant.componentManager?.GetRuntime<FireComponent>("character.fire");
		FireComponentExtendCactus extension = plant.componentManager?.GetRuntime<FireComponentExtendCactus>("character.fire.cactus");
		Check(fire != null && !fire.IsReleased && extension != null && !extension.IsReleased, "Ice Cactus fire runtimes must be active.");
		if (fire == null || fire.IsReleased || extension == null || extension.IsReleased)
		{
			return;
		}
		fire.groundRight = 10000f;
		fire.timer = 0f;
		fire.checkInterval = 0;
		int num = 2;
		Check(fire.TryGetCheckRayLocalTransform(0, out var localTransform) && Mathf.IsEqualApprox(localTransform.Origin.Y, manager.gridSize.Y) && fire.TryGetCheckRayLocalTransform(2, out var localTransform2) && Mathf.IsEqualApprox(localTransform2.Origin.Y, 0f - manager.gridSize.Y), "Ice Cactus adjacent-row rays must follow the active map grid height.");
		Check(fire.CanFireCheckOnce(null, num), "The configured Ice Cactus rays must acquire a real balloon target.");
		extension.IdleProcessing(0.0);
		Check(extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.up", "An airborne target must move Ice Cactus into its extension state.");
		extension.sprite.clipOver = true;
		extension.UpProcessing(0.0);
		Check(extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.idle", "A completed raise clip must recover to cactus.idle even when its animation-completed signal is missed.");
		Check(fire.fireNum == 2, "Completing the extension animation must enable the airborne two-projectile volley.");
		extension.IdleProcessing(0.0);
		Check(extension.CanRun(), "An extended Ice Cactus must release the FireComponent gate while the balloon remains in range.");
		fire.IdleProcessing(0.0);
		Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack", "The released gate must let FireComponent enter its attack state.");
		IceCactusAirFireRuntimeTest iceCactusAirFireRuntimeTest = this;
		int condition;
		if (fire.runningCheckId == 0)
		{
			FireComponentCheckConfig runningCheck = fire.runningCheck;
			condition = ((runningCheck != null && runningCheck.GetCollisionFlags() == num) ? 1 : 0);
		}
		else
		{
			condition = 0;
		}
		iceCactusAirFireRuntimeTest.Check((byte)condition != 0, "The real volley must keep the airborne check instead of falling through to the ground check.");
		string activeAirFireClip = fire.fireAnimeClips;
		balloon.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		groundTarget.Position = new Vector2(500f, 176f);
		groundTarget.gridPos = new Vector2I(5, 2);
		groundTarget.inGame = true;
		characterNode.AddChild(groundTarget, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		extension.IdleProcessing(0.0);
		Check(extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.idle" && extension.IsUp() && fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack" && extension.sprite.clip == activeAirFireClip, $"A ground target must not interrupt an active airborne volley with Down; cactusState={extension.StateMachine?.CurrentStateHandle?.StableId}, fireState={fire.StateMachine?.CurrentStateHandle?.StableId}, up={extension.IsUp()}, clip={extension.sprite.clip}.");
		fire.AnimeCompleted(activeAirFireClip);
		Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.idle", "Completing the active airborne volley must return FireComponent to idle before lowering.");
		plant.Idle();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		fire.timer = 0f;
		fire.checkInterval = 0;
		extension.IdleProcessing(0.0);
		Check(extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.down", "Removing the balloon must lower an extended Ice Cactus even when a ground target remains.");
		extension.sprite.clipOver = true;
		extension.DownProcessing(0.0);
		Check(extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.idle", "A completed lower clip must recover to cactus.idle even when its animation-completed signal is missed.");
		Check(!extension.IsUp() && fire.fireNum == 1, "Completing the lowering animation must restore normal ground posture and a single projectile.");
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Check(extension.CanRun(), "A lowered Ice Cactus must release the FireComponent gate for its ground target.");
		Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack" && fire.runningCheckId == 1, $"After lowering, Ice Cactus must automatically re-enter fire.attack using its ground fire check; fireState={fire.StateMachine?.CurrentStateHandle?.StableId}, componentRunning={plant.componentRunning}, canRun={extension.CanRun()}.");
		extension.AnimeCompleted(extension.downAnimeClips);
		Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack" && plant.componentRunning && extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.idle", "A late Down completion signal must not cancel the ground attack that already started after landing.");
		string activeGroundFireClip = fire.fireAnimeClips;
		TowerDefenseZombieBalloon returningBalloon = balloonPacked?.Instantiate<TowerDefenseZombieBalloon>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(returningBalloon), "A second balloon must instantiate for the ground-to-air transition regression.");
		if (GodotObject.IsInstanceValid(returningBalloon))
		{
			returningBalloon.Position = new Vector2(500f, 176f);
			returningBalloon.gridPos = new Vector2I(5, 2);
			returningBalloon.inGame = true;
			characterNode.AddChild(returningBalloon, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			extension.IdleProcessing(0.0);
			Check(extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.idle" && !extension.IsUp() && fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack" && extension.sprite.clip == activeGroundFireClip, $"An airborne target must not interrupt an active ground volley with Up; cactusState={extension.StateMachine?.CurrentStateHandle?.StableId}, fireState={fire.StateMachine?.CurrentStateHandle?.StableId}, up={extension.IsUp()}, clip={extension.sprite.clip}.");
			fire.AnimeCompleted(activeGroundFireClip);
			Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.idle", "Completing the active ground volley must return FireComponent to idle before rising.");
			plant.Idle();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			fire.timer = 0f;
			fire.checkInterval = 0;
			extension.IdleProcessing(0.0);
			Check(extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.up", "After the ground volley completes, the waiting balloon must move Ice Cactus into its extension state.");
			extension.sprite.clipOver = true;
			extension.UpProcessing(0.0);
			extension.IdleProcessing(0.0);
			fire.IdleProcessing(0.0);
			Check(extension.IsUp() && fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack" && fire.runningCheckId == 0, $"After rising, Ice Cactus must automatically attack the waiting balloon; fireState={fire.StateMachine?.CurrentStateHandle?.StableId}, runningCheckId={fire.runningCheckId}, canRun={extension.CanRun()}.");
			returningBalloon.QueueFree();
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		plant.ProcessMode = ProcessModeEnum.Disabled;
		TowerDefenseCharacter overlapGround = groundTargetPacked?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		TowerDefenseZombieBalloon overlapBalloon = balloonPacked?.Instantiate<TowerDefenseZombieBalloon>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(overlapGround) && GodotObject.IsInstanceValid(overlapBalloon), "Real ground and balloon zombies must instantiate for projectile collision isolation.");
		List<int> probeIndices;
		List<int> groundBulletIndices;
		if (GodotObject.IsInstanceValid(overlapGround) && GodotObject.IsInstanceValid(overlapBalloon))
		{
			bulletField.ClearActiveBullets();
			fire.runningCheck = fire.fireCheckList[1];
			fire.runningCheckId = 1;
			probeIndices = new List<int>();
			bulletField.OnBulletSpawned += CaptureProbeBullet;
			fire.Fire();
			bulletField.OnBulletSpawned -= CaptureProbeBullet;
			Check(probeIndices.Count == 3 && bulletField.IsBulletActive(probeIndices[0]), $"The Ice Cactus probe volley must create three projectiles; got {probeIndices.Count}.");
			Vector2 overlapPosition = Vector2.Zero;
			Vector2I gridPos = Vector2I.Zero;
			if (probeIndices.Count > 0 && bulletField.IsBulletActive(probeIndices[0]))
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(probeIndices[0]);
				overlapPosition = bulletDataRef.pos + new Vector2(150f, (float)bulletDataRef.height);
				gridPos = new Vector2I(5, bulletDataRef.gridY);
			}
			bulletField.ClearActiveBullets();
			overlapGround.inGame = true;
			overlapGround.SetLogicalGlobalPosition(overlapPosition);
			overlapGround.gridPos = gridPos;
			overlapBalloon.inGame = true;
			overlapBalloon.SetLogicalGlobalPosition(overlapPosition);
			overlapBalloon.gridPos = gridPos;
			characterNode.AddChild(overlapGround, forceReadableName: false, InternalMode.Disabled);
			characterNode.AddChild(overlapBalloon, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			Rect2 worldHitRect = overlapGround.WorldHitRect;
			Rect2 worldHitRect2 = overlapBalloon.WorldHitRect;
			overlapGround.SetLogicalGlobalPosition(overlapGround.GetLogicalGlobalPosition() + overlapPosition - (worldHitRect.Position + worldHitRect.Size * 0.5f));
			overlapBalloon.SetLogicalGlobalPosition(overlapBalloon.GetLogicalGlobalPosition() + overlapPosition - (worldHitRect2.Position + worldHitRect2.Size * 0.5f));
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			Check(overlapGround.IsHitBoxEnabled && overlapBalloon.IsHitBoxEnabled && overlapGround.camp != plant.camp && overlapBalloon.camp != plant.camp, "Both overlap targets must have enabled hit boxes and oppose the Ice Cactus camp.");
			double groundHitpointsBefore = overlapGround.instance.hitpoints;
			double airHitpointsBefore = overlapBalloon.instance.hitpoints;
			int groundHurtEvents = 0;
			int airHurtEvents = 0;
			overlapGround.OnBodyHurt += delegate
			{
				groundHurtEvents++;
			};
			overlapBalloon.OnBodyHurt += delegate
			{
				airHurtEvents++;
			};
			groundBulletIndices = new List<int>();
			bulletField.OnBulletSpawned += CaptureGroundBullet;
			fire.Fire();
			bulletField.OnBulletSpawned -= CaptureGroundBullet;
			int num2 = ((groundBulletIndices.Count > 0) ? groundBulletIndices[0] : (-1));
			bool condition2 = groundBulletIndices.Count == 3 && num2 >= 0 && bulletField.IsBulletActive(num2);
			Check(condition2, $"The real Ice Cactus ground volley must create its three active BulletField projectiles; got {groundBulletIndices.Count}.");
			int num3 = 1;
			int num4 = 2;
			int num5 = 0;
			foreach (int item in groundBulletIndices)
			{
				if (bulletField.IsBulletActive(item))
				{
					int collisionFlags = bulletField.GetBulletDataRef(item).collisionFlags;
					if ((collisionFlags & num3) == 0 || (collisionFlags & num4) != 0)
					{
						num5 = collisionFlags;
					}
				}
			}
			Check(num5 == 0, $"Every Ice Cactus ground projectile must exclude airborne collision; invalid collisionFlags={num5}.");
			await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
			Check(groundHurtEvents > 0 && overlapGround.instance.hitpoints < groundHitpointsBefore, $"The real Ice Spike must damage the overlapping ground zombie; hp={overlapGround.instance.hitpoints}/{groundHitpointsBefore}, events={groundHurtEvents}.");
			Check(airHurtEvents == 0 && Mathf.IsEqualApprox(overlapBalloon.instance.hitpoints, airHitpointsBefore), $"The same ground Ice Spike must not affect the overlapping balloon; hp={overlapBalloon.instance.hitpoints}/{airHitpointsBefore}, events={airHurtEvents}.");
			overlapGround.QueueFree();
			overlapBalloon.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		plant.QueueFree();
		groundTarget.QueueFree();
		characterNode.QueueFree();
		control.Free();
		manager.currentControl = null;
		void CaptureGroundBullet(int index)
		{
			groundBulletIndices.Add(index);
		}
		void CaptureProbeBullet(int index)
		{
			probeIndices.Add(index);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[IceCactusAirFireRuntimeTest] " + message);
		}
	}
}
