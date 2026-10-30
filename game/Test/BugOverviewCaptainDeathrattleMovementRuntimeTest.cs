using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewCaptainDeathrattleMovementRuntimeTest : Node
{
	private const string CaptainScenePath = "res://Asset/Anime/Character/Zombie/Chapter7/Captain/Scene/TowerDefenseZombieCaptain.tscn";

	private const string CaptainPacketPath = "res://Asset/Anime/Character/Zombie/Chapter7/Captain/Packet/ZombieCaptain.tres";

	private const string CrewScenePath = "res://Asset/Anime/Character/Zombie/Chapter7/Crew/Scene/TowerDefenseZombieCrew.tscn";

	private const string CrewPacketPath = "res://Asset/Anime/Character/Zombie/Chapter7/Crew/Packet/ZombieCrew.tres";

	private int _checks;

	private int _failures;

	private Node2D _characterNode;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew control = null;
		TowerDefenseInGameLevelControl levelGuard = null;
		TowerDefenseInGameLevelControl previousLevelControl = TowerDefenseInGameLevelControl.instance;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available for the real packet path.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0076;
				}
				TowerDefenseProjectileRegistry.Init();
				RegisterRealPacketFixtures();
				control = new TowerDefenseControlNew
				{
					isGameRunning = true
				};
				_characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				AddChild(_characterNode, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = _characterNode;
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				levelGuard = (TowerDefenseInGameLevelControl.instance = new TowerDefenseInGameLevelControl
				{
					awardCreate = false
				});
				TowerDefenseZombieCaptain sourceCaptain = Instantiate<TowerDefenseZombieCaptain>("res://Asset/Anime/Character/Zombie/Chapter7/Captain/Scene/TowerDefenseZombieCaptain.tscn");
				TowerDefenseZombieCrew crew = Instantiate<TowerDefenseZombieCrew>("res://Asset/Anime/Character/Zombie/Chapter7/Crew/Scene/TowerDefenseZombieCrew.tscn");
				Check(GodotObject.IsInstanceValid(sourceCaptain), "The real Captain scene must instantiate as the deathrattle source.");
				Check(GodotObject.IsInstanceValid(crew), "The real Crew scene must instantiate as the replacement target.");
				if (!GodotObject.IsInstanceValid(sourceCaptain) || !GodotObject.IsInstanceValid(crew))
				{
					goto end_IL_0076;
				}
				sourceCaptain.gridPos = new Vector2I(5, 3);
				sourceCaptain.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(sourceCaptain.gridPos);
				crew.gridPos = new Vector2I(4, 3);
				crew.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(crew.gridPos);
				_characterNode.AddChild(sourceCaptain, forceReadableName: false, InternalMode.Disabled);
				_characterNode.AddChild(crew, forceReadableName: false, InternalMode.Disabled);
				await WaitPhysicsFrames(4);
				Check(GodotObject.IsInstanceValid(sourceCaptain.instance) && GodotObject.IsInstanceValid(crew.instance), "The real Captain and Crew scenes must finish runtime initialization.");
				Check(crew.IsInGroup("ZombieCrew"), "The real Crew scene must register in the ZombieCrew group used by Captain deathrattle.");
				if (!GodotObject.IsInstanceValid(sourceCaptain.instance) || !GodotObject.IsInstanceValid(crew.instance))
				{
					goto end_IL_0076;
				}
				sourceCaptain.Hypnoses();
				crew.Hypnoses();
				await WaitPhysicsFrames(2);
				Check(sourceCaptain.instance.hypnoses && sourceCaptain.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && crew.instance.hypnoses && crew.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, "The source Captain and promoted Crew must both be hypnotized before the deathrattle.");
				sourceCaptain.instance.hitpoints = 0.0;
				sourceCaptain.DestroySet();
				TowerDefenseZombieCaptain replacement = await WaitForReplacementCaptain(sourceCaptain, 30);
				Check(GodotObject.IsInstanceValid(replacement), "Captain deathrattle must replace the nearby Crew with one real Captain scene.");
				if (!GodotObject.IsInstanceValid(replacement))
				{
					goto end_IL_0076;
				}
				int num = 0;
				foreach (Node child in _characterNode.GetChildren())
				{
					if (child is TowerDefenseZombieCaptain towerDefenseZombieCaptain && towerDefenseZombieCaptain != sourceCaptain)
					{
						num++;
					}
				}
				Check(num == 1, $"Captain deathrattle must create exactly one replacement Captain; got {num}.");
				Check(!GodotObject.IsInstanceValid(crew) || crew.isDestroy, "The Crew selected by the deathrattle must be consumed by the replacement.");
				await WaitPhysicsFrames(2);
				Check((replacement.instance?.hypnoses ?? false) && replacement.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, "The replacement Captain must inherit hypnosis from the dead hypnotized Captain and promoted Crew.");
				for (int frame = 0; frame < 60; frame++)
				{
					if (!(replacement.CurrentStateHandle?.StableId != "zombie.walk"))
					{
						GroundMoveComponent groundMoveComponent = replacement.groundMoveComponent;
						if (groundMoveComponent != null && groundMoveComponent.Alive)
						{
							break;
						}
					}
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				}
				Check(replacement.spawnTimer < 2.0 && replacement.CurrentStateHandle?.StableId != "zombie.captain.spawn", $"The replacement movement check must occur before its first crew summon; timer={replacement.spawnTimer:F3}, state={replacement.CurrentStateHandle?.StableId ?? "<null>"}.");
				Check(replacement.CurrentStateHandle?.StableId == "zombie.walk", "The replacement Captain must enter zombie.walk immediately; got " + (replacement.CurrentStateHandle?.StableId ?? "<null>") + ".");
				BugOverviewCaptainDeathrattleMovementRuntimeTest bugOverviewCaptainDeathrattleMovementRuntimeTest = this;
				GroundMoveComponent groundMoveComponent2 = replacement.groundMoveComponent;
				bugOverviewCaptainDeathrattleMovementRuntimeTest.Check(groundMoveComponent2 != null && !groundMoveComponent2.IsReleased && groundMoveComponent2.Alive && replacement.groundMoveComponent.HasMovementSource, "The replacement Captain must activate its ground movement runtime before summoning crew.");
				Vector2 beforeMove = replacement.GlobalPosition;
				await WaitPhysicsFrames(30);
				float num2 = Mathf.Abs(replacement.GlobalPosition.X - beforeMove.X);
				Check(num2 > 0.01f, $"The replacement Captain must physically move before its first crew summon; deltaX={num2:F4}.");
				goto end_IL_0053;
				end_IL_0076:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewCaptainDeathrattleMovementRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0053;
			}
			return;
			end_IL_0053:;
		}
		finally
		{
			TowerDefenseInGameLevelControl.instance = previousLevelControl;
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = null;
			}
			if (GodotObject.IsInstanceValid(_characterNode))
			{
				_characterNode.Free();
			}
			_characterNode = null;
			UnregisterRealPacketFixtures();
			if (GodotObject.IsInstanceValid(levelGuard))
			{
				levelGuard.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.Free();
			}
			for (int frame = 0; frame < 4; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		bool flag = _failures == 0;
		GD.Print($"CAPTAIN_DEATHRATTLE_MOVEMENT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task<TowerDefenseZombieCaptain> WaitForReplacementCaptain(TowerDefenseZombieCaptain sourceCaptain, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			foreach (Node child in _characterNode.GetChildren())
			{
				if (child is TowerDefenseZombieCaptain towerDefenseZombieCaptain && towerDefenseZombieCaptain != sourceCaptain)
				{
					return towerDefenseZombieCaptain;
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		return null;
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

	private static void RegisterRealPacketFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.TOWERDEFENSE_PACKETS["ZombieCrew"] = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter7/Crew/Packet/ZombieCrew.tres", null, ResourceLoader.CacheMode.Ignore);
			instance.TOWERDEFENSE_PACKETS["ZombieCaptain"] = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter7/Captain/Packet/ZombieCaptain.tres", null, ResourceLoader.CacheMode.Ignore);
			instance.TOWERDEFENSE_CHARCATERS["ZombieCrew"] = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter7/Crew/Scene/TowerDefenseZombieCrew.tscn", null, ResourceLoader.CacheMode.Ignore);
			instance.TOWERDEFENSE_CHARCATERS["ZombieCaptain"] = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter7/Captain/Scene/TowerDefenseZombieCaptain.tscn", null, ResourceLoader.CacheMode.Ignore);
		}
	}

	private static void UnregisterRealPacketFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.TOWERDEFENSE_PACKETS.Remove("ZombieCrew");
			instance.TOWERDEFENSE_PACKETS.Remove("ZombieCaptain");
			instance.TOWERDEFENSE_CHARCATERS.Remove("ZombieCrew");
			instance.TOWERDEFENSE_CHARCATERS.Remove("ZombieCaptain");
		}
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewCaptainDeathrattleMovementRuntimeTest] " + message);
		}
	}
}
