using System;
using Godot;
using Godot.Collections;

public partial class FireProjectileHitHotPathRuntimeTest : Node
{
	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		TowerDefenseControlNew control = null;
		TowerDefenseCharacter zombie = null;
		try
		{
			Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(manager))
			{
				return;
			}
			control = (manager.currentControl = new TowerDefenseControlNew
			{
				isGameRunning = true
			});
			zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(zombie), "Normal-zombie scene must instantiate.");
			if (!GodotObject.IsInstanceValid(zombie))
			{
				return;
			}
			zombie.inGame = true;
			AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			zombie.buff.AddBuff(new TowerDefenseCharacterBuffFrozen
			{
				time = 8.0,
				iceSpeedDownTime = 15.0
			});
			zombie.buff.AddBuff(new TowerDefenseCharacterBuffIceSpeedDown
			{
				time = 15.0
			});
			zombie.buff.ApplyFireHit();
			TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig = zombie.buff.BuffGet("FireHit");
			Check(towerDefenseCharacterBuffConfig is TowerDefenseCharacterBuffFireHit, "First built-in FireHit application must create one runtime state.");
			Check(!zombie.buff.BuffHas("Frozen") && !zombie.buff.BuffHas("IceSpeedDown"), "FireHit must still clear Frozen and IceSpeedDown immediately.");
			zombie.buff.ApplyFireHit();
			Check(towerDefenseCharacterBuffConfig == zombie.buff.BuffGet("FireHit"), "Repeated built-in FireHit in one frame must refresh the existing runtime.");
			zombie.buff.BuffUpdate(0.01);
			Check(!zombie.buff.BuffHas("FireHit"), "Built-in FireHit must retain its one-update lifetime.");
			FireHitModRuntimeProbe.RuntimeInstanceCreates = 0;
			FireHitModRuntimeProbe fireHitModRuntimeProbe = new FireHitModRuntimeProbe();
			fireHitModRuntimeProbe._Init();
			zombie.buff.ApplyFireHit(fireHitModRuntimeProbe);
			FireHitModRuntimeProbe fireHitModRuntimeProbe2 = zombie.buff.BuffGet("FireHit") as FireHitModRuntimeProbe;
			Check(fireHitModRuntimeProbe2 != null, "Mod-derived FireHit must use its explicit runtime factory.");
			Check(FireHitModRuntimeProbe.RuntimeInstanceCreates == 1, "First Mod-derived FireHit must create exactly one runtime instance.");
			zombie.buff.ApplyFireHit(fireHitModRuntimeProbe);
			Check(fireHitModRuntimeProbe2 == zombie.buff.BuffGet("FireHit"), "Repeated Mod-derived FireHit must refresh the existing runtime.");
			Check(FireHitModRuntimeProbe.RuntimeInstanceCreates == 1, "Refreshing a Mod-derived FireHit must not create another runtime Resource.");
			Check(fireHitModRuntimeProbe2.RefreshCount == 1, "Mod-derived FireHit refresh override must still execute.");
			Array<TowerDefenseCharacterBuffConfig> buffList = new Array<TowerDefenseCharacterBuffConfig> { fireHitModRuntimeProbe };
			TowerDefenseCharacterEventAddBuff.Run(zombie, buffList);
			Check(fireHitModRuntimeProbe2 == zombie.buff.BuffGet("FireHit"), "AddBuff events must share the Mod-derived FireHit runtime.");
			Check(FireHitModRuntimeProbe.RuntimeInstanceCreates == 1, "AddBuff events must not allocate another Mod-derived FireHit runtime.");
			Check(fireHitModRuntimeProbe2.RefreshCount == 2, "AddBuff events must preserve Mod-derived FireHit refresh behavior.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[FireProjectileHitHotPathRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			control?.Free();
		}
		bool flag = _failures == 0 && _checks == 14;
		GD.Print($"FIRE_PROJECTILE_HIT_HOT_PATH_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[FireProjectileHitHotPathRuntimeTest] " + message);
		}
	}
}
