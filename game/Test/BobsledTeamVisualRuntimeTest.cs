using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BobsledTeamVisualRuntimeTest : Node
{
	private CubeBoxBloverControl _control;

	private int _failures;

	public override async void _Ready()
	{
		_ = 6;
		try
		{
			typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ResourceManager.Instance, GameplayResourceLoadState.Ready);
			Register("ZombieBobsledTeam", "Zombie/Chapter2/BobsledTeam", "TowerDefenseZombieBobsledTeam");
			Register("ZombieBobsledMember", "Zombie/Chapter2/BobsledTeam", "TowerDefenseZombieBobsledMember");
			AddChild(new Sprite2D
			{
				Texture = GD.Load<Texture2D>("res://Asset/Texture/TowerDefense/Background/TowerDefenseMap/Frontlawn/Frontlawn.jpg"),
				Centered = false,
				ZIndex = -100
			}, forceReadableName: false, InternalMode.Disabled);
			_control = new CubeBoxBloverControl
			{
				isInit = true,
				isGameRunning = true,
				levelConfig = new TowerDefenseLevelConfig()
			};
			AddChild(_control, forceReadableName: false, InternalMode.Disabled);
			_control.SetPhysicsProcess(enable: false);
			_control.characterNode = new Node2D();
			_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			instance.currentControl = _control;
			TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
			{
				isNight = false
			};
			instance.gridNum = towerDefenseMapConfig.gridNum;
			instance.gridSize = towerDefenseMapConfig.gridSize;
			instance.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
			TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
			{
				config = towerDefenseMapConfig,
				mapConfig = towerDefenseMapConfig,
				control = _control
			};
			towerDefenseBattleFeatureMap.groundRect = new Rect2(towerDefenseMapConfig.gridBeginPos, towerDefenseMapConfig.gridSize * new Vector2(towerDefenseMapConfig.gridNum.X, towerDefenseMapConfig.gridNum.Y));
			towerDefenseBattleFeatureMap.mapControl = new CubeBoxBloverMap
			{
				mapFeature = towerDefenseBattleFeatureMap
			};
			_control.AddChild(towerDefenseBattleFeatureMap.mapControl, forceReadableName: false, InternalMode.Disabled);
			_control.featureDictionary["Map"] = towerDefenseBattleFeatureMap;
			towerDefenseBattleFeatureMap.PlantGridInit();
			for (int i = 1; i <= 9; i++)
			{
				for (int j = 1; j <= 5; j++)
				{
					towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
				}
			}
			towerDefenseBattleFeatureMap.mapControl.mapIceCap = new Node2D();
			towerDefenseBattleFeatureMap.mapControl.AddChild(towerDefenseBattleFeatureMap.mapControl.mapIceCap, forceReadableName: false, InternalMode.Disabled);
			instance.SetIceCapPos(3, new Vector2(350f, 0f));
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieBobsledTeam");
			TowerDefenseZombieBobsledTeam vehicle = packetConfig.Create(new Vector2(750f, (float)TowerDefenseManager.GetMapLineY(3)), new Vector2I(7, 3)) as TowerDefenseZombieBobsledTeam;
			_control.characterNode.AddChild(vehicle, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(5);
			vehicle.WalkReady();
			await WaitFrames(10);
			await Capture("boarding");
			await WaitFrames(80);
			BobsledTeamComponent runtime = vehicle.componentManager.GetRuntime<BobsledTeamComponent>();
			TowerDefenseZombie[] passengers = new TowerDefenseZombie[4];
			for (int k = 0; k < 4; k++)
			{
				passengers[k] = runtime.GetPassenger(k) as TowerDefenseZombie;
				AdobeAnimateSlot node = vehicle.GetNode<AdobeAnimateSlot>($"SpriteGroup/TransformPoint/ZombieBobsledVehicle/Passenger{k}Slot");
				GD.Print($"BOBSLED_SLOT slot={k} marker={node.GlobalPosition} offset={node.offset} passenger={passengers[k]?.GlobalPosition}");
			}
			GD.Print($"BOBSLED_VEHICLE pos={vehicle.GlobalPosition} z={vehicle.z} ground={vehicle.groundHeight} sprite={vehicle.sprite.GlobalPosition} phase={vehicle.CurrentPhase} state={vehicle.CurrentStateHandle?.StableId} show={vehicle.isShow} inGame={vehicle.inGame} dead={vehicle.die} near={vehicle.nearDie}");
			await Capture("riding");
			vehicle.Hurt(10000.0);
			await WaitFrames(20);
			float minX = 1f / 0f;
			float maxX = -1f / 0f;
			for (int l = 0; l < 4; l++)
			{
				TowerDefenseZombie towerDefenseZombie = passengers[l];
				if (!GodotObject.IsInstanceValid(towerDefenseZombie) || !towerDefenseZombie.Visible || towerDefenseZombie.isPause)
				{
					_failures++;
					continue;
				}
				minX = Math.Min(minX, towerDefenseZombie.GlobalPosition.X);
				maxX = Math.Max(maxX, towerDefenseZombie.GlobalPosition.X);
				GD.Print($"BOBSLED_RELEASE slot={l} pos={towerDefenseZombie.GlobalPosition} z={towerDefenseZombie.z} ground={towerDefenseZombie.groundHeight}");
			}
			if (maxX - minX < 120f)
			{
				_failures++;
			}
			await Capture("released");
			GD.Print($"BOBSLED_VISUAL_RESULT passed={_failures == 0} spread={maxX - minX} failures={_failures}");
			GetTree().Quit((_failures != 0) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PushError(ex.ToString());
			GetTree().Quit(2);
		}
	}

	private void Register(string key, string directory, string scene)
	{
		ResourceManager.Instance.TOWERDEFENSE_PACKETS[key] = GD.Load<TowerDefensePacketConfig>($"res://Asset/Anime/Character/{directory}/Packet/{key}.tres");
		ResourceManager.Instance.TOWERDEFENSE_CHARCATERS[key] = GD.Load<PackedScene>($"res://Asset/Anime/Character/{directory}/Scene/{scene}.tscn");
	}

	private async Task Capture(string stage)
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		string environment = OS.GetEnvironment("BOBSLED_CAPTURE_DIR");
		DirAccess.MakeDirRecursiveAbsolute(environment);
		using Image image = GetViewport().GetTexture().GetImage();
		if (image.SavePng(environment + "/" + stage + ".png") != Error.Ok)
		{
			throw new InvalidOperationException("截图保存失败。");
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}
}
