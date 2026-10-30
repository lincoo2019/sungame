using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewDiggerPickMagnetEmergenceRuntimeTest : Node
{
	private const string DiggerScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/Base/TowerDefenseZombieDigger.tscn";

	private const string MagnetShroomScenePath = "res://Asset/Anime/Character/Plant/Chapter0/MagnetsShroom/Scene/TowerDefensePlantMagnetsShroom.tscn";

	private static readonly Vector2I EncounterGrid = new Vector2I(4, 2);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		DiggerPickMagnetRuntimeControlStub control = null;
		TowerDefenseZombieDigger digger = null;
		TowerDefensePlantMagnetsShroom magnetShroom = null;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("TowerDefenseManager autoload is unavailable.");
				}
				control = new DiggerPickMagnetRuntimeControlStub
				{
					Name = "DiggerPickMagnetRuntimeControl",
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
				digger = InstantiateCharacter<TowerDefenseZombieDigger>("res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/Base/TowerDefenseZombieDigger.tscn", control.characterNode, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, new Vector2(400f, 200f));
				magnetShroom = InstantiateCharacter<TowerDefensePlantMagnetsShroom>("res://Asset/Anime/Character/Plant/Chapter0/MagnetsShroom/Scene/TowerDefensePlantMagnetsShroom.tscn", control.characterNode, TowerDefenseEnum.CHARACTER_CAMP.PLANT, new Vector2(300f, 200f));
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(digger) && digger.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/Base/TowerDefenseZombieDigger.tscn" && digger.config?.name == "ZombieDigger", "The regression must instantiate the real base Digger Zombie scene.");
				Check(GodotObject.IsInstanceValid(magnetShroom) && magnetShroom.SceneFilePath == "res://Asset/Anime/Character/Plant/Chapter0/MagnetsShroom/Scene/TowerDefensePlantMagnetsShroom.tscn" && magnetShroom.config?.name == "PlantMagnetsShroom", "The regression must instantiate the real Magnet-shroom scene.");
				if (!GodotObject.IsInstanceValid(digger) || !GodotObject.IsInstanceValid(magnetShroom))
				{
					throw new InvalidOperationException("The real Digger/Magnet-shroom actors could not be instantiated.");
				}
				manager.CharacterRegister(digger);
				manager.CharacterRegister(magnetShroom);
				digger.SendStateEvent("ToDig");
				await WaitFrames(2);
				TowerDefenseArmorInstance pick = digger.GetArmorFromName("Pick");
				MagnetComponent magnet = magnetShroom.componentManager?.GetRuntime<MagnetComponent>();
				Check(GodotObject.IsInstanceValid(pick) && pick.slotConfig?.armorName == "Pick" && pick.IsMetallic() && !pick.isRemove, "The real Digger must begin with its authored metallic Pick armor.");
				Check(magnet != null && !magnet.IsReleased && magnet.Alive && magnet.parent == magnetShroom, "The real Magnet-shroom MagnetComponent must be alive and bound to its owner.");
				Check(digger.CurrentStateHandle?.StableId == "zombie.digger.dig" && !digger.digOver && (digger.instance.collisionFlags & 0x10) != 0, "The Digger must start in the real underground Dig state.");
				if (!GodotObject.IsInstanceValid(pick) || magnet == null || magnet.IsReleased)
				{
					throw new InvalidOperationException("The real pick/magnet runtime is unavailable.");
				}
				int armorHurtSignalCount = 0;
				int pickArmorEmptySignalCount = 0;
				digger.OnArmorHurt += delegate
				{
					armorHurtSignalCount++;
				};
				digger.instance.armorHitpointsEmpty += delegate(string armorName)
				{
					if (armorName == "Pick")
					{
						pickArmorEmptySignalCount++;
					}
				};
				Check(await magnet.CanArmorDraw() && magnet.drawArmorCharacter == digger && magnet.drawArmor == pick, "Magnet-shroom must select the underground Digger's real Pick armor.");
				Node2D characterNode = control.characterNode;
				Node2D pickSpriteBeforeFailure = pick.sprite;
				double orphanCountBeforeFailure = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);
				control.characterNode = null;
				TowerDefenseMagnet failedMagnet = digger.ArmorDraw(pick);
				control.characterNode = characterNode;
				await WaitFrames(1);
				double monitor = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);
				Check(!GodotObject.IsInstanceValid(failedMagnet), "ArmorDraw must report failure when the production character container is unavailable.");
				Check(digger.instance.armorList.Contains(pick) && !pick.isRemove && pick.sprite == pickSpriteBeforeFailure, "A failed MagnetCreate must preserve the Pick list entry, removal flag, and authored sprite state.");
				Check(armorHurtSignalCount == 0 && pickArmorEmptySignalCount == 0 && !digger.digOver && digger.CurrentStateHandle?.StableId == "zombie.digger.dig", "A failed draw must emit no armor signals and leave the Digger in its real Dig state.");
				Check(monitor <= orphanCountBeforeFailure, "A failed MagnetCreate must immediately free its unattached magnet node.");
				TowerDefenseArmorInstance firstDrawnArmor = magnet.ArmorDraw();
				await WaitFrames(2);
				Check(firstDrawnArmor == pick && magnet.breakDownArmor == pick && GodotObject.IsInstanceValid(magnet.magnet), "The first draw must use the production armor-to-magnet visual chain.");
				Check(pick.isRemove && !digger.instance.armorList.Contains(pick), "The extracted Pick must be marked removed and deleted from the live armor list.");
				Check(armorHurtSignalCount == 1 && pickArmorEmptySignalCount == 1, "A successful production draw must emit exactly one ArmorHurt and one Pick-empty signal.");
				Check(digger.digOver && digger.CurrentStateHandle?.StableId == "zombie.digger.drill", "Losing the Pick underground must immediately route the Digger into Drill emergence.");
				Check((digger.instance.collisionFlags & 0x10) != 0 && (digger.instance.collisionFlags & 1) == 0, "The Digger must remain underground only for the authored Drill emergence animation.");
				Check(!(await magnet.CanArmorDraw()) && magnet.drawArmorCharacter == null && magnet.drawArmor == null, "A removed Pick must never be selected for a second draw.");
				Check(magnet.ArmorDraw() == null, "Calling ArmorDraw without a valid selection must not recreate the removed Pick.");
				digger.AnimeCompleted("Drill");
				await WaitFrames(1);
				Check(digger.CurrentStateHandle?.StableId == "zombie.digger.land" && (digger.instance.collisionFlags & 1) != 0 && (digger.instance.maskFlags & 8) != 0, "Completing Drill must make the Digger ground-targetable in the real Land state.");
				digger.AnimeCompleted("Dizzy");
				await WaitFrames(1);
				Check(digger.CurrentStateHandle?.StableId == "zombie.walk", "Completing Dizzy must resume the Digger's production walk state instead of leaving it stuck underground.");
				Check(!digger.instance.armorList.Contains(pick) && pick.isRemove, "The completed production path must leave no removed Pick entry that can be selected again.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewDiggerPickMagnetEmergenceRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				if (GodotObject.IsInstanceValid(digger))
				{
					manager.CharacterUnregister(digger);
				}
				if (GodotObject.IsInstanceValid(magnetShroom))
				{
					manager.CharacterUnregister(magnetShroom);
				}
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(digger))
			{
				digger.QueueFree();
			}
			if (GodotObject.IsInstanceValid(magnetShroom))
			{
				magnetShroom.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 21;
		GD.Print($"DIGGER_PICK_MAGNET_EMERGENCE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T InstantiateCharacter<T>(string path, Node parent, TowerDefenseEnum.CHARACTER_CAMP camp, Vector2 position) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		T val = ((packedScene != null) ? packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled) : null);
		if (!GodotObject.IsInstanceValid(val))
		{
			return null;
		}
		val.inGame = false;
		val.editorPreviewMode = true;
		val.camp = camp;
		val.gridPos = EncounterGrid;
		val.GlobalPosition = position;
		parent.AddChild(val, forceReadableName: false, InternalMode.Disabled);
		return val;
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
			GD.PushError("[BugOverviewDiggerPickMagnetEmergenceRuntimeTest] " + message);
		}
	}
}
