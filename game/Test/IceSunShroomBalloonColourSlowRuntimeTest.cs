using System;
using Godot;
using Godot.Collections;

public partial class IceSunShroomBalloonColourSlowRuntimeTest : Node
{
	private const string BalloonColourConfigPath = "res://Asset/Anime/Character/Zombie/Chapter1/BalloonColour/Config/TowerDefenseZombieBalloonColour.tres";

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		TowerDefenseCharacter towerDefenseCharacter = null;
		TowerDefenseCharacterInstance towerDefenseCharacterInstance = null;
		try
		{
			int num = 2;
			int num2 = 2;
			int num3 = 4;
			int num4 = 1;
			string fileAsString = FileAccess.GetFileAsString("res://Asset/Anime/Character/Zombie/Chapter1/BalloonColour/Config/TowerDefenseZombieBalloonColour.tres");
			int num5 = num2 | num3;
			Check(fileAsString.Contains($"maskFlags = {num}") && fileAsString.Contains($"unUseBuffFlags = {num5}"), "ZombieBalloonColour config must retain its airborne mask and authored Buff immunities.");
			towerDefenseCharacter = new TowerDefenseCharacter();
			towerDefenseCharacterInstance = (towerDefenseCharacter.instance = new TowerDefenseCharacterInstance
			{
				maskFlags = num,
				unUseBuffFlags = num5
			});
			towerDefenseCharacter.buff = new BuffComponent
			{
				parent = towerDefenseCharacter
			};
			Check((towerDefenseCharacterInstance.maskFlags & num) != 0, "The fixture must still be an airborne ZombieBalloonColour.");
			Check((towerDefenseCharacterInstance.unUseBuffFlags & num2) != 0, "ZombieBalloonColour must retain its authored Frozen immunity.");
			Check((towerDefenseCharacterInstance.unUseBuffFlags & num4) == 0, "ZombieBalloonColour must remain eligible for IceSpeedDown.");
			Array<TowerDefenseCharacterEventBase> array = TowerDefenseCharacter.CreateSnowEventList(new Array<TowerDefenseCharacterEventBase>());
			TowerDefenseCharacterEventForzen towerDefenseCharacterEventForzen = null;
			foreach (TowerDefenseCharacterEventBase item in array)
			{
				if (item is TowerDefenseCharacterEventForzen towerDefenseCharacterEventForzen2)
				{
					towerDefenseCharacterEventForzen = towerDefenseCharacterEventForzen2;
					break;
				}
			}
			Check(towerDefenseCharacterEventForzen != null, "The Ice Sun-shroom cold pipeline must include its Frozen event.");
			if (towerDefenseCharacterEventForzen == null)
			{
				return;
			}
			towerDefenseCharacterEventForzen.Execute(towerDefenseCharacter.GetLogicalGlobalPosition(), towerDefenseCharacter);
			Check(!towerDefenseCharacter.buff.BuffHas("Frozen"), "Airborne ZombieBalloonColour must still reject Frozen.");
			Check(towerDefenseCharacter.buff.BuffHas("IceSpeedDown"), "Rejected Ice Sun-shroom Frozen must fall back to IceSpeedDown.");
			towerDefenseCharacter.timeScale = 1.0;
			towerDefenseCharacter.buff.BuffUpdate(0.25);
			Check(Math.Abs(towerDefenseCharacter.timeScale - 0.5) < 0.0001, $"IceSpeedDown must halve ZombieBalloonColour timeScale; got {towerDefenseCharacter.timeScale}.");
			towerDefenseCharacterEventForzen.Execute(towerDefenseCharacter.GetLogicalGlobalPosition(), towerDefenseCharacter);
			Check(!towerDefenseCharacter.buff.BuffHas("Frozen") && towerDefenseCharacter.buff.BuffHas("IceSpeedDown"), "Repeated Ice Sun-shroom effects must refresh only IceSpeedDown while airborne.");
			TowerDefenseCharacterBuffIceSpeedDown towerDefenseCharacterBuffIceSpeedDown = towerDefenseCharacter.buff.BuffGet("IceSpeedDown") as TowerDefenseCharacterBuffIceSpeedDown;
			Check(towerDefenseCharacterBuffIceSpeedDown != null && Math.Abs(towerDefenseCharacterBuffIceSpeedDown.currentTime) < 0.0001, "Repeated cold application must refresh the active slowdown timer.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[IceSunShroomBalloonColourSlowRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.buff?.BuffClear();
				towerDefenseCharacter.Free();
			}
			towerDefenseCharacterInstance?.Dispose();
		}
		bool flag = _failures == 0;
		GD.Print($"ICE_SUN_SHROOM_BALLOON_COLOUR_SLOW_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[IceSunShroomBalloonColourSlowRuntimeTest] " + message);
		}
	}
}
