using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class ZombieDynamicZBattlefieldVisibilityRuntimeTest : Node
{
	private sealed class ZombieFixture
	{
		public string Name { get; }

		public string PacketPath { get; }

		public string ScenePath { get; }

		public bool ExpectedLocalRenderDuringZMotion { get; }

		public Resource PreviousPacket { get; set; }

		public Resource PreviousCharacter { get; set; }

		public bool PacketWasMissing { get; set; }

		public bool CharacterWasMissing { get; set; }

		public ZombieFixture(string name, string packetPath, string scenePath, bool expectedLocalRenderDuringZMotion = false)
		{
			Name = name;
			PacketPath = packetPath;
			ScenePath = scenePath;
			ExpectedLocalRenderDuringZMotion = expectedLocalRenderDuringZMotion;
		}
	}

	private readonly struct ChangedPixelStats(int count, long yTotal)
	{
		public int Count { get; } = count;

		public long YTotal { get; } = yTotal;

		public double AverageY
		{
			get
			{
				if (Count <= 0)
				{
					return 0.0;
				}
				return (double)YTotal / (double)Count;
			}
		}
	}

	private static readonly ZombieFixture[] Fixtures = new ZombieFixture[8]
	{
		new ZombieFixture("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn"),
		new ZombieFixture("ZombieJackson", "res://Asset/Anime/Character/Zombie/Chapter2/Jackson/Packet/ZombieJackson.tres", "res://Asset/Anime/Character/Zombie/Chapter2/Jackson/Scene/TowerDefenseZombieJackson.tscn"),
		new ZombieFixture("ZombieDolphinrider", "res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Packet/ZombieDolphinrider.tres", "res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Scene/TowerDefenseZombieDolphinrider.tscn"),
		new ZombieFixture("ZombiePogo", "res://Asset/Anime/Character/Zombie/Chapter4/Pogo/Packet/ZombiePogo.tres", "res://Asset/Anime/Character/Zombie/Chapter4/Pogo/Scene/TowerDefenseZombiePogo.tscn"),
		new ZombieFixture("ZombiePogoJackson", "res://Asset/Anime/Character/Zombie/Chapter5/PogoJackson/Packet/ZombiePogoJackson.tres", "res://Asset/Anime/Character/Zombie/Chapter5/PogoJackson/Scene/TowerDefenseZombiePogoJackson.tscn"),
		new ZombieFixture("ZombieChess", "res://Asset/Anime/Character/Zombie/Chapter5/Chess/Packet/ZombieChess.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Chess/Scene/TowerDefenseZombieChess.tscn"),
		new ZombieFixture("ZombieBungi", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn", expectedLocalRenderDuringZMotion: true),
		new ZombieFixture("ZombieGhost", "res://Asset/Anime/Character/Zombie/Chapter8/Ghost/Packet/ZombieGhost.tres", "res://Asset/Anime/Character/Zombie/Chapter8/Ghost/Scene/TowerDefenseZombieGhost.tscn")
	};

	private static readonly Vector2I SpawnLineGrid = new Vector2I(-1, 3);

	private const float BattlefieldX = 720f;

	private const double DynamicZHeight = 120.0;

	private const int ChecksPerFixture = 8;

	private int _checks;

	private int _failures;

	private readonly List<Resource> _registeredResources = new List<Resource>();

	public override async void _Ready()
	{
		ZombieFixture[] selectedFixtures = SelectFixtures();
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ZombieDynamicZBattlefieldVisibilityControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				throw new InvalidOperationException("Required battle autoloads are unavailable.");
			}
			RegisterRealFixtures(selectedFixtures);
			control = new ZombieDynamicZBattlefieldVisibilityControlStub
			{
				Name = "ZombieDynamicZBattlefieldVisibilityControl",
				isGameRunning = false,
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
			manager.gridBeginPos = new Vector2(200f, 100f);
			manager.gridSize = new Vector2(80f, 76f);
			manager.gridNum = new Vector2I(9, 5);
			mapControl = new TowerDefenseMapControl
			{
				Name = "MapControl"
			};
			mapFeature = CreateMapFeature(mapControl, manager);
			mapFeature.control = control;
			control.featureDictionary[new StringName("Map")] = mapFeature;
			ZombieFixture[] array = selectedFixtures;
			foreach (ZombieFixture fixture in array)
			{
				await VerifyFixture(fixture, control);
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[ZombieDynamicZBattlefieldVisibilityRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
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
			RestoreRealFixtures(selectedFixtures);
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			foreach (Resource registeredResource in _registeredResources)
			{
				registeredResource?.Dispose();
			}
			_registeredResources.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		int num = selectedFixtures.Length * 8;
		bool flag = _failures == 0 && _checks == num;
		GD.Print($"ZOMBIE_DYNAMIC_Z_BATTLEFIELD_VISIBILITY_RESULT passed={flag} fixtures={selectedFixtures.Length} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static ZombieFixture[] SelectFixtures()
	{
		string environment = OS.GetEnvironment("PVZHE_ZOMBIE_VISIBILITY_FIXTURE");
		if (string.IsNullOrWhiteSpace(environment))
		{
			return new ZombieFixture[1] { Fixtures[0] };
		}
		ZombieFixture[] fixtures = Fixtures;
		foreach (ZombieFixture zombieFixture in fixtures)
		{
			if (string.Equals(zombieFixture.Name, environment, StringComparison.Ordinal))
			{
				return new ZombieFixture[1] { zombieFixture };
			}
		}
		throw new InvalidOperationException("Unknown zombie visibility fixture: " + environment);
	}

	private async Task VerifyFixture(ZombieFixture fixture, ZombieDynamicZBattlefieldVisibilityControlStub control)
	{
		TowerDefenseCharacter character = null;
		try
		{
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(fixture.Name);
			Check(GodotObject.IsInstanceValid(packetConfig) && packetConfig.characterConfig?.name == fixture.Name, fixture.Name + ": the production packet must load through ResourceManager.");
			if (!GodotObject.IsInstanceValid(packetConfig))
			{
				throw new InvalidOperationException(fixture.Name + ": packet registration failed.");
			}
			character = packetConfig.Spawn(SpawnLineGrid.Y, 0.0, isIdle: true);
			Check(GodotObject.IsInstanceValid(character) && character.GetParent() == control.characterNode, fixture.Name + ": the production packet path must mount the zombie in the battlefield.");
			if (!GodotObject.IsInstanceValid(character))
			{
				throw new InvalidOperationException(fixture.Name + ": battlefield spawn failed.");
			}
			character.invisible = false;
			if (GodotObject.IsInstanceValid(character.sprite))
			{
				character.sprite.pause = true;
			}
			await WaitFrames(6);
			character.isGround = false;
			character.gravityUse = false;
			character.ySpeed = 0.0;
			character.z = character.groundHeight + 120.0;
			character.SetZ();
			character.SetPhysicsProcess(enable: false);
			await WaitFrames(8);
			Rect2 viewportRect = GetViewport().GetVisibleRect();
			Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
			Check(logicalGlobalPosition.X > viewportRect.End.X && character.gridPos.Y == SpawnLineGrid.Y, $"{fixture.Name}: the selected renderer must first publish from the production off-lawn position; position={logicalGlobalPosition}, viewport={viewportRect}.");
			ZombieDynamicZBattlefieldVisibilityRuntimeTest zombieDynamicZBattlefieldVisibilityRuntimeTest = this;
			bool? flag = character.sprite?.forceLocalRender;
			bool cached = fixture.ExpectedLocalRenderDuringZMotion;
			zombieDynamicZBattlefieldVisibilityRuntimeTest.Check(flag == cached, $"{fixture.Name}: dynamic height selected the wrong render path; expectedLocal={fixture.ExpectedLocalRenderDuringZMotion}, actualLocal={character.sprite?.forceLocalRender}.");
			character.SetLogicalGlobalPosition(new Vector2(720f, logicalGlobalPosition.Y));
			character.gridPos = new Vector2I(7, SpawnLineGrid.Y);
			await WaitFrames(12);
			character.sprite.GetRuntimeCrowdCullingDebugState(out cached, out var cachedVisible, out var currentVisible, out var currentVisibleWithPrefetch, out var nativeCanvasSuppressed);
			character.ActivateGameplayProcessing();
			character.sprite.GetRuntimeCrowdCullingDebugState(out currentVisibleWithPrefetch, out currentVisible, out cachedVisible, out cached, out var nativeCanvasSuppressed2);
			Check(!nativeCanvasSuppressed2, $"{fixture.Name}: gameplay activation must restore the native Canvas before the first new GPU publication; before={nativeCanvasSuppressed}, after={nativeCanvasSuppressed2}, forceLocal={character.sprite.forceLocalRender}.");
			await WaitFrames(2);
			Vector2 logicalGlobalPosition2 = character.GetLogicalGlobalPosition();
			float airborneSpriteY = character.GetLogicalGlobalPosition(character.sprite).Y;
			float airborneGroupY = character.spriteGroup.Position.Y;
			double airborneZ = character.z;
			Check(viewportRect.HasPoint(logicalGlobalPosition2) && character.IsVisibleInTree() && (character.sprite?.Visible ?? false) && !character.sprite.invisible && !character.invisible, $"{fixture.Name}: the zombie must remain logically visible after entering the lawn; position={logicalGlobalPosition2}, visible={character.IsVisibleInTree()}, spriteVisible={character.sprite?.Visible}, invisible={character.invisible}.");
			int publishedInstances = CountPublishedInstances(GetTree().Root);
			Check(publishedInstances > 0, $"{fixture.Name}: its selected render path must publish instances; count={publishedInstances}.");
			using Image airborneImage = GetViewport().GetTexture().GetImage();
			character.Visible = false;
			await WaitFrames(3);
			using Image airborneHiddenImage = GetViewport().GetTexture().GetImage();
			ChangedPixelStats airbornePixels = GetChangedPixelStats(airborneImage, airborneHiddenImage);
			character.Visible = true;
			character.z = character.groundHeight;
			character.isGround = true;
			character.SetZ();
			await WaitFrames(10);
			float groundedSpriteY = character.GetLogicalGlobalPosition(character.sprite).Y;
			float groundedGroupY = character.spriteGroup.Position.Y;
			double groundedZ = character.z;
			using Image groundedImage = GetViewport().GetTexture().GetImage();
			character.Visible = false;
			await WaitFrames(3);
			using Image hidden = GetViewport().GetTexture().GetImage();
			ChangedPixelStats changedPixelStats = GetChangedPixelStats(groundedImage, hidden);
			double num = changedPixelStats.AverageY - airbornePixels.AverageY;
			float num2 = groundedSpriteY - airborneSpriteY;
			Check((double)num2 >= 48.0 && airbornePixels.Count >= 128 && changedPixelStats.Count >= 128 && num >= (double)num2 * 0.45 && Math.Abs(num - (double)num2) <= Math.Max(20.0, (double)num2 * 0.35), $"{fixture.Name}: the selected renderer must draw the airborne body at its dynamic height; airChanged={airbornePixels.Count}, groundChanged={changedPixelStats.Count}, logicalYShift={num2:F1}, pixelYShift={num:F1}.");
			GD.Print($"ZOMBIE_DYNAMIC_Z_BATTLEFIELD_VISIBILITY_METRIC name={fixture.Name} airChanged={airbornePixels.Count} groundChanged={changedPixelStats.Count} logicalYShift={num2:F1} pixelYShift={num:F1} instances={publishedInstances} forceLocal={character.sprite?.forceLocalRender} position={character.GetLogicalGlobalPosition()} airZ={airborneZ:F1} groundZ={groundedZ:F1} groundHeight={character.groundHeight:F1} airGroupY={airborneGroupY:F1} groundGroupY={groundedGroupY:F1} airSpriteY={airborneSpriteY:F1} groundSpriteY={groundedSpriteY:F1}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(character))
			{
				character.QueueFree();
			}
			await WaitFrames(6);
		}
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, TowerDefenseManager manager)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = manager.gridNum,
			gridBeginPos = manager.gridBeginPos,
			gridSize = manager.gridSize,
			plantOffset = 50.0,
			edge = new Vector4(200f, 0f, 1100f, 600f)
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(manager.gridNum.X + 1);
		for (int i = 0; i <= manager.gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(manager.gridNum.Y + 1);
			for (int j = 1; j <= manager.gridNum.Y; j++)
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
		towerDefenseBattleFeatureMap.iceCapList.Resize(manager.gridNum.Y + 1);
		towerDefenseBattleFeatureMap.lineUse.Resize(manager.gridNum.Y + 1);
		for (int k = 1; k <= manager.gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures(IEnumerable<ZombieFixture> fixtures)
	{
		ResourceManager instance = ResourceManager.Instance;
		foreach (ZombieFixture fixture in fixtures)
		{
			if (instance.TOWERDEFENSE_PACKETS.TryGetValue(fixture.Name, out var value))
			{
				fixture.PreviousPacket = value;
			}
			else
			{
				fixture.PacketWasMissing = true;
			}
			if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(fixture.Name, out var value2))
			{
				fixture.PreviousCharacter = value2;
			}
			else
			{
				fixture.CharacterWasMissing = true;
			}
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(fixture.PacketPath, null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>(fixture.ScenePath, null, ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene))
			{
				throw new InvalidOperationException(fixture.Name + ": real fixture resources failed to load.");
			}
			_registeredResources.Add(towerDefensePacketConfig);
			_registeredResources.Add(packedScene);
			instance.TOWERDEFENSE_PACKETS[fixture.Name] = towerDefensePacketConfig;
			instance.TOWERDEFENSE_CHARCATERS[fixture.Name] = packedScene;
		}
	}

	private static void RestoreRealFixtures(IEnumerable<ZombieFixture> fixtures)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (ZombieFixture fixture in fixtures)
		{
			if (fixture.PacketWasMissing)
			{
				instance.TOWERDEFENSE_PACKETS.Remove(fixture.Name);
			}
			else
			{
				instance.TOWERDEFENSE_PACKETS[fixture.Name] = fixture.PreviousPacket;
			}
			if (fixture.CharacterWasMissing)
			{
				instance.TOWERDEFENSE_CHARCATERS.Remove(fixture.Name);
			}
			else
			{
				instance.TOWERDEFENSE_CHARCATERS[fixture.Name] = fixture.PreviousCharacter;
			}
		}
	}

	private static int CountPublishedInstances(Node node)
	{
		int num = ((node is MultiMeshInstance2D multiMeshInstance2D && GodotObject.IsInstanceValid(multiMeshInstance2D.Multimesh)) ? Math.Max(0, multiMeshInstance2D.Multimesh.VisibleInstanceCount) : 0);
		foreach (Node child in node.GetChildren())
		{
			num += CountPublishedInstances(child);
		}
		return num;
	}

	private static ChangedPixelStats GetChangedPixelStats(Image visible, Image hidden)
	{
		int num = Math.Min(visible.GetWidth(), hidden.GetWidth());
		int num2 = Math.Min(visible.GetHeight(), hidden.GetHeight());
		int num3 = 0;
		long num4 = 0L;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				if (ColorDistance(visible.GetPixel(j, i), hidden.GetPixel(j, i)) > 0.08f)
				{
					num3++;
					num4 += i;
				}
			}
		}
		return new ChangedPixelStats(num3, num4);
	}

	private static float ColorDistance(Color left, Color right)
	{
		return Mathf.Max(Mathf.Max(Mathf.Abs(left.R - right.R), Mathf.Abs(left.G - right.G)), Mathf.Max(Mathf.Abs(left.B - right.B), Mathf.Abs(left.A - right.A)));
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
			GD.PushError("[ZombieDynamicZBattlefieldVisibilityRuntimeTest] " + message);
		}
	}
}
