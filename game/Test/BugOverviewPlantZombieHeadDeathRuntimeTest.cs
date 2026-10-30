using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPlantZombieHeadDeathRuntimeTest : Node
{
	private const string PlantZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		PlantZombieHeadDeathRuntimeControlStub control = null;
		TowerDefenseZombieNormalPeaShooterSingle plantZombie = null;
		AdobeAnimateSpriteBase plantHead = null;
		try
		{
			_ = 10;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ObjectManager.Instance), "ObjectManager autoload must be available for the production battle fixture.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
				{
					goto end_IL_0134;
				}
				control = new PlantZombieHeadDeathRuntimeControlStub
				{
					Name = "PlantZombieHeadDeathRuntimeControl",
					isGameRunning = true,
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
				plantZombie = Instantiate<TowerDefenseZombieNormalPeaShooterSingle>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn");
				Check(GodotObject.IsInstanceValid(plantZombie) && plantZombie.config?.name == "ZombieNormalPeaShooterSingle", "The reported real Peashooter plant-zombie scene must instantiate.");
				if (!GodotObject.IsInstanceValid(plantZombie))
				{
					goto end_IL_0134;
				}
				plantZombie.editorPreviewMode = false;
				plantZombie.inGame = true;
				plantZombie.gridPos = new Vector2I(5, 3);
				plantZombie.GlobalPosition = new Vector2(500f, 228f);
				control.characterNode.AddChild(plantZombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(6);
				plantZombie.Walk();
				await WaitFrames(5);
				ZombieNormalPeaShooterSingleSprite body = plantZombie.sprite as ZombieNormalPeaShooterSingleSprite;
				plantHead = body?.head;
				Check(GodotObject.IsInstanceValid(body) && GodotObject.IsInstanceValid(plantHead) && plantHead.GetParent() == body && plantHead.Visible, "The live plant-zombie must begin with its authored visible plant Head attached to the body.");
				BugOverviewPlantZombieHeadDeathRuntimeTest bugOverviewPlantZombieHeadDeathRuntimeTest = this;
				ZombieDeathComponent zombieDeathComponent = plantZombie.zombieDeathComponent;
				int condition;
				if (zombieDeathComponent != null && !zombieDeathComponent.IsReleased)
				{
					GroundMoveComponent groundMoveComponent = plantZombie.groundMoveComponent;
					condition = ((groundMoveComponent != null && !groundMoveComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewPlantZombieHeadDeathRuntimeTest.Check((byte)condition != 0, "The real fixture must bind its shared death and authored ground-movement runtimes.");
				Check(CountOwnedDrawItems(body, plantHead) > 0, "The attached plant Head must contribute visible draw items before near-death.");
				if (!GodotObject.IsInstanceValid(body) || !GodotObject.IsInstanceValid(plantHead))
				{
					goto end_IL_0134;
				}
				plantZombie.zombieDeathComponent.dropFeatureName = "";
				DamagePartBatcher damagePartBatcher = DamagePartBatcher.GetOrCreate();
				if (!GodotObject.IsInstanceValid(damagePartBatcher))
				{
					throw new InvalidOperationException("The shared DamagePart RID batch owner must be available.");
				}
				int activeDamagePartsBeforeArm = damagePartBatcher.ActiveCount;
				int treeNodesBeforeArm = GetTree().GetNodeCount();
				double num = plantZombie.instance.hitpoints - (plantZombie.config.hitpointsNearDeath + 1.0);
				plantZombie.instance.SkipInvincibleDealHurt(num, playSplatAudio: false);
				await WaitFrames(3);
				Check(damagePartBatcher.ActiveCount == activeDamagePartsBeforeArm + 1 && GetTree().GetNodeCount() <= treeNodesBeforeArm && CountLegacyDamagePartDrops(control.characterNode) == 0, "The ordinary Slot-authored Arm fragment must enter the RID batch with zero per-fragment Nodes.");
				int activeDamagePartsBefore = damagePartBatcher.ActiveCount;
				int treeNodesBeforeDamagePart = GetTree().GetNodeCount();
				double num2 = plantZombie.instance.hitpoints - plantZombie.config.hitpointsNearDeath;
				plantZombie.instance.SkipInvincibleDealHurt(num2, playSplatAudio: false);
				await WaitFrames(3);
				Check(plantZombie.nearDie && !plantZombie.die && plantZombie.instance.hitpoints > 0.0 && plantZombie.instance.hitpoints <= plantZombie.config.hitpointsNearDeath, "Reaching the Head damage point must enter the normal near-death phase without skipping directly to death.");
				Check(damagePartBatcher.ActiveCount == activeDamagePartsBefore + 1 && damagePartBatcher.VisualRidCountForTest > 0, $"The authored plant Head must be captured into one live retained-RID damage part; before={activeDamagePartsBefore}, after={damagePartBatcher.ActiveCount}, visualRids={damagePartBatcher.VisualRidCountForTest}.");
				Check(!GodotObject.IsInstanceValid(plantHead) || (plantHead.GetParent() != body && !body.IsAncestorOf(plantHead)), "The captured plant Head source must leave the living body immediately.");
				Check(CountOwnedDrawItems(body, plantHead) == 0, "The near-death body render snapshot must not retain a stale copy of the detached Head.");
				Check(GetTree().GetNodeCount() <= treeNodesBeforeDamagePart && CountLegacyDamagePartDrops(control.characterNode) == 0, "Capturing the falling Head must create zero per-fragment Nodes.");
				Check(damagePartBatcher.ActiveLandingOffsetsWithinForTest(2000f), "The retained falling Head must keep a finite battlefield landing plane.");
				double nearDeathHitpoints = plantZombie.instance.hitpoints;
				await WaitSeconds(1.0);
				await WaitFrames(2);
				Check(GodotObject.IsInstanceValid(plantZombie) && plantZombie.instance.hitpoints < nearDeathHitpoints && !plantZombie.die, "The shared three-second near-death countdown must progress while the plant-zombie is still alive.");
				Check(plantZombie.CurrentStateHandle?.StableId == "zombie.walk", $"The plant-zombie must remain in its authored walk state during near-death; state={plantZombie.CurrentStateHandle?.StableId}, clip={plantZombie.sprite?.clip}.");
				Check(CountOwnedDrawItems(body, plantHead) == 0, "The walking near-death body must remain headless after the falling Head has separated.");
				Check(await WaitUntil(() => GodotObject.IsInstanceValid(plantZombie) && plantZombie.die, 240) && plantZombie.CurrentStateHandle?.StableId == "zombie.die", "The near-death countdown must enter the shared zombie.die state; state=" + plantZombie.CurrentStateHandle?.StableId + ".");
				Check(plantZombie.zombieDeathComponent.IsDeathAnimationClip(plantZombie.sprite.clip), "The real plant-zombie must play an authored death clip; clip=" + plantZombie.sprite?.clip + ".");
				await WaitFrames(3);
				float deathX = plantZombie.GlobalPosition.X;
				await WaitSeconds(0.4);
				Check(GodotObject.IsInstanceValid(plantZombie) && Mathf.Abs(plantZombie.GlobalPosition.X - deathX) <= 0.05f && !plantZombie.groundMoveComponent.Alive, "Entering death must stop ground movement instead of advancing the corpse.");
				Check(await WaitUntil(() => !GodotObject.IsInstanceValid(plantHead), 300), "The detached plant Head node must be freed when its damage-part drop is recycled.");
				Check(await WaitUntil(() => !GodotObject.IsInstanceValid(plantZombie), 420), "The plant-zombie corpse must be removed after its authored death clip and shared fade.");
				Check(CountLiveDamagePartVisuals(control.characterNode, damagePartBatcher) == 0, "No visible plant Head or empty damage-part visual may remain after both lifecycles complete.");
				goto end_IL_00fc;
				end_IL_0134:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPlantZombieHeadDeathRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00fc;
			}
			return;
			end_IL_00fc:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(plantZombie))
			{
				plantZombie.QueueFree();
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
			await WaitFrames(3);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(3);
			plantZombie = null;
			plantHead = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"PLANT_ZOMBIE_HEAD_DEATH_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T Instantiate<T>(string scenePath) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static int CountOwnedDrawItems(AdobeAnimateSprite root, AdobeAnimateSprite owner)
	{
		if (!GodotObject.IsInstanceValid(root) || owner == null || !root.TryBuildRenderSnapshot(out var snapshot, allowUnchanged: false))
		{
			return -1;
		}
		List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
		AdobeAnimateDrawItemBuilder.Build(snapshot, list, snapshot.Definition?.GpuPoseTextureArray);
		int num = 0;
		foreach (AdobeAnimateDrawItem item in list)
		{
			if (item.Owner == owner)
			{
				num++;
			}
		}
		return num;
	}

	private static int CountLegacyDamagePartDrops(Node parent)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return 0;
		}
		int num = 0;
		foreach (Node child in parent.GetChildren())
		{
			if (child is DamagePartDrop)
			{
				num++;
			}
		}
		return num;
	}

	private static int CountLiveDamagePartVisuals(Node parent, DamagePartBatcher batcher)
	{
		return CountLegacyDamagePartDrops(parent) + (GodotObject.IsInstanceValid(batcher) ? batcher.ActiveCount : 0);
	}

	private async Task<bool> WaitUntil(Func<bool> condition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (condition())
			{
				return true;
			}
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
			GD.PushError("[BugOverviewPlantZombieHeadDeathRuntimeTest] " + message);
		}
	}
}
