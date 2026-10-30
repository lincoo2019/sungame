using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewCloudImpSpikeDropRuntimeTest : Node
{
	private const string CloudScenePath = "res://Asset/Anime/Character/Zombie/Chapter8/ImpCloud/Scene/TowerDefenseZombieImpCloud.tscn";

	private const string SpikePacketPath = "res://Asset/Anime/Character/Zombie/Chapter8/ImpDiggerSpike/Packet/ZombieImpDiggerSpike.tres";

	private const string SpikeScenePath = "res://Asset/Anime/Character/Zombie/Chapter8/ImpDiggerSpike/Scene/TowerDefenseZombieImpDiggerSpike.tscn";

	private static readonly Vector2I CloudGrid = new Vector2I(5, 2);

	private int _checks;

	private int _failures;

	private Resource _previousPacket;

	private Resource _previousCharacter;

	private bool _hadPacket;

	private bool _hadCharacter;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		CloudImpSpikeDropRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseZombieImpCloud cloud = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00e1;
				}
				RegisterSpikeImpFixtures();
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance.TOWERDEFENSE_PACKETS["ZombieImpDiggerSpike"]) && GodotObject.IsInstanceValid(ResourceManager.Instance.TOWERDEFENSE_CHARCATERS["ZombieImpDiggerSpike"]), "The real Spike Imp packet and scene must be registered.");
				control = new CloudImpSpikeDropRuntimeControlStub
				{
					Name = "CloudImpSpikeDropRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				Node2D characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = characterNode;
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				cloud = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter8/ImpCloud/Scene/TowerDefenseZombieImpCloud.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieImpCloud>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(cloud), "The real Cloud Imp scene must instantiate.");
				if (!GodotObject.IsInstanceValid(cloud))
				{
					goto end_IL_00e1;
				}
				cloud.editorPreviewMode = true;
				cloud.inGame = true;
				cloud.gridPos = CloudGrid;
				cloud.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(CloudGrid);
				characterNode.AddChild(cloud, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(cloud.sprite) && cloud.sprite.flashAnimeData.mediaDictionary.ContainsKey("Zombie_cloud_ball.png"), "The real Cloud Imp animation must expose its authored spike-ball media.");
				Check(CountSpikeImps(characterNode) == 0, "The focused fixture must begin without a spawned Spike Imp.");
				cloud.AnimeEvent("fire", default(Variant));
				await WaitFrames(2);
				Check(CountSpikeBallVisuals(characterNode) == 1, "The Cloud Imp throw must render exactly one texture-array-compatible spike-ball visual.");
				await WaitSeconds(0.85);
				await WaitFrames(3);
				TowerDefenseZombieImpDiggerSpike towerDefenseZombieImpDiggerSpike = FindNewestSpikeImp(characterNode);
				Check(CountSpikeImps(characterNode) == 1 && GodotObject.IsInstanceValid(towerDefenseZombieImpDiggerSpike), "The Cloud Imp fire action must drop exactly one real Spike Imp.");
				Check(towerDefenseZombieImpDiggerSpike != null && towerDefenseZombieImpDiggerSpike.inGame && towerDefenseZombieImpDiggerSpike.gridPos.Y == CloudGrid.Y, "The dropped Spike Imp must enter gameplay on the Cloud Imp's row.");
				Check(towerDefenseZombieImpDiggerSpike != null && towerDefenseZombieImpDiggerSpike.instance?.hypnoses == false, "A normal Cloud Imp must drop a normal-camp Spike Imp.");
				cloud.AnimeEvent("fire", default(Variant));
				cloud.QueueFree();
				cloud = null;
				await WaitSeconds(0.85);
				await WaitFrames(3);
				Check(CountSpikeImps(characterNode) == 2, "A spike ball already thrown must still land after its Cloud Imp is removed.");
				goto end_IL_00be;
				end_IL_00e1:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewCloudImpSpikeDropRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00be;
			}
			return;
			end_IL_00be:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(cloud))
			{
				cloud.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreSpikeImpFixtures();
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 11;
		GD.Print($"CLOUD_IMP_SPIKE_DROP_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterSpikeImpFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		_hadPacket = instance.TOWERDEFENSE_PACKETS.TryGetValue("ZombieImpDiggerSpike", out _previousPacket);
		_hadCharacter = instance.TOWERDEFENSE_CHARCATERS.TryGetValue("ZombieImpDiggerSpike", out _previousCharacter);
		instance.TOWERDEFENSE_PACKETS["ZombieImpDiggerSpike"] = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter8/ImpDiggerSpike/Packet/ZombieImpDiggerSpike.tres", null, ResourceLoader.CacheMode.Ignore);
		instance.TOWERDEFENSE_CHARCATERS["ZombieImpDiggerSpike"] = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter8/ImpDiggerSpike/Scene/TowerDefenseZombieImpDiggerSpike.tscn", null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreSpikeImpFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			if (_hadPacket)
			{
				instance.TOWERDEFENSE_PACKETS["ZombieImpDiggerSpike"] = _previousPacket;
			}
			else
			{
				instance.TOWERDEFENSE_PACKETS.Remove("ZombieImpDiggerSpike");
			}
			if (_hadCharacter)
			{
				instance.TOWERDEFENSE_CHARCATERS["ZombieImpDiggerSpike"] = _previousCharacter;
			}
			else
			{
				instance.TOWERDEFENSE_CHARCATERS.Remove("ZombieImpDiggerSpike");
			}
		}
	}

	private static int CountSpikeImps(Node parent)
	{
		int num = 0;
		foreach (Node child in parent.GetChildren())
		{
			if (child is TowerDefenseZombieImpDiggerSpike)
			{
				num++;
			}
		}
		return num;
	}

	private static int CountSpikeBallVisuals(Node parent)
	{
		int num = 0;
		foreach (Node child in parent.GetChildren())
		{
			if (child is AdobeAnimatePart)
			{
				num++;
			}
		}
		return num;
	}

	private static TowerDefenseZombieImpDiggerSpike FindNewestSpikeImp(Node parent)
	{
		TowerDefenseZombieImpDiggerSpike result = null;
		foreach (Node child in parent.GetChildren())
		{
			if (child is TowerDefenseZombieImpDiggerSpike towerDefenseZombieImpDiggerSpike)
			{
				result = towerDefenseZombieImpDiggerSpike;
			}
		}
		return result;
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
			GD.PushError("[BugOverviewCloudImpSpikeDropRuntimeTest] " + message);
		}
	}
}
