using System;
using System.Reflection;
using Godot;

public partial class LuckyBloverSurvivalRoundPacketRetentionRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		TowerDefenseInGameSeedBank towerDefenseInGameSeedBank = null;
		Node node = null;
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow2 = null;
		try
		{
			towerDefenseInGameSeedBank = new TowerDefenseInGameSeedBank();
			node = (towerDefenseInGameSeedBank.packetContainer = new Node());
			towerDefenseInGamePacketShow = CreateInteractivePacket("LuckyBloverPacket");
			towerDefenseInGamePacketShow2 = CreateInteractivePacket("RandomPacket");
			node.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
			node.AddChild(towerDefenseInGamePacketShow2, forceReadableName: false, InternalMode.Disabled);
			towerDefenseInGameSeedBank.packetList.Add(towerDefenseInGamePacketShow);
			towerDefenseInGameSeedBank.packetList.Add(towerDefenseInGamePacketShow2);
			towerDefenseInGamePacketShow.OnPressed += towerDefenseInGameSeedBank.DeletePacket;
			towerDefenseInGamePacketShow2.OnPressed += towerDefenseInGameSeedBank.DeletePacket;
			Check(HasPressedHandler(towerDefenseInGamePacketShow, towerDefenseInGameSeedBank, "DeletePacket") && HasPressedHandler(towerDefenseInGamePacketShow2, towerDefenseInGameSeedBank, "DeletePacket"), "The probe must begin with both round packets in the old removable state.");
			towerDefenseInGameSeedBank.ReadyPackets();
			Check(towerDefenseInGameSeedBank.packetList.Count == 2 && node.GetChildCount() == 2, "Freezing round entry must retain both the mandatory Lucky Blover and the random packet.");
			Check(IsFrozen(towerDefenseInGamePacketShow) && IsFrozen(towerDefenseInGamePacketShow2), "Every packet must become non-interactive immediately at round entry.");
			Check(!HasPressedHandler(towerDefenseInGamePacketShow, towerDefenseInGameSeedBank, "DeletePacket") && !HasPressedHandler(towerDefenseInGamePacketShow2, towerDefenseInGameSeedBank, "DeletePacket"), "Round-entry freezing must detach the deletion handler from every packet.");
			Check(!towerDefenseInGamePacketShow.IsQueuedForDeletion() && !towerDefenseInGamePacketShow2.IsQueuedForDeletion(), "Neither packet may be queued for deletion during the freeze.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[LuckyBloverSurvivalRoundPacketRetentionRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(node))
			{
				node.Free();
			}
			if (GodotObject.IsInstanceValid(towerDefenseInGameSeedBank))
			{
				towerDefenseInGameSeedBank.Free();
			}
		}
		bool flag = _failures == 0;
		GD.Print($"LUCKY_BLOVER_SURVIVAL_PACKET_RETENTION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseInGamePacketShow CreateInteractivePacket(string name)
	{
		return new TowerDefenseInGamePacketShow
		{
			Name = name,
			alive = true,
			@lock = false,
			onlyDraw = false,
			start = true
		};
	}

	private static bool IsFrozen(TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(packet) && !packet.alive && packet.@lock)
		{
			return packet.onlyDraw;
		}
		return false;
	}

	private static bool HasPressedHandler(TowerDefenseInGamePacketShow packet, object target, string methodName)
	{
		if (!(typeof(TowerDefenseInGamePacketShow).GetField("OnPressed", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(packet) is Delegate obj))
		{
			return false;
		}
		Delegate[] invocationList = obj.GetInvocationList();
		foreach (Delegate obj2 in invocationList)
		{
			if (obj2.Target == target && obj2.Method.Name == methodName)
			{
				return true;
			}
		}
		return false;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[LuckyBloverSurvivalRoundPacketRetentionRuntimeTest] " + message);
		}
	}
}
