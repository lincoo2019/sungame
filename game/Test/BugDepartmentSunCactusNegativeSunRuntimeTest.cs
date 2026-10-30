using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentSunCactusNegativeSunRuntimeTest : Node
{
	private const string SunCactusScenePath = "res://Asset/Anime/Character/Plant/Gold/SunCactus/Scene/TowerDefensePlantSunCactus.tscn";

	private const string WhiteFireSpikeConfigPath = "res://Asset/Config/Projectile/Spike/SpikeDefault.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		TowerDefenseControlNew control = null;
		TowerDefenseBattleFeatureSun sunFeature = null;
		Node2D characterNode = null;
		TowerDefensePlantSunCactus plant = null;
		BulletField bulletField = null;
		bool ownsBulletField = false;
		ResourceManager resources = ResourceManager.Instance;
		Resource previousProjectile = null;
		bool hadPreviousProjectile = resources?.PROJECTILE_CONFIG.TryGetValue("WhiteFireSpike", out previousProjectile) ?? false;
		try
		{
			Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
			Check(GodotObject.IsInstanceValid(resources), "ResourceManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(resources))
			{
				throw new InvalidOperationException("Required autoloads are unavailable.");
			}
			bulletField = BulletField.Instance;
			if (!GodotObject.IsInstanceValid(bulletField))
			{
				bulletField = new BulletField
				{
					Name = "SunCactusNegativeSunBulletField"
				};
				AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
				ownsBulletField = true;
			}
			Check(BulletField.Instance == bulletField, "The real BulletField must be mounted for Sun Cactus volleys.");
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Spike/SpikeDefault.tres", null, ResourceLoader.CacheMode.Ignore);
			resources.PROJECTILE_CONFIG["WhiteFireSpike"] = towerDefenseProjectileConfig;
			Check(GodotObject.IsInstanceValid(towerDefenseProjectileConfig), "The authored WhiteFireSpike projectile config must load.");
			control = new TowerDefenseControlNew
			{
				Name = "SunCactusNegativeSunControl",
				isGameRunning = true,
				isInit = true,
				levelConfig = new TowerDefenseLevelConfig()
			};
			characterNode = new Node2D
			{
				Name = "CharacterNode"
			};
			AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
			control.characterNode = characterNode;
			manager.currentControl = control;
			manager.gridBeginPos = Vector2.Zero;
			manager.gridSize = new Vector2(100f, 76f);
			manager.gridNum = new Vector2I(9, 5);
			sunFeature = new TowerDefenseBattleFeatureSun
			{
				control = control
			};
			sunFeature.Init(new Dictionary { ["Begin"] = -600L });
			control.featureDictionary[new StringName("Sun")] = sunFeature;
			Check(manager.SetSun(EconomyAccountId.Local, -600L) && sunFeature.GetSun(EconomyAccountId.Local) == -600, "The real local sun ledger must enter the reported negative-sun state.");
			plant = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Gold/SunCactus/Scene/TowerDefensePlantSunCactus.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefensePlantSunCactus>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(plant) && plant.config?.name == "PlantSunCactus", "The fixture must instantiate the real authored Sun Cactus scene.");
			if (!GodotObject.IsInstanceValid(plant))
			{
				throw new InvalidOperationException("The real Sun Cactus did not instantiate.");
			}
			Check(plant.TryAssignEconomyOwner(EconomyAccountId.Local) && plant.HasEconomyOwner && plant.EconomyOwnerAccountId == EconomyAccountId.Local, "The real Sun Cactus must read the negative local owner wallet.");
			plant.Position = new Vector2(200f, 200f);
			plant.gridPos = new Vector2I(2, 2);
			plant.inGame = true;
			characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(3);
			FireComponent fireComponent = plant.componentManager?.GetRuntime<FireComponent>("character.fire");
			Check(fireComponent != null && !fireComponent.IsReleased && fireComponent.fireCheckList.Count >= 2, "The real Sun Cactus FireComponent and authored checks must be active.");
			if (fireComponent == null || fireComponent.IsReleased)
			{
				throw new InvalidOperationException("The Sun Cactus FireComponent is unavailable.");
			}
			fireComponent.runningCheck = fireComponent.fireCheckList[0];
			bulletField.ClearActiveBullets();
			int activeCount = bulletField.ActiveCount;
			plant.FireVolley(101uL);
			int num = bulletField.ActiveCount - activeCount;
			Check(num == 6, $"At -600 sun, base fireNum=2 must still create six real projectiles across three rows; got {num}.");
			Check(sunFeature.GetSun(EconomyAccountId.Local) == -600, "Firing must not mutate the negative owner balance.");
			bulletField.ClearActiveBullets();
			Check(manager.SetSun(EconomyAccountId.Local, 600L), "The real ledger must accept the positive comparison balance.");
			plant.FireVolley(102uL);
			Check(bulletField.ActiveCount == 12, $"At 600 sun, base two plus two bonus shots must create twelve projectiles; got {bulletField.ActiveCount}.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugDepartmentSunCactusNegativeSunRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
			}
			if (hadPreviousProjectile)
			{
				resources.PROJECTILE_CONFIG["WhiteFireSpike"] = previousProjectile;
			}
			else
			{
				resources?.PROJECTILE_CONFIG.Remove("WhiteFireSpike");
			}
			if (GodotObject.IsInstanceValid(plant) && !plant.IsQueuedForDeletion())
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(characterNode) && !characterNode.IsQueuedForDeletion())
			{
				characterNode.QueueFree();
			}
			sunFeature?.Destroy();
			if (GodotObject.IsInstanceValid(control))
			{
				control.Free();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (ownsBulletField && GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.QueueFree();
			}
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(3);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"SUN_CACTUS_NEGATIVE_SUN_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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
			GD.PushError("[BugDepartmentSunCactusNegativeSunRuntimeTest] " + message);
		}
	}
}
