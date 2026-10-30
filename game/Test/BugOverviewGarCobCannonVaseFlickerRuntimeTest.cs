using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGarCobCannonVaseFlickerRuntimeTest : Node
{
	private const string MapControlScenePath = "res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn";

	private const string GarCobProjectilePath = "res://Registry/Projectile/Config/CannonCob/GarCobCannonCob.tres";

	private const string VasePacketPath = "res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres";

	private const string VaseScenePath = "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn";

	private const string ContentPacketPath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Packet/ZombieBoss.tres";

	private static readonly string[] GarCobContentKeys = new string[5] { "ZombieGargantuar", "ZombieGargantuarRedEyes", "ZombieFootballGargantuar", "ZombieFootballGargantuarBlack", "ZombieDiscoGargantuar" };

	private int _checks;

	private int _failures;

	private TowerDefenseControlNew _control;

	public override async void _Ready()
	{
		try
		{
			_ = 1;
			try
			{
				await SetupBattleFixture();
				await VerifyRealGarCobCannonVase();
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[GarCobCannonVaseFlicker] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				TowerDefenseManager.Instance.currentControl = null;
			}
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.QueueFree();
			}
			UnregisterFixtures();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0;
		GD.Print($"BUG_OVERVIEW_I45_GAR_COB_VASE_FLICKER_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task SetupBattleFixture()
	{
		RegisterFixtures();
		_control = new BugOverviewGarCobCannonVaseControlStub
		{
			Name = "GarCobCannonVaseControl",
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		_control.levelControl = new BugOverviewGarCobCannonVaseLevelControlStub
		{
			Name = "LevelControl"
		};
		_control.AddChild(_control.levelControl, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			throw new InvalidOperationException("TowerDefenseManager autoload is unavailable.");
		}
		instance.currentControl = _control;
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = new Vector2I(9, 5),
			gridBeginPos = new Vector2(0f, 100f),
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
		};
		instance.gridNum = towerDefenseMapConfig.gridNum;
		instance.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
		instance.gridSize = towerDefenseMapConfig.gridSize;
		TowerDefenseMapControl towerDefenseMapControl = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseMapControl>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(towerDefenseMapControl))
		{
			throw new InvalidOperationException("The real map control did not instantiate.");
		}
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (towerDefenseMapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			mapControl = towerDefenseMapControl
		});
		_control.featureDictionary["Map"] = towerDefenseBattleFeatureMap;
		CreateGrid(towerDefenseBattleFeatureMap, towerDefenseMapConfig.gridNum);
		_control.AddChild(towerDefenseMapControl, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private async Task VerifyRealGarCobCannonVase()
	{
		TowerDefenseProjectileData towerDefenseProjectileData = ResourceLoader.Load<TowerDefenseProjectileData>("res://Registry/Projectile/Config/CannonCob/GarCobCannonCob.tres", null, ResourceLoader.CacheMode.Ignore);
		Check(towerDefenseProjectileData?.hitEffect != null, "The canonical GarCobCannonCob must retain its real landing effect.");
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = towerDefenseProjectileData?.hitEffect?.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
		Check(towerDefenseProjectileEffectBase is TowerDefenseProjectileEffectGarCobCannonExplode, "GarCobCannonCob must instantiate the vase-spawning production effect.");
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileEffectBase))
		{
			return;
		}
		Vector2I gridPos = new Vector2I(4, 3);
		towerDefenseProjectileEffectBase.Init(gridPos, TowerDefenseEnum.CHARACTER_CAMP.PLANT, 0, null);
		towerDefenseProjectileEffectBase.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos);
		_control.characterNode.AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseVase vase = null;
		for (int frame = 0; frame < 20; frame++)
		{
			if (GodotObject.IsInstanceValid(vase))
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			vase = FindSpawnedVase();
		}
		Check(vase is TowerDefenseVaseZombie, "The real Gar-cob landing path must create the real Zombie Vase entity.");
		if (!GodotObject.IsInstanceValid(vase))
		{
			return;
		}
		AdobeAnimateSprite front = vase.sprite;
		AdobeAnimateSprite back = vase.backSprite;
		Check(GodotObject.IsInstanceValid(front) && GodotObject.IsInstanceValid(back), "The spawned vase must own both real animation shell layers.");
		if (GodotObject.IsInstanceValid(front) && GodotObject.IsInstanceValid(back))
		{
			Check(front.IsAncestorOf(back), "The rear vase shell must live under the front shell hierarchy instead of as a sibling root.");
			Check(!front.runtimeViewportCullingEnabled, "The spawned front shell must opt out of independent viewport culling.");
			Check(!back.runtimeViewportCullingEnabled, "The spawned rear shell must opt out of independent viewport culling.");
			Check(back.ZAsRelative && back.ZIndex == -1 && front.ZIndex == 0, "The rear shell must use a dedicated relative Z layer below the front shell.");
			double z = vase.z + 37.0;
			vase.z = z;
			AdobeAnimateRenderSnapshot snapshot;
			bool flag = front.TryBuildRenderSnapshot(out snapshot);
			AdobeAnimateRenderSnapshot snapshot2;
			bool flag2 = back.TryBuildRenderSnapshot(out snapshot2);
			Check(flag && flag2 && snapshot.GlobalTransform.IsEqualApprox(snapshot2.GlobalTransform), "Changing the vase Z must refresh both shell roots in the same render transaction.");
			bool policiesStayedPaired = true;
			for (int frame = 0; frame < 12; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				policiesStayedPaired &= GodotObject.IsInstanceValid(vase) && !front.runtimeViewportCullingEnabled && !back.runtimeViewportCullingEnabled && back.ZAsRelative && back.ZIndex == -1 && front.ZIndex == 0;
			}
			Check(policiesStayedPaired, "Both shell layers must retain the atomic render policy throughout entry and content assignment.");
		}
	}

	private TowerDefenseVase FindSpawnedVase()
	{
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseVase towerDefenseVase && GodotObject.IsInstanceValid(towerDefenseVase))
			{
				return towerDefenseVase;
			}
		}
		return null;
	}

	private static void CreateGrid(TowerDefenseBattleFeatureMap mapFeature, Vector2I gridNum)
	{
		TowerDefenseCellConfig config = new TowerDefenseCellConfig();
		mapFeature.plantGrid.Clear();
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			for (int j = 0; j <= gridNum.Y; j++)
			{
				if (i == 0 || j == 0)
				{
					array.Add(default(Variant));
					continue;
				}
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(config);
				array.Add(towerDefenseCellInstance);
			}
			mapFeature.plantGrid.Add(array);
		}
		mapFeature.iceCapList.Clear();
		mapFeature.lineUse.Clear();
		for (int k = 0; k <= gridNum.Y; k++)
		{
			mapFeature.iceCapList.Add(default(Variant));
			mapFeature.lineUse.Add(k > 0);
		}
	}

	private static void RegisterFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		TowerDefensePacketConfig value = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres", null, ResourceLoader.CacheMode.Ignore);
		PackedScene value2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn", null, ResourceLoader.CacheMode.Ignore);
		TowerDefensePacketConfig value3 = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Boss/Boss/Packet/ZombieBoss.tres", null, ResourceLoader.CacheMode.Ignore);
		instance.TOWERDEFENSE_PACKETS["VaseZombie"] = value;
		instance.TOWERDEFENSE_CHARCATERS["VaseZombie"] = value2;
		string[] garCobContentKeys = GarCobContentKeys;
		foreach (string key in garCobContentKeys)
		{
			instance.TOWERDEFENSE_PACKETS[key] = value3;
		}
	}

	private static void UnregisterFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.TOWERDEFENSE_PACKETS.Remove("VaseZombie");
			instance.TOWERDEFENSE_CHARCATERS.Remove("VaseZombie");
			string[] garCobContentKeys = GarCobContentKeys;
			foreach (string key in garCobContentKeys)
			{
				instance.TOWERDEFENSE_PACKETS.Remove(key);
			}
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[GarCobCannonVaseFlicker] " + message);
		}
	}
}
