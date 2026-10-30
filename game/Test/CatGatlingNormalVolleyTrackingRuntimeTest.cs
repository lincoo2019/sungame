using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CatGatlingNormalVolleyTrackingRuntimeTest : Node
{
	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Gold/CatGatlingPea/Scene/TowerDefensePlantCatGatlingPea.tscn";

	private const string TargetScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const int ExpectedNormalShots = 4;

	private const int ExpectedScatterShots = 5;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		try
		{
			await RunRealVolley();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[CatGatlingNormalVolleyTrackingRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"CAT_GATLING_NORMAL_VOLLEY_TRACKING_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunRealVolley()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		Check(GodotObject.IsInstanceValid(instance), "TowerDefenseManager autoload must be available.");
		Check(GodotObject.IsInstanceValid(ObjectManager.Instance), "ObjectManager autoload must be available.");
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
		{
			return;
		}
		instance.gridBeginPos = new Vector2(0f, 100f);
		instance.gridSize = new Vector2(100f, 76f);
		instance.gridNum = new Vector2I(9, 5);
		TowerDefenseProjectileRegistry.Init();
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Gold/CatGatlingPea/Scene/TowerDefensePlantCatGatlingPea.tscn", null, ResourceLoader.CacheMode.Ignore);
		PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
		TowerDefensePlantCatGatlingPea plant = packedScene?.Instantiate<TowerDefensePlantCatGatlingPea>(PackedScene.GenEditState.Disabled);
		TowerDefenseCharacter target = packedScene2?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(target), "The real Cat Star Gatling and normal-zombie target scenes must instantiate.");
		if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(target))
		{
			return;
		}
		plant.inGame = false;
		plant.editorPreviewMode = true;
		plant.Position = new Vector2(200f, 100f);
		plant.gridPos = new Vector2I(2, 1);
		target.Position = new Vector2(700f, 100f);
		target.gridPos = new Vector2I(7, 1);
		target.inGame = true;
		AddChild(plant, forceReadableName: false, InternalMode.Disabled);
		AddChild(target, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		FireComponent fireComponent = plant.componentManager?.GetRuntime<FireComponent>("character.fire");
		Check(fireComponent != null && !fireComponent.IsReleased, "The real Cat Star Gatling FireComponent must be active.");
		if (fireComponent == null || fireComponent.IsReleased)
		{
			return;
		}
		fireComponent.alive = true;
		TowerDefenseProjectileCreateData projetile = fireComponent.fireCheckList[0].projectile.GetProjetile();
		Check(GodotObject.IsInstanceValid(projetile), "The normal Star projectile create data must be available.");
		if (!GodotObject.IsInstanceValid(projetile))
		{
			return;
		}
		int num = 1;
		int num2 = 32;
		Check((projetile.fireMethodFlags & num) != 0, "The normal four-shot create data must retain SHOOTER.");
		Check((projetile.fireMethodFlags & num2) == 0, "The normal four-shot create data must not carry TRACK.");
		if (System.Environment.GetEnvironmentVariable("PVZHE_CAT_GATLING_LEGACY_ALL_TRACK") == "1")
		{
			projetile.fireMethodFlags = num2;
			projetile.InvalidateConfigCache();
		}
		HashSet<ulong> hashSet = new HashSet<ulong>();
		foreach (Node item in GetTree().GetNodesInGroup("Projectile"))
		{
			if (item is TowerDefenseProjectile towerDefenseProjectile)
			{
				hashSet.Add(towerDefenseProjectile.GetInstanceId());
			}
		}
		for (int i = 0; i < 4; i++)
		{
			fireComponent.Fire();
			plant.AnimeEvent("fire", default(Variant));
		}
		List<TowerDefenseProjectile> list = new List<TowerDefenseProjectile>();
		foreach (Node item2 in GetTree().GetNodesInGroup("Projectile"))
		{
			if (item2 is TowerDefenseProjectile towerDefenseProjectile2 && !hashSet.Contains(towerDefenseProjectile2.GetInstanceId()))
			{
				list.Add(towerDefenseProjectile2);
			}
		}
		Check(list.Count == 9, $"One real round must spawn 9 projectiles; got {list.Count}.");
		int num3 = 0;
		int num4 = 0;
		foreach (TowerDefenseProjectile item3 in list)
		{
			bool flag = (item3.fireMethodFlags & num) != 0;
			bool flag2 = (item3.fireMethodFlags & num2) != 0;
			if (flag && !flag2)
			{
				num3++;
			}
			if (flag2 && !flag)
			{
				num4++;
			}
		}
		Check(num3 == 4, $"The four normal bullets must stay non-tracking SHOOTER; got {num3}.");
		Check(num4 == 5, $"Only the five radial bonus stars should TRACK; got {num4}.");
		foreach (TowerDefenseProjectile item4 in list)
		{
			item4.QueueFree();
		}
		plant.QueueFree();
		target.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[CatGatlingNormalVolleyTrackingRuntimeTest] " + message);
		}
	}
}
