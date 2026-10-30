using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewScaredyShroomTrackUpgradeRuntimeTest : Node
{
	private const string LevelPath = "res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_13.tres";

	private const string ScaredyShroomScenePath = "res://Asset/Anime/Character/Plant/Chapter0/ScaredyShroom/Scene/TowerDefensePlantScaredyShroom.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string TrackProjectileConfigPath = "res://Asset/Config/Projectile/Star/StarBigCatGatlingPea.tres";

	private static readonly Vector2 GridBegin = new Vector2(260f, 75f);

	private static readonly Vector2 GridSize = new Vector2(80f, 98f);

	private static readonly Vector2I GridNum = new Vector2I(9, 5);

	private static readonly Vector2I PlantGrid = new Vector2I(2, 3);

	private static readonly Vector2I ZombieGrid = new Vector2I(6, 1);

	private const int TrackFlag = 32;

	private const int ShooterFlag = 1;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewScaredyShroomTrackUpgradeControlStub control = null;
		TowerDefensePlantScaredyShroom plant = null;
		TowerDefenseCharacter zombie = null;
		TowerDefenseProjectile projectile = null;
		Resource previousTrackConfig = null;
		bool hadPreviousTrackConfig = false;
		try
		{
			Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(manager))
			{
				throw new InvalidOperationException("TowerDefenseManager autoload is unavailable.");
			}
			if (!GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				throw new InvalidOperationException("ResourceManager autoload is unavailable.");
			}
			TowerDefenseProjectileRegistry.Init();
			hadPreviousTrackConfig = ResourceManager.Instance.PROJECTILE_CONFIG.TryGetValue("StarBigCatGatlingPea", out previousTrackConfig);
			ResourceManager.Instance.PROJECTILE_CONFIG["StarBigCatGatlingPea"] = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Star/StarBigCatGatlingPea.tres", null, ResourceLoader.CacheMode.Ignore);
			TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_13.tres", null, ResourceLoader.CacheMode.Ignore);
			Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig) && (StringName)towerDefenseLevelConfig.name == new StringName("Shooting_Level1_13"), "The real Shooting Level 1-13 resource must load.");
			TowerDefenseLevelPacketConfig trackUpgrade = FindPacket(towerDefenseLevelConfig, "PlantScaredyShroom", (TowerDefenseLevelPacketConfig packet) => HasPropertyValue(packet.@override?.characterOverride, "projectileName", (Variant variant) => variant.AsString() == "StarBigCatGatlingPea"));
			Check(GodotObject.IsInstanceValid(trackUpgrade), "Shooting Level 1-13 must contain the real StarBigCatGatlingPea Scaredy-shroom upgrade.");
			TowerDefenseProjectileConfig projectileConfig = TowerDefenseManager.GetProjectileConfig("StarBigCatGatlingPea");
			Check(GodotObject.IsInstanceValid(projectileConfig) && (projectileConfig.fireMethodFlags & 0x20) != 0, "The selected real upgrade projectile must be registered as TRACK.");
			if (!GodotObject.IsInstanceValid(trackUpgrade) || !GodotObject.IsInstanceValid(projectileConfig))
			{
				throw new InvalidOperationException("The real level upgrade or projectile config could not be loaded.");
			}
			manager.gridBeginPos = GridBegin;
			manager.gridSize = GridSize;
			manager.gridNum = GridNum;
			control = new BugOverviewScaredyShroomTrackUpgradeControlStub
			{
				Name = "ScaredyShroomTrackUpgradeControl",
				isGameRunning = false,
				isInit = true,
				levelConfig = towerDefenseLevelConfig
			};
			AddChild(control, forceReadableName: false, InternalMode.Disabled);
			control.characterNode = new Node2D
			{
				Name = "CharacterNode"
			};
			control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
			manager.currentControl = control;
			plant = Instantiate<TowerDefensePlantScaredyShroom>("res://Asset/Anime/Character/Plant/Chapter0/ScaredyShroom/Scene/TowerDefensePlantScaredyShroom.tscn");
			zombie = Instantiate<TowerDefenseCharacter>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
			Check(GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(zombie), "The real Scaredy-shroom and Normal zombie scenes must instantiate.");
			if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(zombie))
			{
				throw new InvalidOperationException("The real Scaredy-shroom or Normal zombie scene could not instantiate.");
			}
			PlaceCharacter(plant, PlantGrid);
			PlaceCharacter(zombie, ZombieGrid);
			control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(4);
			plant.ProcessMode = ProcessModeEnum.Disabled;
			zombie.ProcessMode = ProcessModeEnum.Disabled;
			FireComponent fireComponent = plant.componentManager?.GetRuntime<FireComponent>("character.fire");
			Check(fireComponent != null && !fireComponent.IsReleased, "The real Scaredy-shroom FireComponent must be active.");
			if (fireComponent == null || fireComponent.IsReleased)
			{
				throw new InvalidOperationException("The real Scaredy-shroom FireComponent is unavailable.");
			}
			fireComponent.alive = true;
			fireComponent.groundRight = 10000f;
			fireComponent.timer = 0f;
			fireComponent.checkInterval = 0;
			fireComponent.checkIntreval = 0;
			control.isGameRunning = true;
			Check(fireComponent.checkRayResources.Count > 0, "The real Scaredy-shroom must retain its authored fixed forward ray.");
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = ((fireComponent.fireCheckList.Count == 1) ? fireComponent.fireCheckList[0].projectile.GetProjectile() : null);
			Check(GodotObject.IsInstanceValid(towerDefenseProjectileCreateData) && towerDefenseProjectileCreateData.projectileName == new StringName("Puff") && (towerDefenseProjectileCreateData.fireMethodFlags & 1) != 0 && (towerDefenseProjectileCreateData.fireMethodFlags & 0x20) == 0, "Before the level upgrade, the real Scaredy-shroom must use its lane-scoped Puff shot.");
			Check(GodotObject.IsInstanceValid(towerDefenseProjectileCreateData) && !fireComponent.CanFireCheckOnce(towerDefenseProjectileCreateData), "Before the TRACK upgrade, a zombie found only on another row must not satisfy the fixed ray.");
			trackUpgrade.@override.characterOverride.ExecuteCharacter(plant);
			TowerDefenseProjectileCreateData projectile2 = fireComponent.fireCheckList[0].projectile.GetProjectile();
			Check(projectile2.projectileName == new StringName("StarBigCatGatlingPea"), "The actual Shooting Level 1-13 upgrade must reach the live Scaredy-shroom projectile source.");
			Check((projectile2.fireMethodFlags & 0x20) != 0, "The actual TRACK projectile upgrade must explicitly reach the live projectile fire flags.");
			Check(fireComponent.CanFireCheckOnce(projectile2), "A TRACK upgrade must acquire the cross-row zombie before consulting the authored fixed ray.");
			projectile = fireComponent.CreateProjectile(0, new Vector2(300f, 0f), projectile2);
			Check(GodotObject.IsInstanceValid(projectile) && (projectile.fireMethodFlags & 0x20) != 0 && projectile.target == zombie, "The real upgraded shot must spawn as TRACK and retain the unique cross-row zombie target.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[ScaredyShroomTrackUpgrade] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(projectile))
			{
				projectile.QueueFree();
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				if (hadPreviousTrackConfig)
				{
					ResourceManager.Instance.PROJECTILE_CONFIG["StarBigCatGatlingPea"] = previousTrackConfig;
				}
				else
				{
					ResourceManager.Instance.PROJECTILE_CONFIG.Remove("StarBigCatGatlingPea");
				}
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 13;
		GD.Print($"BUG_OVERVIEW_I88_SCAREDY_SHROOM_TRACK_UPGRADE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static void PlaceCharacter(TowerDefenseCharacter character, Vector2I gridPos)
	{
		character.Position = GridBegin + new Vector2((float)(gridPos.X - 1) * GridSize.X, (float)(gridPos.Y - 1) * GridSize.Y);
		character.gridPos = gridPos;
		character.inGame = true;
	}

	private static TowerDefenseLevelPacketConfig FindPacket(TowerDefenseLevelConfig level, string packetName, Func<TowerDefenseLevelPacketConfig, bool> predicate)
	{
		if (!GodotObject.IsInstanceValid(level))
		{
			return null;
		}
		HashSet<ulong> visited = new HashSet<ulong>();
		foreach (Variant packetBank in level.packetBankList)
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindPacket(packetBank.AsGodotObject() as TowerDefenseLevelPacketConfig, packetName, predicate, visited);
			if (GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig))
			{
				return towerDefenseLevelPacketConfig;
			}
		}
		return null;
	}

	private static TowerDefenseLevelPacketConfig FindPacket(TowerDefenseLevelPacketConfig packet, string packetName, Func<TowerDefenseLevelPacketConfig, bool> predicate, HashSet<ulong> visited)
	{
		if (!GodotObject.IsInstanceValid(packet) || !visited.Add(packet.GetInstanceId()))
		{
			return null;
		}
		if (packet.packetName == packetName && predicate(packet))
		{
			return packet;
		}
		if (!GodotObject.IsInstanceValid(packet.@override))
		{
			return null;
		}
		foreach (CardActionBehaviorDefinition useSucceededAction in packet.@override.useSucceededActions)
		{
			if (useSucceededAction is CardActionBehaviorChangePacket cardActionBehaviorChangePacket)
			{
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindPacket(cardActionBehaviorChangePacket.levelPacketConfig, packetName, predicate, visited);
				if (GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig))
				{
					return towerDefenseLevelPacketConfig;
				}
			}
		}
		return null;
	}

	private static bool HasPropertyValue(TowerDefenseCharacterOverride characterOverride, string propertyName, Func<Variant, bool> predicate)
	{
		if (!GodotObject.IsInstanceValid(characterOverride))
		{
			return false;
		}
		foreach (TowerDefenseCharacterPropertyChangeConfig item in characterOverride.propertyChange)
		{
			if (GodotObject.IsInstanceValid(item) && item.propertyName == propertyName)
			{
				return predicate(item.value);
			}
		}
		return false;
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
			GD.PushError("[ScaredyShroomTrackUpgrade] " + message);
		}
	}
}
