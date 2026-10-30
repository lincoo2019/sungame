using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewRainModePacketRestoreRuntimeTest : Node
{
	private const string PacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		RainModePacketRestoreRuntimeControlStub control = null;
		TowerDefenseBattleFeatureRainMode feature = null;
		TowerDefenseBattleFeatureRainMode restoredFeature = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "The production managers must be available.");
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("BugOverviewRainModePacketRestoreRuntimeTest");
				control = new RainModePacketRestoreRuntimeControlStub
				{
					Name = "RainModePacketRestoreControl",
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
				TowerDefensePacketConfig packetConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
				feature = CreateFeature(control);
				TowerDefenseInGamePacketShow packet = feature.SpawnPacket(packetConfig);
				await WaitFrames(5);
				Vector2 savedPosition = new Vector2(420f, 180f);
				packet.ProcessMode = ProcessModeEnum.Disabled;
				packet.GlobalPosition = savedPosition;
				packet.aliveTime = 30.0;
				packet.aliveTimer = 3.0;
				Check(GodotObject.IsInstanceValid(packet) && packet.config?.saveKey == "PlantPeaShooterSingle" && packet.GetParent() == control.characterNode, "The scenario must create a real RainMode packet in the production character container.");
				Dictionary state = feature.SaveFeature();
				Godot.Collections.Array array = state.GetValueOrDefault("packets", new Godot.Collections.Array()).AsGodotArray();
				Check(array.Count == 1, $"RainMode must save its one active dropped card; saved={array.Count}.");
				packet.QueueFree();
				await WaitFrames(3);
				Check(!GodotObject.IsInstanceValid(packet), "The original dropped card must be gone before restoring the feature state.");
				restoredFeature = CreateFeature(control);
				restoredFeature.LoadFeature(state, null);
				await WaitFrames(5);
				TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = FindPacket(control.characterNode);
				Check(GodotObject.IsInstanceValid(towerDefenseInGamePacketShow) && towerDefenseInGamePacketShow.config?.saveKey == "PlantPeaShooterSingle", "Reloading RainMode must recreate the saved dropped card.");
				Check(GodotObject.IsInstanceValid(towerDefenseInGamePacketShow) && towerDefenseInGamePacketShow.GlobalPosition.IsEqualApprox(savedPosition) && towerDefenseInGamePacketShow.aliveTimer >= 3.0 && towerDefenseInGamePacketShow.aliveTimer < 3.5, $"The restored card must retain position and continue lifetime progress after restoration; position={towerDefenseInGamePacketShow?.GlobalPosition}, timer={towerDefenseInGamePacketShow?.aliveTimer}.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewRainModePacketRestoreRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			restoredFeature?.Destroy();
			feature?.Destroy();
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
		}
		bool flag = _failures == 0 && _checks == 6;
		GD.Print($"RAIN_MODE_PACKET_RESTORE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureRainMode CreateFeature(TowerDefenseControlNew control)
	{
		return new TowerDefenseBattleFeatureRainMode
		{
			control = control,
			config = new TowerDefenseRainModeConfig
			{
				type = "Plant",
				aliveTime = 30.0
			}
		};
	}

	private static TowerDefenseInGamePacketShow FindPacket(Node characterNode)
	{
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow) && !towerDefenseInGamePacketShow.IsQueuedForDeletion())
			{
				return towerDefenseInGamePacketShow;
			}
		}
		return null;
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
			GD.PushError("[BugOverviewRainModePacketRestoreRuntimeTest] " + message);
		}
	}
}
