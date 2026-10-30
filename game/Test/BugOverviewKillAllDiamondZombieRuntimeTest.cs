using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewKillAllDiamondZombieRuntimeTest : Node
{
	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string DiamondZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter7/Diamond/Scene/TowerDefenseZombieDiamond.tscn";

	private const string DiamondHelmetSlotPath = "res://Asset/Anime/Character/Zombie/Chapter7/Diamond/Armor/Config/ZombieDiamondArmorHelmetDiamond.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseZombie normal = null;
		TowerDefenseZombie diamond = null;
		try
		{
			normal = InstantiateZombie("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
			diamond = InstantiateZombie("res://Asset/Anime/Character/Zombie/Chapter7/Diamond/Scene/TowerDefenseZombieDiamond.tscn");
			Check(GodotObject.IsInstanceValid(normal) && GodotObject.IsInstanceValid(diamond), "The real normal and Diamond zombie scenes must instantiate.");
			if (!GodotObject.IsInstanceValid(normal) || !GodotObject.IsInstanceValid(diamond))
			{
				return;
			}
			diamond.currentArmor = new Array<string> { "HelmetDiamond" };
			normal.inGame = false;
			normal.editorPreviewMode = false;
			diamond.inGame = false;
			diamond.editorPreviewMode = false;
			AddChild(normal, forceReadableName: false, InternalMode.Disabled);
			AddChild(diamond, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(3);
			ArmorSlotConfig slotConfig = ResourceLoader.Load<ArmorSlotConfig>("res://Asset/Anime/Character/Zombie/Chapter7/Diamond/Armor/Config/ZombieDiamondArmorHelmetDiamond.tres", null, ResourceLoader.CacheMode.Ignore);
			TowerDefenseArmorInstance item = new TowerDefenseArmorInstance(diamond, slotConfig);
			diamond.instance.armorList.Add(item);
			diamond.instance.armorHelm.Add(item);
			diamond.instance.RefreshArmorRuntimeIndex();
			Check(normal.IsInGroup("Zombie") && diamond.IsInGroup("Zombie"), "Both real characters must be discoverable by the killall command.");
			Check(diamond.instance.armorHelm.Count == 1, $"Diamond zombie must start with its packet-authored helmet; count={diamond.instance.armorHelm.Count}.");
			TowerDefenseArmorInstance diamondHelmet = ((diamond.instance.armorHelm.Count > 0) ? diamond.instance.armorHelm[0] : null);
			Check(GodotObject.IsInstanceValid(diamondHelmet) && diamondHelmet.typeData.limitMaxHit == 20.0, "The real Diamond helmet must retain its 20-damage per-hit cap that reproduced the report.");
			Check(normal.instance.hitpoints > 0.0 && diamond.instance.hitpoints > 0.0, "Both fixtures must be alive before the console command runs.");
			CommandRegistry.Init();
			CommandConfig command = CommandRegistry.GetCommand("killall");
			Check(command != null, "The built-in killall command must be registered.");
			command?.Callback.Call();
			await WaitFrames(3);
			Check(normal.instance.die && normal.instance.hitpoints <= 0.0, $"killall must still kill a normal zombie; hp={normal.instance.hitpoints}, die={normal.instance.die}.");
			Check(diamond.instance.die && diamond.instance.hitpoints <= 0.0, $"killall must bypass Diamond helmet hit caps and kill the body; hp={diamond.instance.hitpoints}, die={diamond.instance.die}.");
			Check(Math.Abs(diamondHelmet.hitPoints) < 0.001, $"Direct body settlement must still run normal death armor cleanup; helmet hp={diamondHelmet.hitPoints}.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewKillAllDiamondZombieRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(normal))
			{
				normal.QueueFree();
			}
			if (GodotObject.IsInstanceValid(diamond))
			{
				diamond.QueueFree();
			}
		}
		bool flag = _failures == 0;
		GD.Print($"BUG_OVERVIEW_KILLALL_DIAMOND_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseZombie InstantiateZombie(string scenePath)
	{
		return ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
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
			GD.PushError("[BugOverviewKillAllDiamondZombieRuntimeTest] " + message);
		}
	}
}
