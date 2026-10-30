using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewIcePlanternDeathPeaDamageRuntimeTest : Node
{
	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter3/PlanternSix/Scene/TowerDefensePlanternSix.tscn";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I SourceGrid = new Vector2I(4, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		IcePlanternDeathPeaRuntimeControlStub control = null;
		TowerDefenseZombie zombie = null;
		TowerDefenseZombie centerZombie = null;
		TowerDefensePlanternSix plant = null;
		TowerDefenseProjectileEffectPlanternSix effect = null;
		BulletField bulletField = null;
		Action<int> bulletSpawnedHandler = null;
		int peaBulletsSpawned = 0;
		List<int> spawnedBulletIndices = new List<int>();
		try
		{
			_ = 6;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ObjectManager.Instance), "ObjectManager autoload must be available for node projectiles.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
				{
					throw new InvalidOperationException("Required runtime autoloads are unavailable.");
				}
				bulletField = new BulletField
				{
					Name = "IcePlanternDeathPeaBulletField"
				};
				AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
				Check(BulletField.Instance == bulletField, "The live BulletField must be mounted so the forced-node routing fix is exercised.");
				bulletSpawnedHandler = delegate(int bulletIndex)
				{
					if (bulletField.IsBulletActive(bulletIndex))
					{
						string text = bulletField.GetBulletDataRef(bulletIndex).config?.name;
						bool flag2;
						switch (text)
						{
						case "Pea":
						case "FirePea":
						case "SnowPea":
							flag2 = true;
							break;
						default:
							flag2 = false;
							break;
						}
						if (flag2)
						{
							spawnedBulletIndices.Add(bulletIndex);
						}
						if (text == "Pea")
						{
							peaBulletsSpawned++;
						}
					}
				};
				bulletField.OnBulletSpawned += bulletSpawnedHandler;
				control = new IcePlanternDeathPeaRuntimeControlStub
				{
					Name = "IcePlanternDeathPeaRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				Node2D characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = characterNode;
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				TowerDefenseProjectileRegistry.Init();
				Vector2 sourcePosition = new Vector2(500f, 300f);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
				zombie = packedScene?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
				centerZombie = packedScene?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie), "The real normal-zombie target scene must instantiate.");
				Check(GodotObject.IsInstanceValid(centerZombie), "The real center-overlap normal-zombie scene must instantiate.");
				if (!GodotObject.IsInstanceValid(zombie) || !GodotObject.IsInstanceValid(centerZombie))
				{
					throw new InvalidOperationException("The real normal-zombie scene is unavailable.");
				}
				zombie.inGame = true;
				zombie.editorPreviewMode = false;
				zombie.gridPos = new Vector2I(5, SourceGrid.Y);
				zombie.GlobalPosition = sourcePosition + new Vector2(75f, 30f);
				characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				zombie.instance.hitpoints = 100000.0;
				zombie.instance.canBeCollection = false;
				double initialHitpoints = zombie.instance.hitpoints;
				int bodyHurtEvents = 0;
				zombie.OnBodyHurt += delegate
				{
					bodyHurtEvents++;
				};
				Check(initialHitpoints > 0.0 && zombie.IsHitBoxEnabled, "The real zombie target must begin alive with its hit box enabled.");
				centerZombie.inGame = true;
				centerZombie.editorPreviewMode = false;
				centerZombie.gridPos = new Vector2I(4, SourceGrid.Y);
				centerZombie.GlobalPosition = sourcePosition + new Vector2(0f, 30f);
				characterNode.AddChild(centerZombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				centerZombie.ProcessMode = ProcessModeEnum.Disabled;
				centerZombie.instance.hitpoints = 100000.0;
				centerZombie.instance.canBeCollection = false;
				Check(centerZombie.instance.hitpoints > 0.0 && centerZombie.IsHitBoxEnabled, "The center-overlap zombie must begin alive with its hit box enabled.");
				bool orbitHitObserved = false;
				int orbitHitBulletIndex = -1;
				zombie.OnBodyHurt += delegate
				{
					if (GodotObject.IsInstanceValid(effect) && effect.timer < 2.0 && TryGetSingleExternalOrbitBulletOverlappingTarget(bulletField, spawnedBulletIndices, zombie, out var bulletIndex))
					{
						orbitHitObserved = true;
						orbitHitBulletIndex = bulletIndex;
					}
				};
				plant = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter3/PlanternSix/Scene/TowerDefensePlanternSix.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefensePlanternSix>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(plant), "The real Plantern Six plant scene must instantiate.");
				if (!GodotObject.IsInstanceValid(plant))
				{
					throw new InvalidOperationException("The real Plantern Six plant scene is unavailable.");
				}
				plant.inGame = true;
				plant.editorPreviewMode = false;
				plant.gridPos = SourceGrid;
				plant.GlobalPosition = sourcePosition;
				characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				plant.ProcessMode = ProcessModeEnum.Disabled;
				int collisionFlags = plant.config.collisionFlags;
				Check(collisionFlags != 0, "Plantern Six must have a nonzero authored projectile collision mask.");
				plant.instance.DieMethod();
				Check(plant.instance.collisionFlags == 0, "The real death lifecycle must clear the runtime instance collision flags.");
				plant.DestroySet();
				foreach (Node child in characterNode.GetChildren())
				{
					if (child is TowerDefenseProjectileEffectPlanternSix towerDefenseProjectileEffectPlanternSix)
					{
						effect = towerDefenseProjectileEffectPlanternSix;
						break;
					}
				}
				Check(GodotObject.IsInstanceValid(effect), "The real Plantern Six DestroySet must create its death-burst effect.");
				if (!GodotObject.IsInstanceValid(effect))
				{
					throw new InvalidOperationException("The real Plantern Six death-burst effect was not created.");
				}
				Check(effect.collisionFlag == collisionFlags, $"The death burst must retain authored collision flags after DieMethod; got {effect.collisionFlag}, expected {collisionFlags}.");
				Check(HasExactOrbitLayout(bulletField, spawnedBulletIndices, sourcePosition, 6, 3, 3, requireRadialRotation: true), "The first wave must synchronously occupy three unique 120-degree inner slots and three unique 120-degree outer slots.");
				int num = CountExternalOrbitBulletsOverlappingTarget(bulletField, spawnedBulletIndices, centerZombie);
				Check(num >= 0 && num < 6, $"The real center zombie must reject an all-six spawn-point overlap; overlaps={num}.");
				for (int frame = 0; frame < 120; frame++)
				{
					if (spawnedBulletIndices.Count >= 12)
					{
						break;
					}
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				Check(spawnedBulletIndices.Count == 12, $"The second synchronous wave must expose exactly twelve tracked slots; got {spawnedBulletIndices.Count}.");
				Check(HasExactOrbitLayout(bulletField, spawnedBulletIndices, sourcePosition, 12, 6, 6, requireRadialRotation: false), "After the list grows, all twelve bullets must be synchronously regrouped into six unique 60-degree inner slots and six unique 60-degree outer slots.");
				int num2 = CountExternalOrbitBulletsOverlappingTarget(bulletField, spawnedBulletIndices, centerZombie);
				Check(num2 >= 0 && num2 < 12, $"The real center zombie must reject an all-twelve stale-center overlap after regrouping; overlaps={num2}.");
				zombie.instance.canBeCollection = true;
				await WaitSeconds(1.5);
				await WaitFrames(3);
				Check(effect.timer < 2.0, $"The damage probe must run before any death-burst bullet finishes its two-second orbit; timer={effect.timer:F3}.");
				Check(bodyHurtEvents > 0, "An orbiting death-burst projectile must hit the real zombie before the orbit completes.");
				Check(zombie.instance.hitpoints < initialHitpoints, $"Plantern Six death projectiles must deal damage during the orbit; hp stayed {zombie.instance.hitpoints}/{initialHitpoints}.");
				Check(orbitHitObserved && orbitHitBulletIndex >= 0, "The outer target's hurt callback must identify exactly one live external-controlled orbit slot as its damage source.");
				await WaitSeconds(2.0);
				await WaitFrames(3);
				Check(peaBulletsSpawned >= 20, $"The real death burst must emit all authored ordinary Peas through BulletField; got {peaBulletsSpawned}, expected at least 20.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewIcePlanternDeathPeaDamageRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			foreach (Node item in GetTree().GetNodesInGroup("Projectile"))
			{
				if (item is TowerDefenseProjectile towerDefenseProjectile && GodotObject.IsInstanceValid(towerDefenseProjectile))
				{
					towerDefenseProjectile.QueueFree();
				}
			}
			if (GodotObject.IsInstanceValid(effect))
			{
				effect.QueueFree();
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(centerZombie))
			{
				centerZombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(bulletField))
			{
				if (bulletSpawnedHandler != null)
				{
					bulletField.OnBulletSpawned -= bulletSpawnedHandler;
				}
				bulletField.ClearActiveBullets();
				bulletField.QueueFree();
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
			await WaitFrames(4);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"ICE_PLANTERN_DEATH_PEA_DAMAGE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool HasExactOrbitLayout(BulletField bulletField, List<int> bulletIndices, Vector2 center, int expectedTotal, int expectedInnerCount, int expectedOuterCount, bool requireRadialRotation)
	{
		if (!GodotObject.IsInstanceValid(bulletField) || bulletIndices.Count != expectedTotal || bulletField.ActiveCount != expectedTotal || expectedInnerCount <= 0 || expectedOuterCount <= 0 || expectedInnerCount + expectedOuterCount != expectedTotal)
		{
			return false;
		}
		List<float> list = new List<float>(expectedInnerCount);
		List<float> list2 = new List<float>(expectedOuterCount);
		foreach (int bulletIndex in bulletIndices)
		{
			if (!bulletField.IsBulletActive(bulletIndex))
			{
				continue;
			}
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(bulletIndex);
			if (!bulletDataRef.externalControlled)
			{
				return false;
			}
			Vector2 vector = bulletDataRef.pos - center;
			float num = vector.Length();
			if (num <= 0.25f)
			{
				return false;
			}
			float num2 = vector.Angle();
			if (num2 < 0f)
			{
				num2 += (float)Math.PI * 2f;
			}
			if (requireRadialRotation && Mathf.Abs(Mathf.AngleDifference(bulletDataRef.rotation, num2)) > 0.02f)
			{
				return false;
			}
			if (Mathf.Abs(num - 30f) <= 0.25f)
			{
				list.Add(num2);
				continue;
			}
			if (Mathf.Abs(num - 60f) <= 0.25f)
			{
				list2.Add(num2);
				continue;
			}
			return false;
		}
		if (HasEvenAngularSpacing(list, expectedInnerCount))
		{
			return HasEvenAngularSpacing(list2, expectedOuterCount);
		}
		return false;
	}

	private static bool HasEvenAngularSpacing(List<float> angles, int expectedCount)
	{
		if (angles.Count != expectedCount || expectedCount <= 0)
		{
			return false;
		}
		angles.Sort();
		float num = (float)Math.PI * 2f / (float)expectedCount;
		for (int i = 0; i < angles.Count; i++)
		{
			if (Mathf.Abs(((i + 1 < angles.Count) ? angles[i + 1] : (angles[0] + (float)Math.PI * 2f)) - angles[i] - num) > 0.02f)
			{
				return false;
			}
		}
		return true;
	}

	private static int CountExternalOrbitBulletsOverlappingTarget(BulletField bulletField, List<int> bulletIndices, TowerDefenseCharacter target)
	{
		if (!GodotObject.IsInstanceValid(bulletField) || !GodotObject.IsInstanceValid(target) || !target.TryGetActiveWorldHitRect(out var rect))
		{
			return -1;
		}
		int num = 0;
		foreach (int bulletIndex in bulletIndices)
		{
			if (!bulletField.IsBulletActive(bulletIndex))
			{
				continue;
			}
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(bulletIndex);
			if (bulletDataRef.externalControlled)
			{
				Vector2 vector = bulletDataRef.pos + new Vector2(0f, (float)bulletDataRef.height);
				Vector2 collisionHalfSize = BulletField.GetCollisionHalfSize(ref bulletDataRef);
				if (new Rect2(vector - collisionHalfSize, collisionHalfSize * 2f).Intersects(rect))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static bool TryGetSingleExternalOrbitBulletOverlappingTarget(BulletField bulletField, List<int> bulletIndices, TowerDefenseCharacter target, out int bulletIndex)
	{
		bulletIndex = -1;
		if (!GodotObject.IsInstanceValid(bulletField) || !GodotObject.IsInstanceValid(target) || !target.TryGetActiveWorldHitRect(out var rect))
		{
			return false;
		}
		foreach (int bulletIndex2 in bulletIndices)
		{
			if (!bulletField.IsBulletActive(bulletIndex2))
			{
				continue;
			}
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(bulletIndex2);
			if (!bulletDataRef.externalControlled)
			{
				continue;
			}
			Vector2 vector = bulletDataRef.pos + new Vector2(0f, (float)bulletDataRef.height);
			Vector2 collisionHalfSize = BulletField.GetCollisionHalfSize(ref bulletDataRef);
			if (new Rect2(vector - collisionHalfSize, collisionHalfSize * 2f).Intersects(rect))
			{
				if (bulletIndex >= 0 && bulletIndex != bulletIndex2)
				{
					bulletIndex = -1;
					return false;
				}
				bulletIndex = bulletIndex2;
			}
		}
		return bulletIndex >= 0;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewIcePlanternDeathPeaDamageRuntimeTest] " + message);
		}
	}
}
