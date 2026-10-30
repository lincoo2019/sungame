using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewAlienButterRecoveryRuntimeTest : Node
{
	private const string AlienScenePath = "res://Asset/Anime/Character/Zombie/Challenge/Alien/Scene/TowerDefenseZombieAlien.tscn";

	private const string CornpultScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Cornpult/Scene/TowerDefensePlantCornpult.tscn";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private const string ButterConfigPath = "res://Asset/Config/Projectile/Butter/ButterDefault.tres";

	private const double StepSeconds = 1.0 / 60.0;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewAlienButterRecoveryControlStub control = null;
		TowerDefenseZombieAlien alien = null;
		TowerDefensePlantCornpult cornpult = null;
		TowerDefensePlant wallnut = null;
		Node butterProjectileVisual = null;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					return;
				}
				control = new BugOverviewAlienButterRecoveryControlStub
				{
					Name = "AlienButterRecoveryControl",
					isGameRunning = false,
					isInit = true
				};
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
				alien = LoadCharacter<TowerDefenseZombieAlien>("res://Asset/Anime/Character/Zombie/Challenge/Alien/Scene/TowerDefenseZombieAlien.tscn");
				cornpult = LoadCharacter<TowerDefensePlantCornpult>("res://Asset/Anime/Character/Plant/Chapter0/Cornpult/Scene/TowerDefensePlantCornpult.tscn");
				wallnut = LoadCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
				Check(GodotObject.IsInstanceValid(alien) && alien.config?.name == "ZombieAlien" && GodotObject.IsInstanceValid(cornpult) && cornpult.config?.name == "PlantCornpult" && GodotObject.IsInstanceValid(wallnut) && wallnut.config?.name == "PlantWallnut", "The fixture must instantiate the real Alien, Cornpult, and Wall-nut scenes.");
				if (!GodotObject.IsInstanceValid(alien) || !GodotObject.IsInstanceValid(cornpult) || !GodotObject.IsInstanceValid(wallnut))
				{
					return;
				}
				Vector2 position = new Vector2(400f, 252f);
				Vector2I gridPos = new Vector2I(4, 2);
				PrepareCharacter(alien, gridPos, position);
				PrepareCharacter(wallnut, gridPos, position);
				PrepareCharacter(cornpult, new Vector2I(2, 2), new Vector2(200f, 252f));
				control.characterNode.AddChild(wallnut, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(cornpult, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(alien, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				AttackComponent attack = alien.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				FireComponent fireComponent = cornpult.componentManager?.GetRuntime<FireComponent>("character.fire");
				BugOverviewAlienButterRecoveryRuntimeTest bugOverviewAlienButterRecoveryRuntimeTest = this;
				int condition;
				if (GodotObject.IsInstanceValid(alien.sprite))
				{
					BuffComponent buff = alien.buff;
					if (buff != null && !buff.IsReleased)
					{
						if (attack != null && !attack.IsReleased)
						{
							condition = ((fireComponent != null && !fireComponent.IsReleased) ? 1 : 0);
							goto IL_04b4;
						}
					}
				}
				condition = 0;
				goto IL_04b4;
				IL_04b4:
				bugOverviewAlienButterRecoveryRuntimeTest.Check((byte)condition != 0, "The production animation, Buff, Alien Attack, and Cornpult Fire runtimes must initialize.");
				if (!GodotObject.IsInstanceValid(alien.sprite) || (alien.buff?.IsReleased ?? true) || (attack?.IsReleased ?? true) || (fireComponent?.IsReleased ?? true))
				{
					goto end_IL_0100;
				}
				Check(CornpultAuthorsButter(fireComponent), "The real Cornpult FireComponent must retain its authored weighted Butter projectile.");
				TowerDefenseProjectileConfig towerDefenseProjectileConfig = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Butter/ButterDefault.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff = ((towerDefenseProjectileConfig != null && towerDefenseProjectileConfig.hitTargetEventList?.Count == 1) ? (towerDefenseProjectileConfig.hitTargetEventList[0] as TowerDefenseCharacterEventAddBuff) : null);
				Check(GodotObject.IsInstanceValid(towerDefenseProjectileConfig) && towerDefenseProjectileConfig.name == "Butter" && GodotObject.IsInstanceValid(towerDefenseProjectileConfig.projectileScene) && GodotObject.IsInstanceValid(towerDefenseCharacterEventAddBuff), "The production Butter config, projectile scene, and hit AddBuff event must load.");
				if (!GodotObject.IsInstanceValid(towerDefenseProjectileConfig) || !GodotObject.IsInstanceValid(towerDefenseProjectileConfig.projectileScene) || !GodotObject.IsInstanceValid(towerDefenseCharacterEventAddBuff))
				{
					goto end_IL_0100;
				}
				butterProjectileVisual = towerDefenseProjectileConfig.projectileScene.Instantiate(PackedScene.GenEditState.Disabled);
				butterProjectileVisual.Name = "RealButterProjectileVisual";
				AddChild(butterProjectileVisual, forceReadableName: false, InternalMode.Disabled);
				Check(butterProjectileVisual is Sprite2D sprite2D && GodotObject.IsInstanceValid(sprite2D.Texture), "The dedicated scene must contain the real Butter projectile visual and texture.");
				alien.ProcessMode = ProcessModeEnum.Disabled;
				cornpult.ProcessMode = ProcessModeEnum.Disabled;
				wallnut.ProcessMode = ProcessModeEnum.Disabled;
				control.isGameRunning = true;
				alien.groundRight = 1000.0;
				attack.groundRight = 1000.0;
				attack.alive = true;
				attack.checkIntrevalNow = 0;
				attack.timer = 10.0;
				attack.target = wallnut;
				alien.Walk();
				Check(attack.CanAttack() && attack.target == wallnut, "The real Alien must acquire the overlapping real Wall-nut as its live laser target.");
				towerDefenseCharacterEventAddBuff.ExecuteProject(null, alien);
				TowerDefenseCharacterBuffButter activeButter = alien.BuffGet("Butter") as TowerDefenseCharacterBuffButter;
				Check(GodotObject.IsInstanceValid(activeButter) && Math.Abs(activeButter.time - 4.0) <= 0.0001, "The production Butter hit event must add its authored four-second Butter buff.");
				if (!GodotObject.IsInstanceValid(activeButter))
				{
					goto end_IL_0100;
				}
				await ManualPhysicsStep(alien);
				await ManualPhysicsStep(alien);
				Check(IsStopped(alien.timeScale) && IsStopped(alien.sprite.timeScale), "An active Butter buff must freeze both the Alien and its authored animation.");
				double hitpoints = wallnut.instance.hitpoints;
				string clip = alien.sprite.clip;
				int frameIndex = alien.sprite.frameIndex;
				attack.timer = 0.0;
				attack.target = wallnut;
				alien.WalkProcessing(1.0 / 60.0);
				Check(Math.Abs(wallnut.instance.hitpoints - hitpoints) <= 0.0001, "The Alien must not fire its laser while Butter has stopped its gameplay clock.");
				Check(alien.sprite.clip == clip && alien.sprite.frameIndex == frameIndex && Math.Abs(attack.timer) <= 0.0001, "Butter must not let Alien reset into Shooting or consume its ready cooldown.");
				activeButter.currentTime = activeButter.time;
				await ManualPhysicsStep(alien);
				Check(alien.BuffGet("Butter") == null, "Butter must leave the Alien through the production Buff expiration path.");
				await ManualPhysicsStep(alien);
				attack.timer = 0.0;
				attack.target = wallnut;
				double hpBeforeRecoveryShot = wallnut.instance.hitpoints;
				await ManualPhysicsStep(alien);
				Check(alien.timeScale > 0.0 && alien.sprite.timeScale > 0.0, "Alien gameplay and animation clocks must recover after Butter expires.");
				Check(wallnut.instance.hitpoints < hpBeforeRecoveryShot, "The recovered Alien must execute a later real laser hit on Wall-nut.");
				Check(alien.sprite.clip == "Shooting" && attack.timer > 0.0, "The recovered laser must enter the real Shooting clip and refresh its cooldown.");
				float xBeforeAnimationRecovery = alien.GlobalPosition.X;
				double timerBeforeAnimationRecovery = attack.timer;
				alien.ProcessMode = ProcessModeEnum.Inherit;
				Check(await WaitUntil(() => GodotObject.IsInstanceValid(alien) && GodotObject.IsInstanceValid(alien.sprite) && alien.sprite.clip == "Walk", 120), "The post-Butter one-shot Shooting animation must complete and return to Walk.");
				await WaitFrames(8);
				Check(alien.GlobalPosition.X < xBeforeAnimationRecovery && attack.timer < timerBeforeAnimationRecovery, "After Butter, Alien movement and its production attack cooldown must both advance.");
				goto end_IL_00d5;
				end_IL_0100:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewAlienButterRecoveryRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00d5;
			}
			return;
			end_IL_00d5:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(alien) && !alien.IsQueuedForDeletion())
			{
				alien.QueueFree();
			}
			if (GodotObject.IsInstanceValid(cornpult) && !cornpult.IsQueuedForDeletion())
			{
				cornpult.QueueFree();
			}
			if (GodotObject.IsInstanceValid(wallnut) && !wallnut.IsQueuedForDeletion())
			{
				wallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(butterProjectileVisual) && !butterProjectileVisual.IsQueuedForDeletion())
			{
				butterProjectileVisual.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 17;
		GD.Print($"BUG_OVERVIEW_ALIEN_BUTTER_RECOVERY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool CornpultAuthorsButter(FireComponent fire)
	{
		List<TowerDefenseProjectileCreateData> list = new List<TowerDefenseProjectileCreateData>();
		HashSet<FireComponentProjectileResource> visited = new HashSet<FireComponentProjectileResource>();
		foreach (FireComponentCheckConfig fireCheck in fire.fireCheckList)
		{
			fireCheck?.projectile?.CollectProjectileData(list, visited);
		}
		foreach (TowerDefenseProjectileCreateData item in list)
		{
			if (GodotObject.IsInstanceValid(item) && item.projectileName.ToString() == "Butter")
			{
				return true;
			}
		}
		return false;
	}

	private static T LoadCharacter<T>(string path) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I gridPos, Vector2 position)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = gridPos;
		character.GlobalPosition = position;
	}

	private async Task ManualPhysicsStep(TowerDefenseCharacter character)
	{
		character.BatchUpdate(1.0 / 60.0);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private async Task<bool> WaitUntil(Func<bool> condition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (condition())
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		return condition();
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private static bool IsStopped(double value)
	{
		return Math.Abs(value) <= 0.0001;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewAlienButterRecoveryRuntimeTest] " + message);
		}
	}
}
