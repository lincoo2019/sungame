using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSkeletuarReviveRuntimeTest : Node
{
	private const string SkeletuarScenePath = "res://Asset/Anime/Character/Zombie/Challenge/Skeletuar/Scene/TowerDefenseZombieSkeletuar.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewSkeletuarReviveControlStub control = null;
		TowerDefenseZombieSkeletuar original = null;
		TowerDefenseZombieSkeletuar revived = null;
		TowerDefenseZombieSkeletuar restored = null;
		TowerDefenseZombieSkeletuar remoteSpawn = null;
		TowerDefenseZombieSkeletuar lateJoin = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00ef;
				}
				control = new BugOverviewSkeletuarReviveControlStub
				{
					Name = "SkeletuarReviveControl",
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
				original = LoadSkeletuar();
				PrepareCharacter(original, new Vector2I(6, 2), new Vector2(600f, 200f));
				control.characterNode.AddChild(original, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				original.ProcessMode = ProcessModeEnum.Disabled;
				Check(original.config?.name == "ZombieSkeletuar" && original.impName == "ZombieImpSkeleton", "The fixture must use the real Skeleton Gargantuar scene and Imp payload.");
				Check(!original.over && original.impThrowDamagePointName == "ThrowImp", "A first-life Skeleton Gargantuar must retain its authored one-time Imp throw.");
				revived = LoadSkeletuar();
				PrepareCharacter(revived, new Vector2I(5, 2), new Vector2(500f, 200f));
				revived.over = true;
				revived.impThrowFlag = true;
				control.characterNode.AddChild(revived, forceReadableName: false, InternalMode.Disabled);
				Check(revived.over && revived.impThrowDamagePointName == "" && !revived.impThrowFlag, "A revived Skeleton Gargantuar must disable the Imp throw synchronously in Ready, without a one-frame race.");
				await WaitFrames(2);
				revived.ProcessMode = ProcessModeEnum.Disabled;
				Check(revived.sprite.clip == "Relife", "A revived Skeleton Gargantuar must enter its real revive clip; clip=" + revived.sprite?.clip + ".");
				int skeletonsBeforeSecondDeath = CountCharactersNamed(control.characterNode, "Skeleton");
				revived.AnimeCompleted(FirstDeathClip(revived));
				await WaitFrames(2);
				Check(CountCharactersNamed(control.characterNode, "Skeleton") == skeletonsBeforeSecondDeath, "Completing the revived Gargantuar's second death must not create another Skeleton revive pile.");
				original.over = true;
				original.impThrowFlag = true;
				Dictionary save = original.ExportVariantSave();
				Check(save.GetValueOrDefault("over", false).AsBool() && save.GetValueOrDefault("impThrowFlag", false).AsBool(), "Progress save data must retain both the revived marker and inherited pending-throw field.");
				restored = LoadSkeletuar();
				PrepareCharacter(restored, new Vector2I(4, 2), new Vector2(400f, 200f));
				control.characterNode.AddChild(restored, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(1);
				restored.ImportVariantSave(save);
				Check(restored.over && restored.impThrowDamagePointName == "" && !restored.impThrowFlag, "Importing a revived progress save must immediately suppress the stale inherited Imp throw.");
				restored.ProcessMode = ProcessModeEnum.Disabled;
				remoteSpawn = LoadSkeletuar();
				PrepareCharacter(remoteSpawn, new Vector2I(3, 2), new Vector2(300f, 200f));
				Dictionary dictionary = original.ExportNetworkSpawnState();
				Check(remoteSpawn != null && dictionary.GetValueOrDefault("skeletuar_revived", false).AsBool(), "Skeleton Gargantuar must export and accept deterministic pre-Ready network spawn state.");
				((INetworkSpawnStateReceiver)remoteSpawn)?.ImportNetworkSpawnState(dictionary);
				Check(remoteSpawn.over && remoteSpawn.impThrowDamagePointName == "", "Replicated revive spawn state must disable the throw before the node enters the tree.");
				control.characterNode.AddChild(remoteSpawn, forceReadableName: false, InternalMode.Disabled);
				Check(remoteSpawn.over && remoteSpawn.impThrowDamagePointName == "" && !remoteSpawn.impThrowFlag, "The remote revived instance's first Ready must observe the authoritative revived state.");
				await WaitFrames(1);
				remoteSpawn.ProcessMode = ProcessModeEnum.Disabled;
				Dictionary special = original.ExportNetworkSpecialState();
				Check(special.GetValueOrDefault("over", false).AsBool() && original.GetNetworkSpecialStateRevision() != 0, "Late-join special state and its priority revision must expose the revived marker.");
				lateJoin = LoadSkeletuar();
				PrepareCharacter(lateJoin, new Vector2I(2, 2), new Vector2(200f, 200f));
				control.characterNode.AddChild(lateJoin, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(1);
				lateJoin.impThrowFlag = true;
				lateJoin.ImportNetworkSpecialState(special);
				Check(lateJoin.over && lateJoin.impThrowDamagePointName == "" && !lateJoin.impThrowFlag, "A late-join/full-snapshot import must immediately apply the revived no-throw contract.");
				lateJoin.ProcessMode = ProcessModeEnum.Disabled;
				goto end_IL_00cc;
				end_IL_00ef:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSkeletuarReviveRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00cc;
			}
			return;
			end_IL_00cc:;
		}
		finally
		{
			TowerDefenseZombieSkeletuar[] array = new TowerDefenseZombieSkeletuar[5] { original, revived, restored, remoteSpawn, lateJoin };
			foreach (TowerDefenseZombieSkeletuar towerDefenseZombieSkeletuar in array)
			{
				if (GodotObject.IsInstanceValid(towerDefenseZombieSkeletuar) && !towerDefenseZombieSkeletuar.IsQueuedForDeletion())
				{
					towerDefenseZombieSkeletuar.QueueFree();
				}
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
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 13;
		GD.Print($"SKELETUAR_REVIVE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseZombieSkeletuar LoadSkeletuar()
	{
		return ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/Skeletuar/Scene/TowerDefenseZombieSkeletuar.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieSkeletuar>(PackedScene.GenEditState.Disabled);
	}

	private static void PrepareCharacter(TowerDefenseZombieSkeletuar character, Vector2I gridPos, Vector2 position)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = gridPos;
		character.GlobalPosition = position;
	}

	private static string FirstDeathClip(TowerDefenseZombieSkeletuar character)
	{
		return character.dieAnimeClip.Split('&', StringSplitOptions.RemoveEmptyEntries)[0];
	}

	private static int CountCharactersNamed(Node parent, string configName)
	{
		int num = 0;
		foreach (Node child in parent.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter && towerDefenseCharacter.config?.name == configName)
			{
				num++;
			}
		}
		return num;
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
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
			GD.PushError("[BugOverviewSkeletuarReviveRuntimeTest] " + message);
		}
	}
}
